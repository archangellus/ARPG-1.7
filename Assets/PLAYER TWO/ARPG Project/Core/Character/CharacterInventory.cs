using System.Collections.Generic;

namespace PLAYERTWO.ARPGProject
{
    public class CharacterInventory
    {
        public int initialMoney;
        public Dictionary<ItemInstance, InventoryCell> initialItems;

        public int currentMoney => m_inventory ? m_inventory.instance.money : initialMoney;
        public Dictionary<ItemInstance, InventoryCell> currentItems =>
            m_inventory != null ? m_inventory.instance.items : initialItems;

        protected EntityInventory m_inventory;

        public CharacterInventory() { }

        public CharacterInventory(Character data) => Initialize(data.inventory, data.initialMoney);

        public CharacterInventory(CharacterInventoryItem[] items, int money) => Initialize(items, money);

        public virtual void InitializeInventory(EntityInventory inventory)
        {
            m_inventory = inventory;
            m_inventory.instance.money = initialMoney;

            foreach (var item in initialItems)
            {
                var row = item.Value.row;
                var column = item.Value.column;
                m_inventory.instance.TryInsertItem(item.Key, row, column);
            }
        }

        protected virtual void Initialize(CharacterInventoryItem[] items, int money)
        {
            initialMoney = money;
            initialItems = new();

            foreach (var inventoryItem in items)
            {
                if (inventoryItem.item.data == null) continue;

                var instance = inventoryItem.item.ToItemInstance();
                initialItems.Add(instance, new(inventoryItem.row, inventoryItem.column));
            }
        }

        public static CharacterInventory CreateFromSerializer(InventorySerializer serializer)
        {
            var inventory = new CharacterInventory();
            inventory.initialMoney = serializer.money;
            inventory.initialItems = new();

            foreach (var entry in serializer.items)
            {
                var instance = ItemInstance.CreateFromSerializer(entry.item);

                if (instance == null)
                    continue;

                inventory.initialItems.Add(instance, new(entry.row, entry.column));
            }

            RepairDuplicateInstanceIds(inventory.initialItems.Keys);

            return inventory;
        }

        /// <summary>
        /// Save data can end up with two Item Instances sharing the same instanceId (e.g. from
        /// an older save predating that field, or a corrupted entry). Regenerates the id of every
        /// duplicate after the first, so this loaded inventory never carries the collision
        /// forward. The first occurrence keeps its original id.
        /// </summary>
        protected static void RepairDuplicateInstanceIds(IEnumerable<ItemInstance> items)
        {
            var seen = new HashSet<string>();

            foreach (var item in items)
                if (!seen.Add(item.instanceId))
                    item.RegenerateInstanceId();
        }
    }
}
