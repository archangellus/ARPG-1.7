using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CustomUITooltips
{
    [AddComponentMenu("Custom Tooltips/Tooltip Binder (UI Toolkit)")]
    [DisallowMultipleComponent]
    public sealed class TooltipUITKBinder : MonoBehaviour
    {
        [Serializable]
        public sealed class Binding
        {
            [Tooltip("UI Toolkit selector to bind. Use #elementName for a named element, .class-name for every element with a USS class, or a raw name/class without the prefix.")]
            public string selector;

            [Tooltip("Tooltip content, placement, timing, and optional visual override shown for every UI Toolkit element matched by this selector.")]
            public TooltipContent tooltip = new TooltipContent();

            [Tooltip("When enabled, keyboard/gamepad focus on the matched UI Toolkit element also shows the tooltip.")]
            public bool showOnFocus = true;
        }

        [Tooltip("UIDocument whose rootVisualElement will be searched for tooltip binding selectors. Auto-filled from the same GameObject when possible.")]
        public UIDocument targetDocument;

        [Tooltip("Selector-to-tooltip rows. Each row can target one named element or many elements that share a USS class.")]
        public List<Binding> bindings = new List<Binding>();

        private readonly List<IDisposable> activeBindings = new List<IDisposable>();

        private void Reset()
        {
            targetDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            if (targetDocument == null)
                targetDocument = GetComponent<UIDocument>();

            RegisterBindings();
        }

        private void OnDisable()
        {
            DisposeBindings();
        }

        public void RegisterBindings()
        {
            DisposeBindings();

            if (targetDocument == null || targetDocument.rootVisualElement == null)
                return;

            VisualElement root = targetDocument.rootVisualElement;
            foreach (Binding binding in bindings)
            {
                if (binding == null || string.IsNullOrWhiteSpace(binding.selector) || binding.tooltip == null || binding.tooltip.IsEmpty)
                    continue;

                List<VisualElement> matches = FindMatches(root, binding.selector);
                foreach (VisualElement element in matches)
                    activeBindings.Add(TooltipService.Bind(element, binding.tooltip, binding.showOnFocus));
            }
        }

        private void DisposeBindings()
        {
            for (int i = activeBindings.Count - 1; i >= 0; i--)
                activeBindings[i]?.Dispose();

            activeBindings.Clear();
        }

        private static List<VisualElement> FindMatches(VisualElement root, string selector)
        {
            List<VisualElement> results = new List<VisualElement>();
            string trimmed = selector.Trim();

            if (trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                VisualElement element = root.Q<VisualElement>(trimmed.Substring(1));
                if (element != null)
                    results.Add(element);
                return results;
            }

            if (trimmed.StartsWith(".", StringComparison.Ordinal))
            {
                root.Query<VisualElement>(className: trimmed.Substring(1)).ForEach(results.Add);
                return results;
            }

            VisualElement byName = root.Q<VisualElement>(trimmed);
            if (byName != null)
                results.Add(byName);

            // Also allow designers to type a class name without the leading dot.
            root.Query<VisualElement>(className: trimmed).ForEach(element =>
            {
                if (!results.Contains(element))
                    results.Add(element);
            });

            return results;
        }
    }
}
