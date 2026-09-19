using System;
using UnityEngine;

namespace PLAYERTWO.ARPGProject
{
    [Serializable]
    public class SalvageMaterialAmount
    {
        [Tooltip("The Item granted as a salvage reward. Use a stackable, non-equippable Item.")]
        public Item material;

        [Min(1)]
        public int quantity = 1;

        [Range(0, 1)]
        [Tooltip(
            "Chance this reward line is granted at all, rolled once per salvaged item. On "
                + "success the full quantity above is granted; on failure, none of it is. 1 "
                + "means always drops."
        )]
        public float dropChance = 1f;
    }
}
