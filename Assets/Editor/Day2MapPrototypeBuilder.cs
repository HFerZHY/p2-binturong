using System.Linq;
using DialogueSystem.NPC;
using Otowa.HotSpring;
using Otowa.Inquiry;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Day2MapPrototypeBuilder
{
    private const string Day1ScenePath = "Assets/Scenes/Day1World.unity";
    private const string Day2ScenePath = "Assets/Scenes/Day2World.unity";

    [MenuItem("Tools/Day2 Inquiry/Build Map Prototype")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Day2ScenePath) == null
            && !AssetDatabase.CopyAsset(Day1ScenePath, Day2ScenePath))
        {
            Debug.LogError("[Day2MapPrototypeBuilder] Failed to copy Day1World.");
            return;
        }

        var scene = EditorSceneManager.OpenScene(Day2ScenePath, OpenSceneMode.Single);
        RemoveDay1Components();
        ConfigureMapCharacters();
        ConfigureStation();
        ConfigureFlowController();
        EnsureBuildSettings();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Day2MapPrototypeBuilder] Built Day2World from the Day1 map.");
    }

    private static void RemoveDay1Components()
    {
        foreach (var component in Object.FindObjectsByType<Day1MapNpcInquiryController>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(component);

        foreach (var component in Object.FindObjectsByType<Day1MapAmbientDialogueController>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(component);

        foreach (var component in Object.FindObjectsByType<Day1StationInquiryController>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(component);

        foreach (var component in Object.FindObjectsByType<HotSpringEntrance>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(component);

        foreach (var component in Object.FindObjectsByType<Day1MapFlowController>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (component.gameObject.name == "Day1 Map Flow")
                Object.DestroyImmediate(component.gameObject);
            else
                Object.DestroyImmediate(component);
        }
    }

    private static void ConfigureMapCharacters()
    {
        SetActive("Mizuki", false);
        SetActive("Jiro", false);
        SetActive("Inspector", false);
        SetActive("Yuji", true);
        SetActive("Junko", true);
        SetActive("Rintaro", true);
        ConfigureInquiryNpc("Yuji", Day2InquiryNpc.Yuji);
        ConfigureInquiryNpc("Junko", Day2InquiryNpc.Junko);
        ConfigureInquiryNpc("Rintaro", Day2InquiryNpc.Rintaro);
        PlacePlayerAtStationEntrance();
        PlaceInspectorBesidePlayer();
        RemoveInspectorMapBehaviour();

        var inheritedHotSpringEntrance = FindSceneObject("HotSpring Entrance");
        if (inheritedHotSpringEntrance != null)
            Object.DestroyImmediate(inheritedHotSpringEntrance);
    }

    private static void RemoveInspectorMapBehaviour()
    {
        var inspector = FindSceneObject("Inspector");
        if (inspector == null)
            return;

        var dialogue = inspector.GetComponent<NPCDialogueController>();
        if (dialogue != null)
            Object.DestroyImmediate(dialogue);

        var movement = inspector.GetComponent<NPCMovement>();
        if (movement != null)
            Object.DestroyImmediate(movement);
    }

    private static void ConfigureInquiryNpc(string objectName, Day2InquiryNpc npc)
    {
        var gameObject = FindSceneObject(objectName);
        if (gameObject == null)
        {
            Debug.LogWarning($"[Day2MapPrototypeBuilder] Could not configure '{objectName}'.");
            return;
        }

        var inheritedDialogue = gameObject.GetComponent<NPCDialogueController>();
        if (inheritedDialogue != null)
            Object.DestroyImmediate(inheritedDialogue);

        var controller = gameObject.GetComponent<Day2MapNpcInquiryController>();
        if (controller == null)
            controller = gameObject.AddComponent<Day2MapNpcInquiryController>();

        controller.Configure(npc);
        EditorUtility.SetDirty(gameObject);
    }

    private static void PlacePlayerAtStationEntrance()
    {
        var station = FindSceneObject("Train Station");
        var player = GameObject.FindGameObjectWithTag("Player");
        if (station == null || player == null)
            return;

        player.transform.position = station.transform.TransformPoint(new Vector3(-7f, -20f, 0f));
        EditorUtility.SetDirty(player.transform);
    }

    private static void PlaceInspectorBesidePlayer()
    {
        var inspector = FindSceneObject("Inspector");
        var player = GameObject.FindGameObjectWithTag("Player");
        if (inspector == null || player == null)
            return;

        inspector.transform.position = player.transform.position + new Vector3(1.8f, 0f, 0f);
        EditorUtility.SetDirty(inspector.transform);
    }

    private static void ConfigureFlowController()
    {
        var flowObject = FindSceneObject("Day2 Map Flow");
        if (flowObject == null)
            flowObject = new GameObject("Day2 Map Flow");

        if (flowObject.GetComponent<Day2MapFlowController>() == null)
            flowObject.AddComponent<Day2MapFlowController>();
    }

    private static void ConfigureStation()
    {
        var station = FindSceneObject("Train Station");
        if (station == null)
        {
            Debug.LogError("[Day2MapPrototypeBuilder] Could not find 'Train Station' in Day2World.");
            return;
        }

        var collider = station.GetComponent<BoxCollider2D>();
        if (collider == null)
            collider = station.AddComponent<BoxCollider2D>();

        collider.isTrigger = true;
        collider.offset = new Vector2(-7f, -15f);
        collider.size = new Vector2(10f, 8f);
        EditorUtility.SetDirty(collider);

        if (station.GetComponent<Day2StationInquiryController>() == null)
            station.AddComponent<Day2StationInquiryController>();
    }

    private static void EnsureBuildSettings()
    {
        var scenePaths = EditorBuildSettings.scenes.Select(scene => scene.path).ToList();
        if (!scenePaths.Contains(Day2ScenePath))
            scenePaths.Add(Day2ScenePath);

        EditorBuildSettings.scenes = scenePaths
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();
    }

    private static void SetActive(string objectName, bool active)
    {
        var gameObject = FindSceneObject(objectName);
        if (gameObject == null)
        {
            Debug.LogWarning($"[Day2MapPrototypeBuilder] Could not find '{objectName}'.");
            return;
        }

        gameObject.SetActive(active);
        EditorUtility.SetDirty(gameObject);
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
}
