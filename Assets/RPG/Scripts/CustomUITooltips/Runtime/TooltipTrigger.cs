using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CustomUITooltips
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Custom Tooltips/Tooltip Trigger (uGUI)")]
    public sealed class TooltipTrigger : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerMoveHandler,
        IPointerDownHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private const string GeneratedIdPrefix = "tooltip_";

        [Tooltip("Tooltip content, placement, timing, and optional visual override used by this uGUI element.")]
        public TooltipContent tooltip = new TooltipContent();

        [Header("Input")]
        [Tooltip("When enabled, hovering the pointer over this RectTransform shows the tooltip.")]
        public bool showOnHover = true;

        [Tooltip("When enabled, selecting this UI element with keyboard, gamepad, or navigation focus shows the tooltip.")]
        public bool showOnFocus = true;

        [Tooltip("When enabled, the tooltip is hidden automatically if this component or GameObject is disabled.")]
        public bool hideOnDisable = true;

        private Vector2 lastScreenPosition;

        private void Reset()
        {
            EnsureTooltipId();
        }

        private void OnValidate()
        {
            EnsureTooltipId();
        }

        public bool EnsureTooltipId()
        {
            if (tooltip == null)
                tooltip = new TooltipContent();

            if (!string.IsNullOrWhiteSpace(tooltip.id))
                return false;

            tooltip.id = GenerateTooltipId();
            return true;
        }

        private static string GenerateTooltipId()
        {
            return GeneratedIdPrefix + Guid.NewGuid().ToString("N");
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!showOnHover)
                return;

            if (!TooltipManager.TryGetInstance(out TooltipManager manager))
                return;

            lastScreenPosition = eventData.position;
            manager.Show(tooltip, lastScreenPosition, GetTargetScreenRect(), this);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (!TooltipManager.TryGetInstance(out TooltipManager manager, false))
                return;

            lastScreenPosition = eventData.position;
            manager.Move(lastScreenPosition, this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (TooltipManager.TryGetInstance(out TooltipManager manager, false))
                manager.Hide(this);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (tooltip == null || !tooltip.hideOnPress)
                return;

            if (TooltipManager.TryGetInstance(out TooltipManager manager, false))
                manager.Hide(this);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (!showOnFocus)
                return;

            if (!TooltipManager.TryGetInstance(out TooltipManager manager))
                return;

            Rect targetRect = GetTargetScreenRect();
            lastScreenPosition = targetRect.center;
            manager.Show(tooltip, lastScreenPosition, targetRect, this);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (TooltipManager.TryGetInstance(out TooltipManager manager, false))
                manager.Hide(this);
        }

        private void OnDisable()
        {
            if (!hideOnDisable)
                return;

            if (TooltipManager.TryGetInstance(out TooltipManager manager, false))
                manager.Hide(this);
        }

        public void ShowNow()
        {
            if (!TooltipManager.TryGetInstance(out TooltipManager manager))
                return;

            Rect targetRect = GetTargetScreenRect();
            lastScreenPosition = targetRect.center;
            manager.Show(tooltip, lastScreenPosition, targetRect, this);
        }

        public void HideNow()
        {
            if (TooltipManager.TryGetInstance(out TooltipManager manager, false))
                manager.Hide(this);
        }

        private Rect GetTargetScreenRect()
        {
            RectTransform rectTransform = transform as RectTransform;
            if (rectTransform == null)
                return new Rect(lastScreenPosition.x, lastScreenPosition.y, 1f, 1f);

            Canvas canvas = GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;

            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            Vector2 max = min;
            for (int i = 1; i < corners.Length; i++)
            {
                Vector2 p = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
