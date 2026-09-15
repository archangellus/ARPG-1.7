using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PLAYERTWO.ARPGProject
{
    public class GUIInspector : MonoBehaviour
    {
        protected Canvas m_canvas;
        protected CanvasGroup m_canvasGroup;

        protected const float k_fadeDelay = 0.15f;
        protected const float k_fadeDuration = 0.1f;

        protected Vector3[] temp_corners = new Vector3[4];
        protected readonly List<RectTransform> m_visualRects = new();

        protected Canvas canvas
        {
            get
            {
                if (!m_canvas)
                    m_canvas = GetComponentInParent<Canvas>();

                return m_canvas;
            }
        }

        protected CanvasGroup canvasGroup
        {
            get
            {
                if (!m_canvasGroup)
                    m_canvasGroup = GetComponent<CanvasGroup>();

                return m_canvasGroup;
            }
        }

        protected RectTransform m_rect => (RectTransform)transform;

        protected WaitForSeconds m_waitForFadeDelay;

        protected virtual void InitializeWaits()
        {
            m_waitForFadeDelay = new WaitForSeconds(k_fadeDelay);
        }

        /// <summary>
        /// Sets the position of this inspector to a corner relative to a given Rect Transform.
        /// </summary>
        /// <param name="other">The Rect Transform to read the corners from.</param>
        public virtual void SetPositionRelativeTo(RectTransform other)
        {
            other.GetWorldCorners(temp_corners);

            var pivot = CalculatePivotFrom(other.position);
            var x = pivot.x == 1 ? temp_corners[1].x : temp_corners[2].x;
            var y = pivot.y == 1 ? temp_corners[1].y : temp_corners[0].y;

            m_rect.pivot = pivot;
            m_rect.position = new Vector2(x, y);
            ClampToCanvasBounds();
        }

        /// <summary>
        /// Moves the inspector back inside its canvas without relying on the canvas scale,
        /// render mode, or the Rect Transform pivot. This is intentionally performed in the
        /// canvas' local space so inspectors also stay visible when a Canvas Scaler is active.
        /// </summary>
        protected virtual void ClampToCanvasBounds()
        {
            if (!canvas || !(canvas.transform is RectTransform canvasRect))
                return;

            // The inspector prefab's root does not necessarily contain all of its artwork.
            // ContentSizeFitters, text, ornamental frames, etc. can extend beyond that root,
            // so clamping only m_rect still lets visible content leave the screen.
            m_rect.GetComponentsInChildren(false, m_visualRects);

            var hasBounds = false;
            var min = Vector2.zero;
            var max = Vector2.zero;

            foreach (var visualRect in m_visualRects)
            {
                if (!visualRect.gameObject.activeInHierarchy)
                    continue;

                visualRect.GetWorldCorners(temp_corners);

                foreach (var worldCorner in temp_corners)
                {
                    var corner = (Vector2)canvasRect.InverseTransformPoint(worldCorner);

                    if (!hasBounds)
                    {
                        min = corner;
                        max = corner;
                        hasBounds = true;
                    }
                    else
                    {
                        min = Vector2.Min(min, corner);
                        max = Vector2.Max(max, corner);
                    }
                }
            }

            if (!hasBounds)
                return;

            var bounds = canvasRect.rect;
            var offset = Vector2.zero;

            if (max.x - min.x <= bounds.width)
            {
                if (min.x < bounds.xMin)
                    offset.x = bounds.xMin - min.x;
                else if (max.x > bounds.xMax)
                    offset.x = bounds.xMax - max.x;
            }
            else
            {
                // An inspector wider than the canvas cannot fit on both sides. Keep its
                // leading edge visible rather than allowing an arbitrary off-screen position.
                offset.x = bounds.xMin - min.x;
            }

            if (max.y - min.y <= bounds.height)
            {
                if (min.y < bounds.yMin)
                    offset.y = bounds.yMin - min.y;
                else if (max.y > bounds.yMax)
                    offset.y = bounds.yMax - max.y;
            }
            else
            {
                offset.y = bounds.yMax - max.y;
            }

            if (offset != Vector2.zero)
                m_rect.position += canvasRect.TransformVector(offset);
        }

        protected virtual Vector2 CalculatePivotFrom(Vector2 position)
        {
            var x = position.x > Screen.width / 2 ? 1 : 0;
            var y = HasRoomBelow(position) ? 1 : 0;
            return new Vector2(x, y);
        }

        /// <summary>
        /// Returns true if this inspector's height fits below a given position without
        /// overflowing the bottom of the screen.
        /// </summary>
        /// <param name="position">The position to check room below, in screen space.</param>
        protected virtual bool HasRoomBelow(Vector2 position)
        {
            var canvasYScale = canvas.transform.localScale.y;
            return position.y - m_rect.sizeDelta.y * canvasYScale > 0;
        }

        protected virtual void UpdatePivot()
        {
            var position = EntityInputs.GetPointerPosition();
            m_rect.pivot = CalculatePivotFrom(position);
        }

        protected virtual void UpdatePosition()
        {
            transform.position = EntityInputs.GetPointerPosition();
            ClampToCanvasBounds();
        }

        protected void FadIn(System.Action callback = null) =>
            StartCoroutine(FadeRoutine(0, 1, callback));

        protected void FadeOut(System.Action callback = null) =>
            StartCoroutine(FadeRoutine(1, 0, callback));

        protected IEnumerator FadeRoutine(float from, float to, System.Action callback)
        {
            if (canvasGroup)
            {
                canvasGroup.alpha = from;

                yield return m_waitForFadeDelay;

                var time = 0f;

                while (time <= k_fadeDuration)
                {
                    time += Time.deltaTime;
                    canvasGroup.alpha = Mathf.Lerp(from, to, time / k_fadeDuration);
                    yield return null;
                }
            }

            callback?.Invoke();
        }

        protected virtual void Awake()
        {
            InitializeWaits();
            Canvas.willRenderCanvases += ClampBeforeCanvasRender;
        }

        /// <summary>
        /// Clamps after Unity's layout rebuild has resolved ContentSizeFitter and text sizes.
        /// LateUpdate is too early for dynamically-sized inspector content and can otherwise
        /// use the previous frame's bounds on the first visible frame.
        /// </summary>
        protected virtual void ClampBeforeCanvasRender()
        {
            if (isActiveAndEnabled)
                ClampToCanvasBounds();
        }

        protected virtual void OnDestroy()
        {
            Canvas.willRenderCanvases -= ClampBeforeCanvasRender;
        }

#if UNITY_STANDALONE || UNITY_WEBGL
        protected virtual void LateUpdate()
        {
            UpdatePivot();
            UpdatePosition();
        }
#endif
    }
}
