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
            "Caps how much of Quantity a single salvaged item can grant, as a percentage. Each "
                + "item randomly grants a whole number between 0 and Quantity * Drop Chance "
                + "(rounded), rolled once per salvaged item. E.g. Quantity 100 with Drop Chance "
                + "0.459 grants a random amount from 0 to 46. 1 means the full range up to "
                + "Quantity."
        )]
        public float dropChance = 1f;
    }
}
