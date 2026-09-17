using System;
using UnityEngine;

namespace PLAYERTWO.ARPGProject
{
    [CreateAssetMenu(
        fileName = "Salvage Material",
        menuName = "PLAYER TWO/ARPG Project/Salvage/Material"
    )]
    public class SalvageMaterialDefinition : ScriptableObject
    {
        [SerializeField]
        private string m_id;

        public string displayName;
        public Sprite icon;
        public string id => m_id;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(m_id))
            {
                m_id = Guid.NewGuid().ToString("N");
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
