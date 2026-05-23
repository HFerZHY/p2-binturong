using ExhibitionSystem.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor tool that generates UI prefabs for the exhibition system.
/// Run via Tools > Museum > Rebuild Prefabs.
/// </summary>
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
        CreateItemTooltipPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Prefab Builder",
            "Prefabs rebuilt successfully!\n\n" +
            $"Location: {PREFAB_PATH}",
            "OK");
    }

    // ── Prefab Creators ─────────────────────────────────────────────────────────

    private static void CreateShelfSlotPrefab()
    {
        var root = new GameObject("ShelfSlot");

        // Background image
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.3f, 0.25f, 0.2f, 0.8f);

        // CanvasGroup for drag
        var cg = root.AddComponent<CanvasGroup>();

        // RectTransform
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80, 80);

        // Icon child
        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(root.transform, false);
        var icon = iconObj.AddComponent<Image>();
        icon.raycastTarget = false;
        var iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.offsetMin = new Vector2(5, 5);
        iconRt.offsetMax = new Vector2(-5, -5);

        // ShelfSlotUI script
        var slotUI = root.AddComponent<ShelfSlotUI>();
        SetPrivateField(slotUI, "_icon", icon);

        SavePrefab(root, "ShelfSlot");
        Object.DestroyImmediate(root);
    }

    private static void CreateDisplaySlotPrefab()
    {
        var root = new GameObject("DisplaySlot");

        // Background image
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.4f, 0.35f, 0.3f, 0.8f);

        // CanvasGroup
        var cg = root.AddComponent<CanvasGroup>();

        // RectTransform
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100, 100);

        // Highlight child
        var highlightObj = new GameObject("Highlight");
        highlightObj.transform.SetParent(root.transform, false);
        var highlight = highlightObj.AddComponent<Image>();
        highlight.color = new Color(1, 1, 1, 0.1f);
        highlight.raycastTarget = false;
        var highlightRt = highlightObj.GetComponent<RectTransform>();
        highlightRt.anchorMin = Vector2.zero;
        highlightRt.anchorMax = Vector2.one;
        highlightRt.offsetMin = Vector2.zero;
        highlightRt.offsetMax = Vector2.zero;

        // Icon child
        var iconObj = new GameObject("ItemIcon");
        iconObj.transform.SetParent(root.transform, false);
        var icon = iconObj.AddComponent<Image>();
        icon.raycastTarget = false;
        icon.enabled = false;
        var iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.offsetMin = new Vector2(10, 10);
        iconRt.offsetMax = new Vector2(-10, -10);

        // Feedback icon child
        var feedbackObj = new GameObject("FeedbackIcon");
        feedbackObj.transform.SetParent(root.transform, false);
        var feedback = feedbackObj.AddComponent<Image>();
        feedback.raycastTarget = false;
        feedback.enabled = false;
        var feedbackRt = feedbackObj.GetComponent<RectTransform>();
        feedbackRt.anchorMin = new Vector2(1, 1);
        feedbackRt.anchorMax = new Vector2(1, 1);
        feedbackRt.pivot = new Vector2(1, 1);
        feedbackRt.sizeDelta = new Vector2(30, 30);
        feedbackRt.anchoredPosition = new Vector2(-5, -5);

        // DisplaySlotUI script
        var slotUI = root.AddComponent<DisplaySlotUI>();
        SetPrivateField(slotUI, "_highlight", highlight);
        SetPrivateField(slotUI, "_itemIcon", icon);
        SetPrivateField(slotUI, "_feedbackIcon", feedback);

        SavePrefab(root, "DisplaySlot");
        Object.DestroyImmediate(root);
    }

    private static void CreateThemeListItemPrefab()
    {
        var root = new GameObject("ThemeListItem");

        // Background/Button
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.35f, 0.3f, 0.25f, 0.9f);
        var button = root.AddComponent<Button>();
        button.targetGraphic = bg;

        // RectTransform
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 80);

        // Layout
        var hlg = root.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(10, 10, 10, 10);
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // Content container
        var contentObj = new GameObject("Content");
        contentObj.transform.SetParent(root.transform, false);
        var contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
        contentLayout.childAlignment = TextAnchor.MiddleLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.spacing = 2;
        var contentLe = contentObj.AddComponent<LayoutElement>();
        contentLe.flexibleWidth = 1;

        // Title text
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(contentObj.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Exhibition Title";
        titleText.fontSize = 18;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;

        // Slots text
        var slotsObj = new GameObject("Slots");
        slotsObj.transform.SetParent(contentObj.transform, false);
        var slotsText = slotsObj.AddComponent<TextMeshProUGUI>();
        slotsText.text = "4 slots";
        slotsText.fontSize = 14;
        slotsText.color = new Color(0.8f, 0.8f, 0.8f, 1f);

        // Completed icon
        var completedObj = new GameObject("CompletedIcon");
        completedObj.transform.SetParent(root.transform, false);
        var completedIcon = completedObj.AddComponent<Image>();
        completedIcon.color = new Color(0.3f, 0.9f, 0.3f, 1f);
        completedIcon.enabled = false;
        var completedLe = completedObj.AddComponent<LayoutElement>();
        completedLe.minWidth = 30;
        completedLe.minHeight = 30;

        // ThemeListItem script
        var listItem = root.AddComponent<ThemeListItem>();
        SetPrivateField(listItem, "_titleText", titleText);
        SetPrivateField(listItem, "_slotsText", slotsText);
        SetPrivateField(listItem, "_completedIcon", completedIcon);
        SetPrivateField(listItem, "_button", button);

        SavePrefab(root, "ThemeListItem");
        Object.DestroyImmediate(root);
    }

    private static void CreateItemTooltipPrefab()
    {
        var root = new GameObject("ItemTooltip");

        // Panel background
        var panel = root.AddComponent<Image>();
        panel.color = new Color(0.15f, 0.12f, 0.1f, 0.95f);

        // CanvasGroup
        var cg = root.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        // RectTransform
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(220, 150);
        rt.pivot = new Vector2(0, 1); // Top-left pivot

        // Vertical layout
        var vlg = root.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(15, 15, 15, 15);
        vlg.spacing = 8;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // ContentSizeFitter to auto-resize
        var csf = root.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Name text
        var nameObj = new GameObject("NameText");
        nameObj.transform.SetParent(root.transform, false);
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = "Item Name";
        nameText.fontSize = 18;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = new Color(1f, 0.95f, 0.85f, 1f);
        var nameLe = nameObj.AddComponent<LayoutElement>();
        nameLe.minHeight = 24;

        // Separator
        var sepObj = new GameObject("Separator");
        sepObj.transform.SetParent(root.transform, false);
        var sep = sepObj.AddComponent<Image>();
        sep.color = new Color(0.5f, 0.45f, 0.4f, 0.5f);
        var sepLe = sepObj.AddComponent<LayoutElement>();
        sepLe.minHeight = 1;
        sepLe.preferredHeight = 1;

        // Description text
        var descObj = new GameObject("DescriptionText");
        descObj.transform.SetParent(root.transform, false);
        var descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.text = "Item description goes here.";
        descText.fontSize = 14;
        descText.color = new Color(0.9f, 0.85f, 0.8f, 1f);
        var descLe = descObj.AddComponent<LayoutElement>();
        descLe.minHeight = 40;
        descLe.preferredWidth = 190;

        // History container
        var historyObj = new GameObject("HistoryContainer");
        historyObj.transform.SetParent(root.transform, false);
        var historyLayout = historyObj.AddComponent<VerticalLayoutGroup>();
        historyLayout.spacing = 2;
        historyLayout.childControlWidth = true;
        historyLayout.childControlHeight = false;
        historyLayout.childForceExpandWidth = true;
        historyLayout.childForceExpandHeight = false;

        // History entry prefab (nested)
        var historyEntryObj = new GameObject("HistoryEntry");
        var historyEntryText = historyEntryObj.AddComponent<TextMeshProUGUI>();
        historyEntryText.text = "✓ Used in: Exhibition";
        historyEntryText.fontSize = 12;
        historyEntryText.color = new Color(0.5f, 0.9f, 0.5f, 1f);
        historyEntryObj.SetActive(false);
        historyEntryObj.transform.SetParent(historyObj.transform, false);

        // ItemTooltip script
        var tooltip = root.AddComponent<ItemTooltip>();
        SetPrivateField(tooltip, "_canvasGroup", cg);
        SetPrivateField(tooltip, "_panel", rt);
        SetPrivateField(tooltip, "_nameText", nameText);
        SetPrivateField(tooltip, "_descriptionText", descText);
        SetPrivateField(tooltip, "_historyContainer", historyObj.transform);
        SetPrivateField(tooltip, "_historyEntryPrefab", historyEntryObj);

        SavePrefab(root, "ItemTooltip");
        Object.DestroyImmediate(root);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        if (!AssetDatabase.IsValidFolder("Assets/Resources/Exhibitions"))
            AssetDatabase.CreateFolder("Assets/Resources", "Exhibitions");

        if (!AssetDatabase.IsValidFolder(PREFAB_PATH))
            AssetDatabase.CreateFolder("Assets/Resources/Exhibitions", "Prefabs");
    }

    private static void SavePrefab(GameObject obj, string name)
    {
        string path = $"{PREFAB_PATH}/{name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(obj, path);
        Debug.Log($"[PrefabBuilder] Created: {path}");
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (field != null)
            field.SetValue(target, value);
        else
            Debug.LogWarning($"[PrefabBuilder] Field not found: {fieldName}");
    }
}
