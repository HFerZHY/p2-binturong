using System.Collections.Generic;
using Otowa.Day3;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Day3HikaruArrivalSceneBuilder
{
    private const string SCENE_PATH = "Assets/Scenes/Day3HikaruArrival.unity";
    private const string MENU_PATH = "Tools/Day3/Build Hikaru Arrival Scene";

    [MenuItem(MENU_PATH)]
    public static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Day3HikaruArrival";

        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        cameraObject.GetComponent<Camera>().backgroundColor = Color.black;

        var controllerObject = new GameObject("Day3HikaruArrivalController");
        controllerObject.AddComponent<Day3HikaruArrivalController>();

        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        EnsureBuildSettings(SCENE_PATH);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Day3HikaruArrivalSceneBuilder] Built {SCENE_PATH}");
    }

    private static void EnsureBuildSettings(string scenePath)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var scene in scenes)
        {
            if (scene.path == scenePath)
                return;
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
