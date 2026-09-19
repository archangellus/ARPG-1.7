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
    }
}
