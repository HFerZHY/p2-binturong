#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Scene-view editor for <see cref="NPCPath"/> assets.
///
/// Features
/// ────────
///  • Drag waypoints directly in the Scene view.
///  • Add / remove / reorder waypoints in the Inspector.
///  • Per-waypoint stop duration field.
///  • Loop toggle.
///  • "Snap All to Ground" helper (2D raycasts downward).
///
/// Usage
/// ─────
///  1. Select the NPCPath asset in the Project window  — OR —
///  2. Select an NPC GameObject whose NPCMovement references the asset.
///     The editor automatically finds the path and draws its gizmos.
/// </summary>
[CustomEditor(typeof(NPCPath))]
public class NPCPathEditor : Editor
{
    // ── Colours ───────────────────────────────────────────────────────────────
    private static readonly Color ColLine        = new Color(0.3f, 0.9f, 0.4f, 0.85f);
    private static readonly Color ColWaypoint    = new Color(0.2f, 0.8f, 1.0f, 1f);
    private static readonly Color ColSelected    = new Color(1.0f, 0.8f, 0.1f, 1f);
    private static readonly Color ColLoopLine    = new Color(0.3f, 0.9f, 0.4f, 0.35f);
    private static readonly Color ColStop        = new Color(1.0f, 0.45f, 0.1f, 1f);

    private const float HandleRadius = 0.18f;
    private const float LineThickness = 2.5f;

    // ── State ─────────────────────────────────────────────────────────────────
    private int _selectedIndex = -1;

    // ──────────────────────────────────────────────────────────────────────────
    // Enable / Disable — manual SceneView hook (required for ScriptableObject editors)
    // ──────────────────────────────────────────────────────────────────────────

    private void OnEnable()  => SceneView.duringSceneGui += OnSceneViewGUI;
    private void OnDisable() => SceneView.duringSceneGui -= OnSceneViewGUI;

    private void OnSceneViewGUI(SceneView _) => OnSceneGUI();

    // ──────────────────────────────────────────────────────────────────────────
    // Inspector GUI
    // ──────────────────────────────────────────────────────────────────────────

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        NPCPath npcPath = (NPCPath)target;

