using UnityEngine;
using UnityEngine.UI;

namespace PLAYERTWO.ARPGProject
{
    /// <summary>Displays one salvaged material's icon and quantity in the Salvage panel's results row.</summary>
    [AddComponentMenu("PLAYER TWO/ARPG Project/GUI/GUI Blacksmith Material Icon")]
    public class GUIBlacksmithMaterialIcon : MonoBehaviour
    {
        [Tooltip("The Image that displays the material Item's icon.")]
        public Image icon;

        [Tooltip("The Text that displays the granted quantity.")]
        public Text quantityText;

        public virtual void Initialize(Item material, long quantity)
        {
            if (icon)
            {
                icon.sprite = material.SafeGet(m => m.image);
                icon.enabled = icon.sprite != null;
            }

            if (quantityText)
                quantityText.text = quantity.ToString();
        }
    }
}
