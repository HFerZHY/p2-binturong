using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Otowa.Intro;
using Otowa.Minimap;

/// <summary>
/// Editor utilities for setting up Otowa scenes.
/// Access via Tools → Otowa in the Unity menu bar.
/// </summary>
public static class OtowaSetupTools
{
    // ── Ryotei ────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Otowa/1 - Fix Ryotei Entrance")]
    public static void FixRyoteiEntrance()
    {
        var ryotei = GameObject.Find("Ryotei");
        if (ryotei == null)
        {
            EditorUtility.DisplayDialog("Not Found",
                "No GameObject named 'Ryotei' in the active scene.\n" +
                "Make sure TutorialToRyotei is the open scene.", "OK");
            return;
        }

        // Copy layer from an existing NPC so PlayerInteractor can detect it
        int targetLayer = CopyLayerFromNpc(ryotei.layer);
        ryotei.layer = targetLayer;

        // Ensure a visible red sprite
        var sr = ryotei.GetComponent<SpriteRenderer>();
        if (sr == null) sr = ryotei.AddComponent<SpriteRenderer>();
        if (sr.sprite == null)
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.color = new Color(0.85f, 0.12f, 0.12f, 0.80f);
        sr.sortingOrder = 30;

        // Scale to ~3x2.5 world units if untouched
        if (Mathf.Approximately(ryotei.transform.localScale.x, 1f))
        {
            var spriteSize = sr.sprite.bounds.size;
            ryotei.transform.localScale = new Vector3(
                3f / spriteSize.x,
                2.5f / spriteSize.y, 1f);
        }

        // Fix collider — size in local space so world size stays ~4x3.5 units
        var col = ryotei.GetComponent<BoxCollider2D>();
        if (col == null) col = ryotei.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(
            4f / ryotei.transform.localScale.x,
            3.5f / ryotei.transform.localScale.y);

        // Ensure RyoteiEntrance script
        if (ryotei.GetComponent<RyoteiEntrance>() == null)
            ryotei.AddComponent<RyoteiEntrance>();

        MarkDirtyAndSave();
        Debug.Log($"[OtowaSetupTools] Ryotei fixed — layer: {LayerMask.LayerToName(targetLayer)}");
        EditorUtility.DisplayDialog("Done",
            $"Ryotei entrance fixed!\nLayer: {LayerMask.LayerToName(targetLayer)}\n" +
            "Collider: 4×3.5 world units, Is Trigger ✓\nRyoteiEntrance script attached.", "OK");
    }

    // ── Minimap ───────────────────────────────────────────────────────────────

    [MenuItem("Tools/Otowa/2 - Add Minimap To Scene")]
    public static void AddMinimapToScene()
    {
        var existing = Object.FindObjectOfType<MinimapController>();
        MinimapController controller;

        if (existing != null)
        {
            controller = existing;
            Debug.Log("[OtowaSetupTools] MinimapController found — updating values.");
        }
        else
        {
            var go = new GameObject("MinimapManager");
            controller = go.AddComponent<MinimapController>();
            Undo.RegisterCreatedObjectUndo(go, "Add MinimapManager");
            Selection.activeGameObject = go;
        }

        var so = new SerializedObject(controller);
        so.FindProperty("worldMin").vector2Value   = new Vector2(-22f, -18f);
        so.FindProperty("worldMax").vector2Value   = new Vector2( 22f,  14f);
        so.FindProperty("panelSize").vector2Value  = new Vector2(820f, 600f);
        so.FindProperty("panelAlpha").floatValue   = 0.70f;
        so.FindProperty("iconSize").vector2Value   = new Vector2(40f, 40f);
        so.FindProperty("locationSize").vector2Value = new Vector2(32f, 32f);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);

