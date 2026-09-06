using UnityEngine;
using UnityEngine.UI;

namespace PLAYERTWO.ARPGProject
{
    [AddComponentMenu("PLAYER TWO/ARPG Project/GUI/GUI Socket Slot")]
    public class GUISocketSlot : MonoBehaviour
    {
        [Tooltip(
            "The Image component that displays the socketed item's sprite. Disabled while the "
                + "slot is empty, leaving the rest of the prefab (e.g. an empty socket frame) visible."
        )]
        public Image image;

        /// <summary>
        /// Sets the sprite shown for this socket slot. Pass null to show it as empty.
        /// </summary>
        /// <param name="sprite">The sprite of the socketed item, or null if the slot is empty.</param>
        public virtual void SetIcon(Sprite sprite)
        {
            image.sprite = sprite;
            image.enabled = sprite != null;
        }
    }
}
