#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Inspector and Scene-view handles for LightSource2D.
///
/// Inspector features:
///   • Shape selector with a visual icon row
///   • Context-sensitive size fields (Radius for circle; W/H for capsule & rect)
///   • Light color swatch + intensity/extra-light sliders
///   • Falloff slider with live preview label
///   • "Focus in Scene" button
///
/// Scene handles:
///   • Drag handles on every "control point" of the shape (resize)
///   • Colour-coded: outer = falloff boundary, inner = hard edge
///   • Ctrl+drag snaps to 0.25 unit grid
/// </summary>
[CustomEditor(typeof(LightSource2D))]
[CanEditMultipleObjects]
public class LightSource2DEditor : Editor
{
    // ── Serialized properties ──────────────────────────────────────
    private SerializedProperty _shape;
    private SerializedProperty _size;
    private SerializedProperty _lightColor;
    private SerializedProperty _intensity;
    private SerializedProperty _extraLight;
    private SerializedProperty _falloff;

    // ── Styles ────────────────────────────────────────────────────
    private GUIStyle _headerStyle;
    private GUIStyle _sectionStyle;

    // ── Handle IDs ────────────────────────────────────────────────
    private static readonly int[] HandleIDs = new int[8];

    // ── Colours ───────────────────────────────────────────────────
    private static readonly Color HardEdgeColor   = new Color(1f,  0.9f, 0.3f, 0.9f);
    private static readonly Color FalloffColor    = new Color(1f,  0.5f, 0.1f, 0.5f);
    private static readonly Color HandleFillColor = new Color(1f,  1f,  1f,  0.15f);

    // ─────────────────────────────────────────────────────────────
    //  Enable / Disable
    // ─────────────────────────────────────────────────────────────
    private void OnEnable()
    {
        _shape      = serializedObject.FindProperty("shape");
        _size       = serializedObject.FindProperty("size");
        _lightColor = serializedObject.FindProperty("lightColor");
        _intensity  = serializedObject.FindProperty("intensity");
        _extraLight = serializedObject.FindProperty("extraLight");
        _falloff    = serializedObject.FindProperty("falloff");

        for (int i = 0; i < HandleIDs.Length; i++)
            HandleIDs[i] = GUIUtility.GetControlID(FocusType.Passive);
    }

    // ─────────────────────────────────────────────────────────────
    //  Inspector GUI
    // ─────────────────────────────────────────────────────────────
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EnsureStyles();

        // ── Header ──
        EditorGUILayout.Space(4);
        GUILayout.Label("Light Source 2D", _headerStyle);
        EditorGUILayout.Space(6);

        // ── Shape selector ──
        DrawShapeSelector();
        EditorGUILayout.Space(4);

        // ── Size ──
        DrawSizeFields();
        EditorGUILayout.Space(8);

        // ── Light settings ──
        DrawSection("Light", () =>
        {
            EditorGUILayout.PropertyField(_lightColor, new GUIContent("Color"));
            EditorGUILayout.Slider(_intensity,  0f, 1f, new GUIContent("Intensity",  "How much darkness is removed."));
            EditorGUILayout.Slider(_extraLight, 0f, 1f, new GUIContent("Extra Light","Brightness added on top of the scene."));
        });

        EditorGUILayout.Space(4);

        // ── Falloff ──
        DrawSection("Falloff", () =>
        {
            EditorGUILayout.Slider(_falloff, 0f, 1f, new GUIContent("Softness",
                "0 = hard edge, 1 = fully feathered."));

            // Live label
            float f = _falloff.floatValue;
            string label = f < 0.1f ? "Hard edge"
                         : f < 0.4f ? "Subtle glow"
                         : f < 0.7f ? "Soft halo"
                                    : "Very diffuse";
            EditorGUILayout.LabelField(" ", label, EditorStyles.centeredGreyMiniLabel);
        });

        EditorGUILayout.Space(8);

