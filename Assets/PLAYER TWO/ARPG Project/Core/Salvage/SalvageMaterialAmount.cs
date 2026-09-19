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
            "The fraction of Quantity granted every time an item salvages into this reward line "
                + "(e.g. 0.5 grants half of Quantity, rounded to the nearest whole unit). Always "
                + "granted, once per salvaged item — there is no chance of getting nothing "
                + "because of this value. 1 means the full Quantity every time."
        )]
        public float dropChance = 1f;
    }
}
