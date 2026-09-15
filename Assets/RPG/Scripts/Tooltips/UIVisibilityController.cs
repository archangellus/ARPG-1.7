using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enables or disables UI targets according to one or more watched windows.
/// Each target chooses whether it is enabled or disabled when its rule is satisfied.
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

    public enum SharedTargetMode
    {
        AllRulesMustEnable,
        AnyRuleCanEnable
    }

    [Serializable]
    public class GameObjectTarget
    {
        [Tooltip("The entire UI GameObject to toggle, including its children.")]
        public GameObject target;

        [Tooltip("Checked: turn this object on when the rule matches and off otherwise. " +
                 "Unchecked: turn this object off when the rule matches and on otherwise.")]
        public bool enableWhenConditionsMet = true;
    }

    [Serializable]
    public class ComponentTarget
    {
        [Tooltip("The component whose Enabled checkbox will be controlled. " +
                 "Drag the component header from the Inspector, such as Image or TextMeshProUGUI.")]
        public Behaviour target;

        [Tooltip("Checked: enable this component when the rule matches and disable it otherwise. " +
                 "Unchecked: disable this component when the rule matches and enable it otherwise.")]
        public bool enableWhenConditionsMet = true;
    }

    [Serializable]
    public class VisibilityRule
    {
        [Tooltip("The windows or panels to watch. Each missing reference counts as hidden. " +
                 "An empty list always disables the targets.")]
        public List<GameObject> watchedUIs = new List<GameObject>();

        [Tooltip("All Showing: the condition is met while every watched UI is showing. " +
                 "Any Showing: the condition is met while at least one watched UI is showing.")]
        public WatchMode watchMode = WatchMode.AllShowing;

        [Tooltip("Also check CanvasGroups on each watched UI and its parents.")]
        public bool checkCanvasGroupAlpha = true;

        [Range(0f, 1f)]
        [Tooltip("The UI counts as hidden when its combined CanvasGroup alpha is at or below this value.")]
        public float hiddenAlphaThreshold = 0.001f;

        [Tooltip("Each entry has its own Enable When Conditions Met checkbox. " +
                 "One rule can turn some objects on and other objects off.")]
        public List<GameObjectTarget> objectTargets = new List<GameObjectTarget>();

        [Tooltip("Optional: toggle only a component's Enabled checkbox, such as Image or TextMeshProUGUI. " +
                 "Each entry chooses its own state. Disabling a Button component alone does not hide its image.")]
        public List<ComponentTarget> componentTargets = new List<ComponentTarget>();

        // Keep the original field names and types so existing scene and prefab assignments
        // can be moved into the new lists without changing their serialized format in place.
        [SerializeField, HideInInspector]
        private GameObject watchedUI;

        [SerializeField, HideInInspector]
        private bool invertResult;

        [SerializeField, HideInInspector]
        private List<GameObject> objectsToToggle;

        [SerializeField, HideInInspector]
        private List<Behaviour> componentsToToggle;

        internal void MigrateLegacyData()
        {
            if (watchedUI != null)
            {
                if (watchedUIs == null)
                    watchedUIs = new List<GameObject>();

                if (!watchedUIs.Contains(watchedUI))
                    watchedUIs.Insert(0, watchedUI);

                watchedUI = null;
            }

            // A previously inverted rule becomes an unchecked checkbox on each migrated target.
            if (objectsToToggle != null && objectsToToggle.Count > 0)
            {
                if (objectTargets == null)
                    objectTargets = new List<GameObjectTarget>();

                foreach (GameObject target in objectsToToggle)
                {
                    objectTargets.Add(new GameObjectTarget
                    {
                        target = target,
                        enableWhenConditionsMet = !invertResult
                    });
                }

                objectsToToggle.Clear();
            }

            if (componentsToToggle != null && componentsToToggle.Count > 0)
            {
                if (componentTargets == null)
                    componentTargets = new List<ComponentTarget>();

                foreach (Behaviour target in componentsToToggle)
                {
                    componentTargets.Add(new ComponentTarget
                    {
                        target = target,
                        enableWhenConditionsMet = !invertResult
                    });
                }

                componentsToToggle.Clear();
            }

            invertResult = false;
        }
    }

    [Tooltip("All Rules Must Enable: a shared target is enabled only when all its rules request it. " +
             "Any Rule Can Enable: a shared target is enabled when at least one rule requests it, " +
             "even if other rules request disabling it. This applies only to rules containing that target.")]
    public SharedTargetMode sharedTargetMode = SharedTargetMode.AllRulesMustEnable;

    [Tooltip("Each rule checks its Watched UIs using Watch Mode. Every target has its own " +
             "Enable When Conditions Met checkbox, so one rule can turn different targets on and off. " +
             "When multiple rules control the same target, Shared Target Mode combines their results. " +
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

                bool hasConditions = rule.watchedUIs != null && rule.watchedUIs.Count > 0;
                bool conditionMet = IsRuleSatisfied(rule);

                if (rule.objectTargets != null)
                {
                    foreach (GameObjectTarget entry in rule.objectTargets)
                    {
                        if (entry == null || entry.target == null)
                            continue;

                        GameObject target = entry.target;
                        bool shouldEnable = hasConditions &&
                            (conditionMet == entry.enableWhenConditionsMet);

                        bool previous;
                        m_objectStates[target] = m_objectStates.TryGetValue(target, out previous)
                            ? CombineRequestedStates(previous, shouldEnable)
                            : shouldEnable;
                    }
                }

                if (rule.componentTargets != null)
                {
                    foreach (ComponentTarget entry in rule.componentTargets)
                    {
                        if (entry == null || entry.target == null || entry.target == this)
                            continue;

                        Behaviour target = entry.target;
                        bool shouldEnable = hasConditions &&
                            (conditionMet == entry.enableWhenConditionsMet);

                        bool previous;
                        m_componentStates[target] = m_componentStates.TryGetValue(target, out previous)
                            ? CombineRequestedStates(previous, shouldEnable)
                            : shouldEnable;
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

    private bool CombineRequestedStates(bool previous, bool requested)
    {
        return sharedTargetMode == SharedTargetMode.AnyRuleCanEnable
            ? previous || requested
            : previous && requested;
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
                rule.MigrateLegacyData();
        }
    }
}
