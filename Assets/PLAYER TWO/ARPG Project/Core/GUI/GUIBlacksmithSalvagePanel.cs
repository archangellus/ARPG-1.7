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

        [Tooltip("Optional icon that follows the pointer while picking mode is active.")]
        public RectTransform pickingCursorIcon;

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

            return TryPreviewAndCommit(new[] { item.instanceId });
        }

        /// <summary>Salvages every eligible carried item of the given rarity immediately.</summary>
        public virtual void SalvageByRarity(int rarityId) =>
            SalvageWhere(item => item.rarityId == rarityId, true);

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
            var ids = new List<string>();

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
                    ids.Add(item.instanceId);
            }

            if (ids.Count == 0)
            {
                SetMessage("No eligible items to salvage.");
                return;
            }

            TryPreviewAndCommit(ids);
        }

        protected virtual bool TryPreviewAndCommit(IEnumerable<string> ids)
        {
            m_service.InvalidateAll();

            if (
                !m_service.TryCreatePreview(
                    ids,
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
                    () => Commit(preview, true)
                );
                return true;
            }

            Commit(preview, false);
            return true;
        }

        protected virtual void Commit(SalvagePreview preview, bool highValueConfirmed)
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
                DisplayRewards(preview.materials);
                DisplayReturnedSocketables(preview.returnedSocketables);
                return;
            }

            SetMessage(error);
        }

        protected virtual void DisplayRewards(IReadOnlyList<SalvageRewardTotal> materials)
        {
            foreach (var icon in m_rewardIcons)
                if (icon)
                    Destroy(icon.gameObject);
            m_rewardIcons.Clear();

            if (!salvageRewardsContainer || !materialIconPrefab)
                return;

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
        /// Instantiates one category button per entry of <see cref="GameDatabase.itemRarities"/>
        /// (in index order, matching <c>ItemInstance.rarityId</c>), plus a trailing "All Items"
        /// button. Destroys and recreates any existing children first, so this is safe to call
        /// again if the rarity list changes.
        /// </summary>
        protected virtual void InitializeCategories()
        {
            if (!categoriesContainer || !categoryButtonPrefab)
                return;

            foreach (Transform child in categoriesContainer)
                Destroy(child.gameObject);

            var rarities = GameDatabase.instance.itemRarities;

            for (var i = 0; i < rarities.Count; i++)
            {
                var rarityId = i;
                CreateCategoryButton(rarities[i].displayName, () => SalvageByRarity(rarityId));
            }

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
            pickingCursorIcon.SafeCall(icon => icon.gameObject.SetActive(picking));

        protected virtual void Start()
        {
            InitializeCategories();
            pickingToggle?.onValueChanged.AddListener(SetPickingVisual);
            SetPickingVisual(isPicking);
        }

        protected virtual void Update()
        {
            if (!isPicking || !pickingCursorIcon || !pickingCursorIcon.gameObject.activeSelf)
                return;

            pickingCursorIcon.position = EntityInputs.GetPointerPosition();
        }
    }
}
