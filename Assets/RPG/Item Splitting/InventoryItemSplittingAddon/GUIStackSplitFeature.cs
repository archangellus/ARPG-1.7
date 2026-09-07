using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PLAYERTWO.ARPGProject
{
    /// <summary>
    /// Add-on stack splitting for the unmodified ARPG Project GUI classes.
    ///
    /// Usage:
    /// 1. Add this component to the same GameObject as GUI.
    /// 2. Pick up a stack normally with left click.
    /// 3. Hold Shift and left-click an empty inventory cell, compatible stack, or compatible slot.
    /// 4. Use the quantity menu to increase/decrease the moved amount and confirm.
    ///
    /// The very early execution order lets this component consume a split click before the stock
    /// GUIInventory / GUIItem / GUIItemSlot pointer handlers process the same click.
    /// </summary>
    [DefaultExecutionOrder(-32000)]
    [RequireComponent(typeof(GUI))]
    [AddComponentMenu("PLAYER TWO/ARPG Project/GUI/Stack Split/GUI Stack Split Feature")]
    public class GUIStackSplitFeature : MonoBehaviour
    {
        [Header("Stack Split Settings")]
        [Tooltip("Optional custom split-menu prefab. Leave empty to generate a default menu.")]
        public GUIStackSplitMenuAddon stackSplitMenuPrefab;

        [Tooltip("Optional menu parent. Leave empty to use the GUI transform.")]
        public RectTransform stackSplitMenuContainer;

        [Tooltip("Show a translucent copy at the source cell while Shift is held.")]
        public bool showSourcePreview = true;

        [Range(0.05f, 1f)]
        public float sourcePreviewAlpha = 0.55f;

        private GUI m_gui;
        private GUIStackSplitMenuAddon m_menu;
        private GUIItem m_sourceGuiItem;
        private GUIInventory m_sourceInventory;
        private InventoryCell m_sourceCell;
        private GUIItem m_preview;
        private readonly List<RaycastResult> m_raycastResults = new();

        private void Awake()
        {
            m_gui = GetComponent<GUI>();
        }

        private void Update()
        {
#if UNITY_STANDALONE || UNITY_WEBGL || UNITY_EDITOR
            if (!m_gui)
                m_gui = GUI.instance;

            if (!m_gui || Mouse.current == null)
            {
                DestroyPreview();
                return;
            }

            if (m_menu && m_menu.gameObject.activeInHierarchy)
            {
                DestroyPreview();
                return;
            }

            HandlePreview();

            if (!Mouse.current.leftButton.wasPressedThisFrame)
                return;

            BuildRaycastResults();

            if (!m_gui.selected)
            {
                CapturePotentialSource();
                return;
            }

            if (!IsSplitModifierPressed() || !CanSplitSelected())
                return;

            TryHandleSplitClick();
#endif
        }

        private bool IsSplitModifierPressed()
        {
            return Keyboard.current != null
                && (
                    Keyboard.current.leftShiftKey.isPressed
                    || Keyboard.current.rightShiftKey.isPressed
                );
        }

        private bool CanSplitSelected()
        {
            var selected = m_gui.selected;

            return selected
                && selected == m_sourceGuiItem
                && m_sourceInventory
                && selected.item != null
                && selected.item.IsStackable()
                && selected.item.stack > 1
                && !selected.onMerchant;
        }

        private void BuildRaycastResults()
        {
            m_raycastResults.Clear();

            if (EventSystem.current == null)
                return;

            var pointer = new PointerEventData(EventSystem.current)
            {
                position = EntityInputs.GetPointerPosition(),
            };

            EventSystem.current.RaycastAll(pointer, m_raycastResults);
        }

        private void CapturePotentialSource()
        {
            var item = FindFirstTarget<GUIItem>();
            if (!item || item.item == null || item.onMerchant || !item.item.IsStackable())
                return;

            var inventory = item.GetComponentInParent<GUIInventory>();
            if (!inventory || !inventory.itemsContainer)
                return;

            m_sourceGuiItem = item;
            m_sourceInventory = inventory;
            m_sourceCell = CalculateItemCell(inventory, item);
        }

        private void TryHandleSplitClick()
        {
            var destinationItem = FindFirstTarget<GUIItem>();
            if (destinationItem && destinationItem != m_gui.selected)
            {
                if (TrySplitToExistingStack(destinationItem))
                    SuppressForCurrentPointerFrame(destinationItem);

                return;
            }

            var destinationSlot = FindFirstTarget<GUIItemSlot>();
            if (destinationSlot)
            {
                var handled = TrySplitToSlot(destinationSlot);
                if (handled)
                    SuppressForCurrentPointerFrame(destinationSlot);

                return;
            }

            var destinationInventory = FindFirstTarget<GUIInventory>();
            if (destinationInventory)
            {
                var handled = TrySplitToInventory(destinationInventory);
                if (handled)
                    SuppressForCurrentPointerFrame(destinationInventory);
            }
        }

        private bool TrySplitToInventory(GUIInventory inventory)
        {
            if (!inventory || !CanSplitSelected())
                return false;

            if (WouldOverlapSourceCell(inventory))
            {
                PlayDeniedSound();
                return true;
            }

            var source = m_gui.selected.item;
            var splitItem = source.CopyForStackSplit(1);
            var splitGuiItem = m_gui.CreateGUIItem(splitItem);

            if (!inventory.TryPlace(splitGuiItem))
            {
                splitGuiItem.interactable = false;
                Destroy(splitGuiItem.gameObject);
                PlayDeniedSound();
                return true;
            }

            CompleteInitialSplit(source, splitItem, 0);
            SuppressForCurrentPointerFrame(splitGuiItem);
            return true;
        }

        private bool TrySplitToExistingStack(GUIItem destination)
        {
            if (!destination || destination.onMerchant || !CanSplitSelected())
                return false;

            var source = m_gui.selected.item;
            var one = source.CopyForStackSplit(1);

            if (destination.item == null || !destination.item.CanStack(one))
                return false;

            var destinationInitialStack = destination.item.stack;
            destination.item.stack += 1;
            CompleteInitialSplit(source, destination.item, destinationInitialStack);
            return true;
        }

        private bool TrySplitToSlot(GUIItemSlot slot)
        {
            if (!slot || !CanSplitSelected())
                return false;

            var source = m_gui.selected.item;
            var splitItem = source.CopyForStackSplit(1);
            var splitGuiItem = m_gui.CreateGUIItem(splitItem);
            var destinationInitialStack = 0;

            if (slot.item && slot.item.item != null && slot.item.item.CanStack(splitItem))
            {
                destinationInitialStack = slot.item.item.stack;
                slot.item.item.stack += 1;
                Destroy(splitGuiItem.gameObject);
                splitItem = slot.item.item;
            }
            else if (slot.CanEquip(splitGuiItem))
            {
                slot.Equip(splitGuiItem);
            }
            else
            {
                Destroy(splitGuiItem.gameObject);
                PlayDeniedSound();
                return true;
            }

            CompleteInitialSplit(source, splitItem, destinationInitialStack);
            return true;
        }

        private void CompleteInitialSplit(
            ItemInstance source,
            ItemInstance destination,
            int destinationInitialStack
        )
        {
            source.stack -= 1;
            DestroyPreview();

            var selected = m_gui.selected;
            if (selected)
                selected.TryMoveToLastPosition();

            ShowSplitMenu(source, destination, destinationInitialStack);
        }

        private void ShowSplitMenu(
            ItemInstance source,
            ItemInstance destination,
            int destinationInitialStack
        )
        {
            if (!m_menu)
            {
                Transform parent = stackSplitMenuContainer
                    ? stackSplitMenuContainer
                    : m_gui.transform;
                m_menu = GUIStackSplitMenuAddon.Create(parent, stackSplitMenuPrefab);
            }

            m_menu.Show(source, destination, 1, destinationInitialStack);
        }

        private bool WouldOverlapSourceCell(GUIInventory inventory)
        {
            if (inventory != m_sourceInventory || !m_gui.selected)
                return false;

            var target = inventory.FindClosestCell(m_gui.selected);
            var item = m_gui.selected.item;

            return target.x < m_sourceCell.row + item.rows
                && target.x + item.rows > m_sourceCell.row
                && target.y < m_sourceCell.column + item.columns
                && target.y + item.columns > m_sourceCell.column;
        }

        private InventoryCell CalculateItemCell(GUIInventory inventory, GUIItem item)
        {
            var rect = (RectTransform)item.transform;
            var anchored = rect.anchoredPosition;
            var column = Mathf.RoundToInt(
                (anchored.x + inventory.size.x / 2f - item.size.x / 2f) / Inventory.CellSize
            );
            var row = Mathf.RoundToInt(
                (inventory.size.y / 2f - item.size.y / 2f - anchored.y) / Inventory.CellSize
            );

            return new InventoryCell(row, column);
        }

        private T FindFirstTarget<T>()
            where T : Component
        {
            foreach (var result in m_raycastResults)
            {
                if (!result.gameObject)
                    continue;

                var target = result.gameObject.GetComponentInParent<T>();
                if (target)
                    return target;
            }

            return null;
        }

        private void HandlePreview()
        {
            if (!showSourcePreview || !m_gui.selected || !IsSplitModifierPressed() || !CanSplitSelected())
            {
                DestroyPreview();
                return;
            }

            if (m_preview)
                return;

            var previewInstance = m_gui.selected.item.CopyForStackSplit(m_gui.selected.item.stack);
            m_preview = m_gui.CreateGUIItem(previewInstance, m_sourceInventory.itemsContainer);
            PositionItemAtCell(m_preview, m_sourceInventory, m_sourceCell);
            m_preview.interactable = false;
            m_preview.group.alpha = sourcePreviewAlpha;
        }

        private void PositionItemAtCell(
            GUIItem item,
            GUIInventory inventory,
            InventoryCell cell
        )
        {
            var posX = cell.column * Inventory.CellSize
                - inventory.size.x / 2f
                + item.size.x / 2f;
            var posY = inventory.size.y / 2f
                - item.size.y / 2f
                - cell.row * Inventory.CellSize;

            item.transform.SetParent(inventory.itemsContainer);
            ((RectTransform)item.transform).anchoredPosition = new Vector2(posX, posY);
        }

        private void DestroyPreview()
        {
            if (m_preview)
                Destroy(m_preview.gameObject);

            m_preview = null;
        }

        private void SuppressForCurrentPointerFrame(Behaviour target)
        {
            if (!target || !target.enabled)
                return;

            target.enabled = false;
            StartCoroutine(ReenableNextFrame(target));
        }

        private IEnumerator ReenableNextFrame(Behaviour target)
        {
            yield return null;

            if (target)
                target.enabled = true;
        }

        private void PlayDeniedSound()
        {
            if (GameAudio.instance)
                GameAudio.instance.PlayDeniedSound();
        }

        private void OnDisable()
        {
            DestroyPreview();
        }
    }
}