        MarkDirtyAndSave();
        Debug.Log("[OtowaSetupTools] MinimapManager configured.");
        EditorUtility.DisplayDialog("Done",
            "MinimapManager configured!\n\n" +
            "Size: 820×600  |  Alpha: 0.70\n" +
            "World bounds: X(−22 → 22), Y(−18 → 14)\n\n" +
            "Adjust World Min/Max if icons appear off the map.\n" +
            "Run step 3 next to add icons to characters.", "OK");
    }

    [MenuItem("Tools/Otowa/3 - Add Minimap Icons To Characters")]
    public static void AddMinimapIconsToCharacters()
    {
        int count = 0;

        // Player
        count += ApplyMinimapIcon("TestPlayer", "Rin",      "rin_portrait",       new Color(0.56f, 0.74f, 0.56f));
        // NPCs
        count += ApplyMinimapIcon("Junko",      "Junko",    "Junko_portrait",     new Color(0.83f, 0.63f, 0.38f));
        count += ApplyMinimapIcon("Yuji",       "Yuji",     "Yuji_portrait",      new Color(0.50f, 0.72f, 0.91f));
        count += ApplyMinimapIcon("Jiro",       "Jiro",     "Jiro_portrait",      new Color(0.60f, 0.60f, 0.66f));
        count += ApplyMinimapIcon("Mizuki",     "Mizuki",   "Mizuki_portrait",    new Color(0.85f, 0.65f, 0.75f));
        count += ApplyMinimapIcon("Rintaro",    "Rintaro",  "Rintaro_portrait",   Color.white);
        count += ApplyMinimapIcon("Inspector",  "Inspector","Inspector_portrait", new Color(0.63f, 0.66f, 0.75f));

        MarkDirtyAndSave();
        Debug.Log($"[OtowaSetupTools] MinimapIcon applied to {count} characters.");
        EditorUtility.DisplayDialog("Done",
            $"MinimapIcon added/updated on {count} character(s).\n\n" +
            "Characters not found in this scene are skipped — run again in other scenes as needed.", "OK");
    }

    // ── Tutorial ──────────────────────────────────────────────────────────────

    [MenuItem("Tools/Otowa/4 - Add Tutorial Dialogue Trigger")]
    public static void AddTutorialTrigger()
    {
        if (Object.FindObjectOfType<TutorialDialogueTrigger>() != null)
        {
            EditorUtility.DisplayDialog("Already Present",
                "TutorialDialogueTrigger already exists in this scene.", "OK");
            return;
        }

        var go = new GameObject("TutorialTrigger");
        go.AddComponent<TutorialDialogueTrigger>();

        Undo.RegisterCreatedObjectUndo(go, "Add TutorialTrigger");
        Selection.activeGameObject = go;

        MarkDirtyAndSave();
        Debug.Log("[OtowaSetupTools] TutorialTrigger added.");
        EditorUtility.DisplayDialog("Done", "TutorialTrigger added to scene.", "OK");
    }

    // ── All-in-one ────────────────────────────────────────────────────────────

    [MenuItem("Tools/Otowa/5 - Add Minimap Location Markers")]
    public static void AddMinimapLocationMarkers()
    {
        int count = 0;
        count += ApplyLocationMarker("Ryotei",           "Ryotei",      new Color(0.85f, 0.12f, 0.12f));
        count += ApplyLocationMarker("HotSpring Entrance","Hot Spring",  new Color(0.35f, 0.72f, 0.92f));

        MarkDirtyAndSave();
        Debug.Log($"[OtowaSetupTools] MinimapLocationMarker applied to {count} location(s).");
        EditorUtility.DisplayDialog("Done",
            $"MinimapLocationMarker added/updated on {count} location(s).\n" +
            "Locations not in this scene are skipped.", "OK");
    }

    [MenuItem("Tools/Otowa/0 - Setup Tutorial Scene (All Steps)")]
    public static void SetupTutorialScene()
    {
        FixRyoteiEntrance();
        AddMinimapToScene();
        AddMinimapIconsToCharacters();
        AddMinimapLocationMarkers();
        AddTutorialTrigger();

        MarkDirtyAndSave();
        Debug.Log("[OtowaSetupTools] Full tutorial scene setup complete.");
        EditorUtility.DisplayDialog("Setup Complete",
            "All steps complete!\n\n" +
            "1. Ryotei entrance fixed\n" +
            "2. MinimapManager added (adjust World Min/Max in Inspector)\n" +
            "3. MinimapIcon added to all characters found in scene\n" +
            "4. MinimapLocationMarker added to Ryotei + HotSpring\n" +
            "5. TutorialTrigger added\n\n" +
            "Run in both TutorialToRyotei and Day1World scenes.", "OK");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int CopyLayerFromNpc(int fallback)
    {
        foreach (string npcName in new[] { "Mizuki", "Yuji", "Jiro", "Junko", "Rintaro" })
        {
            var npc = GameObject.Find(npcName);
            if (npc != null) return npc.layer;
        }
        return fallback;
    }

    private static int ApplyMinimapIcon(string goName, string displayName,
                                        string portraitFile, Color fallbackColor)
    {
        var go = GameObject.Find(goName);
        if (go == null) return 0;

        var icon = go.GetComponent<MinimapIcon>() ?? Undo.AddComponent<MinimapIcon>(go);

        var so = new SerializedObject(icon);
        so.FindProperty("displayName").stringValue  = displayName;
        so.FindProperty("iconColor").colorValue     = fallbackColor;

        var portrait = AssetDatabase.LoadAssetAtPath<Sprite>(
            $"Assets/Resources/Characters/WorldSprite/{portraitFile}.png");
        if (portrait != null)
            so.FindProperty("portrait").objectReferenceValue = portrait;
        else
            Debug.LogWarning($"[OtowaSetupTools] Portrait not found: {portraitFile}.png");

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(go);
        return 1;
    }

    private static int ApplyLocationMarker(string goName, string displayName, Color color)
    {
        var go = GameObject.Find(goName);
        if (go == null) return 0;

        var marker = go.GetComponent<MinimapLocationMarker>() ?? Undo.AddComponent<MinimapLocationMarker>(go);

        var so = new SerializedObject(marker);
        so.FindProperty("locationName").stringValue  = displayName;
        so.FindProperty("markerColor").colorValue    = color;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(go);
        return 1;
    }

    private static void MarkDirtyAndSave()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }
}
