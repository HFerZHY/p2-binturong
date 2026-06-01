using System.Collections.Generic;
using System.Linq;
using Otowa.Inquiry;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Day2RyoteiPrototypeBuilder
{
    private const string Day2MapScenePath = "Assets/Scenes/Day2World.unity";
    private const string Day2RyoteiScenePath = "Assets/Scenes/Day2Ryotei.unity";
    private const string FontPath =
        "Assets/TextMesh Pro/Fonts/CormorantGaramond-VariableFont_wght SDF.asset";

    [MenuItem("Tools/Day2 Inquiry/Build Ryotei Prototype")]
    public static void Build()
    {
        BuildRyoteiScene();
        AddEntranceToDay2Map();
        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Day2RyoteiPrototypeBuilder] Built Day2Ryotei and its Izakaya entrance.");
    }

    private static void BuildRyoteiScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateMainCamera();

        var controllerObject = new GameObject("Day2RyoteiController");
        var controller = controllerObject.AddComponent<Day2RyoteiController>();
        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("serifFont").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, Day2RyoteiScenePath);
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
        var existing = GameObject.Find("Day2 Ryotei Entrance");
        if (existing != null)
            Object.DestroyImmediate(existing);

        var izakaya = GameObject.Find("Izakaya");
        var entrance = new GameObject("Day2 Ryotei Entrance");
        entrance.transform.position = izakaya != null
            ? izakaya.transform.position + new Vector3(0f, -2.4f, 0f)
            : new Vector3(14.2f, -2f, 0f);

        var npc = GameObject.Find("Yuji");
        if (npc != null)
            entrance.layer = npc.layer;

        var collider = entrance.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(4f, 3f);
        collider.isTrigger = true;

        var controller = entrance.AddComponent<Day2InteriorEntrance>();
        controller.Configure("Day2Ryotei");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, Day2MapScenePath);
    }

    private static void EnsureBuildSettings()
    {
        var scenePaths = EditorBuildSettings.scenes.Select(scene => scene.path).ToList();
        AddSceneIfMissing(scenePaths, Day2MapScenePath);
        AddSceneIfMissing(scenePaths, Day2RyoteiScenePath);
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
