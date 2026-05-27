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
        var root = CreateUIObject("ShelfSlot", new Vector2(80, 80));
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.22f, 0.16f, 0.10f, 0.95f);
        root.AddComponent<CanvasGroup>();

        var iconObj = CreateChild(root.transform, "Icon");
        var icon = iconObj.AddComponent<Image>();
        icon.raycastTarget = false;
        Stretch(iconObj.GetComponent<RectTransform>(), 5);

        var slotUI = root.AddComponent<ShelfSlotUI>();
        SetPrivateField(slotUI, "_icon", icon);

        SaveAndDestroy(root, "ShelfSlot");
    }

    private static void CreateDisplaySlotPrefab()
    {
        var root = CreateUIObject("DisplaySlot", new Vector2(96, 96));
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.4f, 0.35f, 0.3f, 0.85f);
        root.AddComponent<CanvasGroup>();

        var highlightObj = CreateChild(root.transform, "Highlight");
        var highlight = highlightObj.AddComponent<Image>();
        highlight.color = new Color(1, 1, 1, 0.1f);
        highlight.raycastTarget = false;
        Stretch(highlightObj.GetComponent<RectTransform>(), 0);

        var iconObj = CreateChild(root.transform, "ItemIcon");
        var icon = iconObj.AddComponent<Image>();
        icon.raycastTarget = false;
        icon.enabled = false;
        Stretch(iconObj.GetComponent<RectTransform>(), 10);

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
        SetPrivateField(slotUI, "_itemIcon", icon);
        SetPrivateField(slotUI, "_statusBadge", badge);

        SaveAndDestroy(root, "DisplaySlot");
    }

    private static void CreateThemeListItemPrefab()
    {
        var root = CreateUIObject("ThemeListItem", new Vector2(760, 108));
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
        layout.padding = new RectOffset(22, 22, 12, 12);
        layout.spacing = 16;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;
        layout.childAlignment = TextAnchor.MiddleLeft;

        var flowerObj = CreateChild(root.transform, "StatusFlower");
        var flower = flowerObj.AddComponent<Image>();
        flower.color = new Color(0.72f, 0.24f, 0.18f, 1f);
        var flowerLayout = flowerObj.AddComponent<LayoutElement>();
        flowerLayout.preferredWidth = 34;
        flowerLayout.preferredHeight = 34;

        var content = CreateChild(root.transform, "Content");
        var contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 4;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        content.AddComponent<LayoutElement>().flexibleWidth = 1;

        var titleText = CreateText(content.transform, "Title", "Theme", 26, FontStyles.Bold, TextAlignmentOptions.Left);
        titleText.color = new Color(0.22f, 0.12f, 0.06f, 1f);
        var descText = CreateText(content.transform, "Description", "Description", 16, FontStyles.Normal, TextAlignmentOptions.Left);
        descText.color = new Color(0.30f, 0.20f, 0.12f, 1f);
        descText.textWrappingMode = TextWrappingModes.Normal;
        var slotsText = CreateText(content.transform, "Slots", "Day 2 • 3 ideas", 15, FontStyles.Italic, TextAlignmentOptions.Left);
        slotsText.color = new Color(0.42f, 0.28f, 0.16f, 1f);

        var completedObj = CreateChild(root.transform, "CompletedIcon");
        var completedIcon = completedObj.AddComponent<Image>();
        completedIcon.color = new Color(0.55f, 0.17f, 0.12f, 0.9f);
        completedObj.SetActive(false);
        var completedLayout = completedObj.AddComponent<LayoutElement>();
        completedLayout.preferredWidth = 112;
        completedLayout.preferredHeight = 34;

        var completedText = CreateText(completedObj.transform, "Text", "Completed", 16, FontStyles.Bold, TextAlignmentOptions.Center);
        completedText.color = new Color(1f, 0.86f, 0.68f, 1f);
        Stretch(completedText.GetComponent<RectTransform>(), 0);

        var listItem = root.AddComponent<ThemeListItem>();
        SetPrivateField(listItem, "_titleText", titleText);
        SetPrivateField(listItem, "_descriptionText", descText);
        SetPrivateField(listItem, "_slotsText", slotsText);
        SetPrivateField(listItem, "_completedIcon", completedIcon);
        SetPrivateField(listItem, "_selectionFrame", frame);
        SetPrivateField(listItem, "_background", bg);
        SetPrivateField(listItem, "_button", button);

        SaveAndDestroy(root, "ThemeListItem");
    }

    private static void CreateInspirationListItemPrefab()
    {
        var root = CreateUIObject("InspirationListItem", new Vector2(620, 82));
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
        layout.padding = new RectOffset(16, 16, 10, 10);
        layout.spacing = 14;
        layout.childControlHeight = true;
        layout.childControlWidth = false;
        layout.childAlignment = TextAnchor.MiddleLeft;

        var idText = CreateText(root.transform, "Id", "#1", 20, FontStyles.Bold, TextAlignmentOptions.Center);
        idText.color = new Color(0.22f, 0.12f, 0.06f, 1f);
        idText.gameObject.AddComponent<LayoutElement>().preferredWidth = 54;

        var bodyText = CreateText(root.transform, "Body", "Idea text", 17, FontStyles.Bold, TextAlignmentOptions.Left);
        bodyText.color = new Color(0.23f, 0.14f, 0.08f, 1f);
        bodyText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        bodyText.textWrappingMode = TextWrappingModes.Normal;

        var matchText = CreateText(root.transform, "KnownMatch", "Known Match", 14, FontStyles.Bold, TextAlignmentOptions.Center);
        matchText.color = new Color(0.72f, 0.95f, 0.68f, 1f);
        matchText.enabled = false;
        matchText.gameObject.AddComponent<LayoutElement>().preferredWidth = 96;

        var item = root.AddComponent<InspirationListItem>();
        SetPrivateField(item, "_idText", idText);
        SetPrivateField(item, "_bodyText", bodyText);
        SetPrivateField(item, "_matchText", matchText);
        SetPrivateField(item, "_selectionImage", bg);
        SetPrivateField(item, "_selectionFrame", frame);
        SetPrivateField(item, "_button", button);

        SaveAndDestroy(root, "InspirationListItem");
    }

    private static void CreateInspirationDisplaySlotPrefab()
    {
        var root = CreateUIObject("InspirationDisplaySlot", new Vector2(300, 154));
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.24f, 0.18f, 0.12f, 0.62f);

        var layout = root.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 4, 4);
        layout.spacing = 4;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childAlignment = TextAnchor.UpperCenter;

        var labelObj = CreateChild(root.transform, "LabelStrip");
        var labelBg = labelObj.AddComponent<Image>();
        labelBg.color = new Color(0.75f, 0.62f, 0.42f, 0.95f);
        labelObj.AddComponent<LayoutElement>().preferredHeight = 28;

        var ideaText = CreateText(labelObj.transform, "InspirationText", "Jiro recreated...", 15, FontStyles.Bold, TextAlignmentOptions.Center);
        ideaText.color = new Color(0.25f, 0.14f, 0.08f, 1f);
        Stretch(ideaText.GetComponent<RectTransform>(), 6);

        var slotFrameObj = CreateChild(root.transform, "SlotFrame");
        var slotFrame = slotFrameObj.AddComponent<Image>();
        slotFrame.color = new Color(0.54f, 0.37f, 0.18f, 0.78f);
        slotFrameObj.AddComponent<LayoutElement>().preferredHeight = 88;

        var displaySlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_PATH}/DisplaySlot.prefab");
        GameObject displaySlotObj;
        if (displaySlotPrefab != null)
            displaySlotObj = (GameObject)PrefabUtility.InstantiatePrefab(displaySlotPrefab, slotFrameObj.transform);
        else
            displaySlotObj = CreateUIObject("DisplaySlot", new Vector2(96, 96));

        displaySlotObj.name = "DisplaySlot";
        displaySlotObj.transform.SetParent(slotFrameObj.transform, false);
        var displayRt = displaySlotObj.GetComponent<RectTransform>();
        displayRt.anchorMin = new Vector2(0.5f, 0.5f);
        displayRt.anchorMax = new Vector2(0.5f, 0.5f);
        displayRt.pivot = new Vector2(0.5f, 0.5f);
        displayRt.sizeDelta = new Vector2(82, 82);
        displayRt.anchoredPosition = Vector2.zero;
        var displaySlot = displaySlotObj.GetComponent<DisplaySlotUI>();

        var statusText = CreateText(root.transform, "StatusText", "Find the item", 13, FontStyles.Italic, TextAlignmentOptions.Center);
        statusText.color = new Color(0.8f, 0.75f, 0.65f, 1f);
        statusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 14;

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
        tooltipRt.anchoredPosition = new Vector2(0, 6);
        tooltipRt.sizeDelta = new Vector2(320, 92);

        var tooltipText = CreateText(tooltipObj.transform, "Text", "Full inspiration text.", 16, FontStyles.Normal, TextAlignmentOptions.Center);
        tooltipText.color = new Color(0.22f, 0.13f, 0.07f, 1f);
        tooltipText.textWrappingMode = TextWrappingModes.Normal;
        tooltipText.raycastTarget = false;
        Stretch(tooltipText.GetComponent<RectTransform>(), 12);

        var slot = root.AddComponent<InspirationDisplaySlot>();
        SetPrivateField(slot, "_inspirationText", ideaText);
        SetPrivateField(slot, "_statusText", statusText);
        SetPrivateField(slot, "_displaySlot", displaySlot);
        SetPrivateField(slot, "_tooltipPanel", tooltipObj);
        SetPrivateField(slot, "_tooltipText", tooltipText);

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

        var charContainer = CreateChild(root.transform, "CharacterContainer");
        var charCg = charContainer.AddComponent<CanvasGroup>();
        charCg.alpha = 0f;
        var rawImage = charContainer.AddComponent<RawImage>();
        rawImage.raycastTarget = false;
        rawImage.texture = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Resources/Exhibitions/VisitorRT.asset");
        charContainer.AddComponent<LayoutElement>().preferredHeight = 120;

        var dialogue = CreateChild(root.transform, "DialoguePanel");
        var dialogueBg = dialogue.AddComponent<Image>();
        dialogueBg.color = new Color(0.15f, 0.12f, 0.1f, 0.9f);
        var dialogueCg = dialogue.AddComponent<CanvasGroup>();
        var dialogueText = CreateText(dialogue.transform, "DialogueText", "Place items in the display slots to begin.", 18, FontStyles.Normal, TextAlignmentOptions.Center);
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
