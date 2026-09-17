using UnityEngine;
using UnityEngine.UI;

namespace PLAYERTWO.ARPGProject
{
    /// <summary>Bridges an Inventory Item row toggle to the open Blacksmith Salvage tab.</summary>
    [RequireComponent(typeof(GUIItem))]
    [AddComponentMenu("PLAYER TWO/ARPG Project/GUI/GUI Blacksmith Salvage Toggle")]
    public class GUIBlacksmithSalvageToggle : MonoBehaviour
    {
        [Tooltip("Selection toggle child displayed while the Blacksmith Salvage tab is active.")]
        public Toggle toggle;

        protected GUIItem m_item;
        protected GUIBlacksmith m_blacksmith;

        protected virtual void Awake()
        {
            m_item = GetComponent<GUIItem>();
            toggle?.onValueChanged.AddListener(OnValueChanged);
        }

        protected virtual void Update()
        {
            if (!m_blacksmith && GUIWindowsManager.instance)
                m_blacksmith = GUIWindowsManager.instance.blacksmith;

            var visible = m_blacksmith && m_blacksmith.isShowingSalvage;
            if (toggle && toggle.gameObject.activeSelf != visible)
                toggle.gameObject.SetActive(visible);

            if (visible && m_item && m_item.item != null)
                toggle.SetIsOnWithoutNotify(m_blacksmith.IsSalvageSelected(m_item.item));
        }

        protected virtual void OnValueChanged(bool selected)
        {
            if (!m_blacksmith || !m_item || m_item.item == null)
                return;

            if (!m_blacksmith.SetSalvageSelected(m_item.item, selected) && selected)
                toggle.SetIsOnWithoutNotify(false);
        }

        protected virtual void OnDisable()
        {
            if (toggle)
                toggle.SetIsOnWithoutNotify(false);
        }
    }
}
