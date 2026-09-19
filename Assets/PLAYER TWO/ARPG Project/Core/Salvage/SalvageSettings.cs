using System;
using System.Collections.Generic;
using UnityEngine;

namespace PLAYERTWO.ARPGProject
{
    [Serializable]
    public class SalvageItemOverride
    {
        public Item item;

        [Tooltip("The materials granted when salvaging this specific Item, overriding its rarity's own rewards.")]
        public List<SalvageMaterialAmount> rewards = new();
    }

    [CreateAssetMenu(
        fileName = "Salvage Settings",
        menuName = "PLAYER TWO/ARPG Project/Salvage/Settings"
    )]
    public class SalvageSettings : ScriptableObject
    {
        public int configurationVersion = 1;

        public List<int> highValueRarityIds = new();
        public List<SalvageItemOverride> itemOverrides = new();

        [Tooltip(
            "Rewards for equipment with no rarity assigned (rarityId -1), or whose rarity has no "
                + "matching Salvage Settings entry for the item's type."
        )]
        public List<SalvageMaterialAmount> fallbackRewards = new();

        public string fingerprint => $"{name}:{configurationVersion}";

        /// <summary>
        /// Resolves the salvage rewards for a given item. Checks, in order: an explicit
        /// per-item-definition override, then the item's own <see cref="ItemRarity"/> asset
        /// (<see cref="ItemRarity.GetSalvageRewards"/>, keyed by item type), then
        /// <see cref="fallbackRewards"/>.
        /// </summary>
        public bool TryGetRewards(
            ItemInstance item,
            out List<SalvageMaterialAmount> rewards,
            out string reason
        )
        {
            rewards = null;
            reason = null;

            if (item?.data == null)
            {
                reason = "Missing item definition.";
                return false;
            }

            foreach (var entry in itemOverrides)
            {
                if (entry.item != item.data)
                    continue;

                if (!ValidateRewards(entry.rewards, out reason))
                    return false;

                rewards = entry.rewards;
                return true;
            }

            var rarity = item.GetRarity();
            var scopeRewards = rarity?.GetSalvageRewards(item.GetItemScope());
            var candidate = scopeRewards != null && scopeRewards.Count > 0 ? scopeRewards : fallbackRewards;

            if (candidate == null || candidate.Count == 0)
            {
                reason = "No salvage rewards are configured for this equipment.";
                return false;
            }

            if (!ValidateRewards(candidate, out reason))
                return false;

            rewards = candidate;
            return true;
        }

        public bool ValidateRewards(List<SalvageMaterialAmount> candidate, out string reason)
        {
            if (candidate == null || candidate.Count == 0)
            {
                reason = "The salvage rewards are incomplete.";
                return false;
            }

            var materials = new HashSet<Item>();

            foreach (var reward in candidate)
            {
                if (
                    reward?.material == null
                    || reward.quantity <= 0
                    || (reward.material.canStack && reward.material.stackCapacity <= 0)
                )
                {
                    reason = "The salvage rewards contain an invalid material reward.";
                    return false;
                }

                if (!materials.Add(reward.material))
                {
                    reason = "The salvage rewards contain a duplicate material.";
                    return false;
                }
            }

            reason = null;
            return true;
        }
    }
}
