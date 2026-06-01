using System.Collections.Generic;
using Otowa.Day3;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Day3NightTrainArrivalSceneBuilder
{
    private const string SCENE_PATH = "Assets/Scenes/Day3NightTrainArrival.unity";
    private const string FONT_PATH =
        "Assets/TextMesh Pro/Fonts/CormorantGaramond-VariableFont_wght SDF.asset";

    [MenuItem("Tools/Day3/Build Night Train Arrival Scene")]
    public static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        var camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;
        camera.orthographicSize = 5f;

        var controllerObject = new GameObject("Day3NightTrainArrivalController");
        var controller = controllerObject.AddComponent<Day3NightTrainArrivalController>();
        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("_font").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Day3NightTrainArrivalSceneBuilder] Built {SCENE_PATH}");
    }

    private static void EnsureBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var scene in scenes)
        {
            if (scene.path == SCENE_PATH)
                return;
        }

        scenes.Add(new EditorBuildSettingsScene(SCENE_PATH, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
