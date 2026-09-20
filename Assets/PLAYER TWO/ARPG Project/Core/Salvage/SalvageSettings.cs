using System;
using System.Collections.Generic;
using UnityEngine;

namespace PLAYERTWO.ARPGProject
{
    [Serializable]
    public class SalvageItemOverride
    {
        public Item item;

        [Tooltip("The materials granted when salvaging this specific Item.")]
        public List<SalvageMaterialAmount> rewards = new();
    }

    [Serializable]
    public class SalvageRarityOverride
    {
        [Tooltip(
            "The rarity this override applies to. Leave empty to match equipment with no rarity "
                + "assigned (plain equipment), unless Salvage Settings' Default Rarity substitutes "
                + "a rarity for plain equipment instead."
        )]
        public ItemRarity rarity;

        [Tooltip(
            "Optional. Restricts this override to equipment of this type (matched against the "
                + "item's own scope, e.g. an entry scoped to Weapon matches both Blade and Bow). "
                + "Leave as None to match every type of equipment with the selected rarity. Add "
                + "several entries for the same rarity with different scopes to grant different "
                + "rewards per equipment type — list the more specific (scoped) entries before "
                + "a general/unscoped one for the same rarity, since the first matching entry wins."
        )]
        public ItemScope scope;

        [Tooltip(
            "The materials granted when salvaging any equipment of this rarity (and scope, if "
                + "set), unless a more specific item override matches."
        )]
        public List<SalvageMaterialAmount> rewards = new();
    }

    [CreateAssetMenu(
        fileName = "Salvage Settings",
        menuName = "PLAYER TWO/ARPG Project/Salvage/Settings"
    )]
    public class SalvageSettings : ScriptableObject
    {
        public int configurationVersion = 1;

        [Tooltip(
            "Optional. When assigned, equipment with no rarity rolled (plain equipment) is "
                + "treated as this rarity when matching Rarity Overrides below, instead of only "
                + "matching an override left with an empty Rarity field."
        )]
        public ItemRarity defaultRarity;

        public List<int> highValueRarityIds = new();
        public List<SalvageItemOverride> itemOverrides = new();
        public List<SalvageRarityOverride> rarityOverrides = new();

        [Tooltip("Rewards for equipment with no matching item or rarity override.")]
        public List<SalvageMaterialAmount> fallbackRewards = new();

        public string fingerprint => $"{name}:{configurationVersion}";

        /// <summary>
        /// Returns the rarity used to match <see cref="rarityOverrides"/> for the given item:
        /// its own rolled rarity, or <see cref="defaultRarity"/> when it has none (plain
        /// equipment).
        /// </summary>
        public virtual ItemRarity GetEffectiveRarity(ItemInstance item) =>
            item?.GetRarity() ?? defaultRarity;

        /// <summary>
        /// Resolves the salvage rewards for a given item. Checks, in order: an explicit
        /// per-item-definition override, then a per-rarity override covering every item of that
        /// rarity, then <see cref="fallbackRewards"/>.
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

            var effectiveRarity = GetEffectiveRarity(item);
            var itemScope = item.GetItemScope();

            foreach (var entry in rarityOverrides)
            {
                if (entry == null || entry.rarity != effectiveRarity)
                    continue;

                if (entry.scope != ItemScope.None && (entry.scope & itemScope) == 0)
                    continue;

                if (!ValidateRewards(entry.rewards, out reason))
                    return false;

                rewards = entry.rewards;
                return true;
            }

            if (fallbackRewards == null || fallbackRewards.Count == 0)
            {
                reason = "No salvage rewards are configured for this equipment.";
                return false;
            }

            if (!ValidateRewards(fallbackRewards, out reason))
                return false;

            rewards = fallbackRewards;
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
                    || reward.dropChance < 0
                    || reward.dropChance > 1
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
