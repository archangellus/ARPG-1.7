using UnityEngine;

namespace PLAYERTWO.ARPGProject
{
    [AddComponentMenu("PLAYER TWO/ARPG Project/NPC/Blacksmith")]
    public class Blacksmith : Interactive
    {
        [Header("Item Repair Settings")]
        [Tooltip("Minimum cost of repairing an Item.")]
        public int minPrice;

        [Tooltip("Maximum cost of repairing an Item.")]
        public int maxPrice;

        [Header("Socket Removal Settings")]
        [Tooltip("The cost to remove each socket from an Item.")]
        public int socketRemovalPrice = 500;

        [Tooltip(
            "If true, the main Item is destroyed after its sockets are removed, and the "
                + "player must confirm before proceeding. If false, sockets are removed "
                + "without destroying the Item and no confirmation is shown."
        )]
        public bool breakItemOnSocketRemoval = true;

        protected Entity m_entity;

        protected GUIBlacksmith m_blacksmithWindow => GUIWindowsManager.instance.blacksmith;

        /// <summary>
        /// Tries to repair a given Item Instance.
        /// </summary>
        /// <param name="item">The Item Instance you're trying to repair.</param>
        /// <returns>Returns true if the Item Instance was successfully repaired.</returns>
        public virtual bool TryRepair(ItemInstance item)
        {
            var price = GetPriceToRepair(item);

            if (m_entity.inventory.instance.money < price)
                return false;

            item.Repair();
            m_entity.inventory.instance.money -= price;
            return true;
        }

        /// <summary>
        /// Tries to repair all the items from the Entity inventory.
        /// </summary>
        /// <returns>Returns true if the items were repaired.</returns>
        public virtual bool TryRepairAll()
        {
            var price = GetPriceToRepairAll();

            if (m_entity.inventory.instance.money < price)
                return false;

            foreach (var item in m_entity.inventory.instance.items)
                item.Key.Repair();

            foreach (var item in m_entity.items.GetEquippedItems())
                item.Repair();

            m_entity.inventory.instance.money -= price;
            return true;
        }

        /// <summary>
        /// Returns the total cost to repair a given Item Instance.
        /// </summary>
        /// <param name="item">The Item Instance you want to get the cost from.</param>
        public virtual int GetPriceToRepair(ItemInstance item)
        {
            if (item == null)
                return 0;

            var durability = item.GetDurabilityRate();

            if (durability == 1)
                return 0;

            return (int)Mathf.Lerp(maxPrice, minPrice, durability);
        }

        /// <summary>
        /// Returns the total cost to repair all items from the Entity's inventory.
        /// </summary>
        public virtual int GetPriceToRepairAll()
        {
            if (!m_entity)
                return 0;

            var total = 0;

            foreach (var item in m_entity.inventory.instance.items)
                total += GetPriceToRepair(item.Key);

            foreach (var item in m_entity.items.GetEquippedItems())
                total += GetPriceToRepair(item);

            return total;
        }

        /// <summary>
        /// Returns the total cost to remove all sockets from a given Item Instance.
        /// </summary>
        /// <param name="item">The Item Instance you want to get the cost from.</param>
        public virtual int GetPriceToRemoveSockets(ItemInstance item)
        {
            if (item == null)
                return 0;

            return item.GetOccupiedSockets().Count * socketRemovalPrice;
        }

        /// <summary>
        /// Returns true if the Entity can afford to remove all sockets from a given Item
        /// Instance and has enough Inventory space to receive every socketed item back.
        /// </summary>
        /// <param name="item">The Item Instance you want to check.</param>
        public virtual bool CanRemoveSockets(ItemInstance item)
        {
            if (!m_entity || item == null)
                return false;

            var price = GetPriceToRemoveSockets(item);

            if (price <= 0)
                return false;

            if (m_entity.inventory.instance.money < price)
                return false;

            return m_entity.inventory.instance.CanAddItems(item.GetOccupiedSockets());
        }

        /// <summary>
        /// Tries to remove all sockets from a given Item Instance: charges the removal cost and
        /// returns every socketed Item Instance to the Entity's Inventory. When
        /// <see cref="breakItemOnSocketRemoval"/> is true, the given Item Instance itself is not
        /// destroyed here — the caller is responsible for discarding its visual representation
        /// once this returns true. When false, the Item Instance's sockets are cleared instead,
        /// leaving it intact.
        /// </summary>
        /// <param name="item">The Item Instance you want to remove sockets from.</param>
        /// <returns>Returns true if the sockets were successfully removed.</returns>
        public virtual bool TryRemoveSockets(ItemInstance item)
        {
            if (!CanRemoveSockets(item))
                return false;

            var price = GetPriceToRemoveSockets(item);
            var socketedItems = item.GetOccupiedSockets();

            m_entity.inventory.instance.money -= price;

            foreach (var socketedItem in socketedItems)
                m_entity.inventory.instance.TryAddItem(socketedItem);

            if (!breakItemOnSocketRemoval)
                item.ClearSockets();

            return true;
        }

        protected override void OnInteract(object other)
        {
            if (other is not Entity)
                return;

            if ((other as Entity) != m_entity)
            {
                m_entity = other as Entity;
                m_entity.inventory.onItemAdded.AddListener((_) => m_blacksmithWindow.Refresh());
                m_entity.inventory.onItemInserted.AddListener((_) => m_blacksmithWindow.Refresh());
                m_entity.inventory.onItemRemoved.AddListener(m_blacksmithWindow.Refresh);
                m_entity.items.onChanged.AddListener(m_blacksmithWindow.Refresh);
            }

            m_blacksmithWindow.Show(this);
        }
    }
}
