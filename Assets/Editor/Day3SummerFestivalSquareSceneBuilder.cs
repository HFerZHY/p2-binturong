using System.Linq;
using DialogueSystem.NPC;
using Otowa.Day3;
using Otowa.HotSpring;
using Otowa.Inquiry;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Day3SummerFestivalSquareSceneBuilder
{
    private const string SOURCE_SCENE_PATH = "Assets/Scenes/Day1World.unity";
    private const string SCENE_PATH = "Assets/Scenes/Day3SummerFestivalSquare.unity";

    [MenuItem("Tools/Day3/Build Summer Festival Square Scene")]
    public static void BuildScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH) != null)
            AssetDatabase.DeleteAsset(SCENE_PATH);

        if (!AssetDatabase.CopyAsset(SOURCE_SCENE_PATH, SCENE_PATH))
        {
            Debug.LogError("[Day3SummerFestivalSquareSceneBuilder] Failed to copy Day1World.");
            return;
        }

        var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        RemoveInheritedComponents();
        ConfigureCharacters();
        ConfigureFlow();
        EnsureBuildSettings();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Day3SummerFestivalSquareSceneBuilder] Built {SCENE_PATH}");
    }

    private static void RemoveInheritedComponents()
    {
        DestroyAll<Day1MapNpcInquiryController>();
        DestroyAll<Day1MapAmbientDialogueController>();
        DestroyAll<Day1StationInquiryController>();
        DestroyAll<Day1MapFlowController>();
        DestroyAll<HotSpringEntrance>();

        var inheritedEntrance = FindSceneObject("HotSpring Entrance");
        if (inheritedEntrance != null)
            Object.DestroyImmediate(inheritedEntrance);

        var inheritedFlow = FindSceneObject("Day1 Map Flow");
        if (inheritedFlow != null)
            Object.DestroyImmediate(inheritedFlow);
    }

    private static void ConfigureCharacters()
    {
        ConfigureNpc("Junko", Day3FestivalNpc.Junko, new Vector3(0.00f, 3.15f, 0f));
        ConfigureNpc("Jiro", Day3FestivalNpc.Jiro, new Vector3(-2.52f, 1.32f, 0f));
        ConfigureNpc("Yuji", Day3FestivalNpc.Yuji, new Vector3(2.52f, 1.32f, 0f));
        ConfigureNpc("Mizuki", Day3FestivalNpc.Mizuki, new Vector3(-1.56f, -1.64f, 0f));
        ConfigureNpc("Rintaro", Day3FestivalNpc.Rintaro, new Vector3(1.56f, -1.64f, 0f));
        PlacePlayerNearSquare();
        SetActive("Inspector", false);
        RemoveHiddenInspectorMovement();
    }

    private static void RemoveHiddenInspectorMovement()
    {
        var inspector = FindSceneObject("Inspector");
        if (inspector == null)
            return;

        var movement = inspector.GetComponent<NPCMovement>();
        if (movement != null)
            Object.DestroyImmediate(movement);
    }

    private static void PlacePlayerNearSquare()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        player.transform.position = new Vector3(0f, -3.75f, 0f);
        EditorUtility.SetDirty(player.transform);
    }

    private static void ConfigureNpc(string objectName, Day3FestivalNpc npc, Vector3 position)
    {
        var gameObject = FindSceneObject(objectName);
        if (gameObject == null)
        {
            Debug.LogError($"[Day3SummerFestivalSquareSceneBuilder] Could not find '{objectName}'.");
            return;
        }

        gameObject.SetActive(true);
        gameObject.transform.position = position;
        var inheritedDialogue = gameObject.GetComponent<NPCDialogueController>();
        if (inheritedDialogue != null)
            Object.DestroyImmediate(inheritedDialogue);

        var movement = gameObject.GetComponent<NPCMovement>();
        if (movement != null)
            Object.DestroyImmediate(movement);

        var controller = gameObject.GetComponent<Day3FestivalNpcController>();
        if (controller == null)
            controller = gameObject.AddComponent<Day3FestivalNpcController>();
        controller.Configure(npc);
        EditorUtility.SetDirty(gameObject);
    }

    private static void ConfigureFlow()
    {
        var flowObject = FindSceneObject("Day3 Festival Flow");
        if (flowObject == null)
            flowObject = new GameObject("Day3 Festival Flow");

        if (flowObject.GetComponent<Day3FestivalFlowController>() == null)
            flowObject.AddComponent<Day3FestivalFlowController>();
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
