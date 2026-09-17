using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PLAYERTWO.ARPGProject
{
    [AddComponentMenu("PLAYER TWO/ARPG Project/GUI/GUI Blacksmith")]
    public class GUIBlacksmith : GUIWindow
    {
        [Header("Blacksmith Settings")]
        [Tooltip("The slot to place the items to repair.")]
        public GUIBlacksmithSlot slot;

        [Tooltip("The reference to the 'repair' Button.")]
        public Button repairButton;

        [Tooltip("The reference to the 'repair all' Button.")]
        public Button repairAllButton;

        [Tooltip("The reference to the 'repair cost' Text.")]
        public Text repairCostText;

        [Tooltip("The reference to the 'repair all cost' Text.")]
        public Text repairAllCostText;

        [Header("Socket Removal Settings")]
        [Tooltip("The reference to the 'remove sockets' Button.")]
        public Button removeSocketsButton;

        [Tooltip("The reference to the 'remove sockets cost' Text.")]
        public Text removeSocketsCostText;

        [Tooltip(
            "The confirmation message shown before destroying the main Item to remove its "
                + "sockets. Use '{0}' as a placeholder for the Item's display name."
        )]
        [TextArea]
        public string removeSocketsConfirmationMessage =
            "Removing the sockets from {0} will destroy it. Do you want to proceed?";

        [Tooltip("The color used for the Item's name in the confirmation message when it has no rarity assigned.")]
        public Color regularColor = new(1, 1, 1, 1);

        [Header("Tabs")]
        [Tooltip("The same UITab prefab used by the Merchant window.")]
        public UITab tabPrefab;

        [Tooltip("A reference to the Toggle Group shared by the Blacksmith tabs.")]
        public ToggleGroup toggleGroup;

        [Tooltip("The container where Repair and Salvage tabs are instantiated.")]
        public RectTransform tabsContainer;

        [Tooltip("Root containing the existing repair and socket-removal controls.")]
        public GameObject repairTabPanel;

        [Tooltip("Root containing the salvage controls.")]
        public GameObject salvageTabPanel;

        [Tooltip("The Audio Clip that plays when switching between tabs.")]
        public AudioClip switchTabClip;

        [Header("Salvage Tab")]
        public Text salvageSelectedCountText;
        public Text salvageRewardsText;
        public Text salvageReturnedSocketablesText;
        public Text salvageMessageText;
        public Dropdown salvageRarityDropdown;
        public Button salvageConfirmButton;
        public Button salvageSelectJunkButton;
        public Button salvageSelectRarityButton;
        public Button salvageSelectAllEligibleButton;
        public Button salvageClearButton;

        [Header("Audio Settings")]
        [Tooltip("The Audio Clip that plays when repairing an Item.")]
        public AudioClip repairAudio;

        [Tooltip("The Audio Clip that plays when removing sockets from an Item.")]
        public AudioClip removeSocketsAudio;

        protected Blacksmith m_blacksmith;
        protected GUIInventory m_inventory;
        protected readonly HashSet<string> m_salvageSelected = new();
        protected readonly SalvageService m_salvageService = new();
        protected SalvagePreview m_salvagePreview;
        protected bool m_showingSalvage;
        protected UITab m_repairTab;
        protected UITab m_salvageTab;

        protected virtual void UpdateButtons()
        {
            repairButton.interactable =
                m_blacksmith.GetPriceToRepair(slot.item.SafeGet(i => i.item)) > 0;
            repairAllButton.interactable = m_blacksmith.GetPriceToRepairAll() > 0;
            removeSocketsButton.interactable =
                m_blacksmith.GetPriceToRemoveSockets(slot.item.SafeGet(i => i.item)) > 0;
        }

        protected virtual void InitializeCallbacks()
        {
            repairButton.onClick.AddListener(OnRepairClicked);
            repairAllButton.onClick.AddListener(OnRepairAllClicked);
            removeSocketsButton.onClick.AddListener(OnRemoveSocketsClicked);
            slot.onEquip.AddListener(OnEquip);
            slot.onUnequip.AddListener(OnUnequip);
            salvageConfirmButton?.onClick.AddListener(OnSalvageConfirmClicked);
            salvageSelectJunkButton?.onClick.AddListener(SelectSalvageJunk);
            salvageSelectRarityButton?.onClick.AddListener(SelectSalvageRarity);
            salvageSelectAllEligibleButton?.onClick.AddListener(SelectAllSalvageEligible);
            salvageClearButton?.onClick.AddListener(ClearSalvageSelection);
        }

        /// <summary>
        /// Creates the Blacksmith tabs with the same UITab prefab, ToggleGroup, and
        /// value-change section switching used by <see cref="GUIMerchant"/>.
        /// </summary>
        protected virtual void InitializeTabs()
        {
            if (!tabPrefab || !tabsContainer || !toggleGroup)
                return;

            foreach (Transform child in tabsContainer)
                Destroy(child.gameObject);

            m_repairTab = Instantiate(tabPrefab, tabsContainer);
            m_salvageTab = Instantiate(tabPrefab, tabsContainer);
            ConfigureTab(m_repairTab, "Repair", repairTabPanel, true);
            ConfigureTab(m_salvageTab, "Salvage", salvageTabPanel, false);
        }

        protected virtual void ConfigureTab(
            UITab tab,
            string title,
            GameObject section,
            bool isFirstSection
        )
        {
            tab.text.text = title;
            tab.toggle.group = toggleGroup;
            tab.toggle.isOn = isFirstSection;
            section.SafeCall(panel => panel.SetActive(isFirstSection));
            tab.toggle.onValueChanged.AddListener(
                value =>
                {
                    section.SafeCall(panel => panel.SetActive(value));
                    m_showingSalvage = value && section == salvageTabPanel;
                    if (m_showingSalvage)
                        RefreshSalvagePreview();
                    if (m_audio)
                        m_audio.PlayUiEffect(switchTabClip);
                }
            );
        }

        public virtual void ShowRepairTab()
        {
            m_showingSalvage = false;
            if (m_repairTab)
                m_repairTab.toggle.isOn = true;
            else
            {
                repairTabPanel.SafeCall(panel => panel.SetActive(true));
                salvageTabPanel.SafeCall(panel => panel.SetActive(false));
            }
        }

        public virtual void ShowSalvageTab()
        {
            if (m_salvageTab)
                m_salvageTab.toggle.isOn = true;
            else
            {
                m_showingSalvage = true;
                repairTabPanel.SafeCall(panel => panel.SetActive(false));
                salvageTabPanel.SafeCall(panel => panel.SetActive(true));
                RefreshSalvagePreview();
            }
        }

        /// <summary>Selection hook for inventory rows shown alongside the Blacksmith window.</summary>
        public virtual bool SetSalvageSelected(ItemInstance item, bool selected)
        {
            if (!m_blacksmith || item == null || !m_blacksmith.interactingEntity)
                return false;

            var inventory = m_blacksmith.interactingEntity.inventory.instance;
            var eligibility = m_salvageService.Evaluate(
                item,
                inventory,
                m_blacksmith.salvageSettings
            );

            if (!eligibility.eligible)
            {
                SetSalvageMessage(eligibility.reason);
                return false;
            }

            if (selected)
                m_salvageSelected.Add(item.instanceId);
            else
                m_salvageSelected.Remove(item.instanceId);

            RefreshSalvagePreview();
            return true;
        }

        public virtual void SelectSalvageJunk() =>
            SelectSalvageWhere(item => item.isJunk, true);

        public virtual void SelectSalvageRarity()
        {
            if (!salvageRarityDropdown)
                return;

            SelectSalvageWhere(item => item.rarityId == salvageRarityDropdown.value - 1, true);
        }

        public virtual void SelectAllSalvageEligible() =>
            SelectSalvageWhere(
                item => !m_blacksmith.salvageSettings.highValueRarityIds.Contains(item.rarityId),
                false
            );

        public virtual void ClearSalvageSelection()
        {
            m_salvageSelected.Clear();
            RefreshSalvagePreview();
        }

        protected virtual void SelectSalvageWhere(
            System.Func<ItemInstance, bool> predicate,
            bool includeHighValue
        )
        {
            if (!m_blacksmith || !m_blacksmith.interactingEntity || !m_blacksmith.salvageSettings)
                return;

            var inventory = m_blacksmith.interactingEntity.inventory.instance;
            foreach (var item in inventory.items.Keys)
            {
                if (!predicate(item))
                    continue;
                if (
                    !includeHighValue
                    && m_blacksmith.salvageSettings.highValueRarityIds.Contains(item.rarityId)
                )
                    continue;
                if (m_salvageService.Evaluate(item, inventory, m_blacksmith.salvageSettings).eligible)
                    m_salvageSelected.Add(item.instanceId);
            }

            RefreshSalvagePreview();
        }

        protected virtual void OnSalvageConfirmClicked()
        {
            if (m_salvagePreview == null)
                return;

            if (m_salvagePreview.requiresHighValueConfirmation)
            {
                UIConfirmationScreen.instance.Show(
                    "Salvage the selected high-value equipment? This cannot be undone.",
                    () => CommitSalvage(true)
                );
                return;
            }

            CommitSalvage(false);
        }

        protected virtual void CommitSalvage(bool highValueConfirmed)
        {
            var owner = m_blacksmith.SafeGet(blacksmith => blacksmith.interactingEntity);
            if (!m_blacksmith || !m_blacksmith.IsSalvageContextValid(owner))
            {
                SetSalvageMessage("The Blacksmith is no longer available.");
                return;
            }

            if (
                m_salvageService.TryCommit(
                    m_salvagePreview,
                    owner,
                    m_blacksmith.salvageSettings,
                    m_blacksmith.salvageProviderId,
                    highValueConfirmed,
                    out var receipt,
                    out var error
                )
            )
            {
                m_salvageSelected.Clear();
                SetSalvageMessage(receipt.summary);
                RefreshSalvagePreview();
                return;
            }

            SetSalvageMessage(error);
        }

        protected virtual void RefreshSalvagePreview()
        {
            if (!m_blacksmith || !m_blacksmith.interactingEntity)
                return;

            var inventory = m_blacksmith.interactingEntity.inventory.instance;
            var carriedIds = new HashSet<string>(
                inventory.items.Keys.Select(item => item.instanceId)
            );
            m_salvageSelected.RemoveWhere(id => !carriedIds.Contains(id));
            m_salvageService.InvalidateAll();
            m_salvagePreview = null;

            if (
                m_salvageSelected.Count > 0
                && !m_salvageService.TryCreatePreview(
                    m_salvageSelected,
                    m_blacksmith.interactingEntity,
                    m_blacksmith.salvageSettings,
                    m_blacksmith.salvageProviderId,
                    out m_salvagePreview,
                    out var error
                )
            )
                SetSalvageMessage(error);

            if (salvageSelectedCountText)
                salvageSelectedCountText.text = $"Selected: {m_salvageSelected.Count}";
            if (salvageRewardsText)
                salvageRewardsText.text =
                    m_salvagePreview == null
                        ? "No rewards"
                        : string.Join(
                            "\n",
                            m_salvagePreview.materials.Select(
                                value => $"{value.material.displayName}: {value.quantity}"
                            )
                        );
            if (salvageReturnedSocketablesText)
                salvageReturnedSocketablesText.text =
                    m_salvagePreview == null || m_salvagePreview.returnedSocketables.Count == 0
                        ? "No socketables returned"
                        : string.Join(
                            "\n",
                            m_salvagePreview.returnedSocketables.Select(item => item.data.name)
                        );
            if (salvageConfirmButton)
                salvageConfirmButton.interactable =
                    m_salvagePreview != null
                    && m_blacksmith.IsSalvageContextValid(m_blacksmith.interactingEntity);
        }

        protected virtual void SetSalvageMessage(string message)
        {
            if (salvageMessageText)
                salvageMessageText.text = message;
        }

        protected virtual void OnRepairClicked()
        {
            if (!m_blacksmith || !slot.item)
                return;

            if (m_blacksmith.TryRepair(slot.item.item))
            {
                ClearRepairCost();
                UpdateButtons();
                m_audio.PlayUiEffect(repairAudio);
            }
        }

        protected virtual void OnRepairAllClicked()
        {
            if (!m_blacksmith)
                return;

            if (m_blacksmith.TryRepairAll())
            {
                UpdateRepairAllCost();
                UpdateButtons();
                m_audio.PlayUiEffect(repairAudio);
            }
        }

        protected virtual void OnRemoveSocketsClicked()
        {
            if (!m_blacksmith || !slot.item)
                return;

            var item = slot.item.item;

            if (!m_blacksmith.CanRemoveSockets(item))
            {
                m_audio.PlayDeniedSound();
                return;
            }

            if (!m_blacksmith.breakItemOnSocketRemoval)
            {
                PerformRemoveSockets();
                return;
            }

            var coloredName = item.GetDisplayName().WithColor(item.GetRarityColor(regularColor));
            var message = string.Format(removeSocketsConfirmationMessage, coloredName);
            UIConfirmationScreen.instance.Show(message, PerformRemoveSockets);
        }

        protected virtual void PerformRemoveSockets()
        {
            if (!m_blacksmith || !slot.item)
                return;

            var guiItem = slot.item;

            if (!m_blacksmith.TryRemoveSockets(guiItem.item))
            {
                m_audio.PlayDeniedSound();
                return;
            }

            if (m_blacksmith.breakItemOnSocketRemoval)
            {
                slot.Unequip();
                Destroy(guiItem.gameObject);
            }
            else
            {
                if (guiItem.TryMoveToLastPosition())
                    slot.Unequip();

                UpdateRemoveSocketsCost();
                UpdateButtons();
            }

            m_audio.PlayUiEffect(removeSocketsAudio);
        }

        public virtual void OnEquip(GUIItem item)
        {
            if (item.item.GetDurabilityRate() == 1)
                ClearRepairCost();
            else
                UpdateRepairCost();

            UpdateRepairAllCost();
            UpdateRemoveSocketsCost();
            UpdateButtons();
        }

        public virtual void OnUnequip(GUIItem _)
        {
            ClearRepairCost();
            ClearRemoveSocketsCost();
            UpdateRepairAllCost();
            UpdateButtons();
        }

        public virtual void Show(Blacksmith blacksmith)
        {
            base.Show();
            m_blacksmith = blacksmith;
            m_inventory = GUIWindowsManager.instance.GetInventory();
            m_inventory.GetComponent<GUIWindow>().SafeCall(w => w.Show());
            UpdateRepairAllCost();
            UpdateRemoveSocketsCost();
            UpdateButtons();
            ShowRepairTab();
        }

        public virtual void Refresh()
        {
            if (!isOpen)
                return;

            UpdateRepairCost();
            UpdateRepairAllCost();
            UpdateRemoveSocketsCost();
            UpdateButtons();
            if (m_showingSalvage)
                RefreshSalvagePreview();
        }

        protected virtual void UpdateRepairCost() =>
            repairCostText.text = m_blacksmith
                .GetPriceToRepair(slot.item.SafeGet(i => i.item))
                .ToMoneyString();

        protected virtual void ClearRepairCost() => repairCostText.text = "0";

        protected virtual void UpdateRepairAllCost() =>
            repairAllCostText.text = m_blacksmith.GetPriceToRepairAll().ToMoneyString();

        protected virtual void UpdateRemoveSocketsCost() =>
            removeSocketsCostText.text = m_blacksmith
                .GetPriceToRemoveSockets(slot.item.SafeGet(i => i.item))
                .ToMoneyString();

        protected virtual void ClearRemoveSocketsCost() => removeSocketsCostText.text = "0";

        protected override void OnClose()
        {
            m_salvageService.InvalidateAll();
            m_salvagePreview = null;
            m_salvageSelected.Clear();
            ShowRepairTab();

            if (!m_inventory)
                return;

            m_inventory.GetComponent<GUIWindow>().SafeCall(w => w.Hide());
        }

        protected override void Start()
        {
            base.Start();
            InitializeTabs();
            InitializeCallbacks();
            UpdateButtons();
        }

        protected virtual void OnDisable()
        {
            if (!slot || !slot.item)
                return;

            if (slot.item.TryMoveToLastPosition())
                slot.Unequip();
        }
    }
}
