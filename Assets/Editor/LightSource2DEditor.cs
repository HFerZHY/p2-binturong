#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LightSource2D))]
[CanEditMultipleObjects]
public class LightSource2DEditor : Editor
{
    private SerializedProperty _radius;
    private SerializedProperty _coreRadius;
    private SerializedProperty _lightColor;
    private SerializedProperty _intensity;

    private void OnEnable()
    {
        _radius     = serializedObject.FindProperty("radius");
        _coreRadius = serializedObject.FindProperty("coreRadius");
        _lightColor = serializedObject.FindProperty("lightColor");
        _intensity  = serializedObject.FindProperty("intensity");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Light Source 2D", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        // Shape
        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.LabelField("Shape", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_radius,     new GUIContent("Radius", "World-space radius of the light."));
        EditorGUILayout.Slider(_coreRadius, 0f, 1f, new GUIContent("Core Radius",
            "Fraction of the radius that is fully bright. The rest fades to zero."));

        // Show a mini visual bar so the ratio is immediately legible
        var barRect = EditorGUILayout.GetControlRect(false, 8f);
        barRect.x     += EditorGUIUtility.labelWidth;
        barRect.width -= EditorGUIUtility.labelWidth;
        EditorGUI.DrawRect(barRect, new Color(0.15f, 0.15f, 0.15f));
        var coreRect = barRect;
        coreRect.width *= _coreRadius.floatValue;
        EditorGUI.DrawRect(coreRect, new Color(1f, 0.9f, 0.3f, 0.85f));
        var fadeRect = barRect;
        fadeRect.x    += coreRect.width;
        fadeRect.width -= coreRect.width;
        EditorGUI.DrawRect(fadeRect, new Color(1f, 0.5f, 0.1f, 0.4f));
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4);

        // Light
        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.LabelField("Light", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_lightColor, new GUIContent("Color"));
        EditorGUILayout.Slider(_intensity, 0f, 1f, new GUIContent("Intensity"));
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        if (GUILayout.Button("Focus in Scene View", GUILayout.Height(24)))
        {
            var ls = (LightSource2D)target;
            SceneView.lastActiveSceneView?.Frame(
                new Bounds(ls.transform.position, Vector3.one * ls.radius * 4f), false);
        }

        serializedObject.ApplyModifiedProperties();
    }

    // ── Scene handles ─────────────────────────────────────────────
    private void OnSceneGUI()
    {
        var ls = (LightSource2D)target;
        Undo.RecordObject(ls, "Edit LightSource2D");

        Vector3 pos = ls.transform.position;
        bool changed = false;

        // Outer radius handle
        using (new Handles.DrawingScope(new Color(1f, 0.9f, 0.3f, 0.9f)))
        {
            Handles.DrawWireDisc(pos, Vector3.forward, ls.radius);

            // 4 cardinal drag points
            Vector3[] dirs = { Vector3.up, Vector3.down, Vector3.right, Vector3.left };
            foreach (var dir in dirs)
            {
                Vector3 hp    = pos + dir * ls.radius;
                Vector3 newHp = Handles.Slider(hp, dir,
                    HandleUtility.GetHandleSize(hp) * 0.12f, Handles.CircleHandleCap, 0f);
                float newR = Vector3.Distance(pos, newHp);
                if (Mathf.Abs(newR - ls.radius) > 0.001f)
                {
                    ls.radius = Snap(Mathf.Max(0.01f, newR));
                    changed = true;
                }
            }
        }

        // Core radius handle (drawn at radius * coreRadius)
        using (new Handles.DrawingScope(new Color(1f, 0.5f, 0.1f, 0.8f)))
        {
            float coreWorld = ls.radius * ls.coreRadius;
            Handles.DrawWireDisc(pos, Vector3.forward, coreWorld);

            Vector3 hp    = pos + Vector3.right * coreWorld;
            Vector3 newHp = Handles.Slider(hp, Vector3.right,
                HandleUtility.GetHandleSize(hp) * 0.1f, Handles.CircleHandleCap, 0f);
            float newCoreWorld = Mathf.Clamp(Vector3.Distance(pos, newHp), 0f, ls.radius);
            float newCoreRatio = newCoreWorld / Mathf.Max(0.001f, ls.radius);
            if (Mathf.Abs(newCoreRatio - ls.coreRadius) > 0.001f)
            {
                ls.coreRadius = Mathf.Clamp01(newCoreRatio);
                changed = true;
            }
        }

        // Radius label
        Handles.Label(pos + Vector3.right * ls.radius + Vector3.up * 0.15f,
            $"  r = {ls.radius:0.##}", EditorStyles.whiteLabel);

        if (changed) EditorUtility.SetDirty(ls);
    }

    private static float Snap(float v)
        => Event.current != null && Event.current.control
            ? Mathf.Round(v / 0.25f) * 0.25f
            : v;
}
#endif