using UnityEngine;

namespace CustomUITooltips
{
    [CreateAssetMenu(menuName = "Custom Tooltips/Tooltip Profile", fileName = "Tooltip Profile")]
    public sealed class TooltipProfile : ScriptableObject
    {
        [Header("Behavior")]
        [Tooltip("Seconds to wait before a tooltip appears when a trigger requests the profile default delay. Per-tooltip Show Delay values of 0 or higher override this.")]
        [Min(0f)] public float defaultShowDelay = 0.25f;

        [Tooltip("When enabled, tooltip delays use real time and still work while Time.timeScale is 0, such as during pause menus.")]
        public bool useUnscaledTime = true;

        [Header("Layout")]
        [Tooltip("Smallest tooltip card width in panel pixels. Title-only tooltips can shrink below this for compact labels.")]
        [Min(40f)] public float minWidth = 120f;

        [Tooltip("Largest tooltip card width in panel pixels before text wraps onto additional lines.")]
        [Min(80f)] public float maxWidth = 360f;

        [Tooltip("Inner space, in panel pixels, between the tooltip card border and its icon/text content.")]
        [Min(0f)] public float padding = 12f;

        [Tooltip("Space, in panel pixels, between the optional icon and the text column.")]
        [Min(0f)] public float gap = 8f;

        [Tooltip("Minimum distance, in panel pixels, kept between the tooltip card and the screen/panel edges.")]
        [Min(0f)] public float screenMargin = 10f;

        [Tooltip("Rounded-corner radius, in panel pixels, applied to all tooltip card corners.")]
        [Min(0f)] public float cornerRadius = 8f;

        [Tooltip("Tooltip card border thickness, in panel pixels. Use 0 for no visible border.")]
        [Min(0f)] public float borderWidth = 1f;

        [Tooltip("Displayed size, in panel pixels, for the optional tooltip icon.")]
        [Min(0f)] public float iconSize = 24f;

        [Header("Text")]
        [Tooltip("When enabled, title and body labels interpret Unity rich text tags such as <b>, <i>, and <color>.")]
        public bool enableRichText = true;

        [Tooltip("Font size, in panel pixels, used for the tooltip title.")]
        [Min(1)] public int titleFontSize = 14;

        [Tooltip("Font size, in panel pixels, used for the tooltip body text.")]
        [Min(1)] public int bodyFontSize = 12;

        [Tooltip("Alignment used by the tooltip title text inside its label box.")]
        public TextAnchor titleTextAlignment = TextAnchor.UpperLeft;

        [Tooltip("Alignment used by the tooltip body text inside its label box.")]
        public TextAnchor bodyTextAlignment = TextAnchor.UpperLeft;

        [Header("Colors")]
        [Tooltip("Fill color of the tooltip card background. Alpha controls tooltip card opacity.")]
        public Color backgroundColor = new Color(0.055f, 0.058f, 0.066f, 0.97f);

        [Tooltip("Color used for all four sides of the tooltip card border.")]
        public Color borderColor = new Color(1f, 1f, 1f, 0.15f);

        [Tooltip("Text color used by the tooltip title label.")]
        public Color titleColor = Color.white;

        [Tooltip("Text color used by the tooltip body label.")]
        public Color bodyColor = new Color(0.86f, 0.88f, 0.92f, 1f);

        public static TooltipProfile CreateRuntimeDefault()
        {
            TooltipProfile profile = CreateInstance<TooltipProfile>();
            profile.name = "Runtime Tooltip Profile";
            profile.hideFlags = HideFlags.HideAndDontSave;
            return profile;
        }
    }
}
