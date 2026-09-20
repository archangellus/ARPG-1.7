using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PLAYERTWO.ARPGProject
{
    /// <summary>
    /// The Blacksmith window's Salvage tab. Two ways to salvage: toggle "Directly in Inventory"
    /// to enter picking mode and left-click carried equipment one at a time (handled by
    /// <see cref="GUIItem"/> through <see cref="GUIBlacksmith"/>'s pass-throughs), or click a
    /// rarity category button to salvage every eligible carried item of that rarity at once.
    /// Both paths preview then immediately commit through <see cref="SalvageService"/>, prompting
    /// for confirmation only when the batch includes a configured high-value rarity. Bound to the
    /// active Blacksmith NPC context by <see cref="GUIBlacksmith"/>.
    /// </summary>
    [AddComponentMenu("PLAYER TWO/ARPG Project/GUI/GUI Blacksmith Salvage Panel")]
    public class GUIBlacksmithSalvagePanel : MonoBehaviour
    {
        [Header("Direct Salvage")]
        [Tooltip(
            "Toggling this on enters picking mode: the next left-click on a carried equipment "
                + "Item salvages it immediately. Stays on until toggled off, right-clicked away, "
                + "or the tab/window closes."
        )]
        public Toggle pickingToggle;

        [Tooltip(
            "Optional sprite that follows the pointer while picking mode is active. Drag a "
                + "Sprite asset directly — no scene setup needed, the icon's GameObject is "
                + "created at runtime."
        )]
        public Sprite pickingCursorSprite;

        [Header("Salvage By Rarity")]
        [Tooltip("Container the rarity category buttons are instantiated into at runtime.")]
        public RectTransform categoriesContainer;

        [Tooltip(
            "Button prefab instantiated once per configured rarity tier, plus one for 'All Items'."
        )]
        public Button categoryButtonPrefab;

        [Header("Results")]
        [Tooltip("Container the salvaged material icons are instantiated into after a salvage.")]
        public RectTransform salvageRewardsContainer;

        [Tooltip("Prefab instantiated once per distinct salvaged material.")]
        public GUIBlacksmithMaterialIcon materialIconPrefab;

        [Tooltip("Lists the socketed items returned to the inventory by the last salvage.")]
        public Text salvageReturnedSocketablesText;

        [Tooltip("Eligibility/error/success feedback.")]
        public Text salvageMessageText;

        protected Blacksmith m_blacksmith;
        protected readonly SalvageService m_service = new();
        protected readonly List<GUIBlacksmithMaterialIcon> m_rewardIcons = new();
        protected Image m_pickingCursorImage;

        /// <summary>True while "Directly in Inventory" picking mode is active.</summary>
        public bool isPicking => pickingToggle && pickingToggle.isOn;

        /// <summary>Binds the Blacksmith NPC context this panel operates against.</summary>
        public virtual void Bind(Blacksmith blacksmith) => m_blacksmith = blacksmith;

        /// <summary>Turns picking mode off, e.g. when leaving the tab or closing the window.</summary>
        public virtual void CancelPicking() => pickingToggle.SafeCall(t => t.isOn = false);

        /// <summary>
        /// Attempts to salvage a single carried Item Instance immediately. Used by picking mode
        /// via <see cref="GUIItem"/>'s click handling.
        /// </summary>
        public virtual bool TrySalvageItem(ItemInstance item)
        {
            if (!m_blacksmith || !m_blacksmith.interactingEntity)
            {
                SetMessage("The Blacksmith is no longer available.");
                return false;
            }

            var inventory = m_blacksmith.interactingEntity.inventory.instance;
            var eligibility = m_service.Evaluate(item, inventory, m_blacksmith.salvageSettings);

            if (!eligibility.eligible)
            {
                SetMessage(eligibility.reason);
                return false;
            }

            return TryPreviewAndCommit(new List<ItemInstance> { item });
        }

        /// <summary>
        /// Salvages every eligible carried item of the given rarity immediately. Matches on the
        /// item's effective rarity (<see cref="SalvageSettings.GetEffectiveRarity"/>), so a
        /// category button also covers plain/unrolled equipment when
        /// <see cref="SalvageSettings.defaultRarity"/> is set to this rarity.
        /// </summary>
        public virtual void SalvageByRarity(ItemRarity rarity) =>
            SalvageWhere(
                item => m_blacksmith.salvageSettings.GetEffectiveRarity(item) == rarity,
                true
            );

        /// <summary>
        /// Salvages every eligible carried item, skipping configured high-value rarities, since
        /// this is a broad/blanket action rather than an explicit choice of a specific rarity.
        /// </summary>
        public virtual void SalvageAllEligible() =>
            SalvageWhere(
                item => !m_blacksmith.salvageSettings.highValueRarityIds.Contains(item.rarityId),
                false
            );

        protected virtual void SalvageWhere(Func<ItemInstance, bool> predicate, bool includeHighValue)
        {
            if (!m_blacksmith || !m_blacksmith.interactingEntity)
            {
                SetMessage("The Blacksmith is no longer available.");
                return;
            }

            if (!m_blacksmith.salvageSettings)
            {
                SetMessage("Salvage settings are not assigned on the Blacksmith.");
                return;
            }

            var inventory = m_blacksmith.interactingEntity.inventory.instance;
            var items = new List<ItemInstance>();

            foreach (var item in inventory.items.Keys)
            {
                if (!predicate(item))
                    continue;
                if (
                    !includeHighValue
                    && m_blacksmith.salvageSettings.highValueRarityIds.Contains(item.rarityId)
                )
                    continue;
                if (m_service.Evaluate(item, inventory, m_blacksmith.salvageSettings).eligible)
                    items.Add(item);
            }

            Debug.Log($"[Salvage] SalvageWhere gathered {items.Count} eligible item(s) for this batch.");

            if (items.Count == 0)
            {
                SetMessage("No eligible items to salvage.");
                return;
            }

            TryPreviewAndCommit(items);
        }

        protected virtual bool TryPreviewAndCommit(List<ItemInstance> items)
        {
            m_service.InvalidateAll();

            if (
                !m_service.TryCreatePreview(
                    items.Select(item => item.instanceId),
                    m_blacksmith.interactingEntity,
                    m_blacksmith.salvageSettings,
                    m_blacksmith.salvageProviderId,
                    out var preview,
                    out var error
                )
            )
            {
                SetMessage(error);
                return false;
            }

            if (preview.requiresHighValueConfirmation)
            {
                UIConfirmationScreen.instance.Show(
                    "Salvage the selected high-value equipment? This cannot be undone.",
                    () => Commit(preview, items, true)
                );
                return true;
            }

            Commit(preview, items, false);
            return true;
        }

        protected virtual void Commit(SalvagePreview preview, List<ItemInstance> items, bool highValueConfirmed)
        {
            var owner = m_blacksmith.SafeGet(b => b.interactingEntity);

            if (!m_blacksmith || !m_blacksmith.IsSalvageContextValid(owner))
            {
                SetMessage("The Blacksmith is no longer available.");
                return;
            }

            if (
                m_service.TryCommit(
                    preview,
                    owner,
                    m_blacksmith.salvageSettings,
                    m_blacksmith.salvageProviderId,
                    highValueConfirmed,
                    out var receipt,
                    out var error
                )
            )
            {
                SetMessage(receipt.summary);
                DestroyConsumedGUIItems(items);
                DisplayRewards(preview.materials);
                DisplayReturnedSocketables(preview.returnedSocketables);
                return;
            }

            SetMessage(error);
        }

        /// <summary>
        /// Destroys the GUI representation of every successfully-salvaged Item Instance.
        /// <see cref="SalvageService.TryCommit"/> only removes items from the data-level
        /// <see cref="Inventory"/>; nothing else in that path is GUI-aware, so without this the
        /// consumed items' <see cref="GUIItem"/>s would keep showing in the grid.
        /// </summary>
        protected virtual void DestroyConsumedGUIItems(List<ItemInstance> items)
        {
            var guiInventory = GUIWindowsManager.instance.GetInventory();

            if (!guiInventory)
                return;

            foreach (var item in items)
            {
                var guiItem = guiInventory.FindGUIItem(item);

                if (guiItem)
                    Destroy(guiItem.gameObject);
            }
        }

        protected virtual void DisplayRewards(IReadOnlyList<SalvageRewardTotal> materials)
        {
            foreach (var icon in m_rewardIcons)
                if (icon)
                    Destroy(icon.gameObject);
            m_rewardIcons.Clear();

            if (!salvageRewardsContainer || !materialIconPrefab)
            {
                Debug.Log(
                    $"[Salvage] DisplayRewards: {materials.Count} material(s) to show, but "
                        + $"salvageRewardsContainer={(salvageRewardsContainer ? "assigned" : "NULL")}, "
                        + $"materialIconPrefab={(materialIconPrefab ? "assigned" : "NULL")} — nothing "
                        + "instantiated."
                );
                return;
            }

            Debug.Log($"[Salvage] DisplayRewards: instantiating {materials.Count} material icon(s).");

            foreach (var total in materials)
            {
                var icon = Instantiate(materialIconPrefab, salvageRewardsContainer);
                icon.Initialize(total.material, total.quantity);
                m_rewardIcons.Add(icon);
            }
        }

        protected virtual void DisplayReturnedSocketables(IReadOnlyList<ItemInstance> socketables)
        {
            if (!salvageReturnedSocketablesText)
                return;

            salvageReturnedSocketablesText.text =
                socketables == null || socketables.Count == 0
                    ? "No socketables returned"
                    : string.Join("\n", socketables.Select(item => item.data.name));
        }

        protected virtual void SetMessage(string message)
        {
            if (salvageMessageText)
                salvageMessageText.text = message;
        }

        /// <summary>
        /// Instantiates one category button per entry of <see cref="GameDatabase.itemRarities"/>,
        /// plus a trailing "All Items" button. Destroys and recreates any existing children
        /// first, so this is safe to call again if the rarity list changes.
        /// </summary>
        protected virtual void InitializeCategories()
        {
            if (!categoriesContainer || !categoryButtonPrefab)
                return;

            foreach (Transform child in categoriesContainer)
                Destroy(child.gameObject);

            foreach (var rarity in GameDatabase.instance.itemRarities)
                CreateCategoryButton(rarity.displayName, () => SalvageByRarity(rarity));

            CreateCategoryButton("All Items", SalvageAllEligible);
        }

        protected virtual void CreateCategoryButton(string label, UnityEngine.Events.UnityAction onClick)
        {
            var button = Instantiate(categoryButtonPrefab, categoriesContainer);
            var text = button.GetComponentInChildren<Text>();

            if (text)
                text.text = label;

            button.onClick.AddListener(onClick);
        }

        protected virtual void SetPickingVisual(bool picking) =>
            m_pickingCursorImage.SafeCall(icon => icon.gameObject.SetActive(picking));

        /// <summary>
        /// Builds the picking cursor's Image at runtime from <see cref="pickingCursorSprite"/>,
        /// parented under the nearest Canvas so it renders above the rest of the UI and follows
        /// the pointer regardless of this panel's own layout. No scene object is needed for it.
        /// </summary>
        protected virtual void InitializePickingCursor()
        {
            if (!pickingCursorSprite)
                return;

            var canvas = GetComponentInParent<Canvas>();

            if (!canvas)
                return;

            var go = new GameObject("Picking Cursor Icon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas.transform, false);

            m_pickingCursorImage = go.GetComponent<Image>();
            m_pickingCursorImage.sprite = pickingCursorSprite;
            m_pickingCursorImage.raycastTarget = false;
            m_pickingCursorImage.SetNativeSize();
            go.SetActive(false);
        }

        protected virtual void Start()
        {
            InitializeCategories();
            InitializePickingCursor();
            pickingToggle?.onValueChanged.AddListener(SetPickingVisual);
            SetPickingVisual(isPicking);
        }

        protected virtual void Update()
        {
            if (!isPicking || !m_pickingCursorImage || !m_pickingCursorImage.gameObject.activeSelf)
                return;

            m_pickingCursorImage.rectTransform.position = EntityInputs.GetPointerPosition();
        }
    }
}
