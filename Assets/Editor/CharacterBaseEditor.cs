// Assets/Editor/CharacterBaseEditor.cs
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CharacterBase))]
public class CharacterBaseEditor : Editor
{
    private PreviewRenderUtility _previewUtil;
    private Material _previewMaterial;
    private Vector2 _drag;
    private bool _isDragging;
    
    // Add this field alongside _drag and _isDragging
    private float _zoom = 0.55f;
    private const float ZoomMin = 0.1f;
    private const float ZoomMax = 3f;

    private Color _currentSkin;
    private Color _currentHair;
    private Color _currentClothes;

    private CharacterBase Target => (CharacterBase)target;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void OnEnable()
    {
        // Always rebuild — never trust a stale _previewMaterial from a previous target
        _currentSkin    = default;
        _currentHair    = default;
        _currentClothes = default;
    
        if (AllFieldsSet())
            RebuildPreviewMaterial();
    }

    private void OnDisable()
    {
        CleanupPreview();
    }

    // ── Inspector GUI ──────────────────────────────────────────────────────

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        bool anyChanged = EditorGUI.EndChangeCheck();

        if (anyChanged)
        {
            if (AllFieldsSet())
            {
                if (_previewMaterial == null)
                {
                    // First time all fields are valid — build from scratch
                    RebuildPreviewMaterial();
                }
                else
                {
                    // Already have an instance — just patch what changed
                    ApplyTexturesToPreview();
                    ApplyColorsToPreview();
                }
            }
            else
            {
                // A field was cleared — destroy the stale preview
                CleanupPreview();
            }

            Repaint();
        }

        if (!AllFieldsSet())
        {
            EditorGUILayout.HelpBox(
                "Assign all fields above to enable preview and randomization.",
                MessageType.Info);
            return;
        }

