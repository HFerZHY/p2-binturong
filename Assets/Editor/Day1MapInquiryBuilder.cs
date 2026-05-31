using DialogueSystem.NPC;
using Otowa.Inquiry;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Day1MapInquiryBuilder
{
    private const string ScenePath = "Assets/Scenes/Day1World.unity";

    [MenuItem("Tools/Day1 Inquiry/Configure Map NPCs")]
    public static void ConfigureMapNpcs()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ConfigureNpc(scene, "Yuji", Day1InquiryNpc.Yuji);
        ConfigureNpc(scene, "Junko", Day1InquiryNpc.Junko);
        HideMapCharacter(scene, "Mizuki");
        HideMapCharacter(scene, "Inspector");
        ConfigureMapFlow(scene);
        ConfigureStation(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Day1MapInquiryBuilder] Configured Day1World inquiry NPCs and hidden map-only extras.");
    }

    private static void ConfigureNpc(UnityEngine.SceneManagement.Scene scene,
                                     string gameObjectName,
                                     Day1InquiryNpc npc)
    {
        var gameObject = FindInScene(scene, gameObjectName);
        if (gameObject == null)
        {
            Debug.LogError($"[Day1MapInquiryBuilder] Could not find '{gameObjectName}' in Day1World.");
            return;
        }

        var oldController = gameObject.GetComponent<NPCDialogueController>();
        if (oldController != null)
            Object.DestroyImmediate(oldController);

        var inquiryController = gameObject.GetComponent<Day1MapNpcInquiryController>();
        if (inquiryController == null)
            inquiryController = gameObject.AddComponent<Day1MapNpcInquiryController>();

        inquiryController.Configure(npc);
        EditorUtility.SetDirty(inquiryController);
    }

    private static void HideMapCharacter(UnityEngine.SceneManagement.Scene scene, string gameObjectName)
    {
        var gameObject = FindInScene(scene, gameObjectName);
        if (gameObject == null)
        {
            Debug.LogError($"[Day1MapInquiryBuilder] Could not find '{gameObjectName}' in Day1World.");
            return;
        }

        gameObject.SetActive(false);
        EditorUtility.SetDirty(gameObject);
    }

    private static GameObject FindInScene(UnityEngine.SceneManagement.Scene scene, string gameObjectName)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name == gameObjectName)
                    return transform.gameObject;
            }
        }

        return null;
    }

    private static void ConfigureMapFlow(UnityEngine.SceneManagement.Scene scene)
    {
        var gameObject = FindInScene(scene, "Day1 Map Flow");
        if (gameObject == null)
            gameObject = new GameObject("Day1 Map Flow");

        if (gameObject.GetComponent<Day1MapFlowController>() == null)
            gameObject.AddComponent<Day1MapFlowController>();
    }

    private static void ConfigureStation(UnityEngine.SceneManagement.Scene scene)
    {
        var station = FindInScene(scene, "Train Station");
        if (station == null)
        {
            Debug.LogError("[Day1MapInquiryBuilder] Could not find 'Train Station' in Day1World.");
            return;
        }

        var collider = station.GetComponent<BoxCollider2D>();
        if (collider == null)
            collider = station.AddComponent<BoxCollider2D>();

        collider.isTrigger = true;
        collider.offset = new Vector2(-7f, -15f);
        collider.size = new Vector2(10f, 8f);
        EditorUtility.SetDirty(collider);

        if (station.GetComponent<Day1StationInquiryController>() == null)
            station.AddComponent<Day1StationInquiryController>();
    }
}
