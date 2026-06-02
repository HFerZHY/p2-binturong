using System.Linq;
using Otowa.Day2End;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Day2EndSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/day2end.unity";
    private const string Day3ScenePath = "Assets/Scenes/ExhibitionDay3Scene.unity";

    [MenuItem("Tools/Day2 Inquiry/Build End Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildCamera();
        new GameObject("Day2EndController").AddComponent<Day2EndController>();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Day2EndSceneBuilder] Built day2end scene.");
    }

    private static void BuildCamera()
    {
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
    }

    private static void EnsureBuildSettings()
    {
        var scenePaths = EditorBuildSettings.scenes.Select(scene => scene.path).ToList();
        if (!scenePaths.Contains(ScenePath))
            scenePaths.Add(ScenePath);
        if (!scenePaths.Contains(Day3ScenePath))
            scenePaths.Add(Day3ScenePath);

        EditorBuildSettings.scenes = scenePaths
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();
    }
}
