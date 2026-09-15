using System.Collections.Generic;
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

        [Header("Images")]
        [Tooltip(
            "References the UI Image displaying the inspected item's inventory sprite. "
                + "Hidden when the item has no image."
        )]
        public Image hoveredItemImage;

        [Tooltip("The image shown for each empty socket slot.")]
        public Sprite emptySocketSprite;

        [Tooltip("The width and height of each empty socket image.")]
        public float emptySocketImageSize = 24f;

        [Header("Price Labels")]
        [Tooltip("References the Buy Value label shown only when buying from the merchant.")]
        public GameObject buyValue;

        [Tooltip("References the Sell Value label shown when not buying, including when the merchant is closed.")]
        public GameObject sellValue;

        [Header("Texts")]
        [FormerlySerializedAs("itemPriceText")]
        [Tooltip("References the Text component displaying the buy price. Only shown when buying.")]
        public Text buyValueText;

        [Tooltip("References the Text component displaying the sell price when not buying, including when the merchant is closed.")]
        public Text sellValueText;

        [Tooltip("A reference to the Text component that represents the Item's name.")]
        public Text itemName;

        [Tooltip(
    "A reference to the Text component displaying the Item's rarity and type (for example, Rune Amulet). "
        + "If omitted, one is created beside the Item Name at runtime for backwards compatibility."
)]
        public Text rarityAndTypeText;

        [Tooltip(
            "A reference to the Text component that represents the Item's potion description."
        )]
        public Text potionDescription;

        [Tooltip("References the Text component displaying the Item's general attributes.")]
        public Text attributesText;

        [Tooltip(
            "References the Text component displaying all item requirements. "
                + "Hidden when there are no requirements to display."
        )]
        public Text requirementsText;

        [Tooltip(
            "Optional Text component displaying Item Power only when greater than 1. Assign a separate Text component "
                + "to style this line independently; when omitted, it remains in "
                + "attributesText for backwards compatibility."
        )]
        public Text itemPowerText;

        [Tooltip(
            "Optional Text component displaying Armor. Assign a separate Text component to "
                + "style this line independently; when omitted, it remains in attributesText "
                + "for backwards compatibility."
        )]
        public Text armorText;

        [Tooltip(
            "Optional Text component displaying the base-property comparison block. Assign a "
                + "separate Text component to style this block independently; when omitted, it "
                + "remains in attributesText for backwards compatibility."
        )]
        public Text baseComparisonText;

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

        [Header("Requirements Colors")]
        [Tooltip("The color used for requirements that the player meets.")]
        public Color requirementsStandardColor = new(1, 1, 1, 1);

        [Tooltip("The color used for requirements that the player does not meet.")]
        public Color requirementsErrorColor = new(1, 0, 0, 1);

        [Header("Comparison Settings")]
        [Tooltip("Heading displayed above base-property differences.")]
        public string baseComparisonHeading = "Comparison";

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
        protected Text m_socketsHeading;
        protected RectTransform m_emptySocketsContainer;
        protected RectTransform m_occupiedSocketsContainer;
        protected readonly List<GameObject> m_emptySocketImages = new();
        protected readonly List<GameObject> m_occupiedSocketRows = new();
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

            if (hoveredItemImage != null)
            {
                hoveredItemImage.sprite = null;
                hoveredItemImage.gameObject.SetActive(false);
            }
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
            UpdateHoveredItemImage();
            UpdatePriceText();
            UpdateItemName();
            UpdateRarityAndTypeText();
            UpdatePotionDescription();
            UpdateAttributes();
            UpdateRequirementsText();
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
            var windows = GUIWindowsManager.instance;
            var showBuyValue =
                m_item != null
                && m_onMerchant
                && windows != null
                && windows.merchantWindow != null
                && windows.merchantWindow.isOpen;
            var showSellValue = m_item != null && !showBuyValue;

            if (buyValue != null)
                buyValue.SetActive(showBuyValue);

            if (sellValue != null)
                sellValue.SetActive(showSellValue);

            if (buyValueText != null)
            {
                buyValueText.gameObject.SetActive(showBuyValue);

                if (showBuyValue)
                    buyValueText.text = m_item.GetPrice().ToMoneyString();
            }

            if (sellValueText != null)
            {
                sellValueText.gameObject.SetActive(showSellValue);

                if (showSellValue)
                    sellValueText.text = m_item.GetSellPrice().ToMoneyString();
            }
        }

        protected virtual void UpdateHoveredItemImage()
        {
            if (hoveredItemImage == null)
                return;

            var sprite = m_item != null && m_item.data != null ? m_item.data.image : null;

            hoveredItemImage.sprite = sprite;
            hoveredItemImage.preserveAspect = true;
            hoveredItemImage.raycastTarget = false;
            hoveredItemImage.gameObject.SetActive(sprite != null);
        }

        protected virtual void UpdateItemName()
        {
            itemName.text = m_item.GetTooltipTitle();

            if (m_item.IsSkill())
                itemName.color = specialColor;
            else
                itemName.color = m_item.GetRarityColor(regularColor);
        }

        /// <summary>
        /// Updates the rarity/type classification line. Existing inspector prefabs that have
        /// not assigned the new field get a copy of the name label, preserving their styling
        /// while ensuring the field appears for every item.
        /// </summary>
        protected virtual void UpdateRarityAndTypeText()
        {
            if (rarityAndTypeText == null && itemName != null)
            {
                rarityAndTypeText = Instantiate(itemName, itemName.transform.parent);
                rarityAndTypeText.name = "Rarity And Type";
                rarityAndTypeText.transform.SetSiblingIndex(
                    itemName.transform.GetSiblingIndex() + 1
                );
            }

            if (rarityAndTypeText == null)
                return;

            rarityAndTypeText.text = m_item.GetRarityAndTypeDisplayName();
            rarityAndTypeText.color = m_item.GetRarityColor(regularColor);
        }
        protected virtual void UpdateRequirementsText()
        {
            if (requirementsText == null)
                return;

            requirementsText.color = requirementsStandardColor;
            requirementsText.supportRichText = true;

            SetTextActive(
                requirementsText,
                m_item.InspectRequirements(entity.stats, requirementsErrorColor)
            );
        }

        protected virtual void UpdateAttributes()
        {
            attributesContainer.SetActive(true);

            if (attributesContainer.activeSelf)
            {
                var power =
                    m_item.GetItemPower() <= 1
                        ? string.Empty
                        : m_item.InspectItemPower(
                            m_comparisonReference,
                            favorableComparisonColor,
                            unfavorableComparisonColor
                        );
                var armor = m_item.InspectArmor(
                    m_comparisonReference,
                    favorableComparisonColor,
                    unfavorableComparisonColor
                );
                var ordinaryAttributes = m_item.Inspect(
                    entity.stats,
                    attentionColor,
                    invalidColor,
                    specialColor,
                    includeRequirements: false
                );

                attributesText.text = string.Empty;

                if (itemPowerText != null)
                {
                    SetTextActive(itemPowerText, power);
                }
                else
                {
                    attributesText.text = power;
                }

                if (armorText != null)
                {
                    SetTextActive(armorText, armor);
                }
                else
                {
                    AppendAttributeBlock(armor);
                }

                AppendAttributeBlock(ordinaryAttributes);

                var differences = m_item.InspectBaseDifferences(
                    m_comparisonReference,
                    favorableComparisonColor,
                    unfavorableComparisonColor
                );
                var comparison = string.IsNullOrEmpty(differences)
                    ? string.Empty
                    : string.IsNullOrEmpty(baseComparisonHeading)
                        ? differences
                        : baseComparisonHeading + "\n" + differences;

                if (baseComparisonText != null)
                {
                    SetTextActive(baseComparisonText, comparison);
                }
                else if (!string.IsNullOrEmpty(comparison))
                {
                    if (!string.IsNullOrEmpty(attributesText.text))
                        attributesText.text += "\n\n";

                    attributesText.text += comparison;
                }

                attributesText.gameObject.SetActive(
                    !string.IsNullOrWhiteSpace(attributesText.text)
                );
            }
        }

        /// <summary>
        /// Updates an optional text block and hides it when it has no content, allowing layouts
        /// to collapse comparison-only fields when no equipped reference is being inspected.
        /// </summary>
        protected virtual void SetTextActive(Text element, string value)
        {
            element.text = value;
            element.gameObject.SetActive(!string.IsNullOrEmpty(value));
        }

        protected virtual void AppendAttributeBlock(string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            if (!string.IsNullOrEmpty(attributesText.text))
                attributesText.text += "\n";

            attributesText.text += value;
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

            UpdateSocketsHeading(m_item.sockets?.Length ?? 0);
            UpdateEmptySocketImages();

            UpdateOccupiedSocketRows();

            socketsText.text = hasSockets ? "" : "No sockets";

            var differences = m_item.InspectSocketDifferences(
                m_comparisonReference,
                favorableComparisonColor,
                unfavorableComparisonColor
            );

            if (!string.IsNullOrEmpty(differences))
                socketsText.text += (socketsText.text.Length > 0 ? "\n\n" : " ") + differences;
            socketsText.gameObject.SetActive(!string.IsNullOrWhiteSpace(socketsText.text));
        }

        protected virtual void UpdateSocketsHeading(int totalSlots)
        {
            if (m_socketsHeading == null)
            {
                foreach (var label in socketsContainer.GetComponentsInChildren<Text>(true))
                {
                    if (label != socketsText && label.text.StartsWith("" +
                        "Socket Slots"))
                    {
                        m_socketsHeading = label;
                        break;
                    }
                }
            }

            if (m_socketsHeading != null)
                m_socketsHeading.text = $"Socket Slots: {totalSlots}";
        }

        protected virtual void UpdateEmptySocketImages()
        {
            if (emptySocketSprite == null || m_item.sockets == null)
            {
                if (m_emptySocketsContainer != null)
                    m_emptySocketsContainer.gameObject.SetActive(false);

                return;
            }

            if (m_emptySocketsContainer == null)
            {
                var container = new GameObject("Empty Socket Images", typeof(RectTransform));
                m_emptySocketsContainer = container.GetComponent<RectTransform>();
                m_emptySocketsContainer.SetParent(socketsContainer.transform, false);
                m_emptySocketsContainer.SetSiblingIndex(socketsText.transform.GetSiblingIndex());

                var layout = container.AddComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                layout.spacing = 4f;

                var fitter = container.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            var emptySocketCount = 0;

            foreach (var socket in m_item.sockets)
            {
                if (socket != null)
                    continue;

                GameObject imageObject;

                if (emptySocketCount < m_emptySocketImages.Count)
                {
                    imageObject = m_emptySocketImages[emptySocketCount];
                    imageObject.SetActive(true);
                }
                else
                {
                    imageObject = new GameObject(
                        "Empty Socket",
                        typeof(RectTransform),
                        typeof(Image)
                    );
                    imageObject.transform.SetParent(m_emptySocketsContainer, false);
                    m_emptySocketImages.Add(imageObject);
                }

                var image = imageObject.GetComponent<Image>();
                image.sprite = emptySocketSprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.color = emptySocketColor;
                imageObject.GetComponent<RectTransform>().sizeDelta =
                    new Vector2(emptySocketImageSize, emptySocketImageSize);
                emptySocketCount++;
            }

            for (var i = emptySocketCount; i < m_emptySocketImages.Count; i++)
                m_emptySocketImages[i].SetActive(false);

            m_emptySocketsContainer.gameObject.SetActive(emptySocketCount > 0);
        }

        /// <summary>
        /// Builds one row per filled socket, with the socket frame and the Socketable's own
        /// inventory image beside the description of the bonus it grants to this item.
        /// </summary>
        protected virtual void UpdateOccupiedSocketRows()
        {
            if (m_item.sockets == null)
            {
                if (m_occupiedSocketsContainer != null)
                    m_occupiedSocketsContainer.gameObject.SetActive(false);

                return;
            }

            EnsureOccupiedSocketsContainer();

            var occupiedCount = 0;
            var itemScope = m_item.GetItemScope();

            foreach (var socket in m_item.sockets)
            {
                if (socket == null)
                    continue;

                var row = GetOccupiedSocketRow(occupiedCount++);
                var socketFrame = row.transform.GetChild(0).GetComponent<Image>();
                var runeImage = socketFrame.transform.GetChild(0).GetComponent<Image>();
                var description = row.transform.GetChild(1).GetComponent<Text>();

                socketFrame.sprite = emptySocketSprite;
                socketFrame.enabled = emptySocketSprite != null;
                runeImage.sprite = socket.data != null ? socket.data.image : null;
                runeImage.enabled = runeImage.sprite != null;
                description.text = ItemAttributes.InspectSocket(socket.GetSocketable(), itemScope);
                row.SetActive(true);
            }

            for (var i = occupiedCount; i < m_occupiedSocketRows.Count; i++)
                m_occupiedSocketRows[i].SetActive(false);

            m_occupiedSocketsContainer.gameObject.SetActive(occupiedCount > 0);
        }

        protected virtual void EnsureOccupiedSocketsContainer()
        {
            if (m_occupiedSocketsContainer != null)
                return;

            var container = new GameObject("Occupied Socket Rows", typeof(RectTransform));
            m_occupiedSocketsContainer = container.GetComponent<RectTransform>();
            m_occupiedSocketsContainer.SetParent(socketsContainer.transform, false);
            m_occupiedSocketsContainer.SetSiblingIndex(socketsText.transform.GetSiblingIndex());

            var layout = container.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 2f;

            var fitter = container.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        protected virtual GameObject GetOccupiedSocketRow(int index)
        {
            if (index < m_occupiedSocketRows.Count)
                return m_occupiedSocketRows[index];

            var row = new GameObject("Occupied Socket", typeof(RectTransform));
            row.transform.SetParent(m_occupiedSocketsContainer, false);

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 4f;

            var rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = emptySocketImageSize;

            var frameObject = new GameObject("Socket Frame", typeof(RectTransform), typeof(Image));
            frameObject.transform.SetParent(row.transform, false);
            var frameRect = frameObject.GetComponent<RectTransform>();
            frameRect.sizeDelta = new Vector2(emptySocketImageSize, emptySocketImageSize);
            var frameLayout = frameObject.AddComponent<LayoutElement>();
            frameLayout.minWidth = emptySocketImageSize;
            frameLayout.preferredWidth = emptySocketImageSize;
            frameLayout.minHeight = emptySocketImageSize;
            frameLayout.preferredHeight = emptySocketImageSize;
            var frame = frameObject.GetComponent<Image>();
            frame.preserveAspect = true;
            frame.raycastTarget = false;

            var runeObject = new GameObject("Rune Image", typeof(RectTransform), typeof(Image));
            runeObject.transform.SetParent(frameObject.transform, false);
            var runeRect = runeObject.GetComponent<RectTransform>();
            runeRect.anchorMin = Vector2.zero;
            runeRect.anchorMax = Vector2.one;
            runeRect.offsetMin = Vector2.zero;
            runeRect.offsetMax = Vector2.zero;
            var rune = runeObject.GetComponent<Image>();
            rune.preserveAspect = true;
            rune.raycastTarget = false;

            var textObject = new GameObject("Effect", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(row.transform, false);
            var description = textObject.GetComponent<Text>();
            CopySocketTextStyle(description);
            var textLayout = textObject.AddComponent<LayoutElement>();
            textLayout.flexibleWidth = 1f;

            m_occupiedSocketRows.Add(row);
            return row;
        }

        protected virtual void CopySocketTextStyle(Text target)
        {
            target.font = socketsText.font;
            target.fontSize = socketsText.fontSize;
            target.fontStyle = socketsText.fontStyle;
            target.color = socketsText.color;
            target.alignment = TextAnchor.MiddleLeft;
            target.supportRichText = socketsText.supportRichText;
            target.horizontalOverflow = HorizontalWrapMode.Wrap;
            target.verticalOverflow = VerticalWrapMode.Overflow;
            target.raycastTarget = false;
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