        EditorGUILayout.Space(6);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ColorField("Skin Color",    _currentSkin);
            EditorGUILayout.ColorField("Hair Color",    _currentHair);
            EditorGUILayout.ColorField("Clothes Color", _currentClothes);
        }

        EditorGUILayout.Space(4);
        
        if (GUILayout.Button("Apply Texture", GUILayout.Height(30)))
        {
            RebuildPreviewMaterial();
            Repaint();
        }

        if (GUILayout.Button("🎲  Randomize Colors", GUILayout.Height(30)))
        {
            PickRandomColors();
            ApplyColorsToPreview();
            Repaint();
        }
    }
    // ── Preview ────────────────────────────────────────────────────────────

    public override bool HasPreviewGUI() => AllFieldsSet() && _previewMaterial != null;

    public override GUIContent GetPreviewTitle() => new GUIContent("Character Preview");

    public override void OnPreviewGUI(Rect rect, GUIStyle background)
    {
        HandleDrag(rect);

        if (Event.current.type != EventType.Repaint) return;
        if (_previewMaterial == null) return;

        EnsurePreviewUtil();

        _previewUtil.BeginPreview(rect, background);

        // Light setup
        _previewUtil.lights[0].transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        _previewUtil.lights[0].intensity = 1f;
        _previewUtil.lights[0].color = Color.white;

        // Camera — ortho-style close framing for a flat quad
        var cam = _previewUtil.camera;
        cam.orthographic = true;
        cam.orthographicSize = _zoom;
        cam.transform.position = new Vector3(0f, 0f, -2f);
        cam.transform.LookAt(Vector3.zero);
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 10f;

        Quaternion rotation = Quaternion.Euler(_drag.y, _drag.x, 0f);
        _previewUtil.DrawMesh(GetQuadMesh(), Vector3.zero, rotation, _previewMaterial, 0);
        _previewUtil.camera.Render();

        Texture result = _previewUtil.EndPreview();
        GUI.DrawTexture(rect, result, ScaleMode.StretchToFill, false);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private bool AllFieldsSet()
    {
        var t = Target;
        return t.lineArtTexture  != null
            && t.colorTexture    != null
            && t.skinColors      != null && t.skinColors.colors    != null && t.skinColors.colors.Length    > 0
            && t.hairColors      != null && t.hairColors.colors    != null && t.hairColors.colors.Length    > 0
            && t.clothesColors   != null && t.clothesColors.colors != null && t.clothesColors.colors.Length > 0
            && t.baseMaterial    != null;
    }

    private void RebuildPreviewMaterial()
    {
        if (_previewMaterial != null)
        {
            DestroyImmediate(_previewMaterial);
            _previewMaterial = null;
        }

        if (!AllFieldsSet()) return;

        _previewMaterial = new Material(Target.baseMaterial);

        // Always force-apply — never rely on inheritance from baseMaterial
        ApplyTexturesToPreview();

        // Seed colors from index 0 if not yet randomized
        _currentSkin    = Target.skinColors.colors[0];
        _currentHair    = Target.hairColors.colors[0];
        _currentClothes = Target.clothesColors.colors[0];

        ApplyColorsToPreview();
    }

    private void ApplyColorsToPreview()
    {
        if (_previewMaterial == null) return;

        _previewMaterial.SetColor("_SkinColor",    _currentSkin);
        _previewMaterial.SetColor("_HairColor",    _currentHair);
        _previewMaterial.SetColor("_ClothesColor", _currentClothes);
        _previewMaterial.SetColor("_LineColor",    Target.lineColor);
    }

    private void PickRandomColors()
    {
        _currentSkin    = RandomFrom(Target.skinColors.colors);
        _currentHair    = RandomFrom(Target.hairColors.colors);
        _currentClothes = RandomFrom(Target.clothesColors.colors);
    }

    private static Color RandomFrom(Color[] palette)
        => palette[Random.Range(0, palette.Length)];

    // ── Drag ───────────────────────────────────────────────────────────────

    private void HandleDrag(Rect rect)
    {
        int id = GUIUtility.GetControlID(FocusType.Passive);
        Event e = Event.current;

        switch (e.GetTypeForControl(id))
        {
            case EventType.MouseDown when rect.Contains(e.mousePosition):
                GUIUtility.hotControl = id;
                _isDragging = true;
                e.Use();
                break;
            case EventType.MouseDrag when _isDragging:
                _drag += e.delta;
                Repaint();
                e.Use();
                break;
            case EventType.MouseUp:
                if (GUIUtility.hotControl == id) GUIUtility.hotControl = 0;
                _isDragging = false;
                break;
            case EventType.ScrollWheel when rect.Contains(e.mousePosition):
                _zoom = Mathf.Clamp(_zoom + e.delta.y * 0.05f, ZoomMin, ZoomMax);
                Repaint();
                e.Use();
                break;
        }
    }

    // ── PreviewRenderUtility ───────────────────────────────────────────────

    private void EnsurePreviewUtil()
    {
        if (_previewUtil != null) return;
        _previewUtil = new PreviewRenderUtility();
    }

    private void CleanupPreview()
    {
        _previewUtil?.Cleanup();
        _previewUtil = null;

        if (_previewMaterial != null)
            DestroyImmediate(_previewMaterial);
        _previewMaterial = null;
    }

    // ── Quad mesh ──────────────────────────────────────────────────────────

    private static Mesh _quadMesh;

    private static Mesh GetQuadMesh()
    {
        if (_quadMesh != null) return _quadMesh;

        _quadMesh = new Mesh { name = "PreviewQuad" };

        _quadMesh.vertices = new[]
        {
            new Vector3(-1f, -1f, 0f),
            new Vector3( 1f, -1f, 0f),
            new Vector3( 1f,  1f, 0f),
            new Vector3(-1f,  1f, 0f),
        };

        _quadMesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
        };

        _quadMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        _quadMesh.RecalculateNormals();
        _quadMesh.RecalculateBounds();

        return _quadMesh;
    }
    
    private void ApplyTexturesToPreview()
    {
        if (_previewMaterial == null) return;

        _previewMaterial.SetTexture("_ColorTexture", Target.colorTexture);
        _previewMaterial.SetTexture("_LineTexture",  Target.lineArtTexture);
    }
}