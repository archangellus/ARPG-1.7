using System.Collections.Generic;
using UnityEngine;

namespace PLAYERTWO.ARPGProject
{
    [CreateAssetMenu(
        fileName = "New Socketable",
        menuName = "PLAYER TWO/ARPG Project/Item/Socketable"
    )]
    public class ItemSocketable : Item
    {
        [System.Serializable]
        public class SocketAttributeEntry
        {
            /// <summary>Which item types this bonus applies to when socketed.</summary>
            [Tooltip("Which item types this bonus applies to when socketed.")]
            public ItemScope scope;

            /// <summary>The type of attribute this entry modifies.</summary>
            [Tooltip("The type of attribute this entry modifies.")]
            public ItemAttributes.AttributeType type;

            /// <summary>The flat value granted when socketed into a matching item type.</summary>
            [Tooltip("The flat value granted when socketed into a matching item type.")]
            public int value;
        }

        [Header("Socket Settings")]
        [Tooltip("The attribute bonuses granted by this Socketable, scoped per item type.")]
        public List<SocketAttributeEntry> attributes = new();
    }
}
