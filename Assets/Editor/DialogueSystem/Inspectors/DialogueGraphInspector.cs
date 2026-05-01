using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DialogueSystem.Data;

namespace DialogueSystem.Editor
{
    /// <summary>
    /// Custom Inspector for the DialogueGraph ScriptableObject.
    /// Shown in the Unity Inspector when the asset is selected in the Project window.
    ///
    /// Provides:
    ///   - "Open in Editor" button to launch the graph editor window
    ///   - Quick summary (node count, entry node)
    ///   - Inline validation errors
    ///   - Default serialized field fallback
    /// </summary>
    [CustomEditor(typeof(DialogueGraph))]
    public class DialogueGraphInspector : UnityEditor.Editor
    {
        private DialogueGraph _graph;

        private void OnEnable()
        {
            _graph = (DialogueGraph)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // ── Open in Editor ────────────────────────────────────────────────

            GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
            if (GUILayout.Button("▶  Open in Dialogue Graph Editor", GUILayout.Height(36)))
                DialogueGraphEditorWindow.OpenWithGraph(_graph);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(8);

            // ── Summary ───────────────────────────────────────────────────────

            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Node count",  _graph.nodes?.Count.ToString() ?? "—");
            EditorGUILayout.LabelField("Entry node",  string.IsNullOrEmpty(_graph.entryNodeId) ? "⚠ Not set" : _graph.entryNodeId);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(8);

            // ── Validation ────────────────────────────────────────────────────

            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

            _graph.BuildLookup();
            List<string> errors = _graph.Validate();

            if (errors.Count == 0)
            {
                EditorGUILayout.HelpBox("✓ Graph is valid.", MessageType.Info);
            }
            else
            {
                foreach (var err in errors)
                    EditorGUILayout.HelpBox(err, MessageType.Warning);
            }

            EditorGUILayout.Space(8);

            // ── Default inspector (entryNodeId + nodes list) ──────────────────

            EditorGUILayout.LabelField("Raw Data", EditorStyles.boldLabel);
            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
