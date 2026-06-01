using System.Collections.Generic;
using System.Linq;
using Otowa.Inquiry;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Day2HotSpringPrototypeBuilder
{
    private const string Day2MapScenePath = "Assets/Scenes/Day2World.unity";
    private const string Day2HotSpringScenePath = "Assets/Scenes/Day2HotSpring.unity";
    private const string FontPath =
        "Assets/TextMesh Pro/Fonts/CormorantGaramond-VariableFont_wght SDF.asset";

    [MenuItem("Tools/Day2 Inquiry/Build Hot Spring Prototype")]
    public static void Build()
    {
        BuildHotSpringScene();
        AddEntranceToDay2Map();
        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Day2HotSpringPrototypeBuilder] Built Day2HotSpring and its map entrance.");
    }

    private static void BuildHotSpringScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateMainCamera();

        var controllerObject = new GameObject("Day2HotSpringController");
        var controller = controllerObject.AddComponent<Day2HotSpringController>();
        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("serifFont").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, Day2HotSpringScenePath);
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

    private static void AddEntranceToDay2Map()
    {
        var scene = EditorSceneManager.OpenScene(Day2MapScenePath, OpenSceneMode.Single);
        var existing = GameObject.Find("Day2 HotSpring Entrance");
        if (existing != null)
            Object.DestroyImmediate(existing);

        var izakaya = GameObject.Find("Izakaya");
        var entrance = new GameObject("Day2 HotSpring Entrance");
        entrance.transform.position = izakaya != null
            ? izakaya.transform.position + new Vector3(-5.2f, -1.6f, 0f)
            : new Vector3(9f, -1.2f, 0f);

        var npc = GameObject.Find("Yuji");
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

        var controller = entrance.AddComponent<Day2InteriorEntrance>();
        controller.Configure("Day2HotSpring");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, Day2MapScenePath);
    }

    private static void EnsureBuildSettings()
    {
        var scenePaths = EditorBuildSettings.scenes.Select(scene => scene.path).ToList();
        AddSceneIfMissing(scenePaths, Day2MapScenePath);
        AddSceneIfMissing(scenePaths, Day2HotSpringScenePath);
        EditorBuildSettings.scenes = scenePaths
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();
    }

    private static void AddSceneIfMissing(List<string> scenePaths, string scenePath)
    {
        if (!scenePaths.Contains(scenePath))
            scenePaths.Add(scenePath);
    }
}
