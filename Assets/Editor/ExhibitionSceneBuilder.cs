using System.Collections.Generic;
using ExhibitionSystem.Core;
using ExhibitionSystem.Data;
using ExhibitionSystem.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Editor tool that generates a complete exhibition scene.
/// Run via Tools > Museum > Build Exhibition Scene.
/// </summary>
public static class ExhibitionSceneBuilder
{
    private const string SCENE_PATH = "Assets/Scenes/ExhibitionScene.unity";
    private const string ITEMS_PATH = "Assets/Resources/Exhibitions/Items";
    private const string THEMES_PATH = "Assets/Resources/Exhibitions/Themes";
    private const string PREFABS_PATH = "Assets/Resources/Exhibitions/Prefabs";

    // Layout constants (reference resolution: 1920x1080)
    private const float SHELF_WIDTH = 400f;
    private const float SHELF_HEIGHT = 450f;
    private const float DISPLAY_WIDTH = 600f;
    private const float DISPLAY_HEIGHT = 150f;
    private const float CONTROL_HEIGHT = 60f;

    [MenuItem("Tools/Museum/Build Exhibition Scene")]
    public static void BuildScene()
    {
        // Ensure test data exists
        if (!HasTestData())
        {
            if (EditorUtility.DisplayDialog("Missing Test Data",
                "Test data not found. Generate it first?",
                "Generate", "Cancel"))
            {
                ExhibitionTestDataBuilder.GenerateTestData();
            }
            else
            {
                return;
            }
        }

        // Ensure prefabs exist
        if (!HasPrefabs())
        {
            if (EditorUtility.DisplayDialog("Missing Prefabs",
                "Prefabs not found. Generate them first?",
                "Generate", "Cancel"))
            {
                ExhibitionPrefabBuilder.RebuildPrefabs();
            }
            else
            {
                return;
            }
        }

        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Setup camera
        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = new Color(0.15f, 0.12f, 0.1f, 1f);
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
        }

        // Create EventSystem
        CreateEventSystem();

        // Create Canvas
        var canvas = CreateCanvas();

        // Create UI elements
        var background = CreateBackground(canvas);
        var shelfPanel = CreateShelfPanel(canvas);
        var visitorPanel = CreateVisitorPanel(canvas);
        var satisfactionBar = CreateSatisfactionBar(canvas);
        var displayPanel = CreateDisplayPanel(canvas);
        var controlPanel = CreateControlPanel(canvas);
        var themePopup = CreateThemePopup(canvas);
        var tooltip = CreateTooltip(canvas);

        // Create managers
        var managers = CreateManagers(canvas, shelfPanel, displayPanel, visitorPanel,
            satisfactionBar, controlPanel.GetComponent<ThemeSelector>(), themePopup, tooltip);

        // Load test data
        ConfigureTestData(managers.Item1);

        // Wire up references
        WireUpReferences(controlPanel.GetComponent<ThemeSelector>(), themePopup);

