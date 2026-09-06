using CustomUITooltips;
using UnityEditor;
using UnityEngine;

namespace CustomUITooltips.Editor
{
    [CustomEditor(typeof(TooltipTrigger))]
    public sealed class TooltipTriggerEditor : UnityEditor.Editor
    {
        private void OnEnable()
        {
            TooltipTrigger trigger = (TooltipTrigger)target;
            if (trigger == null)
                return;

            bool missingId = trigger.tooltip == null || string.IsNullOrWhiteSpace(trigger.tooltip.id);
            if (!missingId)
                return;

            Undo.RecordObject(trigger, "Generate Tooltip ID");
            if (trigger.EnsureTooltipId())
                EditorUtility.SetDirty(trigger);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            TooltipTrigger trigger = (TooltipTrigger)target;
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                EditorGUILayout.Space(8f);
                if (GUILayout.Button(new GUIContent("Preview Tooltip Now", "Shows this tooltip immediately using the current Inspector values. Available only in Play Mode.")))
                    trigger.ShowNow();

                if (GUILayout.Button(new GUIContent("Hide Tooltip", "Hides the currently visible tooltip owned by this trigger. Available only in Play Mode.")))
                    trigger.HideNow();
            }

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Tooltip IDs are generated automatically when missing. Preview buttons are available in Play Mode. In edit mode, fill the remaining Tooltip fields and use the scene setup menu under Tools/Custom Tooltips.", MessageType.Info);
        }
    }
}
