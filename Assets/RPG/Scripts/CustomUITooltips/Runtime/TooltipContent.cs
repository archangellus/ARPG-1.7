using System;
using UnityEngine;

namespace CustomUITooltips
{
    [Serializable]
    public sealed class TooltipContent
    {
        [Tooltip("Unique tooltip identifier for databases, analytics, or scripted lookup. uGUI TooltipTrigger components generate this automatically when it is empty.")]
        public string id;

        [Tooltip("Short heading displayed at the top of the tooltip. Leave empty to show only body text or icon content.")]
        public string title;

        [TextArea(2, 8)]
        [Tooltip("Main tooltip copy. Rich text tags are supported when the active Tooltip Profile enables Rich Text.")]
        public string body;

        [Tooltip("Optional sprite shown to the left of the text. Leave empty for a text-only tooltip.")]
        public Sprite icon;

        [Tooltip("Optional profile used only by this tooltip. Leave empty to use the Tooltip Manager Default Profile.")]
        public TooltipProfile overrideProfile;

        [Tooltip("Where the tooltip should appear relative to the target element or pointer.")]
        public TooltipPlacement placement = TooltipPlacement.FollowPointer;

        [Tooltip("Offset in panel pixels. For Follow Pointer this offsets from the cursor; for anchored placements this controls spacing from the target.")]
        public Vector2 offset = new Vector2(18f, 18f);

        [Tooltip("Seconds to wait before this tooltip appears. Use -1 to inherit Default Show Delay from the active Tooltip Profile.")]
        public float showDelay = -1f;

        [Tooltip("When enabled, the tooltip updates its position as the pointer moves while visible.")]
        public bool followPointer = true;

        [Tooltip("When enabled, pointer or touch press on the source element immediately hides the tooltip.")]
        public bool hideOnPress = true;

        public bool IsEmpty => string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body) && icon == null;

        public static TooltipContent Text(string title, string body = null)
        {
            return new TooltipContent
            {
                title = title,
                body = body
            };
        }
    }
}
