using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace PLAYERTWO.ARPGProject
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    [AddComponentMenu("PLAYER TWO/ARPG Project/GUI/GUI Item Inspector")]
    public class GUIItemInspector : GUIInspector
    {
        [Header("Containers")]
        [Tooltip("References the parent of the general attributes text.")]
        public GameObject attributesContainer;

        [Tooltip("References the parent of the additional attributes text.")]
        public GameObject additionalAttributesContainer;

        [Tooltip("References the parent of the sockets text.")]
        public GameObject socketsContainer;

        [Tooltip("References the parent of a Socketable's own modifiers text.")]
        public GameObject socketableModifiersContainer;

        [Tooltip("References the parent of the potion description text.")]
        public GameObject potionDescriptionContainer;

        [Tooltip(
            "References an indicator (e.g. a text or an icon) shown when this inspector is used "
                + "as a comparison inspector. Optional."
        )]
        public GameObject equippedIndicator;

        [Header("Texts")]
        [Tooltip("A reference to the Text component that represents the Item's price.")]
        public Text itemPriceText;

        [Tooltip("A reference to the Text component that represents the Item's name.")]
        public Text itemName;

        [Tooltip(
            "A reference to the Text component that represents the Item's potion description."
        )]
        public Text potionDescription;

        [Tooltip("References the Text component displaying the Item's general attributes.")]
        public Text attributesText;

        [Tooltip("References the Text component displaying the Item's additional attributes.")]
        public Text additionalAttributesText;

        [Tooltip("References the Text component displaying the Item's socket slots.")]
        public Text socketsText;

        [Tooltip("References the Text component displaying a Socketable's own modifiers.")]
        public Text socketableModifiersText;

        [FormerlySerializedAs("skillInstructionText")]
        [Tooltip("References the Text component displaying the item's usage instruction.")]
        public Text instructionText;

        [Header("Color Settings")]
        [Tooltip("Regular text colors.")]
        public Color regularColor = new(1, 1, 1, 1);

        [Tooltip("Invalid text colors.")]
        public Color invalidColor = new(1, 0, 0, 1);

        [Tooltip("Attention text colors.")]
        public Color attentionColor = new(1, 1, 0, 1);

        [Tooltip("Special text colors.")]
        public Color specialColor = GameColors.LightBlue;

        [Tooltip("The color of an empty socket's text.")]
        public Color emptySocketColor = GameColors.HalfBack;

        [Tooltip("The color used for favorable comparison differences.")]
        public Color favorableComparisonColor = new(0.25f, 1f, 0.35f, 1f);

        [Tooltip("The color used for unfavorable comparison differences and lost properties.")]
        public Color unfavorableComparisonColor = new(1f, 0.3f, 0.25f, 1f);

        [Header("Comparison Settings")]
        [Tooltip(
            "The paired inspector shown alongside this one when the inspected item has an "
                + "equipped counterpart. Only assigned on the primary inspector."
        )]
        public GUIItemInspector comparisonInspector;

        [Tooltip(
            "A second paired inspector, shown beside the comparison inspector when the "
                + "inspected item has a second equipped counterpart (e.g. a Ring, when both "
                + "Ring slots are occupied). Only assigned on the primary inspector."
        )]
        public GUIItemInspector secondaryComparisonInspector;

        [Tooltip(
            "If false, this inspector won't follow the pointer on its own. Set to false on the "
                + "comparison instances, which are instead positioned by their paired primary "
                + "inspector via SetPositionBeside."
        )]
        public bool independentPositioning = true;

        [Tooltip(
            "The distance to offset this inspector away from the primary inspector it's shown "
                + "beside via SetPositionBeside, in the direction of whichever side it's placed "
                + "on. Only relevant on the comparison instance."
        )]
        public float comparisonInspectorMargin = 2f;

        [Header("Comparison Input")]
        [Tooltip(
            "An optional Input System action that toggles equipped item comparisons. If no "
                + "action is assigned, the Alt key is used. Only relevant on the primary inspector."
        )]
        public InputActionReference comparisonToggleAction;

        protected CanvasGroup m_group;
        protected ItemInstance m_item;
        protected bool m_onMerchant;
        protected System.Action m_updateAllHandler;
        protected InputAction m_defaultComparisonToggleAction;
        protected bool m_comparisonActionAutoEnabled;
        protected bool m_showComparison;
        protected ItemInstance m_comparisonReference;

        /// <summary>
        /// Returns the player Entity. Resolved lazily rather than cached in Start, since a
        /// comparison inspector instantiated at runtime may be shown for the first time before
        /// its own Start has had a chance to run.
        /// </summary>
        protected Entity entity => Level.instance.player;

        /// <summary>
        /// Returns a cached delegate for UpdateAll, so subscribing/unsubscribing from
        /// ItemInstance.onChanged on every Show/Hide doesn't allocate a new delegate each time.
        /// </summary>
        protected System.Action updateAllHandler => m_updateAllHandler ??= UpdateAll;

        protected virtual void InitializeCanvasGroup()
        {
            m_group = GetComponent<CanvasGroup>();
            m_group.blocksRaycasts = false;
        }

        protected virtual InputAction comparisonAction =>
            comparisonToggleAction != null
                ? comparisonToggleAction.action
                : m_defaultComparisonToggleAction;

        protected override void Awake()
        {
            base.Awake();

            if (independentPositioning && comparisonToggleAction == null)
            {
                m_defaultComparisonToggleAction = new InputAction(
                    "Toggle Item Compare",
                    InputActionType.Button,
                    "<Keyboard>/alt"
                );
            }
        }

        protected virtual void OnEnable()
        {
            if (!independentPositioning || comparisonAction == null || comparisonAction.enabled)
                return;

            comparisonAction.Enable();
            m_comparisonActionAutoEnabled = true;
        }

        protected virtual void OnDisable()
        {
            if (m_comparisonActionAutoEnabled && comparisonAction != null)
                comparisonAction.Disable();

            m_comparisonActionAutoEnabled = false;
        }

        protected virtual void OnDestroy()
        {
            m_defaultComparisonToggleAction?.Dispose();
        }

        protected virtual void Update()
        {
            if (
                independentPositioning
                && comparisonAction != null
                && comparisonAction.WasPressedThisFrame()
            )
            {
                m_showComparison = !m_showComparison;
                UpdateComparison();
            }
        }

        /// <summary>
        /// Shows the inspector with information from a given Item Instance.
        /// </summary>
        /// <param name="item">The item you want to inspect.</param>
        /// <param name="onMerchant">If true, shows this item's Buy price instead of its Sell price.</param>
        public virtual void Show(ItemInstance item, bool onMerchant = false)
        {
            if (item == null || gameObject.activeSelf)
                return;

            m_item = item;
            m_onMerchant = onMerchant;
            m_item.onChanged += updateAllHandler;
            gameObject.SetActive(true);
            m_rect.SetAsLastSibling();
            UpdateAll();
            FadIn();
            if (m_showComparison)
                UpdateComparison();
        }

        /// <summary>
        /// Shows the inspector with information from a given GUI Item.
        /// </summary>
        /// <param name="item">The item you want to inspect.</param>
        public virtual void Show(GUIItem item)
        {
            if (item != null)
                Show(item.item, item.onMerchant);
        }

        /// <summary>
        /// Hides the inspector.
        /// </summary>
        public virtual void Hide()
        {
            if (m_item != null)
                m_item.onChanged -= updateAllHandler;

            SetComparisonReference(null);

            gameObject.SetActive(false);

            if (comparisonInspector != null)
                comparisonInspector.Hide();

            if (secondaryComparisonInspector != null)
                secondaryComparisonInspector.Hide();
        }

        /// <summary>
        /// Shows or hides the paired comparison inspectors based on whether the currently
        /// inspected item has one or two equipped counterparts in its equipment slot(s).
        /// </summary>
        protected virtual void UpdateComparison()
        {
            if (comparisonInspector == null)
                return;

            if (!m_showComparison)
            {
                SetComparisonReference(null);
                comparisonInspector.Hide();

                if (secondaryComparisonInspector != null)
                    secondaryComparisonInspector.Hide();

                return;
            }

            var counterpart = m_item.GetEquippedCounterpart(entity.items);
            SetComparisonReference(counterpart);

            if (counterpart != null)
            {
                comparisonInspector.Show(counterpart);
                comparisonInspector.SetPositionBeside(m_rect);
            }
            else
            {
                comparisonInspector.Hide();
            }

            UpdateSecondaryComparison();
        }

        /// <summary>
        /// Shows or hides the secondary comparison inspector based on whether the currently
        /// inspected item has a second equipped counterpart (e.g. a Ring with both Ring slots
        /// occupied).
        /// </summary>
        protected virtual void UpdateSecondaryComparison()
        {
            if (secondaryComparisonInspector == null)
                return;

            var secondary = m_item.GetSecondaryEquippedCounterpart(entity.items);

            if (secondary != null && comparisonInspector.gameObject.activeSelf)
            {
                secondaryComparisonInspector.Show(secondary);
                secondaryComparisonInspector.SetPositionBeside(comparisonInspector.m_rect);
            }
            else
            {
                secondaryComparisonInspector.Hide();
            }
        }

        /// <summary>
        /// Positions this inspector beside a given Rect Transform, matching whichever side of
        /// the pointer it is itself being shown on: if the given Rect Transform is shown to the
        /// right of the pointer, this inspector is placed further to its right; if it's shown to
        /// the left of the pointer, this inspector is placed further to its left.
        /// </summary>
        /// <param name="primary">The Rect Transform to position this inspector beside.</param>
        public virtual void SetPositionBeside(RectTransform primary)
        {
            primary.GetWorldCorners(temp_corners);

            var pivotX = primary.pivot.x;

            m_rect.pivot = new Vector2(pivotX, primary.pivot.y);

            var x =
                pivotX == 0
                    ? temp_corners[2].x + comparisonInspectorMargin
                    : temp_corners[0].x - comparisonInspectorMargin;
            var y = primary.pivot.y == 1 ? temp_corners[1].y : temp_corners[0].y;

            m_rect.position = new Vector2(x, y);
        }

        /// <inheritdoc/>
        public override void SetPositionRelativeTo(RectTransform other)
        {
            base.SetPositionRelativeTo(other);
            RepositionComparison();
        }

        /// <inheritdoc/>
        protected override bool HasRoomBelow(Vector2 position)
        {
            var height = m_rect.sizeDelta.y;

            if (comparisonInspector != null && comparisonInspector.gameObject.activeSelf)
                height = Mathf.Max(height, comparisonInspector.m_rect.sizeDelta.y);

            if (
                secondaryComparisonInspector != null
                && secondaryComparisonInspector.gameObject.activeSelf
            )
                height = Mathf.Max(height, secondaryComparisonInspector.m_rect.sizeDelta.y);

            var canvasYScale = canvas.transform.localScale.y;
            return position.y - height * canvasYScale > 0;
        }

        /// <inheritdoc/>
        protected override void UpdatePivot()
        {
            if (independentPositioning)
                base.UpdatePivot();
        }

        /// <inheritdoc/>
        protected override void UpdatePosition()
        {
            if (independentPositioning)
                base.UpdatePosition();

            RepositionComparison();
        }

        protected virtual void RepositionComparison()
        {
            if (comparisonInspector == null || !comparisonInspector.gameObject.activeSelf)
                return;

            comparisonInspector.SetPositionBeside(m_rect);

            if (
                secondaryComparisonInspector != null
                && secondaryComparisonInspector.gameObject.activeSelf
            )
                secondaryComparisonInspector.SetPositionBeside(comparisonInspector.m_rect);
        }

        protected virtual void UpdateAll()
        {
            UpdateEquippedIndicator();
            UpdatePriceText();
            UpdateItemName();
            UpdatePotionDescription();
            UpdateAttributes();
            UpdateAdditionalAttributes();
            UpdateSockets();
            UpdateSocketableModifiers();
            UpdateInstruction();
        }

        protected virtual void SetComparisonReference(ItemInstance reference)
        {
            if (m_comparisonReference == reference)
                return;

            if (m_comparisonReference != null)
                m_comparisonReference.onChanged -= updateAllHandler;

            m_comparisonReference = reference;

            if (m_comparisonReference != null)
                m_comparisonReference.onChanged += updateAllHandler;

            if (m_item != null && gameObject.activeSelf)
            {
                UpdateAttributes();
                UpdateAdditionalAttributes();
                UpdateSockets();
            }
        }

        protected virtual void UpdateEquippedIndicator()
        {
            if (equippedIndicator == null)
                return;

            equippedIndicator.SetActive(!independentPositioning);
        }

        protected virtual void UpdatePriceText()
        {
            itemPriceText.gameObject.SetActive(GUIWindowsManager.instance.merchantWindow.isOpen);

            if (itemPriceText.gameObject.activeSelf)
            {
                var buying = m_onMerchant;
                var price = buying ? m_item.GetPrice() : m_item.GetSellPrice();
                var prefix = buying ? "Buy" : "Sell";
                itemPriceText.text = $"{prefix}:  {price.ToMoneyString()}";
            }
        }

        protected virtual void UpdateItemName()
        {
            itemName.text = m_item.GetTooltipTitle();

            if (m_item.IsSkill())
                itemName.color = specialColor;
            else
                itemName.color = m_item.GetRarityColor(regularColor);
        }

        protected virtual void UpdateAttributes()
        {
            attributesContainer.SetActive(true);

            if (attributesContainer.activeSelf)
            {
                attributesText.text = m_item.InspectPower(
                    m_comparisonReference,
                    favorableComparisonColor,
                    unfavorableComparisonColor
                );
                var ordinaryAttributes = m_item.Inspect(
                    entity.stats,
                    attentionColor,
                    invalidColor,
                    specialColor
                );

                if (!string.IsNullOrEmpty(ordinaryAttributes))
                    attributesText.text += "\n" + ordinaryAttributes;

                var differences = m_item.InspectBaseDifferences(
                    m_comparisonReference,
                    favorableComparisonColor,
                    unfavorableComparisonColor
                );

                if (!string.IsNullOrEmpty(differences))
                    attributesText.text += "\n\nComparison\n" + differences;
            }
        }

        protected virtual void UpdatePotionDescription()
        {
            potionDescriptionContainer.SetActive(m_item.IsPotion());

            if (potionDescriptionContainer.activeSelf)
            {
                potionDescription.text = "";

                if (m_item.GetPotion().healthAmount > 0)
                    potionDescription.text +=
                        $"Increases Health Points by {m_item.GetPotion().healthAmount}.";

                if (m_item.GetPotion().manaAmount > 0)
                {
                    if (potionDescription.text.Length > 0)
                        potionDescription.text += "\n";

                    potionDescription.text +=
                        $"Increases Mana Points by {m_item.GetPotion().manaAmount}.";
                }
            }
        }

        protected virtual void UpdateAdditionalAttributes()
        {
            var socketsAttributes = m_item.GetSocketsAttributes();
            var referenceSocketsAttributes = m_comparisonReference?.GetSocketsAttributes();
            var candidateAttributes = m_item.attributes ?? new ItemAttributes();
            var referenceAttributes =
                m_comparisonReference != null
                    ? m_comparisonReference.attributes ?? new ItemAttributes()
                    : null;
            var text =
                candidateAttributes.InspectComparison(
                    referenceAttributes,
                    socketsAttributes,
                    referenceSocketsAttributes,
                    favorableComparisonColor,
                    unfavorableComparisonColor
                );

            if (text == null || text.Length == 0)
            {
                additionalAttributesContainer.SetActive(false);
                return;
            }

            additionalAttributesContainer.SetActive(true);
            additionalAttributesText.text = text;
        }

        protected virtual void UpdateSockets()
        {
            var hasSockets = m_item.sockets != null && m_item.sockets.Length > 0;
            var referenceHasSockets =
                m_comparisonReference?.sockets != null
                && m_comparisonReference.sockets.Length > 0;

            socketsContainer.SetActive(hasSockets || referenceHasSockets);

            if (!socketsContainer.activeSelf)
                return;

            socketsText.text = hasSockets ? m_item.InspectSockets(emptySocketColor) : "No sockets";

            var differences = m_item.InspectSocketDifferences(
                m_comparisonReference,
                favorableComparisonColor,
                unfavorableComparisonColor
            );

            if (!string.IsNullOrEmpty(differences))
                socketsText.text += "\n\nSocket comparison\n" + differences;
        }

        protected virtual void UpdateSocketableModifiers()
        {
            var isSocketable = m_item.IsSocketable();

            socketableModifiersContainer.SetActive(isSocketable);

            if (isSocketable)
                socketableModifiersText.text = m_item.InspectSocketableModifiers();
        }

        protected virtual void UpdateInstruction()
        {
            var showInstruction = !m_onMerchant && (m_item.IsSkill() || m_item.IsConsumable());

            SetParentActive(instructionText, showInstruction);

            if (!showInstruction)
                return;

#if UNITY_STANDALONE || UNITY_WEBGL
            instructionText.text = m_item.IsSkill()
                ? m_item.GetSkill().pcInstruction
                : m_item.GetConsumable().pcInstruction;
#else
            instructionText.text = m_item.IsSkill()
                ? m_item.GetSkill().mobileInstruction
                : m_item.GetConsumable().mobileInstruction;
#endif
        }

        protected virtual void SetParentActive(Text element, bool value)
        {
            if (element == null || element.transform.parent == null)
                return;

            element.transform.parent.gameObject.SetActive(value);
        }

        protected virtual void Start()
        {
            InitializeCanvasGroup();
        }
    }
}
