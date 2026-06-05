using System.Linq;
using DialogueSystem.NPC;
using Otowa.Day3;
using Otowa.HotSpring;
using Otowa.Inquiry;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Day3InspectorDecisionSceneBuilder
{
    private const string SOURCE_SCENE_PATH = "Assets/Scenes/Day2World.unity";
    private const string SCENE_PATH = "Assets/Scenes/Day3InspectorDecision.unity";
    private const string FONT_PATH =
        "Assets/TextMesh Pro/Fonts/CormorantGaramond-VariableFont_wght SDF.asset";

    [MenuItem("Tools/Day3/Build Inspector Decision Scene")]
    public static void BuildScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH) != null)
            AssetDatabase.DeleteAsset(SCENE_PATH);

        if (!AssetDatabase.CopyAsset(SOURCE_SCENE_PATH, SCENE_PATH))
        {
            Debug.LogError("[Day3InspectorDecisionSceneBuilder] Failed to copy Day2World.");
            return;
        }

        var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        RemoveInheritedComponents();
        ConfigureCharacters();
        ConfigureController();
        EnsureBuildSettings();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Day3InspectorDecisionSceneBuilder] Built {SCENE_PATH}");
    }

    private static void RemoveInheritedComponents()
    {
        DestroyAll<Day1MapNpcInquiryController>();
        DestroyAll<Day1MapAmbientDialogueController>();
        DestroyAll<Day1StationInquiryController>();
        DestroyAll<Day1MapFlowController>();
        DestroyAll<Day2MapNpcInquiryController>();
        DestroyAll<Day2StationInquiryController>();
        DestroyAll<Day2MapFlowController>();
        DestroyAll<HotSpringEntrance>();
        DestroyAll<Day2InteriorEntrance>();

        foreach (var objectName in new[] { "Day1 Map Flow", "Day2 Map Flow", "HotSpring Entrance", "Day2 HotSpring Entrance", "Day2 Ryotei Entrance" })
        {
            var inheritedObject = FindSceneObject(objectName);
            if (inheritedObject != null)
                Object.DestroyImmediate(inheritedObject);
        }
    }

    private static void ConfigureCharacters()
    {
        foreach (var npc in new[] { "Junko", "Jiro", "Yuji", "Mizuki", "Rintaro" })
            SetActive(npc, false);

        var inspector = FindSceneObject("Inspector");
        var player = GameObject.FindGameObjectWithTag("Player");
        if (inspector == null || player == null)
            return;

        inspector.SetActive(true);
        var dialogue = inspector.GetComponent<NPCDialogueController>();
        if (dialogue != null)
            Object.DestroyImmediate(dialogue);
        var movement = inspector.GetComponent<NPCMovement>();
        if (movement != null)
            Object.DestroyImmediate(movement);

        inspector.transform.position = player.transform.position + new Vector3(2.0f, 0f, 0f);
        var scale = inspector.transform.localScale;
        scale.x = -Mathf.Abs(scale.x);
        inspector.transform.localScale = scale;
        EditorUtility.SetDirty(inspector);
        EditorUtility.SetDirty(inspector.transform);
    }

    private static void ConfigureController()
    {
        var flowObject = FindSceneObject("Day3 Inspector Decision");
        if (flowObject == null)
            flowObject = new GameObject("Day3 Inspector Decision");

        var controller = flowObject.GetComponent<Day3InspectorDecisionController>();
        if (controller == null)
            controller = flowObject.AddComponent<Day3InspectorDecisionController>();

        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("_serifFont").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
        serializedController.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetActive(string objectName, bool active)
    {
        var gameObject = FindSceneObject(objectName);
        if (gameObject == null)
            return;

        gameObject.SetActive(active);
        EditorUtility.SetDirty(gameObject);
    }

    private static void DestroyAll<T>() where T : Component
    {
        foreach (var component in Object.FindObjectsByType<T>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
            Object.DestroyImmediate(component);
    }

    private static GameObject FindSceneObject(string objectName)
    {
        foreach (var transform in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (transform.name == objectName)
                return transform.gameObject;
        }

        return null;
    }

    private static void EnsureBuildSettings()
    {
        var paths = EditorBuildSettings.scenes.Select(scene => scene.path).ToList();
        if (!paths.Contains(SCENE_PATH))
            paths.Add(SCENE_PATH);
        EditorBuildSettings.scenes = paths
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();
    }
}
