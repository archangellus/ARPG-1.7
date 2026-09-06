using System.Collections.Generic;
using UnityEngine;

namespace PLAYERTWO.ARPGProject
{
    [CreateAssetMenu(fileName = "New Affixes", menuName = "PLAYER TWO/ARPG Project/Item/Affixes")]
    public class ItemAffixes : ScriptableObject
    {
        [System.Serializable]
        public class AffixEntry
        {
            /// <summary>The display name of this affix (e.g. "Flaming", "of Fire").</summary>
            [Tooltip("The display name of this affix (e.g. \"Flaming\", \"of Fire\").")]
            public string name;

            /// <summary>Which item types this affix applies to.</summary>
            [Tooltip("Which item types this affix applies to.")]
            public ItemScope scope;

            /// <summary>
            /// The minimum rarity tier required for this affix to be available.
            /// A value of 0 means no minimum restriction.
            /// </summary>
            [Tooltip(
                "The minimum rarity tier required for this affix to be available. 0 means no minimum restriction."
            )]
            public int minTier;

            /// <summary>
            /// The maximum rarity tier at which this affix can appear.
            /// A value of 0 means no maximum restriction.
            /// </summary>
            [Tooltip(
                "The maximum rarity tier at which this affix can appear. 0 means no maximum restriction."
            )]
            public int maxTier;

            /// <summary>The attribute modifiers granted by this affix.</summary>
            [Tooltip("The attribute modifiers granted by this affix.")]
            public List<AffixAttributeEntry> attributes = new();
        }

        [System.Serializable]
        public class AffixAttributeEntry
        {
            /// <summary>The type of attribute this entry modifies.</summary>
            [Tooltip("The type of attribute this entry modifies.")]
            public ItemAttributes.AttributeType type;

            /// <summary>The minimum value that can be rolled for this attribute.</summary>
            [Tooltip("The minimum value that can be rolled for this attribute.")]
            public int minValue;

            /// <summary>The maximum value that can be rolled for this attribute.</summary>
            [Tooltip("The maximum value that can be rolled for this attribute.")]
            public int maxValue;
        }

        [Tooltip("Prefix affixes available in this pool.")]
        public List<AffixEntry> prefixes = new();

        [Tooltip("Suffix affixes available in this pool.")]
        public List<AffixEntry> suffixes = new();

        /// <summary>
        /// Returns the indices of all prefix entries whose scope matches the given item scope
        /// and whose tier range includes <paramref name="tier"/>. A tier of 0 skips tier filtering.
        /// </summary>
        public virtual List<int> GetPrefixIndices(ItemScope scope, int tier = 0)
        {
            return GetMatchingIndices(prefixes, scope, tier);
        }

        /// <summary>
        /// Returns the indices of all suffix entries whose scope matches the given item scope
        /// and whose tier range includes <paramref name="tier"/>. A tier of 0 skips tier filtering.
        /// </summary>
        public virtual List<int> GetSuffixIndices(ItemScope scope, int tier = 0)
        {
            return GetMatchingIndices(suffixes, scope, tier);
        }

        /// <summary>
        /// Returns true when <paramref name="entryScope"/> applies to <paramref name="targetScope"/>.
        /// </summary>
        public virtual bool MatchesScope(ItemScope entryScope, ItemScope targetScope)
        {
            return (entryScope & targetScope) != 0;
        }

        protected virtual List<int> GetMatchingIndices(
            List<AffixEntry> source,
            ItemScope scope,
            int tier = 0
        )
        {
            var result = new List<int>();

            for (int i = 0; i < source.Count; i++)
            {
                var entry = source[i];

                if (!MatchesScope(entry.scope, scope) || !IsEntryAllowed(entry))
                    continue;

                if (!MatchesTier(entry, tier))
                    continue;

                result.Add(i);
            }

            return result;
        }

        protected virtual bool MatchesTier(AffixEntry entry, int tier)
        {
            if (tier <= 0)
                return true;

            bool minOk = entry.minTier <= 0 || tier >= entry.minTier;
            bool maxOk = entry.maxTier <= 0 || tier <= entry.maxTier;
            return minOk && maxOk;
        }

        /// <summary>
        /// Returns true if the given affix entry should be included in the available pool.
        /// When the miss system is disabled (<see cref="Game.canMiss"/> is false), any entry
        /// that grants <see cref="ItemAttributes.AttributeType.AccuracyPercent"/> or
        /// <see cref="ItemAttributes.AttributeType.EvasionPercent"/> is excluded.
        /// </summary>
        protected virtual bool IsEntryAllowed(AffixEntry entry)
        {
            if (Game.instance != null && !Game.instance.canMiss)
            {
                foreach (var attr in entry.attributes)
                {
                    if (
                        attr.type == ItemAttributes.AttributeType.Accuracy
                        || attr.type == ItemAttributes.AttributeType.AccuracyPercent
                        || attr.type == ItemAttributes.AttributeType.Evasion
                        || attr.type == ItemAttributes.AttributeType.EvasionPercent
                    )
                        return false;
                }
            }

            return true;
        }
    }
}
