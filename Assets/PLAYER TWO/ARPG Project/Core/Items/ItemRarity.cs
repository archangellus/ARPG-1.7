using System.Collections.Generic;
using UnityEngine;

namespace PLAYERTWO.ARPGProject
{
    [CreateAssetMenu(
        fileName = "New Item Rarity",
        menuName = "PLAYER TWO/ARPG Project/Item/Item Rarity"
    )]
    public class ItemRarity : ScriptableObject
    {
        /// <summary>Determines how affixes are selected when this rarity is rolled on an item.</summary>
        public enum AffixesMode
        {
            /// <summary>
            /// Rolls at most one prefix and/or one suffix.
            /// The <see cref="bothAffixChance"/> controls the probability of receiving both.
            /// </summary>
            Paired,

            /// <summary>
            /// Selects multiple unique affixes from the combined prefix and suffix pool,
            /// up to <see cref="totalAffixes"/>.
            /// </summary>
            Layered,
        }

        [Header("General Settings")]
        [Tooltip("The display name of this rarity tier.")]
        public string displayName;

        [Tooltip("The color used to tint the item name in the UI.")]
        public Color color = Color.white;

        [Tooltip(
            "If true, dropped items of this rarity play a particle effect tinted with this rarity's color."
        )]
        public bool showRarityParticle = false;

        [Header("Audio Settings")]
        [Tooltip(
            "Overrides the Collectible Item's regular drop Audio Clip. Leave empty to use the Collectible's own clip."
        )]
        public AudioClip dropRegularClip;

        [Tooltip(
            "Overrides the Collectible Item's armor drop Audio Clip. Leave empty to use the Collectible's own clip."
        )]
        public AudioClip dropArmorClip;

        [Tooltip(
            "Overrides the Collectible Item's weapon drop Audio Clip. Leave empty to use the Collectible's own clip."
        )]
        public AudioClip dropWeaponClip;

        [Tooltip(
            "Overrides the Collectible Item's regular collect Audio Clip. Leave empty to use the Collectible's own clip."
        )]
        public AudioClip collectRegularClip;

        [Tooltip(
            "Overrides the Collectible Item's armor collect Audio Clip. Leave empty to use the Collectible's own clip."
        )]
        public AudioClip collectArmorClip;

        [Tooltip(
            "Overrides the Collectible Item's weapon collect Audio Clip. Leave empty to use the Collectible's own clip."
        )]
        public AudioClip collectWeaponClip;

        [Header("Affix Settings")]
        [Tooltip("The pool of affixes available for items of this rarity.")]
        public ItemAffixes affixes;

        [Min(1)]
        [Tooltip(
            "The tier level of this rarity. Affixes whose tier range excludes this value will not be rolled."
        )]
        public int tier = 1;

        [Tooltip("Determines how affixes are selected for items of this rarity.")]
        public AffixesMode affixesMode = AffixesMode.Paired;

        [Range(0, 1)]
        [Tooltip(
            "The chance of an item receiving both a prefix and a suffix. "
                + "When this roll fails, only one of the two is assigned at random."
        )]
        public float bothAffixChance = 0.25f;

        [Tooltip("The minimum number of unique affixes to apply to the item.")]
        public int minAffixes = 1;

        [Tooltip(
            "The maximum number of unique affixes to apply to the item. "
                + "If fewer affixes are available, all of them are applied."
        )]
        public int maxAffixes = 2;

        [Range(0, 1)]
        [Tooltip(
            "Controls how rolled affix values are biased within their min–max range. "
                + "0 leans toward minimum values, 0.5 gives a balanced roll, "
                + "1 leans toward maximum values."
        )]
        public float valueWeight = 0.5f;

        [Header("Flat Bonuses")]
        [Tooltip("Flat bonus added to the weapon's minimum and maximum damage.")]
        public int bonusDamage;

        [Tooltip("Flat bonus added to the weapon's attack speed.")]
        public int bonusAttackSpeed;

        [Tooltip("Flat bonus added to the item's maximum durability.")]
        public int bonusMaxDurability;

        [Tooltip("Flat bonus added to the armor or shield's defense.")]
        public int bonusDefense;

        [Tooltip("Flat bonus added to the shield's chance to block.")]
        public int bonusChanceToBlock;

        [System.Serializable]
        public class SocketSlotSetting
        {
            [Tooltip("The item type this setting applies to.")]
            public ItemScope scope;

            [Range(1, 6)]
            [Tooltip("The maximum number of sockets an item of this type can roll.")]
            public int maxSockets = 1;
        }

        [Header("Socket Settings")]
        [Range(0, 1)]
        [Tooltip("The chance for an item of this rarity to roll at least one socket.")]
        public float socketChance;

        [Tooltip(
            "The maximum number of sockets an item can roll, per item type. Item types with no "
                + "entry here never roll sockets."
        )]
        public List<SocketSlotSetting> socketSlotsByType = new();

