using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NPCMovement))]
public class NPCMovementEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        NPCMovement npc = (NPCMovement)target;

        // Provide a shortcut to open the path asset editor.
        SerializedProperty pathProp = serializedObject.FindProperty("path");
        if (pathProp.objectReferenceValue is NPCPath npcPath && npcPath != null)
        {
            EditorGUILayout.Space(4);
            if (GUILayout.Button("Select Path Asset"))
                Selection.activeObject = npcPath;
        }

        // Runtime status (play-mode only)
        if (Application.isPlaying)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Runtime State", EditorStyles.boldLabel);
            GUI.enabled = false;
            EditorGUILayout.Toggle("Is Moving",      npc.IsMoving);
            EditorGUILayout.Toggle("Is Stopped",     npc.IsStopped);
            EditorGUILayout.Toggle("Path Complete",  npc.PathComplete);
            EditorGUILayout.IntField("Target Waypoint", npc.CurrentWaypointIndex);
            GUI.enabled = true;
        }
    }

    // Draw path gizmos when the NPC GameObject itself is selected.
    private void OnSceneGUI()
    {
        NPCMovement npc = (NPCMovement)target;
        SerializedProperty pathProp = serializedObject.FindProperty("path");
        if (pathProp.objectReferenceValue is not NPCPath npcPath || npcPath == null) return;
        if (!npcPath.IsValid) return;

        // Reuse the same drawing logic via a temporary editor instance.
        // This is lightweight — no persistent state needed here.
        var pathEditor = (NPCPathEditor)CreateEditor(npcPath);
        pathEditor.OnSceneGUI();
        DestroyImmediate(pathEditor);
    }
}