using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Otowa.Intro;

/// <summary>
/// Assigns sound effect clips to intro scene controllers.
/// Run each item from the active scene, or use "Wire All Scenes" to batch-process.
/// Menu: Tools/Otowa/Audio
/// </summary>
public static class OtowaAudioSetup
{
    private const string SFX = "Assets/Audio/SoundEffects/";

    // ── Per-scene wiring ──────────────────────────────────────────────────────

    [MenuItem("Tools/Otowa/Audio/Wire Intro-1 Audio (IntroController)")]
    public static void WireIntro1()
    {
        var ctrl = Object.FindObjectOfType<IntroController>();
        if (ctrl == null) { Warn("IntroController", "Intro-1"); return; }

        var so = new SerializedObject(ctrl);
        Assign(so, "ambientClip",    SFX + "wind rustling through leaves.mp3");
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(ctrl);
        SaveScene();
        Debug.Log("[OtowaAudioSetup] Intro-1 audio wired.");
        EditorUtility.DisplayDialog("Done", "Intro-1 audio clips assigned.", "OK");
    }

    [MenuItem("Tools/Otowa/Audio/Wire Intro-3 Audio (StationController)")]
    public static void WireIntro3()
    {
        var ctrl = Object.FindObjectOfType<StationController>();
        if (ctrl == null) { Warn("StationController", "Intro-3"); return; }

        var so = new SerializedObject(ctrl);
        Assign(so, "ambientClip",  SFX + "faint insect chirp.mp3");
        Assign(so, "doorOpenClip", SFX + "wooden door open.mp3");
        Assign(so, "pageTurnClip", SFX + "page turn.mp3");
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(ctrl);
        SaveScene();
        Debug.Log("[OtowaAudioSetup] Intro-3 audio wired.");
        EditorUtility.DisplayDialog("Done",
            "Intro-3 audio clips assigned:\n" +
            "• Ambient: faint insect chirp\n" +
            "• Door open: wooden door open\n" +
            "• Page turn: page turn", "OK");
    }

    [MenuItem("Tools/Otowa/Audio/Wire Intro-5 Audio (RyoteiController)")]
    public static void WireIntro5()
    {
        var ctrl = Object.FindObjectOfType<RyoteiController>();
        if (ctrl == null) { Warn("RyoteiController", "Intro-5"); return; }

        var so = new SerializedObject(ctrl);
        Assign(so, "bgmClip",          SFX + "distant birdsong.mp3");
        Assign(so, "drinkPourClip",    SFX + "drink pour.mp3");
        Assign(so, "glassesToastClip", SFX + "glasses toast.mp3");
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(ctrl);
        SaveScene();
        Debug.Log("[OtowaAudioSetup] Intro-5 audio wired.");
        EditorUtility.DisplayDialog("Done",
            "Intro-5 audio clips assigned:\n" +
            "• BGM: distant birdsong\n" +
            "• Drink pour: drink pour\n" +
            "• Glasses toast: glasses toast", "OK");
    }

    // ── Day1World ─────────────────────────────────────────────────────────────

    [MenuItem("Tools/Otowa/Audio/Wire Day1World Audio (WorldBGM)")]
    public static void WireDay1World()
    {
        var ctrl = Object.FindObjectOfType<WorldBGM>();
        if (ctrl == null)
        {
            EditorUtility.DisplayDialog("Not Found",
                "No WorldBGM component found in the active scene.\n\n" +
                "Create an empty GameObject, attach the WorldBGM script, then run this again.", "OK");
            return;
        }

        var so = new SerializedObject(ctrl);
        Assign(so, "bgmClip", SFX + "Blues beat.mp3");
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(ctrl);
        SaveScene();
        Debug.Log("[OtowaAudioSetup] Day1World audio wired.");
        EditorUtility.DisplayDialog("Done", "Day1World BGM assigned:\n• Blues beat.mp3", "OK");
    }