        /// <summary>
        /// Returns the maximum number of sockets an item of the given scope can roll, or 0 if
        /// no setting matches that scope. A setting matches when it shares at least one flag
        /// with <paramref name="itemScope"/> (e.g. a "Weapon" or "Armor" composite setting
        /// matches every concrete type it covers). When more than one setting matches, the most
        /// specific one (fewest flags) wins, so a single-type override (e.g. "Gloves") takes
        /// priority over a broader composite one (e.g. "Armor") configured on the same rarity.
        /// </summary>
        public virtual int GetMaxSockets(ItemScope itemScope)
        {
            SocketSlotSetting bestMatch = null;

            foreach (var setting in socketSlotsByType)
            {
                if ((setting.scope & itemScope) == 0)
                    continue;

                if (bestMatch == null || CountFlags(setting.scope) < CountFlags(bestMatch.scope))
                    bestMatch = setting;
            }

            return bestMatch != null ? bestMatch.maxSockets : 0;
        }

        static int CountFlags(ItemScope scope)
        {
            var value = (int)scope;
            var count = 0;

            while (value != 0)
            {
                count += value & 1;
                value >>= 1;
            }

            return count;
        }

        /// <summary>
        /// Rolls the number of sockets an item of the given scope should have, based on
        /// <see cref="socketChance"/> and <see cref="GetMaxSockets"/>. Returns 0 when the scope
        /// has no configured sockets or the chance roll fails; otherwise a random value between
        /// 1 and the scope's maximum.
        /// </summary>
        public virtual int RollSocketCount(ItemScope itemScope)
        {
            var maxSockets = GetMaxSockets(itemScope);

            if (maxSockets <= 0 || Random.value > socketChance)
                return 0;

            return Random.Range(1, maxSockets + 1);
        }

        /// <summary>
        /// Rolls and returns the prefix and suffix affix indices for an item with the given scope,
        /// using the selection logic dictated by <see cref="affixesMode"/>.
        /// Only affixes whose tier range includes <see cref="tier"/> are eligible.
        /// Returns empty lists when no affixes are assigned or no matching affixes exist.
        /// </summary>
        public virtual void RollAffixIndices(
            ItemScope itemScope,
            out List<int> prefixIndices,
            out List<int> suffixIndices
        )
        {
            prefixIndices = new List<int>();
            suffixIndices = new List<int>();

            if (affixes == null)
                return;

            if (affixesMode == AffixesMode.Paired)
                RollPairedAffixes(itemScope, prefixIndices, suffixIndices);
            else
                RollLayeredAffixes(itemScope, prefixIndices, suffixIndices);
        }

        /// <summary>
        /// Selects at most one prefix and/or one suffix based on <see cref="bothAffixChance"/>.
        /// </summary>
        protected virtual void RollPairedAffixes(
            ItemScope itemScope,
            List<int> prefixIndices,
            List<int> suffixIndices
        )
        {
            var effectiveTier = Mathf.Max(1, tier);
            var availablePrefixIndices = affixes.GetPrefixIndices(itemScope, effectiveTier);
            var availableSuffixIndices = affixes.GetSuffixIndices(itemScope, effectiveTier);
            var hasPrefixes = availablePrefixIndices.Count > 0;
            var hasSuffixes = availableSuffixIndices.Count > 0;

            if (!hasPrefixes && !hasSuffixes)
                return;

            if (hasPrefixes && hasSuffixes)
            {
                if (Random.value <= bothAffixChance)
                {
                    PickOneAffix(availablePrefixIndices, prefixIndices);
                    PickOneAffix(availableSuffixIndices, suffixIndices);
                }
                else if (Random.value < 0.5f)
                    PickOneAffix(availablePrefixIndices, prefixIndices);
                else
                    PickOneAffix(availableSuffixIndices, suffixIndices);
            }
            else if (hasPrefixes)
                PickOneAffix(availablePrefixIndices, prefixIndices);
            else
                PickOneAffix(availableSuffixIndices, suffixIndices);
        }

        /// <summary>
        /// Selects a random number of unique affixes between <see cref="minAffixes"/> and
        /// <see cref="maxAffixes"/> from the combined prefix and suffix pool, shuffled randomly.
        /// </summary>
        protected virtual void RollLayeredAffixes(
            ItemScope itemScope,
            List<int> prefixIndices,
            List<int> suffixIndices
        )
        {
            var effectiveTier = Mathf.Max(1, tier);
            var availablePrefixIndices = affixes.GetPrefixIndices(itemScope, effectiveTier);
            var availableSuffixIndices = affixes.GetSuffixIndices(itemScope, effectiveTier);

            var combinedPool = new List<(bool isPrefix, int index)>();

            foreach (var i in availablePrefixIndices)
                combinedPool.Add((true, i));

            foreach (var i in availableSuffixIndices)
                combinedPool.Add((false, i));

            Shuffle(combinedPool);

            var pickCount = Mathf.Min(Random.Range(minAffixes, maxAffixes + 1), combinedPool.Count);

            for (int i = 0; i < pickCount; i++)
            {
                var (isPrefix, index) = combinedPool[i];

                if (isPrefix)
                    prefixIndices.Add(index);
                else
                    suffixIndices.Add(index);
            }
        }

        /// <summary>
        /// Randomly picks one index from <paramref name="sourceIndices"/> and appends it to
        /// <paramref name="result"/>.
        /// </summary>
        protected virtual void PickOneAffix(List<int> sourceIndices, List<int> result)
        {
            result.Add(sourceIndices[Random.Range(0, sourceIndices.Count)]);
        }

        /// <summary>
        /// Shuffles a list in-place using the Fisher-Yates algorithm.
        /// </summary>
        protected virtual void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
