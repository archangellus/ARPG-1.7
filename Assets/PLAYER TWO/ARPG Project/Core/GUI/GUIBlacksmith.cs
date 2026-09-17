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

        [Header("Audio Settings")]
        [Tooltip("The Audio Clip that plays when repairing an Item.")]
        public AudioClip repairAudio;

        [Tooltip("The Audio Clip that plays when removing sockets from an Item.")]
        public AudioClip removeSocketsAudio;

        protected Blacksmith m_blacksmith;
        protected GUIInventory m_inventory;

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
        }

        public virtual void Refresh()
        {
            if (!isOpen)
                return;

            UpdateRepairCost();
            UpdateRepairAllCost();
            UpdateRemoveSocketsCost();
            UpdateButtons();
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
            if (!m_inventory)
                return;

            m_inventory.GetComponent<GUIWindow>().SafeCall(w => w.Hide());
        }

        protected override void Start()
        {
            base.Start();
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
