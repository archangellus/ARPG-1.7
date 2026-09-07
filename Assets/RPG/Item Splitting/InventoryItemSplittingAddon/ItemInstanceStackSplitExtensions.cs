using System;
using System.Collections.Generic;

namespace PLAYERTWO.ARPGProject
{
    /// <summary>
    /// Stack-splitting helpers kept outside ItemInstance so the core project file does not need
    /// to be modified. The copy preserves rarity, affixes, attributes, durability and sockets.
    /// </summary>
    public static class ItemInstanceStackSplitExtensions
    {
        public static ItemInstance CopyForStackSplit(this ItemInstance source, int stack)
        {
            if (source == null)
                return null;

            var attributes = CopyAttributes(source.attributes);
            var prefixes = source.prefixIndices != null
                ? new List<int>(source.prefixIndices)
                : null;
            var suffixes = source.suffixIndices != null
                ? new List<int>(source.suffixIndices)
                : null;
            var sockets = CopySockets(source.sockets);

            return new ItemInstance(
                source.data,
                attributes,
                source.durability,
                stack,
                source.rarityId,
                prefixes,
                suffixes,
                sockets
            );
        }

        private static ItemAttributes CopyAttributes(ItemAttributes source)
        {
            if (source == null)
                return null;

            var copy = new ItemAttributes();

            foreach (
                ItemAttributes.AttributeType type in Enum.GetValues(
                    typeof(ItemAttributes.AttributeType)
                )
            )
            {
                copy[type] = source[type];
            }

            return copy;
        }

        private static ItemInstance[] CopySockets(ItemInstance[] source)
        {
            if (source == null)
                return null;

            var copy = new ItemInstance[source.Length];

            for (var i = 0; i < source.Length; i++)
            {
                var socket = source[i];
                if (socket == null)
                    continue;

                copy[i] = socket.CopyForStackSplit(socket.IsStackable() ? socket.stack : 1);
            }

            return copy;
        }
    }
}