        // Ensure Scenes folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        // Save scene
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Scene Builder",
            $"Exhibition scene created successfully!\n\n" +
            $"Location: {SCENE_PATH}\n\n" +
            "Click Play to test the exhibition system.",
            "OK");

        Debug.Log($"[SceneBuilder] Scene saved to: {SCENE_PATH}");
    }

    // ── Creation Methods ────────────────────────────────────────────────────────

    private static void CreateEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        var esObj = new GameObject("EventSystem");
        esObj.AddComponent<EventSystem>();
        esObj.AddComponent<InputSystemUIInputModule>();
    }

    private static Canvas CreateCanvas()
    {
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        return canvas;
    }

    private static GameObject CreateBackground(Canvas canvas)
    {
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvas.transform, false);

        var img = bgObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.18f, 0.15f, 1f);
        img.raycastTarget = false;

        var rt = bgObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return bgObj;
    }

    private static ShelfPanel CreateShelfPanel(Canvas canvas)
    {
        var panelObj = new GameObject("ShelfPanel");
        panelObj.transform.SetParent(canvas.transform, false);

        // Background
        var bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0.25f, 0.22f, 0.18f, 0.9f);

        // Position: left side
        var rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.1f);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(30, 0);
        rt.sizeDelta = new Vector2(SHELF_WIDTH, 0);

        // Grid container
        var gridObj = new GameObject("Grid");
        gridObj.transform.SetParent(panelObj.transform, false);

        var gridRt = gridObj.AddComponent<RectTransform>();
        gridRt.anchorMin = Vector2.zero;
        gridRt.anchorMax = Vector2.one;
        gridRt.offsetMin = new Vector2(20, 20);
        gridRt.offsetMax = new Vector2(-20, -20);

        var grid = gridObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(80, 80);
        grid.spacing = new Vector2(10, 10);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;

        // Load shelf slot prefab
        var slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/ShelfSlot.prefab");

        // ShelfPanel script
        var panel = panelObj.AddComponent<ShelfPanel>();
        SetPrivateField(panel, "_gridContainer", gridObj.transform);
        if (slotPrefab != null)
        {
            var slotUI = slotPrefab.GetComponent<ShelfSlotUI>();
            SetPrivateField(panel, "_slotPrefab", slotUI);
        }

        return panel;
    }

    private static VisitorPanel CreateVisitorPanel(Canvas canvas)
    {
        var panelObj = new GameObject("VisitorPanel");
        panelObj.transform.SetParent(canvas.transform, false);

        // Background
        var bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0.3f, 0.27f, 0.22f, 0.9f);

        // Position: right top
        var rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0.5f);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-30, -30);
        rt.sizeDelta = new Vector2(DISPLAY_WIDTH, 0);

        // Character image
        var charObj = new GameObject("CharacterImage");
        charObj.transform.SetParent(panelObj.transform, false);
        var charImg = charObj.AddComponent<Image>();
        charImg.color = new Color(0.5f, 0.45f, 0.4f, 1f);
        charImg.raycastTarget = false;
        var charRt = charObj.GetComponent<RectTransform>();
        charRt.anchorMin = new Vector2(0, 0);
        charRt.anchorMax = new Vector2(1, 1);
        charRt.offsetMin = new Vector2(20, 80);
        charRt.offsetMax = new Vector2(-20, -20);

        // Dialogue panel
        var dialogueObj = new GameObject("DialoguePanel");
        dialogueObj.transform.SetParent(panelObj.transform, false);
        var dialogueBg = dialogueObj.AddComponent<Image>();
        dialogueBg.color = new Color(0.15f, 0.12f, 0.1f, 0.9f);
        var dialogueCg = dialogueObj.AddComponent<CanvasGroup>();

        var dialogueRt = dialogueObj.GetComponent<RectTransform>();
        dialogueRt.anchorMin = new Vector2(0, 0);
        dialogueRt.anchorMax = new Vector2(1, 0);
        dialogueRt.pivot = new Vector2(0.5f, 0);
        dialogueRt.anchoredPosition = new Vector2(0, 10);
        dialogueRt.sizeDelta = new Vector2(-40, 60);

        // Dialogue text
        var textObj = new GameObject("DialogueText");
        textObj.transform.SetParent(dialogueObj.transform, false);
        var dialogueText = textObj.AddComponent<TextMeshProUGUI>();
        dialogueText.text = "Place items in the display slots to begin.";
        dialogueText.fontSize = 16;
        dialogueText.alignment = TextAlignmentOptions.Center;
        dialogueText.color = Color.white;
        var textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10, 5);
        textRt.offsetMax = new Vector2(-10, -5);

        // VisitorPanel script
        var panel = panelObj.AddComponent<VisitorPanel>();
        SetPrivateField(panel, "_characterImage", charImg);
        SetPrivateField(panel, "_dialogueText", dialogueText);
        SetPrivateField(panel, "_dialoguePanel", dialogueCg);

        return panel;
    }

    private static SatisfactionBar CreateSatisfactionBar(Canvas canvas)
    {
        var barObj = new GameObject("SatisfactionBar");
        barObj.transform.SetParent(canvas.transform, false);

        // Background
        var bg = barObj.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.18f, 0.15f, 0.9f);

        // Position: right middle
        var rt = barObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0.35f);
        rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(1, 0.5f);
        rt.anchoredPosition = new Vector2(-30, 0);
        rt.sizeDelta = new Vector2(DISPLAY_WIDTH, 0);

        // Label
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(barObj.transform, false);
        var labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = "Visitor Satisfaction";
        labelText.fontSize = 16;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.color = new Color(0.9f, 0.85f, 0.8f, 1f);
        var labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0, 0.5f);
        labelRt.anchorMax = new Vector2(0.5f, 1);
        labelRt.offsetMin = new Vector2(15, 5);
        labelRt.offsetMax = new Vector2(-5, -5);

        // Value text
        var valueObj = new GameObject("ValueText");
        valueObj.transform.SetParent(barObj.transform, false);
        var valueText = valueObj.AddComponent<TextMeshProUGUI>();
        valueText.text = "0 / 0";
        valueText.fontSize = 16;
        valueText.alignment = TextAlignmentOptions.Right;
        valueText.color = Color.white;
        var valueRt = valueObj.GetComponent<RectTransform>();
        valueRt.anchorMin = new Vector2(0.5f, 0.5f);
        valueRt.anchorMax = new Vector2(1, 1);
        valueRt.offsetMin = new Vector2(5, 5);
        valueRt.offsetMax = new Vector2(-15, -5);

        // Slider
        var sliderObj = CreateSlider(barObj.transform);
        var slider = sliderObj.GetComponent<Slider>();
        var fillImg = sliderObj.transform.Find("Fill Area/Fill").GetComponent<Image>();

        // SatisfactionBar script
        var bar = barObj.AddComponent<SatisfactionBar>();
        SetPrivateField(bar, "_slider", slider);
        SetPrivateField(bar, "_fillImage", fillImg);
        SetPrivateField(bar, "_valueText", valueText);
        SetPrivateField(bar, "_labelText", labelText);

        return bar;
    }

    private static GameObject CreateSlider(Transform parent)
    {
        var sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(parent, false);

        var sliderRt = sliderObj.AddComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0, 0);
        sliderRt.anchorMax = new Vector2(1, 0.5f);
        sliderRt.offsetMin = new Vector2(15, 10);
        sliderRt.offsetMax = new Vector2(-15, -5);

        // Background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.08f, 0.06f, 1f);
        var bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        // Fill area
        var fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        var fillAreaRt = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.offsetMin = Vector2.zero;
        fillAreaRt.offsetMax = Vector2.zero;

        // Fill
        var fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        var fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.3f, 0.8f, 0.3f, 1f);
        var fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        // Slider component
        var slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = fillRt;
        slider.minValue = 0;
        slider.maxValue = 6;
        slider.value = 0;
        slider.interactable = false;

        return sliderObj;
    }

    private static DisplayPanel CreateDisplayPanel(Canvas canvas)
    {
        var panelObj = new GameObject("DisplayPanel");
        panelObj.transform.SetParent(canvas.transform, false);

        // Background
        var bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0.28f, 0.25f, 0.2f, 0.9f);

        // Position: right bottom (above control)
        var rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0.1f);
        rt.anchorMax = new Vector2(1, 0.35f);
        rt.pivot = new Vector2(1, 0);
        rt.anchoredPosition = new Vector2(-30, 0);
        rt.sizeDelta = new Vector2(DISPLAY_WIDTH, 0);

        // Slot container
        var containerObj = new GameObject("SlotContainer");
        containerObj.transform.SetParent(panelObj.transform, false);

        var containerRt = containerObj.AddComponent<RectTransform>();
        containerRt.anchorMin = Vector2.zero;
        containerRt.anchorMax = Vector2.one;
        containerRt.offsetMin = new Vector2(20, 20);
        containerRt.offsetMax = new Vector2(-20, -20);

        var layout = containerObj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 15;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // Load display slot prefab
        var slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/DisplaySlot.prefab");

        // DisplayPanel script
        var panel = panelObj.AddComponent<DisplayPanel>();
        SetPrivateField(panel, "_slotContainer", containerObj.transform);
        if (slotPrefab != null)
        {
            var slotUI = slotPrefab.GetComponent<DisplaySlotUI>();
            SetPrivateField(panel, "_slotPrefab", slotUI);
        }

        return panel;
    }

    private static GameObject CreateControlPanel(Canvas canvas)
    {
        var panelObj = new GameObject("ControlPanel");
        panelObj.transform.SetParent(canvas.transform, false);

        // Background
        var bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0.22f, 0.2f, 0.16f, 0.95f);

        // Position: bottom
        var rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, CONTROL_HEIGHT);

        // Layout
        var layout = panelObj.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 10, 10);
        layout.spacing = 20;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        // Theme select button
        var selectBtnObj = CreateButton(panelObj.transform, "SelectButton", "Select Theme", 200);
        var selectBtn = selectBtnObj.GetComponent<Button>();

        // Title text
        var titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(panelObj.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Select a Theme";
        titleText.fontSize = 24;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.95f, 0.85f, 1f);
        var titleLe = titleObj.AddComponent<LayoutElement>();
        titleLe.flexibleWidth = 1;

        // Start button
        var startBtnObj = CreateButton(panelObj.transform, "StartButton", "Start Exhibition", 200);
        var startBtn = startBtnObj.GetComponent<Button>();
        var startText = startBtnObj.GetComponentInChildren<TextMeshProUGUI>();

        // ThemeSelector script
        var selector = panelObj.AddComponent<ThemeSelector>();
        SetPrivateField(selector, "_titleText", titleText);
        SetPrivateField(selector, "_selectButton", selectBtn);
        SetPrivateField(selector, "_startButton", startBtn);
        SetPrivateField(selector, "_startButtonText", startText);

        return panelObj;
    }

    private static ThemeSelectionPopup CreateThemePopup(Canvas canvas)
    {
        var panelObj = new GameObject("ThemeSelectionPopup");
        panelObj.transform.SetParent(canvas.transform, false);

        // Overlay background
        var overlay = panelObj.AddComponent<Image>();
        overlay.color = new Color(0, 0, 0, 0.7f);

        var panelRt = panelObj.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        // Popup window
        var windowObj = new GameObject("Window");
        windowObj.transform.SetParent(panelObj.transform, false);
        var windowBg = windowObj.AddComponent<Image>();
        windowBg.color = new Color(0.2f, 0.18f, 0.15f, 0.98f);
        var windowRt = windowObj.GetComponent<RectTransform>();
        windowRt.anchorMin = new Vector2(0.5f, 0.5f);
        windowRt.anchorMax = new Vector2(0.5f, 0.5f);
        windowRt.sizeDelta = new Vector2(400, 400);

        // Title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(windowObj.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Select Exhibition Theme";
        titleText.fontSize = 24;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        var titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, -15);
        titleRt.sizeDelta = new Vector2(-40, 40);

        // List container
        var listObj = new GameObject("ListContainer");
        listObj.transform.SetParent(windowObj.transform, false);
        var listLayout = listObj.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 10;
        listLayout.padding = new RectOffset(20, 20, 10, 10);
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = false;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;
        var listRt = listObj.GetComponent<RectTransform>();
        listRt.anchorMin = new Vector2(0, 0);
        listRt.anchorMax = new Vector2(1, 1);
        listRt.offsetMin = new Vector2(0, 60);
        listRt.offsetMax = new Vector2(0, -60);

        // Close button
        var closeBtnObj = CreateButton(windowObj.transform, "CloseButton", "Close", 100);
        var closeBtn = closeBtnObj.GetComponent<Button>();
        var closeBtnRt = closeBtnObj.GetComponent<RectTransform>();
        closeBtnRt.anchorMin = new Vector2(0.5f, 0);
        closeBtnRt.anchorMax = new Vector2(0.5f, 0);
        closeBtnRt.pivot = new Vector2(0.5f, 0);
        closeBtnRt.anchoredPosition = new Vector2(0, 15);
        // Remove layout element to use anchored position
        var le = closeBtnObj.GetComponent<LayoutElement>();
        if (le != null) Object.DestroyImmediate(le);

        // CanvasGroup
        var cg = panelObj.AddComponent<CanvasGroup>();

        // Load theme list item prefab
        var itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/ThemeListItem.prefab");

        // ThemeSelectionPopup script
        var popup = panelObj.AddComponent<ThemeSelectionPopup>();
        SetPrivateField(popup, "_panel", panelObj);
        SetPrivateField(popup, "_listContainer", listObj.transform);
        SetPrivateField(popup, "_closeButton", closeBtn);
        SetPrivateField(popup, "_canvasGroup", cg);
        if (itemPrefab != null)
        {
            var itemUI = itemPrefab.GetComponent<ThemeListItem>();
            SetPrivateField(popup, "_itemPrefab", itemUI);
        }

        // Start hidden
        panelObj.SetActive(false);

        return popup;
    }

    private static ItemTooltip CreateTooltip(Canvas canvas)
    {
        // Load tooltip prefab
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/ItemTooltip.prefab");
        if (prefab == null)
        {
            Debug.LogWarning("[SceneBuilder] ItemTooltip prefab not found");
            return null;
        }

        var tooltipObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
        tooltipObj.name = "ItemTooltip";
        tooltipObj.SetActive(false);

        return tooltipObj.GetComponent<ItemTooltip>();
    }

    private static (ExhibitionManager, ExhibitionUIManager) CreateManagers(
        Canvas canvas,
        ShelfPanel shelfPanel,
        DisplayPanel displayPanel,
        VisitorPanel visitorPanel,
        SatisfactionBar satisfactionBar,
        ThemeSelector themeSelector,
        ThemeSelectionPopup themePopup,
        ItemTooltip tooltip)
    {
        // ExhibitionManager
        var managerObj = new GameObject("ExhibitionManager");
        var manager = managerObj.AddComponent<ExhibitionManager>();

        // ExhibitionUIManager (on canvas)
        var uiManager = canvas.gameObject.AddComponent<ExhibitionUIManager>();
        SetPrivateField(uiManager, "_shelfPanel", shelfPanel);
        SetPrivateField(uiManager, "_displayPanel", displayPanel);
        SetPrivateField(uiManager, "_visitorPanel", visitorPanel);
        SetPrivateField(uiManager, "_satisfactionBar", satisfactionBar);
        SetPrivateField(uiManager, "_themeSelector", themeSelector);
        SetPrivateField(uiManager, "_themePopup", themePopup);
        SetPrivateField(uiManager, "_tooltip", tooltip);
        SetPrivateField(uiManager, "_rootCanvas", canvas);

        return (manager, uiManager);
    }

    private static void ConfigureTestData(ExhibitionManager manager)
    {
        // Load all items
        var itemGuids = AssetDatabase.FindAssets("t:ExhibitItemData", new[] { ITEMS_PATH });
        var items = new List<ExhibitItemData>();
        foreach (var guid in itemGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var item = AssetDatabase.LoadAssetAtPath<ExhibitItemData>(path);
            if (item != null)
                items.Add(item);
        }

        // Load all themes
        var themeGuids = AssetDatabase.FindAssets("t:ExhibitionTheme", new[] { THEMES_PATH });
        var themes = new List<ExhibitionTheme>();
        foreach (var guid in themeGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var theme = AssetDatabase.LoadAssetAtPath<ExhibitionTheme>(path);
            if (theme != null)
                themes.Add(theme);
        }

        // Configure manager
        SetPrivateField(manager, "_allItems", items);
        SetPrivateField(manager, "_allThemes", themes);

        Debug.Log($"[SceneBuilder] Loaded {items.Count} items and {themes.Count} themes");
    }

    private static void WireUpReferences(ThemeSelector selector, ThemeSelectionPopup popup)
    {
        SetPrivateField(selector, "_popup", popup);
    }

    // ── Helper Methods ──────────────────────────────────────────────────────────

    private static GameObject CreateButton(Transform parent, string name, string text, float width)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0.4f, 0.35f, 0.28f, 1f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;

        var colors = btn.colors;
        colors.normalColor = new Color(0.4f, 0.35f, 0.28f, 1f);
        colors.highlightedColor = new Color(0.5f, 0.45f, 0.38f, 1f);
        colors.pressedColor = new Color(0.35f, 0.3f, 0.23f, 1f);
        colors.disabledColor = new Color(0.3f, 0.28f, 0.22f, 0.5f);
        btn.colors = colors;

        var le = btnObj.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;

        // Text child
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = 16;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;
        var textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        return btnObj;
    }

    private static bool HasTestData()
    {
        return AssetDatabase.IsValidFolder(ITEMS_PATH) && AssetDatabase.IsValidFolder(THEMES_PATH);
    }

    private static bool HasPrefabs()
    {
        return AssetDatabase.IsValidFolder(PREFABS_PATH) &&
               AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/ShelfSlot.prefab") != null;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (field != null)
            field.SetValue(target, value);
        else
            Debug.LogWarning($"[SceneBuilder] Field not found: {target.GetType().Name}.{fieldName}");
    }
}
