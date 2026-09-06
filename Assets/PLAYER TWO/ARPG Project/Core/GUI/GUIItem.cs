using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PLAYERTWO.ARPGProject
{
    [RequireComponent(typeof(RectTransform), typeof(Image), typeof(CanvasGroup))]
    [AddComponentMenu("PLAYER TWO/ARPG Project/GUI/GUI Item")]
    public class GUIItem
        : MonoBehaviour,
            IPointerEnterHandler,
            IPointerExitHandler,
            IPointerDownHandler
#if UNITY_ANDROID || UNITY_IOS
            ,
            IDragHandler,
            IEndDragHandler,
            IDropHandler,
            IDeselectHandler
#endif
    {
        [Tooltip("A reference to the Text component that represents the stack size.")]
        public Text stackText;

        [Tooltip("The parent Rect Transform under which socket slots are instantiated.")]
        public RectTransform socketsContainer;

        [Tooltip("The prefab instantiated for each of this item's socket slots.")]
        public GUISocketSlot socketSlotPrefab;

        protected Image m_image;
        protected CanvasGroup m_group;
        protected GUIItemSlot m_lastSlot;
        protected GUIInventory m_lastInventory;
        protected readonly List<GUISocketSlot> m_socketSlots = new();

        protected bool m_hovering;
        protected bool m_selected;
        protected InventoryCell m_lastInventoryPosition;

        protected float m_lastClickTime;

        protected const float k_doubleClickThreshold = 0.3f;

        /// <summary>
        /// Grid coordinates (0-2 on both axes) for each socket icon, arranged like the pips on
        /// a die face, indexed by [socket count - 1][socket index]. Only covers up to 6 sockets;
        /// beyond that, <see cref="GetSocketAnchor"/> falls back to a plain grid.
        /// </summary>
        protected static readonly Vector2Int[][] k_diceSlotLayouts =
        {
            new Vector2Int[] { new(1, 1) },
            new Vector2Int[] { new(0, 0), new(2, 2) },
            new Vector2Int[] { new(0, 0), new(1, 1), new(2, 2) },
            new Vector2Int[] { new(0, 0), new(2, 0), new(0, 2), new(2, 2) },
            new Vector2Int[] { new(0, 0), new(2, 0), new(1, 1), new(0, 2), new(2, 2) },
            new Vector2Int[] { new(0, 0), new(2, 0), new(0, 1), new(2, 1), new(0, 2), new(2, 2) },
        };

        /// <summary>
        /// Returns the GUI Merchant associated to this GUI Item.
        /// </summary>
        public GUIMerchant merchant { get; set; }

        /// <summary>
        /// Returns the Item Instance that this GUI Item represents.
        /// </summary>
        public ItemInstance item { get; protected set; }

        /// <summary>
        /// Returns the Image component of this GUI Item.
        /// </summary>
        public Image image
        {
            get
            {
                if (!m_image)
                    m_image = GetComponent<Image>();

                return m_image;
            }
        }

        /// <summary>
        /// Returns the Canvas Group of this GUI Item.
        /// </summary>
        public CanvasGroup group
        {
            get
            {
                if (!m_group)
                    m_group = GetComponent<CanvasGroup>();

                return m_group;
            }
        }

        /// <summary>
        /// Returns true if this GUI Item is interactable.
        /// </summary>
        public bool interactable
        {
            get { return group.blocksRaycasts; }
            set { group.blocksRaycasts = value; }
        }

        /// <summary>
        /// Returns true if this item on a Merchant.
        /// </summary>
        public bool onMerchant => merchant;

        protected Entity player => Level.instance.player;

        /// <summary>
        /// Returns the current size of the GUI Item transform.
        /// </summary>
        public Vector2 size => ((RectTransform)transform).sizeDelta;

        protected GUIWindowsManager windowsManager => GUIWindowsManager.instance;
        protected GUIBlacksmith m_blacksmith => windowsManager.blacksmith;
        protected GUIWindow m_stash => windowsManager.stashWindow;
        protected GUIWindow m_merchant => windowsManager.merchantWindow;
        protected GUIInventory m_inventory => windowsManager.GetInventory();

        /// <summary>
        /// Selects this GUI Item.
        /// </summary>
        public virtual void Select()
        {
            group.blocksRaycasts = false;
            ((RectTransform)transform).SetAsLastSibling();

            if (socketsContainer)
                socketsContainer.gameObject.SetActive(false);
        }

        /// <summary>
        /// Deselects this GUI Item.
        /// </summary>
        public virtual void Deselect()
        {
            group.blocksRaycasts = true;

            if (socketsContainer)
                socketsContainer.gameObject.SetActive(true);
        }

        /// <summary>
        /// Returns true if its possible to stack a given item on this one.
        /// </summary>
        /// <param name="other">The item you want to stack.</param>
        public virtual bool CanStack(GUIItem other) => item.CanStack(other.item);

        /// <summary>
        /// Tries to stack a given item on this one.
        /// </summary>
        /// <param name="other">The item you want to stack.</param>
        /// <returns>Returns true if the item was stacked.</returns>
        public virtual bool TryStack(GUIItem other) => item.TryStack(other.item);

        /// <summary>
        /// Returns true if it's possible to attach a given Socketable item to this one.
        /// </summary>
        /// <param name="other">The Socketable item you want to attach.</param>
        public virtual bool CanSocket(GUIItem other) => other && item.CanAddSocket(other.item);

        /// <summary>
        /// Tries to attach a given Socketable item to one of this item's empty socket slots.
        /// </summary>
        /// <param name="other">The Socketable item you want to attach.</param>
        /// <returns>Returns true if the item was attached as a socket.</returns>
        public virtual bool TrySocket(GUIItem other)
        {
            if (!CanSocket(other))
                return false;

            return item.TryAddSocket(other.item);
        }

        /// <summary>
        /// Consumes the currently selected Socketable after it was successfully attached as a
        /// socket: decrements one unit from its stack, or clears it entirely if it isn't
        /// stackable.
        /// </summary>
        protected virtual void HandleSocketed()
        {
            var socketable = GUI.instance.selected;

            if (socketable.item.IsStackable())
            {
                socketable.item.stack -= 1;

                if (socketable.item.stack <= 0)
                    GUI.instance.ClearSelection();
            }
            else
            {
                GUI.instance.ClearSelection();
            }
        }

        protected virtual void HandleLeftClick()
        {
            if (onMerchant)
                HandleBuy();
            else if (!GUI.instance.selected)
                GUI.instance.Select(this);
            else if (TrySocket(GUI.instance.selected))
                HandleSocketed();
            else if (TryStack(GUI.instance.selected))
                GUI.instance.ClearSelection();
            else
                GameAudio.instance.PlayDeniedSound();
        }

        protected virtual void HandleRightClick()
        {
            if (onMerchant)
            {
#if UNITY_ANDROID || UNITY_IOS
                HandleBuy();
#endif
                return;
            }

            if (m_blacksmith.isOpen)
                HandleBlacksmithEquip();
            else if (m_stash.isOpen)
                HandleMoveToStash();
            else if (m_merchant.isOpen)
                HandleSell();
            else
                HandleEquip();
        }

        protected virtual void HandleBuy() => merchant.TrySell(this);

        protected virtual void HandleSell()
        {
            var merchant = m_merchant.GetComponent<GUIMerchant>();

            if (merchant.TryBuy(this))
                this.merchant = merchant;
        }

        protected virtual void HandleBlacksmithEquip()
        {
            if (!m_blacksmith.slot.CanEquip(this))
                return;

            if (m_inventory.TryRemove(this))
                m_blacksmith.slot.Equip(this);
        }

        protected virtual void HandleEquip()
        {
            if (item.IsEquippable())
            {
                if (
                    m_inventory
                    && m_inventory.equipments.TryAutoEquip(this)
                    && m_inventory.TryRemove(this)
                )
                    return;
            }
            else if (item.IsConsumable())
            {
                if (item.GetConsumable().consumeImmediately)
                    HandleConsumeImmediately();
                else
                {
                    var hud = GUIEntity.instance;

                    if (hud && hud.TryEquipConsumable(this) && m_inventory)
                        m_inventory.TryRemove(this);
                }
            }
            else if (item.IsSkill())
            {
                if (player.skills.TryLearnSkill(item.GetSkill()) && m_inventory.TryRemove(this))
                    Destroy(gameObject);
            }
        }

        /// <summary>
        /// Consumes this Consumable item directly from the Inventory, without equipping it to a
        /// consumable slot. The item is only removed once the Consume call reports success.
        /// </summary>
        protected virtual void HandleConsumeImmediately()
        {
            var instance = item;
            var consumable = instance.GetConsumable();

            if (!consumable.CanConsume(player))
                return;

            consumable.Consume(
                player,
                instance,
                () =>
                {
                    if (m_inventory)
                        m_inventory.RemoveStack(instance);
                }
            );
        }

        protected virtual void HandleMoveToStash()
        {
            var source = GetComponentInParent<GUIInventory>();

            if (source is GUIStash)
            {
                if (m_inventory.TryAutoInsert(this))
                    source.TryRemove(this);
            }
            else if (m_stash.GetComponentInChildren<GUIInventory>().TryAutoInsert(this))
                source.TryRemove(this);
        }

        /// <summary>
        /// Updates the stack size text.
        /// </summary>
        public virtual void UpdateStackText()
        {
            if (!stackText || item == null)
                return;

            stackText.enabled = item.IsStackable() && item.stack > 1;

            if (stackText.enabled)
                stackText.text = item.stack.ToString();
        }

        /// <summary>
        /// Updates the socket slots to reflect this item's total socket count and which ones are
        /// currently filled, laid out like the pips on a die face. All slots — filled or empty —
        /// are always instantiated.
        /// </summary>
        public virtual void UpdateSocketIcons()
        {
            if (!socketsContainer || !socketSlotPrefab || item == null || item.sockets == null)
                return;

            var total = item.sockets.Length;

            for (int i = 0; i < total; i++)
            {
                var slot = GetOrCreateSocketSlot(i);
                slot.SetIcon(item.sockets[i]?.data.image);
                PositionSocketSlot(slot, i, total);
            }
        }

        protected virtual GUISocketSlot GetOrCreateSocketSlot(int index)
        {
            while (m_socketSlots.Count <= index)
                m_socketSlots.Add(Instantiate(socketSlotPrefab, socketsContainer));

            return m_socketSlots[index];
        }

        /// <summary>
        /// Anchors a socket slot to its die-pip position for the given index and total socket
        /// count.
        /// </summary>
        protected virtual void PositionSocketSlot(GUISocketSlot slot, int index, int total)
        {
            var rect = (RectTransform)slot.transform;
            var anchor = GetSocketAnchor(index, total);

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Returns the normalized anchor position (0-1 on both axes) for a socket slot at the
        /// given index. Weapons lay their sockets out in a single vertical column; other item
        /// types arrange them like the pips on a die face for up to 6 total sockets, wrapping
        /// into a plain grid beyond that.
        /// </summary>
        protected virtual Vector2 GetSocketAnchor(int index, int total)
        {
            if (item != null && item.IsWeapon())
                return GetVerticalSocketAnchor(index, total);

            if (total <= k_diceSlotLayouts.Length)
            {
                var cell = k_diceSlotLayouts[total - 1][index];
                return new Vector2((cell.x + 0.5f) / 3f, 1f - (cell.y + 0.5f) / 3f);
            }

            var columns = Mathf.CeilToInt(Mathf.Sqrt(total));
            var rows = Mathf.CeilToInt((float)total / columns);
            var column = index % columns;
            var row = index / columns;

            return new Vector2((column + 0.5f) / columns, 1f - (row + 0.5f) / rows);
        }

        /// <summary>
        /// Returns the normalized anchor position for a weapon's socket slot at the given
        /// index, evenly distributed along a single vertical column.
        /// </summary>
        protected virtual Vector2 GetVerticalSocketAnchor(int index, int total) =>
            new(0.5f, 1f - (index + 0.5f) / total);

        /// <summary>
        /// Sets the last position of this GUI Item from a given GUI Inventory.
        /// </summary>
        /// <param name="inventory">The inventory you want to set as last one.</param>
        /// <param name="position">The row and column you want to set as last one.</param>
        public virtual void SetLastPosition(GUIInventory inventory, InventoryCell position)
        {
            m_lastInventory = inventory;
            m_lastInventoryPosition = position;
            m_lastSlot = null;
        }

        /// <summary>
        /// Sets the last position of this GUI Item from a given GUI Slot.
        /// </summary>
        /// <param name="slot">The GUI Slot you want to set as last one.</param>
        public virtual void SetLastPosition(GUIItemSlot slot)
        {
            m_lastSlot = slot;
            m_lastInventory = null;
        }

        /// <summary>
        /// Tries to move this GUI Item to its last position.
        /// </summary>
        /// <returns>Returns true if successfully moved.</returns>
        public virtual bool TryMoveToLastPosition()
        {
            if (GUI.instance.selected == this)
                GUI.instance.Deselect();

            if (m_lastInventory)
            {
                return m_lastInventory.TryInsert(this, m_lastInventoryPosition)
                    || m_lastInventory.TryAutoInsert(this);
            }

            if (m_lastSlot && m_lastSlot.CanEquip(this))
            {
                m_lastSlot.Equip(this);
                return true;
            }
            else if (Level.instance.player.inventory.instance.TryAddItem(item))
            {
                Destroy(gameObject);
                return true;
            }

            return false;
        }

        public void OnPointerEnter(PointerEventData _)
        {
#if UNITY_STANDALONE || UNITY_WEBGL
            m_hovering = true;

            if (!GUI.instance.selected)
                GUIInspectorManager.instance.itemInspector.Show(this);
#endif
        }

        public void OnPointerExit(PointerEventData _)
        {
#if UNITY_STANDALONE || UNITY_WEBGL
            m_hovering = false;
            GUIInspectorManager.instance.itemInspector.Hide();
#endif
        }

        public void OnPointerDown(PointerEventData eventData)
        {
#if UNITY_STANDALONE || UNITY_WEBGL
            switch (eventData.button)
            {
                case PointerEventData.InputButton.Left:
                    HandleLeftClick();
                    break;
                case PointerEventData.InputButton.Right:
                    HandleRightClick();
                    break;
            }
#else
            if (Time.time - m_lastClickTime < k_doubleClickThreshold)
            {
                HandleRightClick();
            }
            else
            {
                GUIInspectorManager.instance.itemInspector.Hide();
                GUIInspectorManager.instance.itemInspector.Show(this);
                GUIInspectorManager.instance.itemInspector.SetPositionRelativeTo(
                    (RectTransform)transform
                );
                EventSystem.current.SetSelectedGameObject(gameObject);
            }

            m_hovering = true;
            m_lastClickTime = Time.time;

#endif
        }

#if UNITY_ANDROID || UNITY_IOS
        public virtual void OnDrag(PointerEventData _)
        {
            if (onMerchant)
                return;

            GUI.instance.Select(this);
        }

        public virtual void OnEndDrag(PointerEventData _) => GUI.instance.DropItem();

        public virtual void OnDrop(PointerEventData _)
        {
            if (TrySocket(GUI.instance.selected))
                HandleSocketed();
            else if (TryStack(GUI.instance.selected))
                GUI.instance.ClearSelection();
        }

        public virtual void OnDeselect(BaseEventData _)
        {
            m_hovering = false;
            GUIInspectorManager.instance.itemInspector.Hide();
        }
#endif

        /// <summary>
        /// Initializes the GUI Item with a given Item Instance.
        /// </summary>
        /// <param name="item">The Item Instance this GUI Item represents.</param>
        public virtual void Initialize(ItemInstance item)
        {
            if (item == null)
                return;

            this.item = item;
            this.item.onStackChanged += UpdateStackText;
            this.item.onChanged += UpdateSocketIcons;

            image.sprite = item.data.image;
            stackText.enabled = item.IsStackable();
            ((RectTransform)transform).sizeDelta =
                new Vector2(item.columns, item.rows) * Inventory.CellSize;
            merchant = GetComponentInParent<GUIMerchant>();

            UpdateStackText();
            UpdateSocketIcons();
        }

        protected virtual void OnDisable()
        {
            if (m_hovering)
            {
                m_hovering = false;
                GUIInspectorManager.instance.itemInspector.Hide();
            }
        }
    }
}
