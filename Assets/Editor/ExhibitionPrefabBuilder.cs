using ExhibitionSystem.Core;
using ExhibitionSystem.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ExhibitionPrefabBuilder
{
    private const string PREFAB_PATH = "Assets/Resources/Exhibitions/Prefabs";

    [MenuItem("Tools/Museum/Rebuild Prefabs")]
    public static void RebuildPrefabs()
    {
        EnsureDirectories();

        CreateShelfSlotPrefab();
        CreateDisplaySlotPrefab();
        CreateThemeListItemPrefab();
        CreateInspirationListItemPrefab();
        CreateInspirationDisplaySlotPrefab();
        CreateItemTooltipPrefab();
        CreateOrUpdateVisitorRT();
        CreateVisitorPanelPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[PrefabBuilder] Prefabs rebuilt in {PREFAB_PATH}");
    }

    private static void CreateShelfSlotPrefab()
    {
        var root = CreateUIObject("ShelfSlot", new Vector2(150, 150));
        var icon = root.AddComponent<Image>();
        icon.color = Color.white;
        icon.preserveAspect = true;
        icon.raycastTarget = true;
        root.AddComponent<CanvasGroup>();

        var slotUI = root.AddComponent<ShelfSlotUI>();
        SetPrivateField(slotUI, "_icon", icon);

        SaveAndDestroy(root, "ShelfSlot");
    }

    private static void CreateDisplaySlotPrefab()
    {
        var root = CreateUIObject("DisplaySlot", new Vector2(172, 152));
        var bg = root.AddComponent<Image>();
        var defaultFrame = LoadSprite("Assets/Resources/Exhibitions/Icons/frame-no-item.png");
        var hoverFrame = LoadSprite("Assets/Resources/Exhibitions/Icons/frame-item-hover.png");
        bg.sprite = defaultFrame;
        bg.color = Color.white;
        bg.type = Image.Type.Simple;
        bg.preserveAspect = false;
        root.AddComponent<CanvasGroup>();

        var highlightObj = CreateChild(root.transform, "Highlight");
        var highlight = highlightObj.AddComponent<Image>();
        highlight.color = new Color(1f, 0.78f, 0.28f, 0.08f);
        highlight.raycastTarget = false;
        highlight.enabled = false;
        Stretch(highlightObj.GetComponent<RectTransform>(), 0);

        var iconObj = CreateChild(root.transform, "ItemIcon");
        var icon = iconObj.AddComponent<Image>();
        icon.raycastTarget = false;
        icon.enabled = false;
        icon.preserveAspect = true;
        Stretch(iconObj.GetComponent<RectTransform>(), 18);

        var badgeObj = CreateChild(root.transform, "StatusBadge");
        var badge = badgeObj.AddComponent<Image>();
        badge.color = Color.white;
        badge.raycastTarget = false;
        badge.enabled = false;
        var badgeRt = badgeObj.GetComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(1, 1);
        badgeRt.anchorMax = new Vector2(1, 1);
        badgeRt.pivot = new Vector2(1, 1);
        badgeRt.sizeDelta = new Vector2(18, 18);
        badgeRt.anchoredPosition = new Vector2(-8, -8);

        var slotUI = root.AddComponent<DisplaySlotUI>();
        SetPrivateField(slotUI, "_highlight", highlight);
        SetPrivateField(slotUI, "_frameImage", bg);
        SetPrivateField(slotUI, "_defaultFrameSprite", defaultFrame);
        SetPrivateField(slotUI, "_hoverFrameSprite", hoverFrame);
        SetPrivateField(slotUI, "_normalColor", new Color(1f, 1f, 1f, 0f));
        SetPrivateField(slotUI, "_itemIcon", icon);
        SetPrivateField(slotUI, "_statusBadge", badge);

        SaveAndDestroy(root, "DisplaySlot");
    }

    private static void CreateThemeListItemPrefab()
    {
        var root = CreateUIObject("ThemeListItem", new Vector2(760, 82));
        var itemLayout = root.AddComponent<LayoutElement>();
        itemLayout.minHeight = 82;
        itemLayout.preferredHeight = 82;

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.78f, 0.66f, 0.46f, 0.98f);
        var button = root.AddComponent<Button>();
        button.targetGraphic = bg;

        var frameObj = CreateChild(root.transform, "SelectionFrame");
        var frame = frameObj.AddComponent<Image>();
        frame.color = new Color(1f, 0.82f, 0.28f, 0.38f);
        frame.raycastTarget = false;
        frame.enabled = false;
        frameObj.AddComponent<LayoutElement>().ignoreLayout = true;
        Stretch(frameObj.GetComponent<RectTransform>(), 0);

        var layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 8, 8);
        layout.spacing = 24;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;
        layout.childAlignment = TextAnchor.MiddleLeft;

        var iconObj = CreateChild(root.transform, "Icon");
        var icon = iconObj.AddComponent<Image>();
        icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Exhibitions/Icons/sakura.png");
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        var iconLayout = iconObj.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 60;
        iconLayout.preferredHeight = 60;

        var content = CreateChild(root.transform, "Content");
        var contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 0;
        contentLayout.childAlignment = TextAnchor.MiddleLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        content.AddComponent<LayoutElement>().flexibleWidth = 1;

        var titleText = CreateText(content.transform, "Title", "Theme", 34, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        titleText.color = new Color(0.22f, 0.12f, 0.06f, 1f);
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        titleText.overflowMode = TextOverflowModes.Ellipsis;
        titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 56;

        var completedObj = CreateChild(root.transform, "CompletedIcon");
        var completedIcon = completedObj.AddComponent<Image>();
        completedIcon.sprite = LoadSprite("Assets/Resources/Exhibitions/Icons/completed-label.png");
        completedIcon.color = Color.white;
        completedIcon.preserveAspect = true;
        completedIcon.raycastTarget = false;
        completedObj.SetActive(false);
        var completedLayout = completedObj.AddComponent<LayoutElement>();
        completedLayout.preferredWidth = 150;
        completedLayout.preferredHeight = 46;

        var listItem = root.AddComponent<ThemeListItem>();
        SetPrivateField(listItem, "_titleText", titleText);
        SetPrivateField(listItem, "_completedIcon", completedIcon);
        SetPrivateField(listItem, "_selectionFrame", frame);
        SetPrivateField(listItem, "_background", bg);
        SetPrivateField(listItem, "_button", button);

        SaveAndDestroy(root, "ThemeListItem");
    }

    private static void CreateInspirationListItemPrefab()
    {
        var root = CreateUIObject("InspirationListItem", new Vector2(360, 96));
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.74f, 0.63f, 0.48f, 0.98f);
        var button = root.AddComponent<Button>();
        button.targetGraphic = bg;

        var frameObj = CreateChild(root.transform, "SelectionFrame");
        var frame = frameObj.AddComponent<Image>();
        frame.color = new Color(1f, 0.78f, 0.28f, 0.26f);
        frame.raycastTarget = false;
        frame.enabled = false;
        frameObj.AddComponent<LayoutElement>().ignoreLayout = true;
        Stretch(frameObj.GetComponent<RectTransform>(), 0);

        var layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 10, 10);
        layout.spacing = 10;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;
        layout.childAlignment = TextAnchor.MiddleLeft;

        var bodyText = CreateText(root.transform, "Body", "Idea text", 22, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        bodyText.color = new Color(0.23f, 0.14f, 0.08f, 1f);
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        bodyText.overflowMode = TextOverflowModes.Ellipsis;
        bodyText.maxVisibleLines = 2;
        bodyText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        var matchBadgeObj = CreateChild(root.transform, "MatchBadge");
        var matchBadgeIcon = matchBadgeObj.AddComponent<Image>();
        matchBadgeIcon.color = Color.white;
        matchBadgeIcon.preserveAspect = true;
        matchBadgeIcon.raycastTarget = false;
        var matchBadgeLayout = matchBadgeObj.AddComponent<LayoutElement>();
        matchBadgeLayout.preferredWidth = 50;
        matchBadgeLayout.preferredHeight = 50;
        matchBadgeLayout.flexibleWidth = 0;
        matchBadgeObj.SetActive(false);

        var matchBadge = matchBadgeObj.AddComponent<InspirationMatchBadge>();
        SetPrivateField(matchBadge, "_badgeIcon", matchBadgeIcon);

        var item = root.AddComponent<InspirationListItem>();
        SetPrivateField(item, "_bodyText", bodyText);
        SetPrivateField(item, "_selectionImage", bg);
        SetPrivateField(item, "_selectionFrame", frame);
        SetPrivateField(item, "_matchBadge", matchBadge);
        SetPrivateField(item, "_button", button);

        SaveAndDestroy(root, "InspirationListItem");
    }

    private static void CreateInspirationDisplaySlotPrefab()
    {
        var root = CreateUIObject("InspirationDisplaySlot", new Vector2(230, 224));

        var labelObj = CreateChild(root.transform, "LabelStrip");
        var labelBg = labelObj.AddComponent<Image>();
        labelBg.sprite = LoadSprite("Assets/Resources/Exhibitions/Icons/label.png");
        labelBg.color = Color.white;
        labelBg.type = Image.Type.Simple;
        labelBg.preserveAspect = false;
        var labelButton = labelObj.AddComponent<Button>();
        labelButton.targetGraphic = labelBg;
        var labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0.5f, 1f);
        labelRt.anchorMax = new Vector2(0.5f, 1f);
        labelRt.pivot = new Vector2(0.5f, 1f);
        labelRt.anchoredPosition = new Vector2(0f, -20f);
        labelRt.sizeDelta = new Vector2(172f, 42f);

        var ideaText = CreateText(labelObj.transform, "InspirationText", "Jiro recreated...", 20, FontStyles.Bold, TextAlignmentOptions.Center);
        ideaText.color = new Color(0.25f, 0.14f, 0.08f, 1f);
        ideaText.textWrappingMode = TextWrappingModes.NoWrap;
        ideaText.overflowMode = TextOverflowModes.Ellipsis;
        Stretch(ideaText.GetComponent<RectTransform>(), 6);

        var displaySlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_PATH}/DisplaySlot.prefab");
        GameObject displaySlotObj;
        if (displaySlotPrefab != null)
            displaySlotObj = (GameObject)PrefabUtility.InstantiatePrefab(displaySlotPrefab, root.transform);
        else
            displaySlotObj = CreateUIObject("DisplaySlot", new Vector2(172, 152));

        displaySlotObj.name = "DisplaySlot";
        displaySlotObj.transform.SetParent(root.transform, false);
        var displayRt = displaySlotObj.GetComponent<RectTransform>();
        displayRt.anchorMin = new Vector2(0.5f, 0.5f);
        displayRt.anchorMax = new Vector2(0.5f, 0.5f);
        displayRt.pivot = new Vector2(0.5f, 0.5f);
        displayRt.sizeDelta = new Vector2(172, 152);
        displayRt.anchoredPosition = new Vector2(0, -26);
        var displaySlot = displaySlotObj.GetComponent<DisplaySlotUI>();

        var tooltipObj = CreateChild(root.transform, "InspirationTooltip");
        var tooltipBg = tooltipObj.AddComponent<Image>();
        tooltipBg.color = new Color(0.88f, 0.78f, 0.58f, 0.98f);
        tooltipBg.raycastTarget = false;
        tooltipObj.SetActive(false);
        tooltipObj.AddComponent<LayoutElement>().ignoreLayout = true;
        var tooltipRt = tooltipObj.GetComponent<RectTransform>();
        tooltipRt.anchorMin = new Vector2(0.5f, 1);
        tooltipRt.anchorMax = new Vector2(0.5f, 1);
        tooltipRt.pivot = new Vector2(0.5f, 0);
        tooltipRt.anchoredPosition = new Vector2(0, 8);
        tooltipRt.sizeDelta = new Vector2(360, 118);

        var tooltipText = CreateText(tooltipObj.transform, "Text", "Full inspiration text.", 22, FontStyles.Normal, TextAlignmentOptions.Center);
        tooltipText.color = new Color(0.22f, 0.13f, 0.07f, 1f);
        tooltipText.textWrappingMode = TextWrappingModes.Normal;
        tooltipText.raycastTarget = false;
        Stretch(tooltipText.GetComponent<RectTransform>(), 12);

        var slot = root.AddComponent<InspirationDisplaySlot>();
        SetPrivateField(slot, "_inspirationText", ideaText);
        SetPrivateField(slot, "_displaySlot", displaySlot);
        SetPrivateField(slot, "_tooltipPanel", tooltipObj);
        SetPrivateField(slot, "_tooltipText", tooltipText);
        SetPrivateField(slot, "_labelButton", labelButton);
        SetPrivateField(slot, "_labelBackground", labelBg);

        SaveAndDestroy(root, "InspirationDisplaySlot");
    }

    private static void CreateItemTooltipPrefab()
    {
        var root = CreateUIObject("ItemTooltip", new Vector2(320, 190));
        var panel = root.AddComponent<Image>();
        panel.color = new Color(0.15f, 0.12f, 0.1f, 0.95f);
        var cg = root.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        var layout = root.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 18, 18);
        layout.spacing = 8;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        root.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var nameText = CreateText(root.transform, "NameText", "Item Name", 24, FontStyles.Bold, TextAlignmentOptions.Left);
        var separatorObj = CreateChild(root.transform, "Separator");
        var separator = separatorObj.AddComponent<Image>();
        separator.color = new Color(0.5f, 0.45f, 0.4f, 0.5f);
        separatorObj.AddComponent<LayoutElement>().preferredHeight = 1;
        var descText = CreateText(root.transform, "DescriptionText", "Item description.", 18, FontStyles.Normal, TextAlignmentOptions.Left);
        descText.textWrappingMode = TextWrappingModes.Normal;

        var historyObj = CreateChild(root.transform, "HistoryContainer");
        var historyLayout = historyObj.AddComponent<VerticalLayoutGroup>();
        historyLayout.spacing = 3;
        historyLayout.childControlWidth = true;
        historyLayout.childControlHeight = false;

        var historyEntryObj = CreateChild(historyObj.transform, "HistoryEntry");
        var historyEntryText = CreateText(historyEntryObj.transform, "Text", "Matched idea", 15, FontStyles.Normal, TextAlignmentOptions.Left);
        historyEntryText.color = new Color(0.55f, 0.9f, 0.55f, 1f);
        Stretch(historyEntryText.GetComponent<RectTransform>(), 0);
        historyEntryObj.SetActive(false);

        var tooltip = root.AddComponent<ItemTooltip>();
        SetPrivateField(tooltip, "_canvasGroup", cg);
        SetPrivateField(tooltip, "_panel", root.GetComponent<RectTransform>());
        SetPrivateField(tooltip, "_nameText", nameText);
        SetPrivateField(tooltip, "_descriptionText", descText);
        SetPrivateField(tooltip, "_historyContainer", historyObj.transform);
        SetPrivateField(tooltip, "_historyEntryPrefab", historyEntryObj);

        SaveAndDestroy(root, "ItemTooltip");
    }

    private static void CreateOrUpdateVisitorRT()
    {
        string rtPath = "Assets/Resources/Exhibitions/VisitorRT.asset";
        var existingRT = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
        if (existingRT != null) return;

        var rt = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32)
        {
            name = "VisitorRT",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        AssetDatabase.CreateAsset(rt, rtPath);
    }

    private static void CreateVisitorPanelPrefab()
    {
        var root = CreateUIObject("VisitorPanel", new Vector2(300, 220));
        var layout = root.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 8;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        CreateVisitorBackground(root.transform);

        var charContainer = CreateChild(root.transform, "CharacterContainer");
        var charCg = charContainer.AddComponent<CanvasGroup>();
        charCg.alpha = 0f;
        var rawImage = charContainer.AddComponent<RawImage>();
        rawImage.raycastTarget = false;
        rawImage.texture = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Resources/Exhibitions/VisitorRT.asset");
        var fitter = charContainer.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 1f;
        charContainer.AddComponent<LayoutElement>().preferredHeight = 120;

        var dialogue = CreateChild(root.transform, "DialoguePanel");
        var dialogueBg = dialogue.AddComponent<Image>();
        dialogueBg.color = new Color(0.15f, 0.12f, 0.1f, 0.9f);
        var dialogueCg = dialogue.AddComponent<CanvasGroup>();
        var dialogueText = CreateText(dialogue.transform, "DialogueText", "", 18, FontStyles.Normal, TextAlignmentOptions.Center);
        Stretch(dialogueText.GetComponent<RectTransform>(), 8);
        dialogue.AddComponent<LayoutElement>().preferredHeight = 70;

        var generator = root.AddComponent<VisitorCharacterGenerator>();
        var characterBases = Resources.LoadAll<CharacterBase>("Characters");
        if (characterBases.Length > 0)
            SetPrivateField(generator, "_characterBases", characterBases);

        var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Resources/Exhibitions/VisitorRT.asset");
        if (rt != null)
            SetPrivateField(generator, "_renderTarget", rt);

        var panel = root.AddComponent<VisitorPanel>();
        SetPrivateField(panel, "_characterRawImage", rawImage);
        SetPrivateField(panel, "_characterCanvasGroup", charCg);
        SetPrivateField(panel, "_dialogueText", dialogueText);
        SetPrivateField(panel, "_dialoguePanel", dialogueCg);
        SetPrivateField(panel, "_characterGenerator", generator);

        SaveAndDestroy(root, "VisitorPanel");
    }

    private static void CreateVisitorBackground(Transform parent)
    {
        var backgroundObj = CreateChild(parent, "PassengerBackground");
        backgroundObj.transform.SetAsFirstSibling();
        var background = backgroundObj.AddComponent<Image>();
        background.sprite = LoadSprite("Assets/Resources/Exhibitions/Icons/passenger-background.png");
        background.color = Color.white;
        background.raycastTarget = false;

        var layout = backgroundObj.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;
        Stretch(backgroundObj.GetComponent<RectTransform>(), 0);
    }

    private static Sprite LoadSprite(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
            return sprite;

        foreach (var asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
        {
            if (asset is Sprite subSprite)
                return subSprite;
        }

        return null;
    }

    private static GameObject CreateUIObject(string name, Vector2 size)
    {
        var obj = new GameObject(name);
        var rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        return obj;
    }

    private static GameObject CreateChild(Transform parent, string name)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, FontStyles style, TextAlignmentOptions alignment)
    {
        var obj = CreateChild(parent, name);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        return tmp;
    }

    private static void Stretch(RectTransform rt, float padding)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);
    }

    private static void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        if (!AssetDatabase.IsValidFolder("Assets/Resources/Exhibitions"))
            AssetDatabase.CreateFolder("Assets/Resources", "Exhibitions");

        if (!AssetDatabase.IsValidFolder(PREFAB_PATH))
            AssetDatabase.CreateFolder("Assets/Resources/Exhibitions", "Prefabs");
    }

    private static void SaveAndDestroy(GameObject obj, string name)
    {
        PrefabUtility.SaveAsPrefabAsset(obj, $"{PREFAB_PATH}/{name}.prefab");
        Object.DestroyImmediate(obj);
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

        Debug.LogWarning($"[PrefabBuilder] Field not found: {target.GetType().Name}.{fieldName}");
    }
}
