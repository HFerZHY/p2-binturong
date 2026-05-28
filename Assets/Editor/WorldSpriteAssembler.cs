#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Tools > World Sprite Assembler
// Drag a character sheet from Assets/Resources/Characters/WorldSprite,
// then click either button below.
public class WorldSpriteAssembler : EditorWindow
{
    // ── Part layout (positions extracted from existing NPCs in WorldScene) ────
    // Each entry: (child name, sprite index 0-5, local position, sorting order)
    private static readonly (string name, int spriteIdx, Vector3 pos, int order)[] Parts =
    {
        ("back leg",  5, new Vector3( 0.45f, -1.30f, 0f),  4),
        ("back arm",  3, new Vector3( 0.54f,  0.98f, 0f),  3),
        ("torso",     2, new Vector3( 0.00f, -0.06f, 0f),  5),
        ("front leg", 4, new Vector3(-0.45f, -1.51f, 0f),  6),
        ("head",      0, new Vector3( 0.17f,  1.07f, 0f), 10),
        ("front arm", 1, new Vector3(-0.81f,  0.88f, 0f),  8),
    };

    // ── Window state ──────────────────────────────────────────────────────────
    private Texture2D _sheet;
    private RuntimeAnimatorController _controller;
    private bool _saveAsPrefab = true;

    [MenuItem("Tools/World Sprite Assembler")]
    public static void ShowWindow() =>
        GetWindow<WorldSpriteAssembler>("World Sprite Assembler");

    private void OnEnable()
    {
        _controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Resources/Characters/WorldSprite/Rig.controller");
    }

    // ── Inspector GUI ─────────────────────────────────────────────────────────
    private void OnGUI()
    {
        GUILayout.Label("World Sprite Assembler", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        _sheet = (Texture2D)EditorGUILayout.ObjectField(
            new GUIContent("Character Sheet",
                "PNG from Assets/Resources/Characters/WorldSprite"),
            _sheet, typeof(Texture2D), false);

        _controller = (RuntimeAnimatorController)EditorGUILayout.ObjectField(
            new GUIContent("Animator Controller",
                "Defaults to WorldSprite/Rig.controller"),
            _controller, typeof(RuntimeAnimatorController), false);

        _saveAsPrefab = EditorGUILayout.Toggle(
            new GUIContent("Save Prefab",
                "Saves to Assets/Prefabs/Characters/{name}.prefab"),
            _saveAsPrefab);

        EditorGUILayout.Space(6);

        GUI.enabled = _sheet != null;

        if (GUILayout.Button("Assemble Character (Rig)", GUILayout.Height(28)))
            Assemble();

        EditorGUILayout.Space(2);

        if (GUILayout.Button("Save Flat Portrait PNG", GUILayout.Height(28)))
            SavePortraitPNG();

        GUI.enabled = true;

        EditorGUILayout.Space(4);
        if (_sheet == null)
            EditorGUILayout.HelpBox(
                "Drag a character PNG from Assets/Resources/Characters/WorldSprite.",
                MessageType.Info);
        else
            EditorGUILayout.HelpBox(
                "Portrait PNG saves next to the source sheet as {name}_portrait.png.",
                MessageType.Info);
    }

    // ── Rig assembly ──────────────────────────────────────────────────────────
    private void Assemble()
    {
        string path = AssetDatabase.GetAssetPath(_sheet);
        Sprite[] sprites = LoadSprites(path);
        if (sprites == null) return;

        string charName = Path.GetFileNameWithoutExtension(path);

        var rig = new GameObject(charName);
        Undo.RegisterCreatedObjectUndo(rig, $"Create {charName} rig");
        rig.transform.localScale = new Vector3(0.2f, 0.2f, 1f);

        if (_controller != null)
            rig.AddComponent<Animator>().runtimeAnimatorController = _controller;

        var body = new GameObject("Body");
        body.transform.SetParent(rig.transform, false);

        foreach (var (name, spriteIdx, pos, order) in Parts)
        {
            var part = new GameObject(name);
            part.transform.SetParent(body.transform, false);
            part.transform.localPosition = pos;
            var sr = part.AddComponent<SpriteRenderer>();
            sr.sprite = sprites[spriteIdx];
            sr.sortingOrder = order;
        }

        if (_saveAsPrefab)
        {
            const string dir = "Assets/Prefabs/Characters";
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/Prefabs", "Characters");

            string prefabPath = $"{dir}/{charName}.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(
                rig, prefabPath, InteractionMode.UserAction);
            AssetDatabase.SaveAssets();
        }

        Selection.activeGameObject = rig;
        EditorUtility.DisplayDialog("Done", $"{charName} rig assembled!", "OK");
    }

