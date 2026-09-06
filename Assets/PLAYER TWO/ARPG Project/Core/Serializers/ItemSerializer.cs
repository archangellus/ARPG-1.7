using UnityEngine;

namespace PLAYERTWO.ARPGProject
{
    [System.Serializable]
    public class ItemSerializer
    {
        public int itemId = -1;
        public int durability;
        public int stack;
        public ItemAttributes.AttributeEntry[] attributes;
        public int rarityId = -1;
        public int[] prefixIndices;
        public int[] suffixIndices;

        /// <summary>
        /// The Game Database item id socketed into each slot, or -1 for an empty slot. A socketed
        /// item is always attached via the bare <see cref="ItemInstance(Item)"/> constructor (see
        /// <see cref="ItemInstance.TryAddSocket"/>), so it never has its own rarity, affixes, or
        /// nested sockets worth persisting — only which Item occupies the slot matters. Storing a
        /// flat id array here (instead of a recursive <c>ItemSerializer[]</c>) avoids Unity's
        /// serializer depth limit, since <see cref="ItemSerializer"/> would otherwise contain
        /// itself.
        /// </summary>
        public int[] socketItemIds;

        public ItemSerializer() { }

        public ItemSerializer(ItemInstance item)
        {
            itemId = GameDatabase.instance.GetElementId<Item>(item.data);
            durability = item.durability;
            stack = item.stack;
            rarityId = item.rarityId;
            prefixIndices = item.prefixIndices?.ToArray();
            suffixIndices = item.suffixIndices?.ToArray();
            socketItemIds =
                item.sockets != null
                    ? System.Array.ConvertAll(
                        item.sockets,
                        socket =>
                            socket != null ? GameDatabase.instance.GetElementId<Item>(socket.data) : -1
                    )
                    : null;

            var types = ItemAttributes.AllTypes;
            attributes = new ItemAttributes.AttributeEntry[types.Length];

            for (int i = 0; i < types.Length; i++)
                attributes[i] = new ItemAttributes.AttributeEntry
                {
                    type = types[i],
                    value = item.ContainAttributes() ? item.attributes[types[i]] : 0,
                };
        }

        public virtual string ToJson() => JsonUtility.ToJson(this);

        public static ItemSerializer FromJson(string json) =>
            JsonUtility.FromJson<ItemSerializer>(json);
    }
}
