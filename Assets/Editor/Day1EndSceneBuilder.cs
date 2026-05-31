using System.Linq;
using Otowa.Day1End;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Day1EndSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/day1end.unity";
    private const string Day2ScenePath = "Assets/Scenes/ExhibitionDay2Scene.unity";
    private const string FontPath =
        "Assets/TextMesh Pro/Fonts/CormorantGaramond-VariableFont_wght SDF.asset";

    [MenuItem("Tools/Day1 End/Build Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateMainCamera();

        var controllerObject = new GameObject("Day1EndController");
        var controller = controllerObject.AddComponent<Day1EndController>();
        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("serifFont").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Day1EndSceneBuilder] Built day1end scene.");
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

    private static void EnsureBuildSettings()
    {
        var scenePaths = EditorBuildSettings.scenes.Select(scene => scene.path).ToList();
        AddSceneIfMissing(scenePaths, ScenePath);
        AddSceneIfMissing(scenePaths, Day2ScenePath);
        EditorBuildSettings.scenes = scenePaths
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();
    }

    private static void AddSceneIfMissing(System.Collections.Generic.List<string> paths, string path)
    {
        if (!paths.Contains(path))
            paths.Add(path);
    }
}
