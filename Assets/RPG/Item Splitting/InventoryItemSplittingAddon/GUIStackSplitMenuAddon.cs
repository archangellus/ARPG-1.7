using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PLAYERTWO.ARPGProject
{
    /// <summary>
    /// Quantity menu used by GUIStackSplitFeature. It can use a custom prefab or generate a
    /// small default UI at runtime.
    /// </summary>
    [AddComponentMenu("PLAYER TWO/ARPG Project/GUI/Stack Split/GUI Stack Split Menu Addon")]
    public class GUIStackSplitMenuAddon : MonoBehaviour
    {
        [Header("Stack Split Controls")]
        public Button decreaseButton;
        public Button increaseButton;
        public Button confirmButton;
        public TMP_Text amountText;

        [Header("Optional Slider")]
        public Slider amountSlider;

        private ItemInstance m_source;
        private ItemInstance m_destination;
        private int m_movedAmount;
        private int m_minMovedAmount = 1;
        private int m_maxMovedAmount = 1;
        private bool m_callbacksInitialized;
        private bool m_updatingControls;

        public static GUIStackSplitMenuAddon Create(
            Transform parent,
            GUIStackSplitMenuAddon prefab = null
        )
        {
            if (prefab)
            {
                var instance = Instantiate(prefab, parent, false);
                instance.gameObject.SetActive(false);
                instance.InitializeCallbacks();
                return instance;
            }

            return CreateDefault(parent);
        }

        private static GUIStackSplitMenuAddon CreateDefault(Transform parent)
        {
            var root = new GameObject(
                "Stack Split Menu",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image)
            );
            root.transform.SetParent(parent, false);

            var rect = (RectTransform)root.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(260f, 120f);
            rect.anchoredPosition = Vector2.zero;

            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

            var menu = root.AddComponent<GUIStackSplitMenuAddon>();
            menu.BuildControls(rect);
            root.SetActive(false);
            return menu;
        }

        public void Show(
            ItemInstance source,
            ItemInstance destination,
            int movedAmount,
            int destinationInitialStack
        )
        {
            if (source == null || destination == null)
                return;

            m_source = source;
            m_destination = destination;
            m_movedAmount = Mathf.Max(1, movedAmount);

            var originalSourceStack = source.stack + m_movedAmount;
            var availableDestinationSpace = destination.data.stackCapacity - destinationInitialStack;
            m_maxMovedAmount = Mathf.Max(
                m_minMovedAmount,
                Mathf.Min(originalSourceStack - 1, availableDestinationSpace)
            );

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            UpdateControls();
        }

        public void Increase()
        {
            if (CanIncrease())
                SetMovedAmount(m_movedAmount + 1);
        }

        public void Decrease()
        {
            if (CanDecrease())
                SetMovedAmount(m_movedAmount - 1);
        }

        public void SetMovedAmount(int amount)
        {
            if (m_source == null || m_destination == null)
                return;

            var targetAmount = Mathf.Clamp(amount, m_minMovedAmount, m_maxMovedAmount);
            if (targetAmount == m_movedAmount)
                return;

            if (targetAmount > m_movedAmount)
            {
                var amountToMove = Mathf.Min(
                    targetAmount - m_movedAmount,
                    m_source.stack - 1,
                    m_destination.data.stackCapacity - m_destination.stack
                );

                if (amountToMove <= 0)
                    return;

                m_source.stack -= amountToMove;
                m_destination.stack += amountToMove;
                m_movedAmount += amountToMove;
            }
            else
            {
                var amountToReturn = Mathf.Min(
                    m_movedAmount - targetAmount,
                    m_movedAmount - m_minMovedAmount
                );

                if (amountToReturn <= 0)
                    return;

                m_source.stack += amountToReturn;
                m_destination.stack -= amountToReturn;
                m_movedAmount -= amountToReturn;
            }

            UpdateControls();
        }

        public void Confirm()
        {
            gameObject.SetActive(false);
            m_source = null;
            m_destination = null;
        }

        private bool CanIncrease()
        {
            return m_source != null
                && m_destination != null
                && m_movedAmount < m_maxMovedAmount
                && m_source.stack > 1
                && m_destination.stack < m_destination.data.stackCapacity;
        }

        private bool CanDecrease()
        {
            return m_source != null
                && m_destination != null
                && m_movedAmount > m_minMovedAmount;
        }

        private void UpdateControls()
        {
            if (amountText)
                amountText.text = m_movedAmount.ToString();

            if (amountSlider)
            {
                m_updatingControls = true;
                amountSlider.wholeNumbers = true;
                amountSlider.minValue = m_minMovedAmount;
                amountSlider.maxValue = m_maxMovedAmount;
                amountSlider.value = m_movedAmount;
                amountSlider.interactable = m_maxMovedAmount > m_minMovedAmount;
                m_updatingControls = false;
            }

            if (increaseButton)
                increaseButton.interactable = CanIncrease();

            if (decreaseButton)
                decreaseButton.interactable = CanDecrease();

            if (confirmButton)
                confirmButton.interactable = m_source != null && m_destination != null;
        }

        private void Awake() => InitializeCallbacks();

        private void InitializeCallbacks()
        {
            if (m_callbacksInitialized)
                return;

            if (decreaseButton)
                decreaseButton.onClick.AddListener(Decrease);

            if (increaseButton)
                increaseButton.onClick.AddListener(Increase);

            if (confirmButton)
                confirmButton.onClick.AddListener(Confirm);

            if (amountSlider)
                amountSlider.onValueChanged.AddListener(HandleSliderValueChanged);

            m_callbacksInitialized = decreaseButton || increaseButton || confirmButton || amountSlider;
        }

        private void HandleSliderValueChanged(float value)
        {
            if (!m_updatingControls)
                SetMovedAmount(Mathf.RoundToInt(value));
        }

        private void BuildControls(RectTransform root)
        {
            amountText = CreateText("Amount", root, new Vector2(0f, 25f), "1", 32f);
            decreaseButton = CreateButton("Decrease", root, new Vector2(-80f, -20f), "-");
            increaseButton = CreateButton("Increase", root, new Vector2(0f, -20f), "+");
            confirmButton = CreateButton("Confirm", root, new Vector2(90f, -20f), "Confirm");
            InitializeCallbacks();
        }

        private Button CreateButton(
            string name,
            RectTransform parent,
            Vector2 anchoredPosition,
            string label
        )
        {
            var buttonObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)
            );
            buttonObject.transform.SetParent(parent, false);

            var rect = (RectTransform)buttonObject.transform;
            rect.sizeDelta = label.Length > 2 ? new Vector2(86f, 36f) : new Vector2(64f, 36f);
            rect.anchoredPosition = anchoredPosition;

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.9f, 0.9f, 0.9f, 1f);

            var button = buttonObject.GetComponent<Button>();
            var colors = button.colors;
            colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.75f);
            button.colors = colors;

            CreateText("Label", rect, Vector2.zero, label, 22f);
            return button;
        }

        private TMP_Text CreateText(
            string name,
            RectTransform parent,
            Vector2 anchoredPosition,
            string text,
            float fontSize
        )
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            var rect = (RectTransform)textObject.transform;
            rect.sizeDelta = new Vector2(120f, 34f);
            rect.anchoredPosition = anchoredPosition;

            var tmp = textObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return tmp;
        }
    }
}
