using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Otowa.HotSpring;

public static class HotSpringPrototypeBuilder
{
    private const string DAY1_SCENE_PATH = "Assets/Scenes/Day1World.unity";
    private const string HOT_SPRING_SCENE_PATH = "Assets/Scenes/Day1HotSpring.unity";
    private const string FONT_PATH =
        "Assets/TextMesh Pro/Fonts/CormorantGaramond-VariableFont_wght SDF.asset";

    [MenuItem("Tools/Map/Build Hot Spring Prototype")]
    public static void Build()
    {
        BuildHotSpringScene();
        AddEntranceToDay1Map();
        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[HotSpringPrototypeBuilder] Built HotSpring scene and Day1World entrance.");
    }

    private static void BuildHotSpringScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateMainCamera();
        var controllerObject = new GameObject("HotSpringController");
        var controller = controllerObject.AddComponent<HotSpringController>();

        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("serifFont").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, HOT_SPRING_SCENE_PATH);
    }

    private static void CreateMainCamera()
    {
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        var camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;
        camera.orthographicSize = 5f;
    }

    private static void AddEntranceToDay1Map()
    {
        var scene = EditorSceneManager.OpenScene(DAY1_SCENE_PATH, OpenSceneMode.Single);
        var existing = GameObject.Find("HotSpring Entrance");
        if (existing != null)
            Object.DestroyImmediate(existing);

        var izakaya = GameObject.Find("Izakaya");
        var entrance = new GameObject("HotSpring Entrance");
        entrance.transform.position = izakaya != null
            ? izakaya.transform.position + new Vector3(-5.2f, -1.6f, 0f)
            : new Vector3(9f, -1.2f, 0f);

        var npc = GameObject.Find("Mizuki");
        if (npc != null)
            entrance.layer = npc.layer;

        var renderer = entrance.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        renderer.color = new Color(0.35f, 0.72f, 0.92f, 0.72f);
        renderer.sortingOrder = 30;
        var spriteSize = renderer.sprite.bounds.size;
        entrance.transform.localScale = new Vector3(3f / spriteSize.x, 2.5f / spriteSize.y, 1f);

        var collider = entrance.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(
            4f / entrance.transform.localScale.x,
            3.5f / entrance.transform.localScale.y);
        collider.isTrigger = true;
        entrance.AddComponent<HotSpringEntrance>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, DAY1_SCENE_PATH);
    }

    private static void EnsureBuildSettings()
    {
        var scenePaths = EditorBuildSettings.scenes.Select(scene => scene.path).ToList();
        AddSceneIfMissing(scenePaths, DAY1_SCENE_PATH);
        AddSceneIfMissing(scenePaths, HOT_SPRING_SCENE_PATH);

        EditorBuildSettings.scenes = scenePaths
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();
    }

    private static void AddSceneIfMissing(System.Collections.Generic.List<string> scenePaths,
                                          string scenePath)
    {
        if (!scenePaths.Contains(scenePath))
            scenePaths.Add(scenePath);
    }
}
