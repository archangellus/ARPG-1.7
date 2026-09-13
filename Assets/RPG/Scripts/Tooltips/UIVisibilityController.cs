using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enables or disables UI targets according to one or more watched windows.
/// Attach to a GameObject that remains active while the watched UI is hidden.
/// Watches GameObject activation and, optionally, CanvasGroup transparency.
/// This does not test screen position, clipping, occlusion or Graphic color alpha.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
[AddComponentMenu("UI/UI Visibility Controller")]
public class UIVisibilityController : MonoBehaviour
{
    public enum WatchMode
    {
        AllShowing,
        AnyShowing
    }

    [Serializable]
    public class VisibilityRule
    {
        [Tooltip("The windows or panels to watch. Each missing reference counts as hidden. " +
                 "An empty list always disables the targets.")]
        public List<GameObject> watchedUIs = new List<GameObject>();

        [Tooltip("All Showing: enable targets only while every watched UI is showing. " +
                 "Any Showing: enable targets while at least one watched UI is showing.")]
        public WatchMode watchMode = WatchMode.AllShowing;

        // Keep the original serialized field name to migrate existing Inspector assignments.
        [SerializeField, HideInInspector]
        private GameObject watchedUI;

        [Tooltip("Also check CanvasGroups on each watched UI and its parents.")]
        public bool checkCanvasGroupAlpha = true;

        [Range(0f, 1f)]
        [Tooltip("The UI counts as hidden when its combined CanvasGroup alpha is at or below this value.")]
        public float hiddenAlphaThreshold = 0.001f;

        [Tooltip("Entire UI GameObjects to show or hide, including their children.")]
        public List<GameObject> objectsToToggle = new List<GameObject>();

        [Tooltip("Optional: toggle only a component's Enabled checkbox, such as Image or TextMeshProUGUI. " +
                 "Drag the component header from the Inspector. Disabling a Button component alone does not hide its image.")]
        public List<Behaviour> componentsToToggle = new List<Behaviour>();

        internal void MigrateLegacyWatch()
        {
            if (watchedUI == null)
                return;

            if (watchedUIs == null)
                watchedUIs = new List<GameObject>();

            if (!watchedUIs.Contains(watchedUI))
                watchedUIs.Insert(0, watchedUI);

            watchedUI = null;
        }
    }

    [Tooltip("Each rule applies its Watch Mode to its Watched UIs list. " +
             "If a target is listed in multiple rules, all those rules must be satisfied to enable it. " +
             "Keep targets separate from watched windows and this controller.")]
    public List<VisibilityRule> rules = new List<VisibilityRule>();

    private readonly List<CanvasGroup> m_groups = new List<CanvasGroup>();
    private readonly Dictionary<GameObject, bool> m_objectStates = new Dictionary<GameObject, bool>();
    private readonly Dictionary<Behaviour, bool> m_componentStates = new Dictionary<Behaviour, bool>();
    private bool m_refreshing;

    protected virtual void OnEnable()
    {
        MigrateLegacyRules();
        Refresh();
    }

    protected virtual void OnValidate()
    {
        MigrateLegacyRules();
    }

    protected virtual void LateUpdate()
    {
        Refresh();
    }

    /// <summary>
    /// Applies the rules immediately. Can also be called from another script or UnityEvent.
    /// The controller continuously owns target active/enabled states while it is running.
    /// </summary>
    public void Refresh()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || m_refreshing || rules == null)
            return;

        m_refreshing = true;

        try
        {
            m_objectStates.Clear();
            m_componentStates.Clear();

            // Read every condition before changing any targets.
            foreach (VisibilityRule rule in rules)
            {
                if (rule == null)
                    continue;

                bool showing = IsRuleSatisfied(rule);

                if (rule.objectsToToggle != null)
                {
                    foreach (GameObject target in rule.objectsToToggle)
                    {
                        if (target == null)
                            continue;

                        bool previous;
                        m_objectStates[target] = m_objectStates.TryGetValue(target, out previous)
                            ? previous && showing
                            : showing;
                    }
                }

                if (rule.componentsToToggle != null)
                {
                    foreach (Behaviour target in rule.componentsToToggle)
                    {
                        if (target == null || target == this)
                            continue;

                        bool previous;
                        m_componentStates[target] = m_componentStates.TryGetValue(target, out previous)
                            ? previous && showing
                            : showing;
                    }
                }
            }

            foreach (KeyValuePair<GameObject, bool> entry in m_objectStates)
            {
                GameObject target = entry.Key;

                if (target == null || target.activeSelf == entry.Value)
                    continue;

                // Never deactivate this controller or a watched UI through a target ancestor.
                if (transform.IsChildOf(target.transform) || ContainsWatchedUI(target.transform))
                    continue;

                target.SetActive(entry.Value);
            }

            foreach (KeyValuePair<Behaviour, bool> entry in m_componentStates)
            {
                Behaviour target = entry.Key;

                if (target == null || target.enabled == entry.Value)
                    continue;

                // A CanvasGroup used as a condition must not also be controlled as an output.
                if (target is CanvasGroup && ContainsWatchedUI(target.transform))
                    continue;

                target.enabled = entry.Value;
            }
        }
        finally
        {
            m_refreshing = false;
        }
    }

    private bool IsRuleSatisfied(VisibilityRule rule)
    {
        if (rule.watchedUIs == null || rule.watchedUIs.Count == 0)
            return false;

        bool requireAll = rule.watchMode != WatchMode.AnyShowing;

        foreach (GameObject watchedUI in rule.watchedUIs)
        {
            bool showing = IsShowing(watchedUI, rule);

            if (requireAll && !showing)
                return false;

            if (!requireAll && showing)
                return true;
        }

        return requireAll;
    }

    private bool IsShowing(GameObject watchedUI, VisibilityRule rule)
    {
        if (watchedUI == null || !watchedUI.activeInHierarchy)
            return false;

        if (!rule.checkCanvasGroupAlpha)
            return true;

        float alpha = 1f;
        Transform current = watchedUI.transform;

        while (current != null)
        {
            current.GetComponents(m_groups);
            bool ignoreParents = false;

            foreach (CanvasGroup group in m_groups)
            {
                if (group == null || !group.isActiveAndEnabled)
                    continue;

                alpha *= group.alpha;
                ignoreParents |= group.ignoreParentGroups;
            }

            if (ignoreParents)
                break;

            current = current.parent;
        }

        return alpha > rule.hiddenAlphaThreshold;
    }

    private bool ContainsWatchedUI(Transform candidate)
    {
        foreach (VisibilityRule rule in rules)
        {
            if (rule == null || rule.watchedUIs == null)
                continue;

            foreach (GameObject watchedUI in rule.watchedUIs)
            {
                if (watchedUI != null && watchedUI.transform.IsChildOf(candidate))
                    return true;
            }
        }

        return false;
    }

    private void MigrateLegacyRules()
    {
        if (rules == null)
            return;

        foreach (VisibilityRule rule in rules)
        {
            if (rule != null)
                rule.MigrateLegacyWatch();
        }
    }
}
