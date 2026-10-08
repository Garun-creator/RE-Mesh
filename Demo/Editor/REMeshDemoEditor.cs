using UnityEditor;
using UnityEngine;

namespace REMesh.Demo.EditorTools
{
    /// <summary>
    /// Draws the demo object's steps as instructions rather than as four text fields.
    ///
    /// The fields are there so the scene file carries the words - a reviewer opening the
    /// scene in a text editor can read them - but nobody wants to edit them, and a stack of
    /// editable TextAreas reads like a form to fill in rather than something to follow.
    /// </summary>
    [CustomEditor(typeof(REMeshDemo))]
    public sealed class REMeshDemoEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var demo = (REMeshDemo)target;

            EditorGUILayout.LabelField("MESHRA — getting started (CODE SYNERGY)", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            var i = 1;
            foreach (var step in new[] { demo.step1, demo.step2, demo.step3, demo.step4 })
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label($"{i++}.", GUILayout.Width(18f));
                    EditorGUILayout.LabelField(step, EditorStyles.wordWrappedLabel);
                }
                EditorGUILayout.Space(2f);
            }

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("Open the MESHRA Studio gallery", GUILayout.Height(30f)))
                EditorApplication.ExecuteMenuItem("Tools/MESHRA/Browse Assets");

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(demo.note, MessageType.None);
        }
    }
}
