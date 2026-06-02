using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Otowa.Minimap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnrichedMapMinimapBuilder
{
    private const string SOURCE_SCENE = "Assets/Scenes/Day1World.unity";
    private const string BACKGROUND_PATH = "Assets/Resources/Map/map_enriched_minimap.png";
    private const string MAP_ICON_PATH = "Assets/Resources/Map/map_icon-removebg-preview.png";
    private const string RIN_PORTRAIT_PATH = "Assets/Resources/Characters/WorldSprite/rin_portrait.png";
    private const string PREFAB_FOLDER = "Assets/Prefabs/Map";
    private const string LANDMARK_PREFAB_PATH = PREFAB_FOLDER + "/MinimapLandmarks_NewMap.prefab";

    private static readonly string[] TargetScenes =
    {
        "Assets/Scenes/TutorialToRyotei.unity",
        "Assets/Scenes/Day1World.unity",
        "Assets/Scenes/Day2World.unity",
        "Assets/Scenes/Day3SummerFestivalSquare.unity"
    };

    private static readonly Vector2 WorldMin = new(-44.5f, -27.5f);
    private static readonly Vector2 WorldMax = new(39.5f, 37.5f);
    private static readonly Vector2 PanelSize = new(840f, 718f);

    private static readonly string[] IncludedRootNames =
    {
        "MapTiles",
        "Railway",
        "Train Station",
        "Izakaya",
        "Onsen",
        "House",
        "House (1)",
        "House (2)",
        "House (3)",
        "House (4)",
        "House (5)",
        "House (6)",
        "House (7)"
    };

    private readonly struct Landmark
    {
        public Landmark(string objectName, string label, Vector2 position, Color color)
        {
            ObjectName = objectName;
            Label = label;
            Position = position;
            Color = color;
        }

        public string ObjectName { get; }
        public string Label { get; }
        public Vector2 Position { get; }
        public Color Color { get; }
    }

    private static readonly Landmark[] Landmarks =
    {
        new("Station", "Station", new Vector2(2.43f, 19.58f), new Color(0.84f, 0.55f, 0.24f)),
        new("Ryotei", "Ryotei", new Vector2(14.20f, 0.41f), new Color(0.86f, 0.32f, 0.22f)),
        new("Hot Spring", "Hot Spring", new Vector2(-14.51f, 27.56f), new Color(0.35f, 0.72f, 0.92f))
    };

    [MenuItem("Tools/Otowa/Minimap/Build Enriched Map Minimap")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[EnrichedMapMinimapBuilder] Exit Play Mode before rebuilding the minimap.");
            return;
        }

        ExportCleanBackground();
        CreateLandmarkPrefab();
        foreach (var scenePath in TargetScenes)
            ConfigureScene(scenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[EnrichedMapMinimapBuilder] Built clean enriched-map minimap and configured tutorial, Day 1, Day 2, and Day 3 festival maps.");
    }

    private static void ExportCleanBackground()
    {
        var sourceScene = OpenSceneAdditive(SOURCE_SCENE, out bool closeAfterward);
        var rootStates = new Dictionary<GameObject, bool>();
        Camera camera = null;
        RenderTexture renderTexture = null;
        Texture2D texture = null;

        try
        {
            foreach (var root in GetAllLoadedSceneRoots())
            {
                rootStates[root] = root.activeSelf;
                root.SetActive(root.scene == sourceScene && IncludedRootNames.Contains(root.name));
            }

            var cameraObject = new GameObject("Temporary Minimap Export Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, sourceScene);
            camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = (WorldMax.y - WorldMin.y) * 0.5f;
            camera.aspect = (WorldMax.x - WorldMin.x) / (WorldMax.y - WorldMin.y);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(31, 48, 39, 255);
            camera.transform.position = new Vector3(
                (WorldMin.x + WorldMax.x) * 0.5f,
                (WorldMin.y + WorldMax.y) * 0.5f,
                -100f);

            renderTexture = new RenderTexture(1008, 780, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = renderTexture;
            camera.Render();

            RenderTexture.active = renderTexture;
            texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();

            File.WriteAllBytes(BACKGROUND_PATH, texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = null;
            if (camera != null) UnityEngine.Object.DestroyImmediate(camera.gameObject);
            if (renderTexture != null) UnityEngine.Object.DestroyImmediate(renderTexture);
            if (texture != null) UnityEngine.Object.DestroyImmediate(texture);

            foreach (var pair in rootStates)
            {
                if (pair.Key != null) pair.Key.SetActive(pair.Value);
            }

            if (closeAfterward) EditorSceneManager.CloseScene(sourceScene, true);
        }

        AssetDatabase.ImportAsset(BACKGROUND_PATH, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(BACKGROUND_PATH);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = false;
        importer.SaveAndReimport();
    }

    private static void CreateLandmarkPrefab()
    {
        EnsureFolder(PREFAB_FOLDER);

        var root = new GameObject("MinimapLandmarks_NewMap");
        try
        {
            foreach (var landmark in Landmarks)
            {
                var markerObject = new GameObject("Minimap Marker - " + landmark.ObjectName);
                markerObject.transform.SetParent(root.transform, false);
                markerObject.transform.position = landmark.Position;

                var marker = markerObject.AddComponent<MinimapLocationMarker>();
                var serializedMarker = new SerializedObject(marker);
                serializedMarker.FindProperty("locationName").stringValue = landmark.Label;
                serializedMarker.FindProperty("markerColor").colorValue = landmark.Color;
                serializedMarker.FindProperty("markerSize").vector2Value = new Vector2(38f, 38f);
                serializedMarker.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(root, LANDMARK_PREFAB_PATH);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void ConfigureScene(string scenePath)
    {
        var scene = OpenSceneAdditive(scenePath, out bool closeAfterward);
        try
        {
            var controller = FindInScene<MinimapController>(scene);
            if (controller == null)
            {
                var managerObject = new GameObject("MinimapManager");
                SceneManager.MoveGameObjectToScene(managerObject, scene);
                controller = managerObject.AddComponent<MinimapController>();
            }

            var background = AssetDatabase.LoadAssetAtPath<Sprite>(BACKGROUND_PATH);
            if (background == null)
                throw new InvalidOperationException("Could not load generated minimap background.");

            var mapIcon = LoadFirstSprite(MAP_ICON_PATH);
            var serializedController = new SerializedObject(controller);
            serializedController.FindProperty("worldMin").vector2Value = WorldMin;
            serializedController.FindProperty("worldMax").vector2Value = WorldMax;
            serializedController.FindProperty("panelSize").vector2Value = PanelSize;
            serializedController.FindProperty("iconSize").vector2Value = new Vector2(100f, 100f);
            serializedController.FindProperty("locationSize").vector2Value = new Vector2(80f, 80f);
            serializedController.FindProperty("mapBackground").objectReferenceValue = background;
            serializedController.FindProperty("mapIconSprite").objectReferenceValue = mapIcon;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);

            EnsurePlayerIcon(scene);

            foreach (var marker in FindAllInScene<MinimapLocationMarker>(scene))
                UnityEngine.Object.DestroyImmediate(marker);

            var previousRoot = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "MinimapLandmarks_NewMap");
            if (previousRoot != null) UnityEngine.Object.DestroyImmediate(previousRoot);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LANDMARK_PREFAB_PATH);
            if (prefab == null)
                throw new InvalidOperationException("Could not load minimap landmark prefab.");

            PrefabUtility.InstantiatePrefab(prefab, scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally
        {
            if (closeAfterward) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void EnsurePlayerIcon(Scene scene)
    {
        var player = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(transform => transform.name == "TestPlayer");
        if (player == null)
            throw new InvalidOperationException("No TestPlayer found in " + scene.path);

        var icon = player.GetComponent<MinimapIcon>() ?? player.gameObject.AddComponent<MinimapIcon>();
        var serializedIcon = new SerializedObject(icon);
        serializedIcon.FindProperty("portrait").objectReferenceValue = LoadFirstSprite(RIN_PORTRAIT_PATH);
        serializedIcon.FindProperty("iconColor").colorValue = new Color(0.56f, 0.74f, 0.56f);
        serializedIcon.FindProperty("displayName").stringValue = "Rin";
        serializedIcon.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(icon);
    }

    private static Sprite LoadFirstSprite(string assetPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().FirstOrDefault();
    }

    private static Scene OpenSceneAdditive(string path, out bool closeAfterward)
    {
        var loaded = SceneManager.GetSceneByPath(path);
        if (loaded.IsValid() && loaded.isLoaded)
        {
            closeAfterward = false;
            return loaded;
        }

        closeAfterward = true;
        return EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
    }

    private static IEnumerable<GameObject> GetAllLoadedSceneRoots()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            foreach (var root in SceneManager.GetSceneAt(i).GetRootGameObjects())
                yield return root;
        }
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        return FindAllInScene<T>(scene).FirstOrDefault();
    }

    private static IEnumerable<T> FindAllInScene<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));
    }

    private static void EnsureFolder(string folderPath)
    {
        var current = "Assets";
        foreach (var folder in folderPath.Split('/').Skip(1))
        {
            var next = current + "/" + folder;
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, folder);
            current = next;
        }
    }
}
