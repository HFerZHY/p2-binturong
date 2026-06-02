using System.Collections.Generic;
using System.Linq;
using Otowa.Intro;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StartMenuSceneBuilder
{
    private const string SCENE_PATH = "Assets/Scenes/StartMenu.unity";

    [MenuItem("Tools/Otowa/Build Start Menu Scene")]
    public static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        var controller = new GameObject("Start Menu").AddComponent<StartMenuController>();
        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("_serifFont").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Fonts/CormorantGaramond-VariableFont_wght SDF.asset");
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        EnsureFirstBuildScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[StartMenuSceneBuilder] Built {SCENE_PATH}");
    }

    private static void EnsureFirstBuildScene()
    {
        var paths = new List<string> { SCENE_PATH };
        paths.AddRange(EditorBuildSettings.scenes
            .Select(scene => scene.path)
            .Where(path => path != SCENE_PATH));
        EditorBuildSettings.scenes = paths
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();
    }
}
