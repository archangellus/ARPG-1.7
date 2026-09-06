using UnityEngine;
using UnityEngine.UI;

namespace PLAYERTWO.ARPGProject
{
    [RequireComponent(typeof(Button))]
    [AddComponentMenu("PLAYER TWO/ARPG Project/GUI/GUI Open Town Portal Button")]
    public class GUIOpenTownPortalButton : MonoBehaviour
    {
        /// <summary>
        /// The Button component of this GUI Open Town Portal Button.
        /// </summary>
        public Button button { get; protected set; }

        protected virtual void InitializeButton()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnClick);
        }

        protected virtual void OnClick()
        {
            var opener = Level.instance.player.GetComponent<EntityPortalOpener>();

            if (opener)
                opener.Open();
        }

        protected virtual void Awake() => InitializeButton();
    }
}
