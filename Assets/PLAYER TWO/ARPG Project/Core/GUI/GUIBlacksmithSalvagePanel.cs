using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PLAYERTWO.ARPGProject
{
    /// <summary>
    /// The Blacksmith window's Salvage tab: selection, preview, and commit. Bound to the active
    /// Blacksmith NPC context by <see cref="GUIBlacksmith"/>. Selection itself is driven by
    /// <see cref="GUIBlacksmithSalvageToggle"/> on the player's inventory item rows, which call
    /// back into this panel through <see cref="GUIBlacksmith"/>'s pass-through methods.
    /// </summary>
    [AddComponentMenu("PLAYER TWO/ARPG Project/GUI/GUI Blacksmith Salvage Panel")]
    public class GUIBlacksmithSalvagePanel : MonoBehaviour
    {
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

        protected Blacksmith m_blacksmith;
        protected readonly HashSet<string> m_selected = new();
        protected readonly SalvageService m_service = new();
        protected SalvagePreview m_preview;

        /// <summary>Binds the Blacksmith NPC context this panel operates against.</summary>
        public virtual void Bind(Blacksmith blacksmith) => m_blacksmith = blacksmith;

        public virtual bool IsSelected(ItemInstance item) =>
            item != null && m_selected.Contains(item.instanceId);

        public virtual bool SetSelected(ItemInstance item, bool selected)
        {
            if (!m_blacksmith || item == null || !m_blacksmith.interactingEntity)
                return false;

            var inventory = m_blacksmith.interactingEntity.inventory.instance;
            var eligibility = m_service.Evaluate(item, inventory, m_blacksmith.salvageSettings);

            if (!eligibility.eligible)
            {
                SetMessage(eligibility.reason);
                return false;
            }

            if (selected)
                m_selected.Add(item.instanceId);
            else
                m_selected.Remove(item.instanceId);

            Refresh();
            return true;
        }

        public virtual void SelectJunk() => SelectWhere(item => item.isJunk, true);

        public virtual void SelectRarity()
        {
            if (!salvageRarityDropdown)
                return;

            SelectWhere(item => item.rarityId == salvageRarityDropdown.value - 1, true);
        }

        public virtual void SelectAllEligible() =>
            SelectWhere(
                item => !m_blacksmith.salvageSettings.highValueRarityIds.Contains(item.rarityId),
                false
            );

        public virtual void ClearSelection()
        {
            m_selected.Clear();
            Refresh();
        }

        /// <summary>Invalidates any pending preview and clears the selection without refreshing
        /// the (possibly hidden) UI. Used when the Blacksmith window closes.</summary>
        public virtual void ResetSelection()
        {
            m_service.InvalidateAll();
            m_preview = null;
            m_selected.Clear();
        }

        protected virtual void SelectWhere(
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
                if (m_service.Evaluate(item, inventory, m_blacksmith.salvageSettings).eligible)
                    m_selected.Add(item.instanceId);
            }

            Refresh();
        }

        protected virtual void OnConfirmClicked()
        {
            if (m_preview == null)
                return;

            if (m_preview.requiresHighValueConfirmation)
            {
                UIConfirmationScreen.instance.Show(
                    "Salvage the selected high-value equipment? This cannot be undone.",
                    () => Commit(true)
                );
                return;
            }

            Commit(false);
        }

        protected virtual void Commit(bool highValueConfirmed)
        {
            var owner = m_blacksmith.SafeGet(b => b.interactingEntity);

            if (!m_blacksmith || !m_blacksmith.IsSalvageContextValid(owner))
            {
                SetMessage("The Blacksmith is no longer available.");
                return;
            }

            if (
                m_service.TryCommit(
                    m_preview,
                    owner,
                    m_blacksmith.salvageSettings,
                    m_blacksmith.salvageProviderId,
                    highValueConfirmed,
                    out var receipt,
                    out var error
                )
            )
            {
                m_selected.Clear();
                SetMessage(receipt.summary);
                Refresh();
                return;
            }

            SetMessage(error);
        }

        /// <summary>Recomputes the preview from the current selection and refreshes every field.</summary>
        public virtual void Refresh()
        {
            if (!m_blacksmith || !m_blacksmith.interactingEntity)
                return;

            var inventory = m_blacksmith.interactingEntity.inventory.instance;
            var carriedIds = new HashSet<string>(
                inventory.items.Keys.Select(item => item.instanceId)
            );
            m_selected.RemoveWhere(id => !carriedIds.Contains(id));
            m_service.InvalidateAll();
            m_preview = null;

            if (
                m_selected.Count > 0
                && !m_service.TryCreatePreview(
                    m_selected,
                    m_blacksmith.interactingEntity,
                    m_blacksmith.salvageSettings,
                    m_blacksmith.salvageProviderId,
                    out m_preview,
                    out var error
                )
            )
                SetMessage(error);

            if (salvageSelectedCountText)
                salvageSelectedCountText.text = $"Selected: {m_selected.Count}";
            if (salvageRewardsText)
                salvageRewardsText.text =
                    m_preview == null
                        ? "No rewards"
                        : string.Join(
                            "\n",
                            m_preview.materials.Select(value => $"{value.material.name}: {value.quantity}")
                        );
            if (salvageReturnedSocketablesText)
                salvageReturnedSocketablesText.text =
                    m_preview == null || m_preview.returnedSocketables.Count == 0
                        ? "No socketables returned"
                        : string.Join(
                            "\n",
                            m_preview.returnedSocketables.Select(item => item.data.name)
                        );
            if (salvageConfirmButton)
                salvageConfirmButton.interactable =
                    m_preview != null && m_blacksmith.IsSalvageContextValid(m_blacksmith.interactingEntity);
        }

        protected virtual void SetMessage(string message)
        {
            if (salvageMessageText)
                salvageMessageText.text = message;
        }

        protected virtual void Start()
        {
            salvageConfirmButton?.onClick.AddListener(OnConfirmClicked);
            salvageSelectJunkButton?.onClick.AddListener(SelectJunk);
            salvageSelectRarityButton?.onClick.AddListener(SelectRarity);
            salvageSelectAllEligibleButton?.onClick.AddListener(SelectAllEligible);
            salvageClearButton?.onClick.AddListener(ClearSelection);
        }
    }
}