        // ── Focus button ──
        if (GUILayout.Button("Focus in Scene View", GUILayout.Height(24)))
        {
            SceneView sv = SceneView.lastActiveSceneView;
            if (sv != null)
            {
                sv.Frame(new Bounds(((LightSource2D)target).transform.position,
                                    Vector3.one * ((LightSource2D)target).size.magnitude * 4f), false);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    // ─────────────────────────────────────────────────────────────
    //  Scene GUI
    // ─────────────────────────────────────────────────────────────
    private void OnSceneGUI()
    {
        LightSource2D ls = (LightSource2D)target;

        Undo.RecordObject(ls, "Edit LightSource2D");

        bool changed = false;

        switch (ls.shape)
        {
            case LightSource2D.LightShape.Circle:
                changed = DrawCircleHandles(ls);
                break;
            case LightSource2D.LightShape.Capsule:
                changed = DrawCapsuleHandles(ls);
                break;
            case LightSource2D.LightShape.Rectangle:
                changed = DrawRectangleHandles(ls);
                break;
        }

        if (changed)
        {
            EditorUtility.SetDirty(ls);
            Repaint(); // refresh inspector sliders
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Scene handle implementations
    // ─────────────────────────────────────────────────────────────

    /// <returns>true if the size was modified</returns>
    private bool DrawCircleHandles(LightSource2D ls)
    {
        Vector3 origin = ls.transform.position;
        float   r      = ls.size.x;
        float   rFall  = r * (1f + ls.falloff * 0.5f);

        bool changed = false;

        // Outer falloff ring (non-interactive, just visual)
        using (new Handles.DrawingScope(FalloffColor))
            Handles.DrawWireDisc(origin, Vector3.forward, rFall);

        // Hard-edge circle
        using (new Handles.DrawingScope(HardEdgeColor))
            Handles.DrawWireDisc(origin, Vector3.forward, r);

        // 4 drag handles on the circle perimeter (N/S/E/W)
        Vector3[] dirs = { Vector3.up, Vector3.down, Vector3.right, Vector3.left };
        for (int i = 0; i < 4; i++)
        {
            Vector3 hp  = origin + dirs[i] * r;
            Vector3 newHp = SliderHandle(hp, dirs[i], 0.12f, HardEdgeColor);
            float   newR  = Vector3.Distance(origin, newHp);
            if (!Approximately(newR, r))
            {
                ls.size = new Vector2(Snap(newR), ls.size.y);
                changed = true;
            }
        }

        return changed;
    }

    private bool DrawCapsuleHandles(LightSource2D ls)
    {
        Vector3 origin = ls.transform.position;
        float   hw     = ls.size.x;
        float   hh     = ls.size.y;
        float   bodyH  = Mathf.Max(0f, hh - hw);
        bool    changed = false;

        // Draw outline
        using (new Handles.DrawingScope(HardEdgeColor))
        {
            Handles.DrawWireArc(origin + Vector3.up   * bodyH, Vector3.forward, Vector3.right, 180f, hw);
            Handles.DrawWireArc(origin + Vector3.down * bodyH, Vector3.forward, Vector3.left,  180f, hw);
            Handles.DrawLine(origin + new Vector3(-hw,  bodyH), origin + new Vector3(-hw, -bodyH));
            Handles.DrawLine(origin + new Vector3( hw,  bodyH), origin + new Vector3( hw, -bodyH));
        }

        // Width handle (right)
        {
            Vector3 hp    = origin + new Vector3(hw, 0f);
            Vector3 newHp = SliderHandle(hp, Vector3.right, 0.12f, HardEdgeColor);
            float   newHW = Snap(Mathf.Max(0.05f, newHp.x - origin.x));
            if (!Approximately(newHW, hw)) { ls.size = new Vector2(newHW, ls.size.y); changed = true; }
        }

        // Height handle (top)
        {
            Vector3 hp    = origin + new Vector3(0f, hh);
            Vector3 newHp = SliderHandle(hp, Vector3.up, 0.12f, HardEdgeColor);
            float   newHH = Snap(Mathf.Max(ls.size.x, newHp.y - origin.y));
            if (!Approximately(newHH, hh)) { ls.size = new Vector2(ls.size.x, newHH); changed = true; }
        }

        return changed;
    }

    private bool DrawRectangleHandles(LightSource2D ls)
    {
        Vector3 origin = ls.transform.position;
        float   hw     = ls.size.x;
        float   hh     = ls.size.y;
        bool    changed = false;

        // Outline
        using (new Handles.DrawingScope(HardEdgeColor))
        {
            Vector3[] corners =
            {
                origin + new Vector3(-hw, -hh),
                origin + new Vector3( hw, -hh),
                origin + new Vector3( hw,  hh),
                origin + new Vector3(-hw,  hh)
            };
            Handles.DrawSolidRectangleWithOutline(corners, HandleFillColor, HardEdgeColor);
        }

        // Falloff outline
        using (new Handles.DrawingScope(FalloffColor))
        {
            float fw = hw * (1f + ls.falloff * 0.5f);
            float fh = hh * (1f + ls.falloff * 0.5f);
            Vector3[] fc =
            {
                origin + new Vector3(-fw, -fh),
                origin + new Vector3( fw, -fh),
                origin + new Vector3( fw,  fh),
                origin + new Vector3(-fw,  fh)
            };
            Handles.DrawSolidRectangleWithOutline(fc, Color.clear, FalloffColor);
        }

        // Edge mid-point handles
        // Right
        {
            Vector3 hp    = origin + new Vector3(hw, 0f);
            Vector3 newHp = SliderHandle(hp, Vector3.right, 0.12f, HardEdgeColor);
            float   nv    = Snap(Mathf.Max(0.05f, newHp.x - origin.x));
            if (!Approximately(nv, hw)) { ls.size = new Vector2(nv, ls.size.y); changed = true; }
        }
        // Left
        {
            Vector3 hp    = origin + new Vector3(-hw, 0f);
            Vector3 newHp = SliderHandle(hp, Vector3.left, 0.12f, HardEdgeColor);
            float   nv    = Snap(Mathf.Max(0.05f, origin.x - newHp.x));
            if (!Approximately(nv, hw)) { ls.size = new Vector2(nv, ls.size.y); changed = true; }
        }
        // Top
        {
            Vector3 hp    = origin + new Vector3(0f, hh);
            Vector3 newHp = SliderHandle(hp, Vector3.up, 0.12f, HardEdgeColor);
            float   nv    = Snap(Mathf.Max(0.05f, newHp.y - origin.y));
            if (!Approximately(nv, hh)) { ls.size = new Vector2(ls.size.x, nv); changed = true; }
        }
        // Bottom
        {
            Vector3 hp    = origin + new Vector3(0f, -hh);
            Vector3 newHp = SliderHandle(hp, Vector3.down, 0.12f, HardEdgeColor);
            float   nv    = Snap(Mathf.Max(0.05f, origin.y - newHp.y));
            if (!Approximately(nv, hh)) { ls.size = new Vector2(ls.size.x, nv); changed = true; }
        }

        // Size label
        Handles.Label(origin + new Vector3(hw + 0.1f, hh + 0.1f),
                      $"  {hw * 2f:0.##} × {hh * 2f:0.##}",
                      EditorStyles.whiteLabel);

        return changed;
    }

    // ─────────────────────────────────────────────────────────────
    //  Inspector helpers
    // ─────────────────────────────────────────────────────────────

    private void DrawShapeSelector()
    {
        EditorGUILayout.LabelField("Shape", EditorStyles.boldLabel);

        LightSource2D.LightShape current = (LightSource2D.LightShape)_shape.enumValueIndex;

        GUIContent[] options =
        {
            new GUIContent("●  Circle"),
            new GUIContent("⬭  Capsule"),
            new GUIContent("■  Rectangle")
        };

        int selected = GUILayout.Toolbar((int)current, options, GUILayout.Height(26));
        if (selected != (int)current)
            _shape.enumValueIndex = selected;
    }

    private void DrawSizeFields()
    {
        LightSource2D.LightShape s = (LightSource2D.LightShape)_shape.enumValueIndex;
        Vector2 v = _size.vector2Value;

        EditorGUI.BeginChangeCheck();

        switch (s)
        {
            case LightSource2D.LightShape.Circle:
                v.x = EditorGUILayout.FloatField("Radius", Mathf.Max(0.01f, v.x));
                v.y = v.x; // keep in sync
                break;

            case LightSource2D.LightShape.Capsule:
                v.x = EditorGUILayout.FloatField("Width (radius)", Mathf.Max(0.01f, v.x));
                v.y = EditorGUILayout.FloatField("Half-Height",    Mathf.Max(v.x,   v.y));
                break;

            case LightSource2D.LightShape.Rectangle:
                v.x = EditorGUILayout.FloatField("Half-Width",  Mathf.Max(0.01f, v.x));
                v.y = EditorGUILayout.FloatField("Half-Height", Mathf.Max(0.01f, v.y));
                break;
        }

        if (EditorGUI.EndChangeCheck())
            _size.vector2Value = v;
    }

    private void DrawSection(string title, System.Action content)
    {
        EditorGUILayout.BeginVertical(_sectionStyle);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.Space(2);
        content();
        EditorGUILayout.EndVertical();
    }

    private void EnsureStyles()
    {
        if (_headerStyle != null) return;

        _headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 14,
            alignment = TextAnchor.MiddleLeft
        };

        _sectionStyle = new GUIStyle("helpbox")
        {
            padding = new RectOffset(8, 8, 6, 6)
        };
    }

    // ─────────────────────────────────────────────────────────────
    //  Utility helpers
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// A round disc + linear slider handle combo.
    /// Returns the new world-space position.
    /// </summary>
    private static Vector3 SliderHandle(Vector3 position, Vector3 direction,
                                        float size, Color color)
    {
        using (new Handles.DrawingScope(color))
        {
            float s = HandleUtility.GetHandleSize(position) * size;
            return Handles.Slider(position, direction, s, Handles.CircleHandleCap, 0f);
        }
    }

    /// <summary>Snap to 0.25 if Ctrl is held, otherwise return as-is.</summary>
    private static float Snap(float v)
    {
        if (Event.current != null && Event.current.control)
            return Mathf.Round(v / 0.25f) * 0.25f;
        return v;
    }

    private static bool Approximately(float a, float b) => Mathf.Abs(a - b) < 0.0001f;
}
#endif