    // ── InspirationManager prefab ─────────────────────────────────────────────

    [MenuItem("Tools/Otowa/Audio/Wire InspirationManager Audio")]
    public static void WireInspirationManager()
    {
        const string prefabPath = "Assets/Resources/InspirationManager.prefab";

        // Ensure Resources folder exists
        if (!System.IO.Directory.Exists("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        // Load or create the prefab
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing == null)
        {
            var temp = new GameObject("InspirationManager");
            temp.AddComponent<InspirationManager>();
            existing = PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
            Object.DestroyImmediate(temp);
        }

        var so = new SerializedObject(existing.GetComponent<InspirationManager>());
        Assign(so, "inspirationUnlockedClip", SFX + "inspiration unlocked short.mp3");
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(existing);
        AssetDatabase.SaveAssets();

        Debug.Log("[OtowaAudioSetup] InspirationManager prefab wired.");
        EditorUtility.DisplayDialog("Done",
            "InspirationManager prefab created/updated at:\n" +
            "Assets/Resources/InspirationManager.prefab\n\n" +
            "• Inspiration unlocked SFX: inspiration unlocked.mp3", "OK");
    }

    // ── Batch ─────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Otowa/Audio/Wire All Intro Scenes")]
    public static void WireAll()
    {
        var originalScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;

        WireScene("Assets/Scenes/Intro-1.unity", () =>
        {
            var c = Object.FindObjectOfType<IntroController>();
            if (c == null) return;
            var so = new SerializedObject(c);
            Assign(so, "ambientClip", SFX + "wind rustling through leaves.mp3");
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(c);
        });

        WireScene("Assets/Scenes/Intro-3.unity", () =>
        {
            var c = Object.FindObjectOfType<StationController>();
            if (c == null) return;
            var so = new SerializedObject(c);
            Assign(so, "ambientClip",  SFX + "faint insect chirp.mp3");
            Assign(so, "doorOpenClip", SFX + "wooden door open.mp3");
            Assign(so, "pageTurnClip", SFX + "page turn.mp3");
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(c);
        });

        WireScene("Assets/Scenes/Intro-5.unity", () =>
        {
            var c = Object.FindObjectOfType<RyoteiController>();
            if (c == null) return;
            var so = new SerializedObject(c);
            Assign(so, "bgmClip",          SFX + "distant birdsong.mp3");
            Assign(so, "drinkPourClip",    SFX + "drink pour.mp3");
            Assign(so, "glassesToastClip", SFX + "glasses toast.mp3");
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(c);
        });

        if (!string.IsNullOrEmpty(originalScene))
            EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);

        WireInspirationManager();

        AssetDatabase.SaveAssets();
        Debug.Log("[OtowaAudioSetup] All intro scenes wired.");
        EditorUtility.DisplayDialog("Done",
            "Audio wired in:\n• Intro-1\n• Intro-3\n• Intro-5\n• InspirationManager prefab\n\nReturned to original scene.", "OK");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void WireScene(string scenePath, System.Action action)
    {
        if (!System.IO.File.Exists(scenePath))
        {
            Debug.LogWarning($"[OtowaAudioSetup] Scene not found: {scenePath}");
            return;
        }
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        action();
        SaveScene();
    }

    private static void Assign(SerializedObject so, string propName, string assetPath)
    {
        var prop = so.FindProperty(propName);
        if (prop == null)
        {
            Debug.LogWarning($"[OtowaAudioSetup] Property '{propName}' not found.");
            return;
        }
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        if (clip == null)
            Debug.LogWarning($"[OtowaAudioSetup] Audio clip not found: {assetPath}");
        else
            prop.objectReferenceValue = clip;
    }

    private static void SaveScene()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void Warn(string componentName, string expectedScene)
    {
        EditorUtility.DisplayDialog("Not Found",
            $"{componentName} not found in the active scene.\n\nOpen {expectedScene} first.", "OK");
    }
}
