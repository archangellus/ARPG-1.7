using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PLAYERTWO.ARPGProject
{
    [AddComponentMenu("PLAYER TWO/ARPG Project/UI/UI Confirmation Screen")]
    public class UIConfirmationScreen : Singleton<UIConfirmationScreen>
    {
        [Tooltip(
            "The RectTransform containing all of this screen's UI elements. Disabled by "
                + "default; toggled to show/hide the screen while this component stays enabled."
        )]
        public RectTransform container;

        [Tooltip("A reference to the Text component that represents the confirmation message.")]
        public TMP_Text message;

        [Tooltip("The reference to the 'confirm' Button.")]
        public Button confirmButton;

        [Tooltip("The reference to the 'cancel' Button.")]
        public Button cancelButton;

        protected System.Action m_onConfirm;

        protected virtual void InitializeCallbacks()
        {
            confirmButton.onClick.AddListener(HandleConfirm);
            cancelButton.onClick.AddListener(HandleCancel);
        }

        protected virtual void HandleConfirm()
        {
            var callback = m_onConfirm;
            m_onConfirm = null;
            Hide();
            callback?.Invoke();
        }

        protected virtual void HandleCancel()
        {
            m_onConfirm = null;
            Hide();
        }

        /// <summary>
        /// Shows the confirmation screen with a given message, invoking a callback only if the
        /// player clicks the confirm button.
        /// </summary>
        /// <param name="text">The message displayed to the player.</param>
        /// <param name="onConfirm">Invoked if the player confirms.</param>
        public virtual void Show(string text, System.Action onConfirm)
        {
            message.text = text;
            m_onConfirm = onConfirm;
            container.gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        /// <summary>
        /// Hides the confirmation screen.
        /// </summary>
        public virtual void Hide() => container.gameObject.SetActive(false);

        protected override void Initialize() => InitializeCallbacks();
    }
}