    // ── Flat portrait PNG export ──────────────────────────────────────────────
    private void SavePortraitPNG()
    {
        string srcPath = AssetDatabase.GetAssetPath(_sheet);
        var importer = (TextureImporter)AssetImporter.GetAtPath(srcPath);

        Sprite[] sprites = LoadSprites(srcPath);
        if (sprites == null) return;

        // Composite back-to-front (ascending sorting order = back drawn first)
        var ordered = Parts.OrderBy(p => p.order).ToArray();

        bool wasReadable = importer.isReadable;
        if (!wasReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        try
        {
            Texture2D src = AssetDatabase.LoadAssetAtPath<Texture2D>(srcPath);
            const float PPU = 100f;

            // ── Compute bounding box (rig space, Y-up) ────────────────────────
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var (_, idx, pos, _) in ordered)
            {
                Sprite sp = sprites[idx];
                float left   = pos.x - sp.pivot.x / PPU;
                float bottom = pos.y - sp.pivot.y / PPU;
                float right  = left   + sp.textureRect.width  / PPU;
                float top    = bottom + sp.textureRect.height / PPU;

                if (left   < minX) minX = left;
                if (bottom < minY) minY = bottom;
                if (right  > maxX) maxX = right;
                if (top    > maxY) maxY = top;
            }

            int dstW = Mathf.RoundToInt((maxX - minX) * PPU);
            int dstH = Mathf.RoundToInt((maxY - minY) * PPU);

            // ── Composite in Unity texture space (Y=0 at bottom) ─────────────
            Color[] dst = new Color[dstW * dstH]; // all transparent
            Color[] srcPx = src.GetPixels();
            int srcW = src.width;

            foreach (var (_, idx, pos, _) in ordered)
            {
                Sprite sp = sprites[idx];
                Rect rect = sp.textureRect;

                int dstLeft   = Mathf.RoundToInt((pos.x - sp.pivot.x / PPU - minX) * PPU);
                int dstBottom = Mathf.RoundToInt((pos.y - sp.pivot.y / PPU - minY) * PPU);

                int rw = Mathf.RoundToInt(rect.width);
                int rh = Mathf.RoundToInt(rect.height);

                for (int y = 0; y < rh; y++)
                for (int x = 0; x < rw; x++)
                {
                    Color c = srcPx[(Mathf.RoundToInt(rect.y) + y) * srcW + Mathf.RoundToInt(rect.x) + x];
                    if (c.a < 0.005f) continue;

                    int dx = dstLeft + x;
                    int dy = dstBottom + y;
                    if ((uint)dx >= dstW || (uint)dy >= dstH) continue;

                    // Alpha-blend over existing pixel (painter's algorithm)
                    Color bg = dst[dy * dstW + dx];
                    dst[dy * dstW + dx] = new Color(
                        Mathf.Lerp(bg.r, c.r, c.a),
                        Mathf.Lerp(bg.g, c.g, c.a),
                        Mathf.Lerp(bg.b, c.b, c.a),
                        bg.a + c.a * (1f - bg.a));
                }
            }

            var tex = new Texture2D(dstW, dstH, TextureFormat.RGBA32, false);
            tex.SetPixels(dst);
            tex.Apply();

            // ── Save beside the source sheet ──────────────────────────────────
            string charName = Path.GetFileNameWithoutExtension(srcPath);
            string saveDir  = Path.GetDirectoryName(srcPath)!;
            string savePath = Path.Combine(saveDir, charName + "_portrait.png")
                                  .Replace('\\', '/');

            File.WriteAllBytes(savePath, tex.EncodeToPNG());
            DestroyImmediate(tex);

            AssetDatabase.Refresh();
            Debug.Log($"[WorldSpriteAssembler] Saved: {savePath}");
            EditorUtility.DisplayDialog("Saved!",
                $"Portrait saved to:\n{savePath}", "OK");
        }
        finally
        {
            if (!wasReadable)
            {
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
        }
    }

    // ── Shared sprite loader ──────────────────────────────────────────────────
    private static Sprite[] LoadSprites(string assetPath)
    {
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<Sprite>()
            .OrderBy(s =>
            {
                int last = s.name.LastIndexOf('_');
                return last >= 0 && int.TryParse(s.name[(last + 1)..], out int n) ? n : 99;
            })
            .ToArray();

        if (sprites.Length >= 6) return sprites;

        EditorUtility.DisplayDialog("Error",
            $"Expected 6 sprites, found {sprites.Length}.\n" +
            "Make sure the sheet uses WorldSpriteTemplate (SpriteMode = Multiple, 6 slices).",
            "OK");
        return null;
    }
}
#endif
