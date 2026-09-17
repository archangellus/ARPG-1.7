using System;
using System.Collections.Generic;
using UnityEngine;

namespace PLAYERTWO.ARPGProject
{
    [Serializable]
    public class SalvageItemOverride
    {
        public Item item;
        public SalvageRecipe recipe;
    }

    [Serializable]
    public class SalvageRoutingRule
    {
        public ItemScope scope =
            ItemScope.Weapon
            | ItemScope.Armor
            | ItemScope.Shield
            | ItemScope.Ring
            | ItemScope.Amulet;

        [Tooltip("-1 matches items without a rarity; -2 matches any rarity.")]
        public int rarityId = -2;

        public int priority;
        public SalvageRecipe recipe;
    }

    [CreateAssetMenu(
        fileName = "Salvage Settings",
        menuName = "PLAYER TWO/ARPG Project/Salvage/Settings"
    )]
    public class SalvageSettings : ScriptableObject
    {
        public int configurationVersion = 1;

        [Min(1)]
        public long materialCap = 999999;

        public List<int> highValueRarityIds = new();
        public List<SalvageItemOverride> itemOverrides = new();
        public List<SalvageRoutingRule> rules = new();
        public SalvageRecipe fallbackRecipe;

        public string fingerprint => $"{name}:{configurationVersion}";

        public bool TryGetRecipe(
            ItemInstance item,
            out SalvageRecipe recipe,
            out string reason
        )
        {
            recipe = null;
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

                recipe = entry.recipe;
                return ValidateRecipe(recipe, out reason);
            }

            var scope = item.GetItemScope();
            var bestPriority = int.MinValue;
            var ambiguous = false;

            foreach (var rule in rules)
            {
                if (
                    rule == null
                    || (rule.scope & scope) == 0
                    || (rule.rarityId != -2 && rule.rarityId != item.rarityId)
                )
                    continue;

                if (rule.priority > bestPriority)
                {
                    bestPriority = rule.priority;
                    recipe = rule.recipe;
                    ambiguous = false;
                }
                else if (rule.priority == bestPriority)
                {
                    ambiguous = true;
                }
            }

            if (ambiguous)
            {
                reason = "Multiple salvage rules have the same highest priority.";
                return false;
            }

            recipe ??= fallbackRecipe;

            if (recipe == null)
            {
                reason = "No salvage recipe is configured for this equipment.";
                return false;
            }

            return ValidateRecipe(recipe, out reason);
        }

        public bool ValidateRecipe(SalvageRecipe recipe, out string reason)
        {
            if (!recipe)
            {
                reason = "The salvage recipe reference is missing.";
                return false;
            }

            if (
                string.IsNullOrEmpty(recipe.id)
                || recipe.rewards == null
                || recipe.rewards.Count == 0
            )
            {
                reason = "The salvage recipe is incomplete.";
                return false;
            }

            var ids = new HashSet<string>();

            foreach (var reward in recipe.rewards)
            {
                if (
                    reward?.material == null
                    || string.IsNullOrEmpty(reward.material.id)
                    || reward.quantity <= 0
                )
                {
                    reason = "The salvage recipe has an invalid material reward.";
                    return false;
                }

                if (!ids.Add(reward.material.id))
                {
                    reason = "The salvage recipe contains a duplicate material.";
                    return false;
                }
            }

            reason = null;
            return true;
        }
    }
}
