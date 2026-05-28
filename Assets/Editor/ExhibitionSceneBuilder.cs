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

    private const float SHELF_ANCHOR_MAX = 0.408f;
    private const float RIGHT_PANEL_ANCHOR_MIN = 0.422f;
    private const float CONTROL_HEIGHT = 112f;
    private const float ITEM_CELL_SIZE = 140f;
    private const int ITEMS_PER_ROW = 4;
    private static readonly Vector2 BACKGROUND_REFERENCE_SIZE = new(1672f, 941f);
    private static readonly Vector2[] SHELF_SLOT_POSITIONS =
    {
        new(-808.6f, 279.23f), new(-627.9f, 285.37f), new(-457.2f, 289.7f), new(-290.7f, 279.95f),
        new(-802.64f, 121.2f), new(-627.9f, 115.7f), new(-455.43f, 136.1f), new(-286.5f, 121.2f),
        new(-802.64f, -29.369f), new(-627.9f, -32.5f), new(-459.5f, -55.9f), new(-290.1f, -44f),
        new(-808.6f, -200.6f), new(-631.7f, -187.7f), new(-454f, -213f), new(-291.9f, -209.9f),
    };
    private static readonly Vector2[] SHELF_SLOT_SIZES =
    {
        new(118.008f, 131.29f), new(114.508f, 119.007f), new(105.01f, 94.012f), new(73.013f, 74.517f),
        new(106.09f, 90.88f), new(70.179f, 69.209f), new(145f, 165f), new(91.922f, 85.179f),
        new(115.23f, 107.491f), new(81.483f, 95.767f), new(74.359f, 64.042f), new(98.868f, 109.428f),
        new(98.579f, 98.769f), new(130.472f, 138.79f), new(80.765f, 85.586f), new(91.924f, 97.199f),
    };
    private static readonly float[] SHELF_SLOT_ROTATIONS =
    {
        0f, 0f, 0f, 0f,
        0f, 0f, 0f, 0f,
        0f, 0f, 356.446f, 17.068f,
        0f, 0f, 8.378f, 352.339f,
    };

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
        var layoutRoot = CreateLayoutRoot(canvas);
        CreateBackground(layoutRoot);

        var shelfPanel = CreateShelfPanel(layoutRoot);
        var visitorPanel = CreateVisitorPanel(layoutRoot);
        var satisfactionBar = CreateSatisfactionBar(layoutRoot);
        var displayPanel = CreateDisplayPanel(layoutRoot);
        var controlPanel = CreateControlPanel(layoutRoot);
        var themePopup = CreateThemePopup(layoutRoot);
        var inspirationPopup = CreateInspirationPopup(layoutRoot);
        var tooltip = CreateTooltip(layoutRoot);

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

    private static RectTransform CreateLayoutRoot(Canvas canvas)
    {
        var rootObj = CreateChild(canvas.transform, "LayoutRoot");
        var rt = rootObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var fitter = rootObj.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 1672f / 941f;
        return rt;
    }

    private static void CreateBackground(Transform parent)
    {
        var bgObj = CreateChild(parent, "Background");
        var img = bgObj.AddComponent<Image>();
        img.sprite = LoadSprite("Assets/Resources/Exhibitions/Icons/back-pic.png");
        img.color = Color.white;
        img.preserveAspect = false;
        img.raycastTarget = false;
        Stretch(bgObj.GetComponent<RectTransform>(), 0);
    }

    private static ShelfPanel CreateShelfPanel(Transform parent)
    {
        var panelObj = CreateChild(parent, "ShelfPanel");

        var rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.013f, 0.105f);
        rt.anchorMax = new Vector2(SHELF_ANCHOR_MAX, 0.985f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var title = CreateText(panelObj.transform, "Title", "Items", 32, FontStyles.Bold, TextAlignmentOptions.Center);
        title.color = new Color(0.98f, 0.9f, 0.72f, 1f);
        var titleRt = title.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, -28);
        titleRt.sizeDelta = new Vector2(-32, 48);

        var slotsObj = CreateChild(parent, "ShelfItemOverlay");
        ConfigureShelfSlotsContainer(slotsObj.GetComponent<RectTransform>());
        var referenceScaler = slotsObj.AddComponent<ReferenceRectScaler>();
        referenceScaler.SetReferenceSize(BACKGROUND_REFERENCE_SIZE);

        var panel = panelObj.AddComponent<ShelfPanel>();
        SetPrivateField(panel, "_gridContainer", slotsObj.transform);
        var slotPrefab = LoadPrefabComponent<ShelfSlotUI>("ShelfSlot");
        var previewItems = LoadAssets<ExhibitItemData>(ITEMS_PATH).OrderBy(item => item.sortOrder).ToList();
        var manualSlots = slotPrefab != null ? CreateManualShelfSlots(slotsObj.transform, slotPrefab, previewItems) : new List<ShelfSlotUI>();
        if (slotPrefab != null)
            SetPrivateField(panel, "_slotPrefab", slotPrefab);
        SetPrivateField(panel, "_manualSlots", manualSlots);

        return panel;
    }

    private static void ConfigureShelfSlotsContainer(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    private static List<ShelfSlotUI> CreateManualShelfSlots(Transform parent, ShelfSlotUI slotPrefab, IReadOnlyList<ExhibitItemData> previewItems)
    {
        var slots = new List<ShelfSlotUI>();

        for (int i = 0; i < ITEMS_PER_ROW * ITEMS_PER_ROW; i++)
        {
            var slotObject = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab.gameObject, parent);
            var slot = slotObject.GetComponent<ShelfSlotUI>();
            slot.name = $"Slot_{i + 1:00}";
            slot.SetSlotIndex(i);

            ApplyManualShelfSlotLayout(slot, i);
            ApplyShelfSlotPreview(slot, i, previewItems);
            PrefabUtility.RecordPrefabInstancePropertyModifications(slot.GetComponent<RectTransform>());
            PrefabUtility.RecordPrefabInstancePropertyModifications(slot.GetComponent<Image>());
            slots.Add(slot);
        }

        return slots;
    }

    private static void ApplyManualShelfSlotLayout(ShelfSlotUI slot, int index)
    {
        if (slot == null) return;

        var rt = slot.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = GetArrayValue(SHELF_SLOT_POSITIONS, index, Vector2.zero);
        rt.sizeDelta = GetArrayValue(SHELF_SLOT_SIZES, index, new Vector2(ITEM_CELL_SIZE, ITEM_CELL_SIZE));
        rt.localScale = Vector3.one;
        rt.localEulerAngles = new Vector3(0f, 0f, GetArrayValue(SHELF_SLOT_ROTATIONS, index, 0f));
    }

    private static void ApplyShelfSlotPreview(ShelfSlotUI slot, int index, IReadOnlyList<ExhibitItemData> previewItems)
    {
        if (slot == null || previewItems == null || index >= previewItems.Count)
            return;

        var item = previewItems[index];
        var icon = slot.GetComponent<Image>();
        if (icon == null)
            return;

        icon.sprite = item.icon;
        icon.enabled = item.icon != null;
        icon.preserveAspect = true;
        icon.raycastTarget = true;
        icon.rectTransform.localScale = Vector3.one;
    }

    private static Vector2 GetArrayValue(Vector2[] values, int index, Vector2 fallback)
    {
        return values != null && index >= 0 && index < values.Length ? values[index] : fallback;
    }

    private static float GetArrayValue(float[] values, int index, float fallback)
    {
        return values != null && index >= 0 && index < values.Length ? values[index] : fallback;
    }

    private static VisitorPanel CreateVisitorPanel(Transform parent)
    {
        var panelObj = CreateChild(parent, "VisitorPanel");

        var rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(RIGHT_PANEL_ANCHOR_MIN, 0.525f);
        rt.anchorMax = new Vector2(0.985f, 0.985f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        CreateVisitorBackground(panelObj.transform);

        var charContainer = CreateChild(panelObj.transform, "CharacterContainer");
        var charCg = charContainer.AddComponent<CanvasGroup>();
        charCg.alpha = 0f;
        var rawImage = charContainer.AddComponent<RawImage>();
        rawImage.color = Color.white;
        rawImage.raycastTarget = false;
        rawImage.texture = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Resources/Exhibitions/VisitorRT.asset");
        var fitter = charContainer.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 1f;
        var charRt = charContainer.GetComponent<RectTransform>();
        charRt.anchorMin = new Vector2(0, 0.18f);
        charRt.anchorMax = new Vector2(1, 1);
        charRt.offsetMin = new Vector2(26, 8);
        charRt.offsetMax = new Vector2(-26, -16);

        var dialogue = CreateChild(panelObj.transform, "DialoguePanel");
        var dialogueBg = dialogue.AddComponent<Image>();
        dialogueBg.color = new Color(0.12f, 0.07f, 0.06f, 0.96f);
        var dialogueCg = dialogue.AddComponent<CanvasGroup>();
        var dialogueRt = dialogue.GetComponent<RectTransform>();
        dialogueRt.anchorMin = new Vector2(0, 0);
        dialogueRt.anchorMax = new Vector2(1, 0);
        dialogueRt.pivot = new Vector2(0.5f, 0);
        dialogueRt.anchoredPosition = new Vector2(-5.867f, 7.523f);
        dialogueRt.sizeDelta = new Vector2(-3.593f, 98.477f);

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

    private static void CreateVisitorBackground(Transform parent)
    {
        var backgroundObj = CreateChild(parent, "PassengerBackground");
        backgroundObj.transform.SetAsFirstSibling();
        var background = backgroundObj.AddComponent<Image>();
        background.sprite = LoadSprite("Assets/Resources/Exhibitions/Icons/passenger-background.png");
        background.color = Color.white;
        background.raycastTarget = false;
        background.preserveAspect = false;
        background.type = Image.Type.Simple;

        var rt = backgroundObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-6.093689f, -4.980011f);
        rt.sizeDelta = new Vector2(-12.188f, -45.96f);
    }

    private static SatisfactionBar CreateSatisfactionBar(Transform parent)
    {
        var barObj = CreateChild(parent, "SatisfactionBar");

        var rt = barObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(RIGHT_PANEL_ANCHOR_MIN, 0.428f);
        rt.anchorMax = new Vector2(0.985f, 0.515f);
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

    private static DisplayPanel CreateDisplayPanel(Transform parent)
    {
        var panelObj = CreateChild(parent, "DisplayPanel");

        var rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(RIGHT_PANEL_ANCHOR_MIN, 0.145f);
        rt.anchorMax = new Vector2(0.985f, 0.405f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var containerObj = CreateChild(panelObj.transform, "SlotContainer");
        var containerRt = containerObj.GetComponent<RectTransform>();
        containerRt.anchorMin = Vector2.zero;
        containerRt.anchorMax = Vector2.one;
        containerRt.offsetMin = new Vector2(24, 34);
        containerRt.offsetMax = new Vector2(-28, -6);

        var grid = containerObj.AddComponent<GridLayoutGroup>();
        grid.spacing = new Vector2(18, 12);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.cellSize = new Vector2(240, 230);

        var panel = panelObj.AddComponent<DisplayPanel>();
        SetPrivateField(panel, "_slotContainer", containerObj.transform);
        SetPrivateField(panel, "_gridLayout", grid);
        var slotPrefab = LoadPrefabComponent<InspirationDisplaySlot>("InspirationDisplaySlot");
        if (slotPrefab != null)
            SetPrivateField(panel, "_slotPrefab", slotPrefab);
        return panel;
    }

    private static GameObject CreateControlPanel(Transform parent)
    {
        var panelObj = CreateChild(parent, "ControlPanel");

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

        var selectBtnObj = CreateButton(panelObj.transform, "SelectButton", "Select Theme", 300, true);
        var startBtnObj = CreateButton(panelObj.transform, "StartButton", "Start Exhibition", 340, true);

        var selector = panelObj.AddComponent<ThemeSelector>();
        SetPrivateField(selector, "_selectButton", selectBtnObj.GetComponent<Button>());
        SetPrivateField(selector, "_startButton", startBtnObj.GetComponent<Button>());
        SetPrivateField(selector, "_startButtonText", startBtnObj.GetComponentInChildren<TextMeshProUGUI>());
        return panelObj;
    }

    private static ThemeSelectionPopup CreateThemePopup(Transform parent)
    {
        var panelObj = CreateOverlay(parent, "ThemeSelectionPopup");
        var windowObj = CreateWindow(panelObj.transform, "Window", new Vector2(860, 680));
        ConfigureLargePopupWindow(windowObj);
        CreatePopupTitle(windowObj.transform, "Theme Selection");

        var hint = CreateText(windowObj.transform, "HintText", "Choose an exhibition theme.", 22, FontStyles.Normal, TextAlignmentOptions.Center);
        var hintRt = hint.GetComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0, 1);
        hintRt.anchorMax = new Vector2(1, 1);
        hintRt.pivot = new Vector2(0.5f, 1);
        hintRt.anchoredPosition = new Vector2(0, -76);
        hintRt.sizeDelta = new Vector2(-96, 48);
        hint.color = new Color(0.96f, 0.86f, 0.66f, 1f);

        var listObj = CreateListContainer(windowObj.transform, new Vector2(34, 132), new Vector2(-34, -128));
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

    private static InspirationSelectionPopup CreateInspirationPopup(Transform parent)
    {
        var panelObj = CreateOverlay(parent, "InspirationSelectionPopup");
        var windowObj = CreateWindow(panelObj.transform, "Window", new Vector2(860, 680));
        ConfigureLargePopupWindow(windowObj);
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
            new Vector2(0.49f, 1),
            new Vector2(40, 176),
            new Vector2(-14, -118));
        var selectedList = CreateListContainer(selectedPanel.transform, Vector2.zero, new Vector2(0, -48));

        var libraryPanel = CreateColumnPanel(
            windowObj.transform,
            "LibraryColumn",
            "Inspiration Library",
            new Vector2(0.51f, 0),
            new Vector2(1, 1),
            new Vector2(14, 176),
            new Vector2(-40, -118));
        var listObj = CreateListContainer(libraryPanel.transform, Vector2.zero, new Vector2(0, -48));

        var hintPanel = CreateChild(windowObj.transform, "HintPanel");
        var hintBg = hintPanel.AddComponent<Image>();
        hintBg.color = new Color(0.12f, 0.08f, 0.05f, 0.92f);
        var hintRt = hintPanel.GetComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0, 0);
        hintRt.anchorMax = new Vector2(1, 0);
        hintRt.pivot = new Vector2(0.5f, 0);
        hintRt.anchoredPosition = new Vector2(0, 88);
        hintRt.sizeDelta = new Vector2(-80, 74);

        var avatarObj = CreateChild(hintPanel.transform, "AvatarPlaceholder");
        var avatar = avatarObj.AddComponent<Image>();
        avatar.color = new Color(0.72f, 0.58f, 0.42f, 1f);
        var avatarRt = avatarObj.GetComponent<RectTransform>();
        avatarRt.anchorMin = new Vector2(0, 0.5f);
        avatarRt.anchorMax = new Vector2(0, 0.5f);
        avatarRt.pivot = new Vector2(0, 0.5f);
        avatarRt.anchoredPosition = new Vector2(14, 0);
        avatarRt.sizeDelta = new Vector2(54, 54);

        var avatarLabel = CreateText(avatarObj.transform, "Label", "Rin", 18, FontStyles.Bold, TextAlignmentOptions.Center);
        avatarLabel.color = new Color(0.22f, 0.12f, 0.06f, 1f);
        Stretch(avatarLabel.GetComponent<RectTransform>(), 0);

        var hint = CreateText(hintPanel.transform, "HintText", "Hmm... which ones should I choose?", 22, FontStyles.Italic, TextAlignmentOptions.MidlineLeft);
        var hintTextRt = hint.GetComponent<RectTransform>();
        hintTextRt.anchorMin = new Vector2(0, 0);
        hintTextRt.anchorMax = new Vector2(1, 1);
        hintTextRt.offsetMin = new Vector2(84, 10);
        hintTextRt.offsetMax = new Vector2(-18, -10);
        hint.textWrappingMode = TextWrappingModes.Normal;
        hint.color = new Color(0.98f, 0.88f, 0.66f, 1f);

        var confirmBtn = CreateAnchoredButton(windowObj.transform, "ConfirmButton", "Confirm Inspirations", 310, new Vector2(155, 20));
        var closeBtn = CreateAnchoredButton(windowObj.transform, "CloseButton", "Back", 220, new Vector2(-180, 20));
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

    private static ItemTooltip CreateTooltip(Transform parent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/ItemTooltip.prefab");
        if (prefab == null) return null;

        var tooltipObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
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

    private static void ConfigureLargePopupWindow(GameObject windowObj)
    {
        var rt = windowObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.1f);
        rt.anchorMax = new Vector2(0.9f, 0.9f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
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
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 22.5f;
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

    private static GameObject CreateButton(Transform parent, string name, string text, float width, bool useImageButton = false)
    {
        var btnObj = CreateChild(parent, name);
        btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 56);
        var img = btnObj.AddComponent<Image>();
        if (useImageButton)
        {
            img.sprite = LoadSprite("Assets/Resources/Exhibitions/Icons/button-1.png");
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(0.17f, 0.28f, 0.14f, 1f);
        }
        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;

        var colors = btn.colors;
        if (useImageButton)
        {
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.96f, 0.88f, 1f);
            colors.pressedColor = new Color(0.88f, 0.78f, 0.64f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.52f);
        }
        else
        {
            colors.normalColor = new Color(0.17f, 0.28f, 0.14f, 1f);
            colors.highlightedColor = new Color(0.23f, 0.38f, 0.18f, 1f);
            colors.pressedColor = new Color(0.12f, 0.20f, 0.10f, 1f);
            colors.disabledColor = new Color(0.16f, 0.15f, 0.12f, 0.72f);
        }
        btn.colors = colors;

        var le = btnObj.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        le.preferredHeight = 56;

        var label = CreateText(btnObj.transform, "Text", text, 24, FontStyles.Bold, TextAlignmentOptions.Center);
        label.color = useImageButton ? new Color(0.22f, 0.12f, 0.06f, 1f) : new Color(0.98f, 0.86f, 0.58f, 1f);
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

    private static Sprite LoadSprite(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
            return sprite;

        return AssetDatabase.LoadAllAssetRepresentationsAtPath(path).OfType<Sprite>().FirstOrDefault();
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
