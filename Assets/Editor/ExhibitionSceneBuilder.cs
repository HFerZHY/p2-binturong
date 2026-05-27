using System.Collections.Generic;
using System.Linq;
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

public static class ExhibitionSceneBuilder
{
    private const string SCENE_PATH = "Assets/Scenes/ExhibitionScene.unity";
    private const string ITEMS_PATH = "Assets/Resources/Exhibitions/Items";
    private const string THEMES_PATH = "Assets/Resources/Exhibitions/Themes";
    private const string INSPIRATIONS_PATH = "Assets/Resources/Exhibitions/Inspirations";
    private const string PREFABS_PATH = "Assets/Resources/Exhibitions/Prefabs";

    private const float SHELF_WIDTH = 430f;
    private const float CONTROL_HEIGHT = 86f;

    [MenuItem("Tools/Museum/Build Exhibition Scene")]
    public static void BuildScene()
    {
        if (!HasTestData())
            ExhibitionTestDataBuilder.GenerateTestData();

        if (!HasPrefabs())
            ExhibitionPrefabBuilder.RebuildPrefabs();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = new Color(0.15f, 0.12f, 0.1f, 1f);
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
        }

        CreateEventSystem();
        var canvas = CreateCanvas();
        CreateBackground(canvas);

        var shelfPanel = CreateShelfPanel(canvas);
        var visitorPanel = CreateVisitorPanel(canvas);
        var satisfactionBar = CreateSatisfactionBar(canvas);
        var displayPanel = CreateDisplayPanel(canvas);
        var controlPanel = CreateControlPanel(canvas);
        var themePopup = CreateThemePopup(canvas);
        var inspirationPopup = CreateInspirationPopup(canvas);
        var tooltip = CreateTooltip(canvas);

        var managers = CreateManagers(
            canvas,
            shelfPanel,
            displayPanel,
            visitorPanel,
            satisfactionBar,
            controlPanel.GetComponent<ThemeSelector>(),
            themePopup,
            inspirationPopup,
            tooltip);

