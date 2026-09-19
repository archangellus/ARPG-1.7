using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PLAYERTWO.ARPGProject
{
    public readonly struct SalvageEligibility
    {
        public readonly bool eligible;
        public readonly string reason;
        public SalvageEligibility(bool eligible, string reason = null) { this.eligible = eligible; this.reason = reason; }
    }

    [Serializable]
    public class SalvageRewardTotal
    {
        public Item material;
        public long quantity;
    }

    public sealed class SalvagePreview
    {
        public string ticketId { get; internal set; }
        public string operationId { get; internal set; }
        public string providerId { get; internal set; }
        public string configurationFingerprint { get; internal set; }
        public IReadOnlyList<string> itemIds { get; internal set; }
        public IReadOnlyList<int> itemRevisions { get; internal set; }
        public IReadOnlyList<SalvageRewardTotal> materials { get; internal set; }
        public IReadOnlyList<ItemInstance> returnedSocketables { get; internal set; }
        public bool requiresHighValueConfirmation { get; internal set; }
        public string requestHash { get; internal set; }
    }

    /// <summary>Authoritative eligibility, deterministic preview, and atomic salvage commit boundary.</summary>
    public class SalvageService
    {
        private readonly Dictionary<string, SalvagePreview> m_tickets = new();
        private bool m_committing;
        public event Action<SalvageReceiptRecord> committed;

        public SalvageEligibility Evaluate(ItemInstance item, Inventory inventory, SalvageSettings settings)
        {
            if (item == null || inventory == null || !inventory.Contains(item)) return new(false, "The item is not in the carried inventory.");
            if (!item.IsEquippable()) return new(false, "Only equipment can be salvaged.");
            if (item.isFavorite) return new(false, "Favorite items cannot be salvaged.");
            if (item.isLocked) return new(false, "Locked items cannot be salvaged.");
            if (!settings) return new(false, "Salvage settings are unavailable.");
            return settings.TryGetRewards(item, out _, out var reason) ? new(true) : new(false, reason);
        }

        public bool TryCreatePreview(IEnumerable<string> selectedIds, Entity owner, SalvageSettings settings, string providerId, out SalvagePreview preview, out string error)
        {
            preview = null;
            error = null;
            if (!owner || string.IsNullOrEmpty(providerId)) { error = "The salvage provider is no longer available."; return false; }
            var ids = selectedIds?.Where(id => !string.IsNullOrEmpty(id)).ToList() ?? new();
            if (ids.Count == 0) { error = "Select at least one item."; return false; }
            if (ids.Distinct().Count() != ids.Count) { error = "The selection contains a duplicate item."; return false; }
            var inventory = owner.inventory.instance;
            var carried = inventory.items.Keys.ToList();
            RepairDuplicateInstanceIds(carried);
            var byId = carried.ToDictionary(item => item.instanceId);
            var items = new List<ItemInstance>();
            foreach (var id in ids)
            {
                if (!byId.TryGetValue(id, out var item)) { error = "A selected item is no longer carried."; return false; }
                var eligibility = Evaluate(item, inventory, settings);
                if (!eligibility.eligible) { error = eligibility.reason; return false; }
                items.Add(item);
            }

            var totals = new Dictionary<Item, long>();
            foreach (var item in items)
            {
                settings.TryGetRewards(item, out var rewards, out _);
                foreach (var reward in rewards)
                {
                    var cap = Mathf.RoundToInt(reward.quantity * reward.dropChance);
                    var granted = UnityEngine.Random.Range(0, cap + 1);
                    Debug.Log(
                        $"[Salvage] {item.data.name} ({item.instanceId[..8]}) -> "
                            + $"{reward.material.name}: quantity={reward.quantity} "
                            + $"dropChance={reward.dropChance:F3} cap={cap} granted={granted}"
                    );
                    if (granted <= 0)
                        continue;
                    try
                    {
                        totals.TryGetValue(reward.material, out var current);
                        totals[reward.material] = checked(current + granted);
                    }
                    catch (OverflowException) { error = "The material reward is too large."; return false; }
                }
            }

            Debug.Log(
                $"[Salvage] Batch of {items.Count} item(s) -> totals: "
                    + string.Join(", ", totals.Select(pair => $"{pair.Key.name}={pair.Value}"))
            );

            var orderedIds = ids.OrderBy(id => id, StringComparer.Ordinal).ToArray();
            var operationId = Guid.NewGuid().ToString("N");
            preview = new SalvagePreview
            {
                ticketId = Guid.NewGuid().ToString("N"), operationId = operationId, providerId = providerId,
                configurationFingerprint = settings.fingerprint, itemIds = orderedIds,
                itemRevisions = orderedIds.Select(id => byId[id].salvageRevision).ToArray(),
                materials = totals.OrderBy(pair => pair.Key.name, StringComparer.Ordinal).Select(pair => new SalvageRewardTotal { material = pair.Key, quantity = pair.Value }).ToArray(),
                returnedSocketables = items.SelectMany(item => item.GetOccupiedSockets()).ToArray(),
                requiresHighValueConfirmation = items.Any(item => settings.highValueRarityIds.Contains(item.rarityId)),
                requestHash = $"{providerId}|{settings.fingerprint}|{string.Join(",", orderedIds)}"
            };
            m_tickets[preview.ticketId] = preview;
            return true;
        }

        public bool TryCommit(SalvagePreview preview, Entity owner, SalvageSettings settings, string providerId, bool highValueConfirmed, out SalvageReceiptRecord receipt, out string error)
        {
            receipt = null; error = null;
            if (preview == null || !owner || m_committing) { error = "The salvage operation is unavailable."; return false; }
            var state = Game.instance.currentCharacter.salvage;
            var existing = state.FindReceipt(preview.operationId);
            if (existing != null)
            {
                if (existing.requestHash != preview.requestHash) { error = "The completed operation does not match this request."; return false; }
                receipt = existing; return true;
            }
            if (!m_tickets.TryGetValue(preview.ticketId, out var owned) || !ReferenceEquals(owned, preview)) { error = "The salvage preview has expired."; return false; }
            if (providerId != preview.providerId || settings.fingerprint != preview.configurationFingerprint) { error = "The salvage preview is stale."; return false; }
            if (preview.requiresHighValueConfirmation && !highValueConfirmed) { error = "Confirm the selected high-value equipment."; return false; }

            m_committing = true;
            try
            {
                var inventory = owner.inventory.instance;
                var carried = inventory.items.Keys.ToList();
                RepairDuplicateInstanceIds(carried);
                var byId = carried.ToDictionary(item => item.instanceId);
                var selected = new List<ItemInstance>();
                for (var i = 0; i < preview.itemIds.Count; i++)
                {
                    if (!byId.TryGetValue(preview.itemIds[i], out var item) || item.salvageRevision != preview.itemRevisions[i]) { error = "A selected item changed; refresh the preview."; return false; }
                    var eligibility = Evaluate(item, inventory, settings);
                    if (!eligibility.eligible) { error = eligibility.reason; return false; }
                    selected.Add(item);
                }

                var positions = selected.ToDictionary(item => item, item => inventory.FindPosition(item));
                var addedSockets = new List<ItemInstance>();
                var addedMaterials = new List<ItemInstance>();
                var receiptRecord = new SalvageReceiptRecord { operationId = preview.operationId, requestHash = preview.requestHash, summary = $"Salvaged {selected.Count} item(s)" };
                try
                {
                    foreach (var item in selected) if (!inventory.TryRemoveItem(item)) throw new InvalidOperationException("A selected item could not be removed.");
                    foreach (var socket in preview.returnedSocketables)
                    {
                        if (!inventory.TryAddItem(socket)) throw new InvalidOperationException("Make space for returned socketables.");
                        addedSockets.Add(socket);
                    }
                    foreach (var total in preview.materials)
                        if (!TryGrantMaterial(inventory, total.material, total.quantity, addedMaterials)) throw new InvalidOperationException($"Make space for the {total.material.name} reward.");
                    state.receipts.Add(receiptRecord);
                    GameSave.instance.Save();
                }
                catch (Exception exception)
                {
                    state.receipts.Remove(receiptRecord);
                    foreach (var material in addedMaterials) inventory.TryRemoveItem(material);
                    foreach (var socket in addedSockets) inventory.TryRemoveItem(socket);
                    foreach (var item in selected) if (!inventory.Contains(item)) inventory.TryInsertItem(item, positions[item].row, positions[item].column);
                    error = exception.Message;
                    return false;
                }
                m_tickets.Remove(preview.ticketId);
                receipt = receiptRecord;
            }
            finally { m_committing = false; }
            try { committed?.Invoke(receipt); } catch (Exception exception) { Debug.LogException(exception); }
            return true;
        }

        public void InvalidateAll() => m_tickets.Clear();

        /// <summary>
        /// Regenerates the id of every carried Item Instance after the first that shares an
        /// instanceId with an earlier one, in place. Defense-in-depth alongside the repair
        /// <see cref="CharacterInventory.CreateFromSerializer"/> already does on load, for a
        /// character that was loaded before that repair existed and is still active this session.
        /// </summary>
        private static void RepairDuplicateInstanceIds(List<ItemInstance> carried)
        {
            var seen = new HashSet<string>();

            foreach (var item in carried)
                if (!seen.Add(item.instanceId))
                    item.RegenerateInstanceId();
        }

        /// <summary>
        /// Grants a salvage material as ordinary carried Item Instances, split into full stacks
        /// when the material is stackable and as individual units otherwise. Each newly inserted
        /// instance is appended to <paramref name="added"/> so the caller can roll every one of
        /// them back on failure. Stops and returns false the moment the inventory has no room
        /// left, leaving whatever was already inserted for the caller to undo.
        /// </summary>
        private static bool TryGrantMaterial(Inventory inventory, Item material, long quantity, List<ItemInstance> added)
        {
            while (quantity > 0)
            {
                var instance = new ItemInstance(material);
                var stackSize = material.canStack ? (int)Math.Min(quantity, material.stackCapacity) : 1;
                instance.stack = stackSize;
                if (!inventory.TryAddItem(instance)) return false;
                added.Add(instance);
                quantity -= material.canStack ? stackSize : 1;
            }
            return true;
        }
    }
}
