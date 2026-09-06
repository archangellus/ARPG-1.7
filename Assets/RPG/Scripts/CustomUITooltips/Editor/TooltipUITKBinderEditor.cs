using CustomUITooltips;
using UnityEditor;
using UnityEngine;

namespace CustomUITooltips.Editor
{
    [CustomEditor(typeof(TooltipUITKBinder))]
    public sealed class TooltipUITKBinderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            TooltipUITKBinder binder = (TooltipUITKBinder)target;
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                EditorGUILayout.Space(8f);
                if (GUILayout.Button(new GUIContent("Rebind Runtime Tooltips", "Clears and recreates tooltip bindings for the selected UIDocument using the current selector rows. Available only in Play Mode.")))
                    binder.RegisterBindings();
            }

            EditorGUILayout.HelpBox("Selectors support #name, .class-name, or a raw element name/class. Designers can set names and classes in UI Builder, then map them to Tooltip rows here.", MessageType.Info);
        }
    }
}