        ConfigureTestData(managers.Item1);
        WireUpReferences(controlPanel.GetComponent<ThemeSelector>(), themePopup);

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        AssetDatabase.Refresh();
        Debug.Log($"[SceneBuilder] Exhibition scene saved to {SCENE_PATH}");
    }

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

    private static void CreateBackground(Canvas canvas)
    {
        var bgObj = CreateChild(canvas.transform, "Background");
        var img = bgObj.AddComponent<Image>();
        img.color = new Color(0.16f, 0.13f, 0.09f, 1f);
        img.raycastTarget = false;
        Stretch(bgObj.GetComponent<RectTransform>(), 0);
    }

    private static ShelfPanel CreateShelfPanel(Canvas canvas)
    {
        var panelObj = CreateChild(canvas.transform, "ShelfPanel");
        var bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0.23f, 0.19f, 0.13f, 0.96f);

        var rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.09f);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(24, 0);
        rt.sizeDelta = new Vector2(SHELF_WIDTH, 0);

        var title = CreateText(panelObj.transform, "Title", "Items", 30, FontStyles.Bold, TextAlignmentOptions.Center);
        title.color = new Color(0.98f, 0.9f, 0.72f, 1f);
        var titleRt = title.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, -14);
        titleRt.sizeDelta = new Vector2(-24, 40);

        var gridObj = CreateChild(panelObj.transform, "Grid");
        var gridRt = gridObj.GetComponent<RectTransform>();
        gridRt.anchorMin = Vector2.zero;
        gridRt.anchorMax = Vector2.one;
        gridRt.offsetMin = new Vector2(22, 24);
        gridRt.offsetMax = new Vector2(-22, -72);

        var grid = gridObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(82, 82);
        grid.spacing = new Vector2(12, 12);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;

        var panel = panelObj.AddComponent<ShelfPanel>();
        SetPrivateField(panel, "_gridContainer", gridObj.transform);
        var slotPrefab = LoadPrefabComponent<ShelfSlotUI>("ShelfSlot");
        if (slotPrefab != null)
            SetPrivateField(panel, "_slotPrefab", slotPrefab);

        return panel;
    }

    private static VisitorPanel CreateVisitorPanel(Canvas canvas)
    {
        var panelObj = CreateChild(canvas.transform, "VisitorPanel");
        var bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0.27f, 0.23f, 0.17f, 0.94f);

        var rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.31f, 0.58f);
        rt.anchorMax = new Vector2(0.985f, 0.985f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var charContainer = CreateChild(panelObj.transform, "CharacterContainer");
        var charCg = charContainer.AddComponent<CanvasGroup>();
        charCg.alpha = 0f;
        var rawImage = charContainer.AddComponent<RawImage>();
        rawImage.color = Color.white;
        rawImage.raycastTarget = false;
        rawImage.texture = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Resources/Exhibitions/VisitorRT.asset");
        var charRt = charContainer.GetComponent<RectTransform>();
        charRt.anchorMin = new Vector2(0, 0.2f);
        charRt.anchorMax = new Vector2(1, 1);
        charRt.offsetMin = new Vector2(22, 6);
        charRt.offsetMax = new Vector2(-22, -18);

        var dialogue = CreateChild(panelObj.transform, "DialoguePanel");
        var dialogueBg = dialogue.AddComponent<Image>();
        dialogueBg.color = new Color(0.12f, 0.07f, 0.06f, 0.96f);
        var dialogueCg = dialogue.AddComponent<CanvasGroup>();
        var dialogueRt = dialogue.GetComponent<RectTransform>();
        dialogueRt.anchorMin = new Vector2(0, 0);
        dialogueRt.anchorMax = new Vector2(1, 0);
        dialogueRt.pivot = new Vector2(0.5f, 0);
        dialogueRt.anchoredPosition = new Vector2(0, 14);
        dialogueRt.sizeDelta = new Vector2(-56, 92);

        var dialogueText = CreateText(dialogue.transform, "DialogueText", "Choose a theme to begin.", 25, FontStyles.Bold, TextAlignmentOptions.Center);
        dialogueText.textWrappingMode = TextWrappingModes.Normal;
        dialogueText.color = new Color(0.96f, 0.89f, 0.76f, 1f);
        Stretch(dialogueText.GetComponent<RectTransform>(), 10);

        var generator = panelObj.AddComponent<VisitorCharacterGenerator>();
        var bases = Resources.LoadAll<CharacterBase>("Characters");
        if (bases.Length > 0)
            SetPrivateField(generator, "_characterBases", bases);
        var visitorRT = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Resources/Exhibitions/VisitorRT.asset");
        if (visitorRT != null)
            SetPrivateField(generator, "_renderTarget", visitorRT);

        var panel = panelObj.AddComponent<VisitorPanel>();
        SetPrivateField(panel, "_characterRawImage", rawImage);
        SetPrivateField(panel, "_characterCanvasGroup", charCg);
        SetPrivateField(panel, "_dialogueText", dialogueText);
        SetPrivateField(panel, "_dialoguePanel", dialogueCg);
        SetPrivateField(panel, "_characterGenerator", generator);
        return panel;
    }

    private static SatisfactionBar CreateSatisfactionBar(Canvas canvas)
    {
        var barObj = CreateChild(canvas.transform, "SatisfactionBar");
        var bg = barObj.AddComponent<Image>();
        bg.color = new Color(0.14f, 0.10f, 0.07f, 0.95f);

        var rt = barObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.31f, 0.485f);
        rt.anchorMax = new Vector2(0.985f, 0.565f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var labelText = CreateText(barObj.transform, "Label", "Visitor Satisfaction", 23, FontStyles.Bold, TextAlignmentOptions.Left);
        labelText.color = new Color(0.98f, 0.9f, 0.72f, 1f);
        var labelRt = labelText.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0, 0.5f);
        labelRt.anchorMax = new Vector2(0.55f, 1);
        labelRt.offsetMin = new Vector2(15, 5);
        labelRt.offsetMax = new Vector2(-5, -5);

        var valueText = CreateText(barObj.transform, "ValueText", "0 / 0", 23, FontStyles.Bold, TextAlignmentOptions.Right);
        valueText.color = new Color(0.98f, 0.9f, 0.72f, 1f);
        var valueRt = valueText.GetComponent<RectTransform>();
        valueRt.anchorMin = new Vector2(0.55f, 0.5f);
        valueRt.anchorMax = new Vector2(1, 1);
        valueRt.offsetMin = new Vector2(5, 5);
        valueRt.offsetMax = new Vector2(-15, -5);

        var sliderObj = CreateSlider(barObj.transform);
        var slider = sliderObj.GetComponent<Slider>();
        var fillImg = sliderObj.transform.Find("Fill Area/Fill").GetComponent<Image>();

        var bar = barObj.AddComponent<SatisfactionBar>();
        SetPrivateField(bar, "_slider", slider);
        SetPrivateField(bar, "_fillImage", fillImg);
        SetPrivateField(bar, "_valueText", valueText);
        SetPrivateField(bar, "_labelText", labelText);
        return bar;
    }

    private static DisplayPanel CreateDisplayPanel(Canvas canvas)
    {
        var panelObj = CreateChild(canvas.transform, "DisplayPanel");
        var bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0.28f, 0.23f, 0.16f, 0.94f);

        var rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.31f, 0.09f);
        rt.anchorMax = new Vector2(0.985f, 0.475f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var title = CreateText(panelObj.transform, "Title", "Inspiration Exhibition", 25, FontStyles.Bold, TextAlignmentOptions.Center);
        title.color = new Color(0.98f, 0.9f, 0.72f, 1f);
        var titleRt = title.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, -12);
        titleRt.sizeDelta = new Vector2(-24, 34);

        var containerObj = CreateChild(panelObj.transform, "SlotContainer");
        var containerRt = containerObj.GetComponent<RectTransform>();
        containerRt.anchorMin = Vector2.zero;
        containerRt.anchorMax = Vector2.one;
        containerRt.offsetMin = new Vector2(28, 24);
        containerRt.offsetMax = new Vector2(-28, -58);

        var grid = containerObj.AddComponent<GridLayoutGroup>();
        grid.spacing = new Vector2(14, 12);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.cellSize = new Vector2(312, 154);

        var panel = panelObj.AddComponent<DisplayPanel>();
        SetPrivateField(panel, "_slotContainer", containerObj.transform);
        SetPrivateField(panel, "_gridLayout", grid);
        var slotPrefab = LoadPrefabComponent<InspirationDisplaySlot>("InspirationDisplaySlot");
        if (slotPrefab != null)
            SetPrivateField(panel, "_slotPrefab", slotPrefab);
        return panel;
    }

    private static GameObject CreateControlPanel(Canvas canvas)
    {
        var panelObj = CreateChild(canvas.transform, "ControlPanel");
        var bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.09f, 0.06f, 0.98f);

        var rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, CONTROL_HEIGHT);

        var layout = panelObj.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(34, 34, 12, 12);
        layout.spacing = 28;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        var selectBtnObj = CreateButton(panelObj.transform, "SelectButton", "Select Theme", 260);
        var titleText = CreateText(panelObj.transform, "TitleText", "Select a Theme", 34, FontStyles.Bold, TextAlignmentOptions.Center);
        titleText.color = new Color(0.98f, 0.86f, 0.56f, 1f);
        var titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredWidth = 520;
        titleLayout.flexibleWidth = 1;
        var startBtnObj = CreateButton(panelObj.transform, "StartButton", "Choose Ideas", 320);

        var selector = panelObj.AddComponent<ThemeSelector>();
        SetPrivateField(selector, "_titleText", titleText);
        SetPrivateField(selector, "_selectButton", selectBtnObj.GetComponent<Button>());
        SetPrivateField(selector, "_startButton", startBtnObj.GetComponent<Button>());
        SetPrivateField(selector, "_startButtonText", startBtnObj.GetComponentInChildren<TextMeshProUGUI>());
        return panelObj;
    }

    private static ThemeSelectionPopup CreateThemePopup(Canvas canvas)
    {
        var panelObj = CreateOverlay(canvas.transform, "ThemeSelectionPopup");
        var windowObj = CreateWindow(panelObj.transform, "Window", new Vector2(860, 680));
        CreatePopupTitle(windowObj.transform, "Theme Selection");

        var hint = CreateText(windowObj.transform, "HintText", "Choose an exhibition theme.", 22, FontStyles.Normal, TextAlignmentOptions.Center);
        var hintRt = hint.GetComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0, 1);
        hintRt.anchorMax = new Vector2(1, 1);
        hintRt.pivot = new Vector2(0.5f, 1);
        hintRt.anchoredPosition = new Vector2(0, -76);
        hintRt.sizeDelta = new Vector2(-96, 48);
        hint.color = new Color(0.96f, 0.86f, 0.66f, 1f);

        var listObj = CreateListContainer(windowObj.transform, new Vector2(34, 104), new Vector2(-34, -96));
        var closeBtn = CreateAnchoredButton(windowObj.transform, "CloseButton", "Back", 220, new Vector2(-150, 20));
        var enterBtn = CreateAnchoredButton(windowObj.transform, "EnterButton", "Enter Theme", 260, new Vector2(160, 20));
        var cg = panelObj.AddComponent<CanvasGroup>();

        var popup = panelObj.AddComponent<ThemeSelectionPopup>();
        SetPrivateField(popup, "_panel", panelObj);
        SetPrivateField(popup, "_listContainer", listObj.transform);
        SetPrivateField(popup, "_closeButton", closeBtn.GetComponent<Button>());
        SetPrivateField(popup, "_enterButton", enterBtn.GetComponent<Button>());
        SetPrivateField(popup, "_hintText", hint);
        SetPrivateField(popup, "_canvasGroup", cg);
        var itemPrefab = LoadPrefabComponent<ThemeListItem>("ThemeListItem");
        if (itemPrefab != null)
            SetPrivateField(popup, "_itemPrefab", itemPrefab);

        panelObj.SetActive(false);
        return popup;
    }

    private static InspirationSelectionPopup CreateInspirationPopup(Canvas canvas)
    {
        var panelObj = CreateOverlay(canvas.transform, "InspirationSelectionPopup");
        var windowObj = CreateWindow(panelObj.transform, "Window", new Vector2(1120, 810));
        var title = CreatePopupTitle(windowObj.transform, "Inspiration Selection");

        var themeLabel = CreateText(windowObj.transform, "ThemeText", "Theme: Exhibition", 22, FontStyles.Bold, TextAlignmentOptions.Center);
        var themeRt = themeLabel.GetComponent<RectTransform>();
        themeRt.anchorMin = new Vector2(0, 1);
        themeRt.anchorMax = new Vector2(1, 1);
        themeRt.pivot = new Vector2(0.5f, 1);
        themeRt.anchoredPosition = new Vector2(0, -70);
        themeRt.sizeDelta = new Vector2(-80, 38);
        themeLabel.color = new Color(0.95f, 0.86f, 0.66f, 1f);

        var selectedPanel = CreateColumnPanel(
            windowObj.transform,
            "SelectedColumn",
            "Selected Inspirations",
            new Vector2(0, 0),
            new Vector2(0.47f, 1),
            new Vector2(40, 104),
            new Vector2(-18, -150));
        var selectedList = CreateListContainer(selectedPanel.transform, Vector2.zero, new Vector2(0, -48));

        var libraryPanel = CreateColumnPanel(
            windowObj.transform,
            "LibraryColumn",
            "Inspiration Library",
            new Vector2(0.49f, 0),
            new Vector2(1, 1),
            new Vector2(18, 104),
            new Vector2(-40, -150));
        var listObj = CreateListContainer(libraryPanel.transform, Vector2.zero, new Vector2(0, -48));

        var hint = CreateText(windowObj.transform, "HintText", "Pick the ideas that fit this exhibition.", 20, FontStyles.Italic, TextAlignmentOptions.Center);
        var hintRt = hint.GetComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0, 0);
        hintRt.anchorMax = new Vector2(1, 0);
        hintRt.pivot = new Vector2(0.5f, 0);
        hintRt.anchoredPosition = new Vector2(0, 82);
        hintRt.sizeDelta = new Vector2(-160, 56);
        hint.textWrappingMode = TextWrappingModes.Normal;
        hint.color = new Color(0.98f, 0.88f, 0.66f, 1f);

        var confirmBtn = CreateAnchoredButton(windowObj.transform, "ConfirmButton", "Confirm Inspirations", 310, new Vector2(175, 20));
        var closeBtn = CreateAnchoredButton(windowObj.transform, "CloseButton", "Back", 220, new Vector2(-185, 20));
        var cg = panelObj.AddComponent<CanvasGroup>();

        var popup = panelObj.AddComponent<InspirationSelectionPopup>();
        SetPrivateField(popup, "_panel", panelObj);
        SetPrivateField(popup, "_titleText", title);
        SetPrivateField(popup, "_themeText", themeLabel);
        SetPrivateField(popup, "_hintText", hint);
        SetPrivateField(popup, "_selectedContainer", selectedList.transform);
        SetPrivateField(popup, "_libraryContainer", listObj.transform);
        SetPrivateField(popup, "_listContainer", listObj.transform);
        SetPrivateField(popup, "_confirmButton", confirmBtn.GetComponent<Button>());
        SetPrivateField(popup, "_closeButton", closeBtn.GetComponent<Button>());
        SetPrivateField(popup, "_canvasGroup", cg);
        var itemPrefab = LoadPrefabComponent<InspirationListItem>("InspirationListItem");
        if (itemPrefab != null)
            SetPrivateField(popup, "_itemPrefab", itemPrefab);

        panelObj.SetActive(false);
        return popup;
    }

    private static ItemTooltip CreateTooltip(Canvas canvas)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/ItemTooltip.prefab");
        if (prefab == null) return null;

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
        InspirationSelectionPopup inspirationPopup,
        ItemTooltip tooltip)
    {
        var managerObj = new GameObject("ExhibitionManager");
        var manager = managerObj.AddComponent<ExhibitionManager>();

        var uiManager = canvas.gameObject.AddComponent<ExhibitionUIManager>();
        SetPrivateField(uiManager, "_shelfPanel", shelfPanel);
        SetPrivateField(uiManager, "_displayPanel", displayPanel);
        SetPrivateField(uiManager, "_visitorPanel", visitorPanel);
        SetPrivateField(uiManager, "_satisfactionBar", satisfactionBar);
        SetPrivateField(uiManager, "_themeSelector", themeSelector);
        SetPrivateField(uiManager, "_themePopup", themePopup);
        SetPrivateField(uiManager, "_inspirationPopup", inspirationPopup);
        SetPrivateField(uiManager, "_tooltip", tooltip);
        SetPrivateField(uiManager, "_rootCanvas", canvas);

        return (manager, uiManager);
    }

    private static void ConfigureTestData(ExhibitionManager manager)
    {
        var items = LoadAssets<ExhibitItemData>(ITEMS_PATH).OrderBy(item => item.sortOrder).ToList();
        var themes = LoadAssets<ExhibitionTheme>(THEMES_PATH).OrderBy(theme => theme.day).ThenBy(theme => theme.title).ToList();
        var inspirations = LoadAssets<InspirationData>(INSPIRATIONS_PATH).OrderBy(inspiration => inspiration.id).ToList();

        SetPrivateField(manager, "_allItems", items);
        SetPrivateField(manager, "_allThemes", themes);
        SetPrivateField(manager, "_allInspirations", inspirations);

        Debug.Log($"[SceneBuilder] Loaded {items.Count} items, {inspirations.Count} inspirations, {themes.Count} themes.");
    }

    private static void WireUpReferences(ThemeSelector selector, ThemeSelectionPopup popup)
    {
        SetPrivateField(selector, "_popup", popup);
    }

    private static GameObject CreateSlider(Transform parent)
    {
        var sliderObj = CreateChild(parent, "Slider");
        var sliderRt = sliderObj.GetComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0, 0);
        sliderRt.anchorMax = new Vector2(1, 0.5f);
        sliderRt.offsetMin = new Vector2(15, 10);
        sliderRt.offsetMax = new Vector2(-15, -5);

        var bgObj = CreateChild(sliderObj.transform, "Background");
        var bg = bgObj.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.08f, 0.06f, 1f);
        Stretch(bgObj.GetComponent<RectTransform>(), 0);

        var fillArea = CreateChild(sliderObj.transform, "Fill Area");
        Stretch(fillArea.GetComponent<RectTransform>(), 0);
        var fill = CreateChild(fillArea.transform, "Fill");
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.3f, 0.8f, 0.3f, 1f);
        Stretch(fill.GetComponent<RectTransform>(), 0);

        var slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.minValue = 0;
        slider.maxValue = 6;
        slider.value = 0;
        slider.interactable = false;
        return sliderObj;
    }

    private static GameObject CreateOverlay(Transform parent, string name)
    {
        var obj = CreateChild(parent, name);
        var overlay = obj.AddComponent<Image>();
        overlay.color = new Color(0, 0, 0, 0.78f);
        Stretch(obj.GetComponent<RectTransform>(), 0);
        return obj;
    }

    private static GameObject CreateWindow(Transform parent, string name, Vector2 size)
    {
        var obj = CreateChild(parent, name);
        var bg = obj.AddComponent<Image>();
        bg.color = new Color(0.24f, 0.17f, 0.10f, 0.99f);
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        return obj;
    }

    private static GameObject CreateColumnPanel(
        Transform parent,
        string name,
        string title,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        var panel = CreateChild(parent, name);
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.14f, 0.11f, 0.08f, 0.42f);

        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        var titleText = CreateText(panel.transform, "Header", title, 22, FontStyles.Bold, TextAlignmentOptions.Center);
        titleText.color = new Color(0.98f, 0.84f, 0.46f, 1f);
        var titleRt = titleText.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, -8);
        titleRt.sizeDelta = new Vector2(-24, 36);

        return panel;
    }

    private static TextMeshProUGUI CreatePopupTitle(Transform parent, string text)
    {
        var title = CreateText(parent, "Title", text, 36, FontStyles.Bold, TextAlignmentOptions.Center);
        title.color = new Color(0.98f, 0.86f, 0.56f, 1f);
        var rt = title.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -18);
        rt.sizeDelta = new Vector2(-40, 52);
        return title;
    }

    private static GameObject CreateListContainer(Transform parent, Vector2 offsetMin, Vector2 offsetMax)
    {
        var scrollObj = CreateChild(parent, "ListScroll");
        var scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0, 0);
        scrollRt.anchorMax = new Vector2(1, 1);
        scrollRt.offsetMin = offsetMin;
        scrollRt.offsetMax = offsetMax;

        var viewportObj = CreateChild(scrollObj.transform, "Viewport");
        var viewportImage = viewportObj.AddComponent<Image>();
        viewportImage.color = new Color(0.05f, 0.035f, 0.025f, 0.18f);
        viewportObj.AddComponent<RectMask2D>();
        Stretch(viewportObj.GetComponent<RectTransform>(), 0);

        var obj = CreateChild(viewportObj.transform, "ListContainer");
        var objRt = obj.GetComponent<RectTransform>();
        objRt.anchorMin = new Vector2(0, 1);
        objRt.anchorMax = new Vector2(1, 1);
        objRt.pivot = new Vector2(0.5f, 1);
        objRt.anchoredPosition = Vector2.zero;
        objRt.sizeDelta = Vector2.zero;

        var layout = obj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12;
        layout.padding = new RectOffset(18, 18, 12, 12);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        var fitter = obj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.viewport = viewportObj.GetComponent<RectTransform>();
        scroll.content = objRt;
        scroll.horizontal = false;
        scroll.vertical = true;
        return obj;
    }

    private static GameObject CreateAnchoredButton(Transform parent, string name, string text, float width, Vector2 anchoredPosition)
    {
        var btn = CreateButton(parent, name, text, width);
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0);
        rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = anchoredPosition;
        var le = btn.GetComponent<LayoutElement>();
        if (le != null)
            Object.DestroyImmediate(le);
        rt.sizeDelta = new Vector2(width, 56);
        return btn;
    }

    private static GameObject CreateButton(Transform parent, string name, string text, float width)
    {
        var btnObj = CreateChild(parent, name);
        btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 56);
        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0.17f, 0.28f, 0.14f, 1f);
        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;

        var colors = btn.colors;
        colors.normalColor = new Color(0.17f, 0.28f, 0.14f, 1f);
        colors.highlightedColor = new Color(0.23f, 0.38f, 0.18f, 1f);
        colors.pressedColor = new Color(0.12f, 0.20f, 0.10f, 1f);
        colors.disabledColor = new Color(0.16f, 0.15f, 0.12f, 0.72f);
        btn.colors = colors;

        var le = btnObj.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        le.preferredHeight = 56;

        var label = CreateText(btnObj.transform, "Text", text, 24, FontStyles.Bold, TextAlignmentOptions.Center);
        label.color = new Color(0.98f, 0.86f, 0.58f, 1f);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        Stretch(label.GetComponent<RectTransform>(), 8);
        return btnObj;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize, FontStyles style, TextAlignmentOptions alignment)
    {
        var obj = CreateChild(parent, name);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        return tmp;
    }

    private static GameObject CreateChild(Transform parent, string name)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    private static void Stretch(RectTransform rt, float padding)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);
    }

    private static T LoadPrefabComponent<T>(string prefabName) where T : Component
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/{prefabName}.prefab");
        return prefab != null ? prefab.GetComponent<T>() : null;
    }

    private static List<T> LoadAssets<T>(string path) where T : Object
    {
        var result = new List<T>();
        foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { path }))
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
                result.Add(asset);
        }
        return result;
    }

    private static bool HasTestData()
    {
        return AssetDatabase.IsValidFolder(ITEMS_PATH) &&
               AssetDatabase.IsValidFolder(THEMES_PATH) &&
               AssetDatabase.IsValidFolder(INSPIRATIONS_PATH);
    }

    private static bool HasPrefabs()
    {
        return AssetDatabase.IsValidFolder(PREFABS_PATH) &&
               AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/ShelfSlot.prefab") != null &&
               AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/InspirationDisplaySlot.prefab") != null;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var type = target.GetType();
        while (type != null)
        {
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
                return;
            }
            type = type.BaseType;
        }

        Debug.LogWarning($"[SceneBuilder] Field not found: {target.GetType().Name}.{fieldName}");
    }
}
