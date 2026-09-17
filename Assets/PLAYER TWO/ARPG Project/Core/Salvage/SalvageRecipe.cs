using System;
using System.Collections.Generic;
using UnityEngine;

namespace PLAYERTWO.ARPGProject
{
    [Serializable]
    public class SalvageMaterialAmount
    {
        public SalvageMaterialDefinition material;

        [Min(1)]
        public int quantity = 1;
    }

    [CreateAssetMenu(
        fileName = "Salvage Recipe",
        menuName = "PLAYER TWO/ARPG Project/Salvage/Recipe"
    )]
    public class SalvageRecipe : ScriptableObject
    {
        [SerializeField]
        private string m_id;

        public List<SalvageMaterialAmount> rewards = new();
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
