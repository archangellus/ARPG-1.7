using UnityEngine;
using UnityEngine.UI;

namespace PLAYERTWO.ARPGProject
{
    /// <summary>
    /// Owns the Blacksmith window's lifecycle and its Repair/Salvage tab switching. The actual
    /// repair and salvage controls live on <see cref="GUIBlacksmithRepairPanel"/> and
    /// <see cref="GUIBlacksmithSalvagePanel"/>, which this component shows, hides, and binds to
    /// the active Blacksmith NPC context.
    /// </summary>
    [AddComponentMenu("PLAYER TWO/ARPG Project/GUI/GUI Blacksmith")]
    public class GUIBlacksmith : GUIWindow
    {
        [Header("Panels")]
        [Tooltip("The panel with the repair and socket-removal controls.")]
        public GUIBlacksmithRepairPanel repairPanel;

        [Tooltip("The panel with the salvage controls.")]
        public GUIBlacksmithSalvagePanel salvagePanel;

        [Header("Tabs")]
        [Tooltip("The same UITab prefab used by the Merchant window.")]
        public UITab tabPrefab;

        [Tooltip("A reference to the Toggle Group shared by the Blacksmith tabs.")]
        public ToggleGroup toggleGroup;

        [Tooltip("The container where Repair and Salvage tabs are instantiated.")]
        public RectTransform tabsContainer;

        [Tooltip(
            "The container that parents the Repair and Salvage panels, mirroring "
                + "GUIMerchant's sectionsContainer. Purely organizational: assigning it "
                + "reparents both panels under it so the hierarchy separates tabs from content."
        )]
        public RectTransform panelsContainer;

        [Tooltip("The Audio Clip that plays when switching between tabs.")]
        public AudioClip switchTabClip;

        protected GUIInventory m_inventory;
        protected UITab m_repairTab;
        protected UITab m_salvageTab;
        protected bool m_showingSalvage;

        protected GameAudio m_audio => GameAudio.instance;

        public bool isShowingSalvage => isOpen && m_showingSalvage;

        /// <summary>
        /// The repair slot from <see cref="repairPanel"/>, exposed here so other GUI components
        /// (e.g. <see cref="GUIEquipmentSlot"/>, <see cref="GUIItem"/>) can keep addressing it as
        /// the Blacksmith's slot without knowing about the panel split.
        /// </summary>
        public GUIBlacksmithSlot slot => repairPanel ? repairPanel.slot : null;

        public virtual bool IsSalvageSelected(ItemInstance item) =>
            salvagePanel && salvagePanel.IsSelected(item);

        /// <summary>Selection hook for inventory rows shown alongside the Blacksmith window.</summary>
        public virtual bool SetSalvageSelected(ItemInstance item, bool selected) =>
            salvagePanel && salvagePanel.SetSelected(item, selected);

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

            if (panelsContainer)
            {
                repairPanel.SafeCall(panel => panel.transform.SetParent(panelsContainer, false));
                salvagePanel.SafeCall(panel => panel.transform.SetParent(panelsContainer, false));
            }

            m_repairTab = Instantiate(tabPrefab, tabsContainer);
            m_salvageTab = Instantiate(tabPrefab, tabsContainer);
            ConfigureTab(m_repairTab, "Repair", repairPanel.SafeGet(panel => panel.gameObject), true);
            ConfigureTab(m_salvageTab, "Salvage", salvagePanel.SafeGet(panel => panel.gameObject), false);
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
                    m_showingSalvage = value && section == salvagePanel.SafeGet(panel => panel.gameObject);
                    if (m_showingSalvage)
                        salvagePanel.SafeCall(panel => panel.Refresh());
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
                repairPanel.SafeCall(panel => panel.gameObject.SetActive(true));
                salvagePanel.SafeCall(panel => panel.gameObject.SetActive(false));
            }
        }

        public virtual void ShowSalvageTab()
        {
            if (m_salvageTab)
                m_salvageTab.toggle.isOn = true;
            else
            {
                m_showingSalvage = true;
                repairPanel.SafeCall(panel => panel.gameObject.SetActive(false));
                salvagePanel.SafeCall(panel => panel.gameObject.SetActive(true));
                salvagePanel.SafeCall(panel => panel.Refresh());
            }
        }

        public virtual void Show(Blacksmith blacksmith)
        {
            base.Show();
            m_inventory = GUIWindowsManager.instance.GetInventory();
            m_inventory.GetComponent<GUIWindow>().SafeCall(w => w.Show());
            repairPanel.SafeCall(panel => panel.Bind(blacksmith));
            salvagePanel.SafeCall(panel => panel.Bind(blacksmith));
            repairPanel.SafeCall(panel => panel.Refresh());
            ShowRepairTab();
        }

        public virtual void Refresh()
        {
            if (!isOpen)
                return;

            repairPanel.SafeCall(panel => panel.Refresh());

            if (m_showingSalvage)
                salvagePanel.SafeCall(panel => panel.Refresh());
        }

        protected override void OnClose()
        {
            salvagePanel.SafeCall(panel => panel.ResetSelection());
            ShowRepairTab();

            if (!m_inventory)
                return;

            m_inventory.GetComponent<GUIWindow>().SafeCall(w => w.Hide());
        }

        protected override void Start()
        {
            base.Start();
            InitializeTabs();
        }
    }
}
