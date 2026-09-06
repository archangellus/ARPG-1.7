using UnityEngine;

namespace PLAYERTWO.ARPGProject
{
    [AddComponentMenu("PLAYER TWO/ARPG Project/GUI/GUI Inspector Manager")]
    public class GUIInspectorManager : Singleton<GUIInspectorManager>
    {
        [Header("Item Inspector Settings")]
        [Tooltip("The prefab instantiated for the primary Item Inspector.")]
        public GUIItemInspector itemInspectorPrefab;

        [Tooltip(
            "The prefab instantiated for the comparison Item Inspector. If not set, the Item "
                + "Inspector Prefab above is used instead."
        )]
        public GUIItemInspector itemComparisonInspectorPrefab;

        [Header("Skill Inspector Settings")]
        [Tooltip("The prefab instantiated for the Skill Inspector.")]
        public GUISkillInspector skillInspectorPrefab;

        [Header("Effect Inspector Settings")]
        [Tooltip("The prefab instantiated for the Effect Inspector.")]
        public GUIEffectInspector effectInspectorPrefab;

        /// <summary>
        /// Returns the cached, primary Item Inspector instance.
        /// </summary>
        public GUIItemInspector itemInspector { get; protected set; }

        /// <summary>
        /// Returns the cached Item Inspector instance used to display an item's equipped
        /// counterpart when comparing it against the primary Item Inspector.
        /// </summary>
        public GUIItemInspector itemComparisonInspector { get; protected set; }

        /// <summary>
        /// Returns the cached Item Inspector instance used to display an item's second equipped
        /// counterpart (e.g. a Ring, when both Ring slots are occupied).
        /// </summary>
        public GUIItemInspector itemSecondaryComparisonInspector { get; protected set; }

        /// <summary>
        /// Returns the cached Skill Inspector instance.
        /// </summary>
        public GUISkillInspector skillInspector { get; protected set; }

        /// <summary>
        /// Returns the cached Effect Inspector instance.
        /// </summary>
        public GUIEffectInspector effectInspector { get; protected set; }

        protected virtual void InitializeItemInspectors()
        {
            var comparisonPrefab = itemComparisonInspectorPrefab
                ? itemComparisonInspectorPrefab
                : itemInspectorPrefab;

            itemInspector = Instantiate(itemInspectorPrefab, transform);
            itemComparisonInspector = Instantiate(comparisonPrefab, transform);
            itemSecondaryComparisonInspector = Instantiate(comparisonPrefab, transform);

            itemInspector.comparisonInspector = itemComparisonInspector;
            itemInspector.secondaryComparisonInspector = itemSecondaryComparisonInspector;
            itemComparisonInspector.independentPositioning = false;
            itemSecondaryComparisonInspector.independentPositioning = false;

            itemInspector.gameObject.SetActive(false);
            itemComparisonInspector.gameObject.SetActive(false);
            itemSecondaryComparisonInspector.gameObject.SetActive(false);
        }

        protected virtual void InitializeSkillInspector()
        {
            skillInspector = Instantiate(skillInspectorPrefab, transform);
            skillInspector.gameObject.SetActive(false);
        }

        protected virtual void InitializeEffectInspector()
        {
            effectInspector = Instantiate(effectInspectorPrefab, transform);
            effectInspector.gameObject.SetActive(false);
        }

        protected virtual void Start()
        {
            InitializeItemInspectors();
            InitializeSkillInspector();
            InitializeEffectInspector();
        }
    }
}