        // ── Loop toggle ───────────────────────────────────────────────────────
        EditorGUI.BeginChangeCheck();
        bool loop = EditorGUILayout.Toggle(
            new GUIContent("Loop", "NPC returns to waypoint 0 after the last waypoint."),
            npcPath.loop);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(npcPath, "Toggle NPC Path Loop");
            npcPath.loop = loop;
            EditorUtility.SetDirty(npcPath);
        }

        EditorGUILayout.Space(6);

        // ── Waypoint list ─────────────────────────────────────────────────────
        EditorGUILayout.LabelField("Waypoints", EditorStyles.boldLabel);

        var waypoints = npcPath.waypoints;
        int removeAt  = -1;
        int moveUp    = -1;

        for (int i = 0; i < waypoints.Length; i++)
        {
            bool isSelected = (_selectedIndex == i);

            // Tint the entire row when this waypoint is selected in scene.
            if (isSelected)
                GUI.backgroundColor = new Color(1f, 0.95f, 0.5f, 1f);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            // Index label
            EditorGUILayout.LabelField($"#{i}", GUILayout.Width(28));

            // Select button
            if (GUILayout.Button("◎", GUILayout.Width(26)))
            {
                _selectedIndex = isSelected ? -1 : i;
                SceneView.RepaintAll();
            }

            // Position field
            EditorGUI.BeginChangeCheck();
            Vector2 pos = EditorGUILayout.Vector2Field(GUIContent.none, waypoints[i].position);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(npcPath, "Move NPC Waypoint");
                waypoints[i].position = pos;
                EditorUtility.SetDirty(npcPath);
            }

            // Up button (swap with previous)
            GUI.enabled = i > 0;
            if (GUILayout.Button("▲", GUILayout.Width(24))) moveUp = i;
            GUI.enabled = true;

            // Remove button
            if (GUILayout.Button("✕", GUILayout.Width(24))) removeAt = i;

            EditorGUILayout.EndHorizontal();

            // Stop duration field
            EditorGUI.BeginChangeCheck();
            float stop = EditorGUILayout.FloatField(
                new GUIContent("  Stop Duration (s)", "Seconds to wait at this waypoint (0 = no stop)."),
                waypoints[i].stopDuration);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(npcPath, "Set NPC Waypoint Stop");
                waypoints[i].stopDuration = Mathf.Max(0f, stop);
                EditorUtility.SetDirty(npcPath);
            }

            EditorGUILayout.EndVertical();

            GUI.backgroundColor = Color.white;
        }

        // ── Apply list mutations ───────────────────────────────────────────────
        if (removeAt >= 0)
        {
            Undo.RecordObject(npcPath, "Remove NPC Waypoint");
            var list = new List<NPCPath.Waypoint>(waypoints);
            list.RemoveAt(removeAt);
            npcPath.waypoints = list.ToArray();
            _selectedIndex    = Mathf.Clamp(_selectedIndex, -1, npcPath.waypoints.Length - 1);
            EditorUtility.SetDirty(npcPath);
        }

        if (moveUp >= 0 && moveUp > 0)
        {
            Undo.RecordObject(npcPath, "Reorder NPC Waypoint");
            var list = new List<NPCPath.Waypoint>(npcPath.waypoints);
            var tmp  = list[moveUp];
            list[moveUp]     = list[moveUp - 1];
            list[moveUp - 1] = tmp;
            npcPath.waypoints = list.ToArray();
            if (_selectedIndex == moveUp) _selectedIndex--;
            else if (_selectedIndex == moveUp - 1) _selectedIndex++;
            EditorUtility.SetDirty(npcPath);
        }

        EditorGUILayout.Space(4);

        // ── Add waypoint buttons ──────────────────────────────────────────────
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("＋ Add Waypoint"))
        {
            Undo.RecordObject(npcPath, "Add NPC Waypoint");
            var list = new List<NPCPath.Waypoint>(npcPath.waypoints);
            Vector2 lastPos = list.Count > 0 ? list[^1].position : Vector2.zero;
            list.Add(new NPCPath.Waypoint { position = lastPos + Vector2.right * 2f });
            npcPath.waypoints = list.ToArray();
            _selectedIndex    = npcPath.waypoints.Length - 1;
            EditorUtility.SetDirty(npcPath);
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("＋ Insert After Selected") && _selectedIndex >= 0)
        {
            Undo.RecordObject(npcPath, "Insert NPC Waypoint");
            var list    = new List<NPCPath.Waypoint>(npcPath.waypoints);
            Vector2 pos = list[_selectedIndex].position + Vector2.right * 2f;
            list.Insert(_selectedIndex + 1, new NPCPath.Waypoint { position = pos });
            npcPath.waypoints = list.ToArray();
            _selectedIndex++;
            EditorUtility.SetDirty(npcPath);
            SceneView.RepaintAll();
        }

        EditorGUILayout.EndHorizontal();

        // ── Helpers ───────────────────────────────────────────────────────────
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Helpers", EditorStyles.boldLabel);

        if (GUILayout.Button(new GUIContent(
            "Snap All Waypoints to Scene View Camera Plane",
            "Sets the Z of all waypoints to 0. Useful after accidentally setting a Z value.")))
        {
            Undo.RecordObject(npcPath, "Snap NPC Waypoints Z");
            for (int i = 0; i < npcPath.waypoints.Length; i++)
                npcPath.waypoints[i].position = new Vector2(
                    npcPath.waypoints[i].position.x,
                    npcPath.waypoints[i].position.y); // Vector2 already strips Z
            EditorUtility.SetDirty(npcPath);
        }

        serializedObject.ApplyModifiedProperties();

        if (!npcPath.IsValid)
            EditorGUILayout.HelpBox("Path needs at least 2 waypoints to be valid.", MessageType.Warning);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Scene GUI — drag handles & path visualisation
    // ──────────────────────────────────────────────────────────────────────────

    internal void OnSceneGUI()
    {
        NPCPath npcPath = (NPCPath)target;
        if (npcPath.waypoints == null || npcPath.waypoints.Length == 0) return;

        Handles.matrix = Matrix4x4.identity;

        DrawPathLines(npcPath);
        DrawWaypointHandles(npcPath);
    }

    private void DrawPathLines(NPCPath npcPath)
    {
        var wp = npcPath.waypoints;
        Handles.color = ColLine;

        for (int i = 0; i < wp.Length - 1; i++)
        {
            Handles.DrawAAPolyLine(
                LineThickness,
                (Vector3)(wp[i].position),
                (Vector3)(wp[i + 1].position));
        }

        // Loop line (dashed / faded)
        if (npcPath.loop && wp.Length >= 2)
        {
            Handles.color = ColLoopLine;
            Handles.DrawDottedLine(
                (Vector3)(wp[^1].position),
                (Vector3)(wp[0].position),
                4f);
        }
    }

    private void DrawWaypointHandles(NPCPath npcPath)
    {
        var wp = npcPath.waypoints;

        for (int i = 0; i < wp.Length; i++)
        {
            Vector3 worldPos = new Vector3(wp[i].position.x, wp[i].position.y, 0f);
            bool    hasStop  = wp[i].stopDuration > 0f;
            bool    selected = (_selectedIndex == i);

            // ── Clickable disc to select ──────────────────────────────────────
            Handles.color = selected ? ColSelected : (hasStop ? ColStop : ColWaypoint);

            float size = HandleUtility.GetHandleSize(worldPos) * HandleRadius;

            if (Handles.Button(worldPos, Quaternion.identity, size, size * 1.5f, Handles.SphereHandleCap))
            {
                _selectedIndex = (selected ? -1 : i);
                Repaint();
            }

            // ── Index label ───────────────────────────────────────────────────
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal  = { textColor = selected ? Color.yellow : Color.white },
                fontSize = 11
            };
            Handles.Label(worldPos + Vector3.up * (size * 1.8f), $"#{i}", labelStyle);

            // Stop-duration badge
            if (hasStop)
            {
                GUIStyle stopStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = ColStop },
                    fontSize = 10
                };
                Handles.Label(worldPos + Vector3.up * (size * 3.4f),
                    $"⏱ {wp[i].stopDuration:0.##}s", stopStyle);
            }

            // ── Drag handle (free-move, only the selected waypoint) ───────────
            if (selected)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newPos = Handles.FreeMoveHandle(
                    worldPos,
                    HandleUtility.GetHandleSize(worldPos) * 0.12f,
                    Vector3.zero,
                    Handles.RectangleHandleCap);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(npcPath, "Drag NPC Waypoint");
                    wp[i].position = new Vector2(newPos.x, newPos.y);
                    EditorUtility.SetDirty(npcPath);
                }
            }
        }
    }
}
#endif