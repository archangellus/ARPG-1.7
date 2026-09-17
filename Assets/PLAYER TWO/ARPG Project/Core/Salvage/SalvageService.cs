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
        public SalvageMaterialDefinition material;
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
            return settings.TryGetRecipe(item, out _, out var reason) ? new(true) : new(false, reason);
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
            if (carried.GroupBy(item => item.instanceId).Any(group => group.Count() > 1)) { error = "Duplicate owned item IDs were detected; save migration must be repaired before salvaging."; return false; }
            var byId = carried.ToDictionary(item => item.instanceId);
            var items = new List<ItemInstance>();
            foreach (var id in ids)
            {
                if (!byId.TryGetValue(id, out var item)) { error = "A selected item is no longer carried."; return false; }
                var eligibility = Evaluate(item, inventory, settings);
                if (!eligibility.eligible) { error = eligibility.reason; return false; }
                items.Add(item);
            }

            var totals = new Dictionary<SalvageMaterialDefinition, long>();
            foreach (var item in items)
            {
                settings.TryGetRecipe(item, out var recipe, out _);
                foreach (var reward in recipe.rewards)
                {
                    try
                    {
                        totals.TryGetValue(reward.material, out var current);
                        totals[reward.material] = checked(current + reward.quantity);
                    }
                    catch (OverflowException) { error = "The material reward is too large."; return false; }
                }
            }
            foreach (var total in totals)
                if (Game.instance.currentCharacter.salvage.Get(total.Key.id) > settings.materialCap - total.Value) { error = $"The {total.Key.displayName} material cap would be exceeded."; return false; }

            var orderedIds = ids.OrderBy(id => id, StringComparer.Ordinal).ToArray();
            var operationId = Guid.NewGuid().ToString("N");
            preview = new SalvagePreview
            {
                ticketId = Guid.NewGuid().ToString("N"), operationId = operationId, providerId = providerId,
                configurationFingerprint = settings.fingerprint, itemIds = orderedIds,
                itemRevisions = orderedIds.Select(id => byId[id].salvageRevision).ToArray(),
                materials = totals.OrderBy(pair => pair.Key.id).Select(pair => new SalvageRewardTotal { material = pair.Key, quantity = pair.Value }).ToArray(),
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
                if (carried.GroupBy(item => item.instanceId).Any(group => group.Count() > 1)) { error = "Duplicate owned item IDs were detected."; return false; }
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
                var oldBalances = state.materials.Select(value => new SalvageMaterialBalance { materialId = value.materialId, quantity = value.quantity }).ToList();
                var oldStateRevision = state.revision;
                var addedSockets = new List<ItemInstance>();
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
                        if (!state.TryCredit(total.material.id, total.quantity, settings.materialCap)) throw new InvalidOperationException($"The {total.material.displayName} material cap would be exceeded.");
                    state.receipts.Add(receiptRecord);
                    GameSave.instance.Save();
                }
                catch (Exception exception)
                {
                    state.receipts.Remove(receiptRecord);
                    state.materials.Clear();
                    state.materials.AddRange(oldBalances);
                    state.revision = oldStateRevision;
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
    }
}
