using UnityEngine;
using UnityEngine.UI;

namespace PLAYERTWO.ARPGProject
{
    /// <summary>
    /// The Blacksmith window's Repair tab: item repair, repair all, and socket removal. Bound to
    /// the active Blacksmith NPC context by <see cref="GUIBlacksmith"/>.
    /// </summary>
    [AddComponentMenu("PLAYER TWO/ARPG Project/GUI/GUI Blacksmith Repair Panel")]
    public class GUIBlacksmithRepairPanel : MonoBehaviour
    {
        [Header("Repair Settings")]
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

        protected GameAudio m_audio => GameAudio.instance;

        /// <summary>Binds the Blacksmith NPC context this panel operates against.</summary>
        public virtual void Bind(Blacksmith blacksmith) => m_blacksmith = blacksmith;

        /// <summary>Refreshes repair/remove-sockets costs and button interactability.</summary>
        public virtual void Refresh()
        {
            UpdateRepairCost();
            UpdateRepairAllCost();
            UpdateRemoveSocketsCost();
            UpdateButtons();
        }

        protected virtual void InitializeCallbacks()
        {
            repairButton.onClick.AddListener(OnRepairClicked);
            repairAllButton.onClick.AddListener(OnRepairAllClicked);
            removeSocketsButton.onClick.AddListener(OnRemoveSocketsClicked);
            slot.onEquip.AddListener(OnEquip);
            slot.onUnequip.AddListener(OnUnequip);
        }

        protected virtual void UpdateButtons()
        {
            repairButton.interactable =
                m_blacksmith.SafeGet(b => b.GetPriceToRepair(slot.item.SafeGet(i => i.item))) > 0;
            removeSocketsButton.interactable =
                m_blacksmith.SafeGet(b => b.GetPriceToRemoveSockets(slot.item.SafeGet(i => i.item))) > 0;
            repairAllButton.interactable = m_blacksmith.SafeGet(b => b.GetPriceToRepairAll()) > 0;
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

        protected virtual void UpdateRepairCost() =>
            repairCostText.text = m_blacksmith
                .SafeGet(b => b.GetPriceToRepair(slot.item.SafeGet(i => i.item)))
                .ToMoneyString();

        protected virtual void ClearRepairCost() => repairCostText.text = "0";

        protected virtual void UpdateRepairAllCost() =>
            repairAllCostText.text = m_blacksmith.SafeGet(b => b.GetPriceToRepairAll()).ToMoneyString();

        protected virtual void UpdateRemoveSocketsCost() =>
            removeSocketsCostText.text = m_blacksmith
                .SafeGet(b => b.GetPriceToRemoveSockets(slot.item.SafeGet(i => i.item)))
                .ToMoneyString();

        protected virtual void ClearRemoveSocketsCost() => removeSocketsCostText.text = "0";

        protected virtual void Start()
        {
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
