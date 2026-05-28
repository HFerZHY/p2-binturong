using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using StationWork.UI;

/// <summary>
/// Builds the Station Work UI hierarchy in the active scene.
/// Run via  Otowa > Build Station Work UI  in the menu bar.
/// Safe to re-run — it destroys and recreates PassengerPanel, ItemRow, SummaryPanel.
/// </summary>
public static class StationWorkSceneBuilder
{
    [MenuItem("Otowa/Build Station Work UI")]
    public static void Build()
    {
        var canvasGO = GameObject.Find("UICanvas");
        if (canvasGO == null)
        {
            EditorUtility.DisplayDialog("Otowa Builder",
                "No GameObject named 'UICanvas' found.\nCreate a Canvas and rename it UICanvas first.", "OK");
            return;
        }

        var canvas = canvasGO.transform;

        // Destroy and rebuild these three so the script is safe to re-run
        DestroyChild(canvas, "PassengerPanel");
        DestroyChild(canvas, "ItemRow");
        DestroyChild(canvas, "SummaryPanel");

        // SummaryPanel is built first so PassengerPanel can hold a reference to it
        var summaryPanel = BuildSummaryPanel(canvas, out var summaryText);
        BuildPassengerPanel(canvas, summaryPanel, summaryText);
        var itemRow = BuildItemRow(canvas);

        // Create the draggable item card prefab and wire it to StationWorkUIManager
        var prefabGO = BuildItemCardPrefab();
        if (prefabGO != null && itemRow != null)
        {
            var mgr = itemRow.GetComponent<StationWorkUIManager>();
            var so  = new SerializedObject(mgr);
            so.FindProperty("_itemCardPrefab").objectReferenceValue =
                prefabGO.GetComponent<ItemCardUI>();
            so.FindProperty("_cardContainer").objectReferenceValue =
                itemRow.GetComponent<RectTransform>();
            so.ApplyModifiedProperties();
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Otowa Builder",
            "Done!\n\nNext: select StationWorkManager in the Hierarchy and fill in:\n" +
            "• Passengers list  (drag your PassengerData assets)\n" +
            "• Starter Items list  (drag your ItemData assets)\n\n" +
            "Then press Play.", "OK");
    }

    // ── PassengerPanel ────────────────────────────────────────────────────────

    static void BuildPassengerPanel(Transform canvas, GameObject summaryPanel, TextMeshProUGUI summaryText)
    {
        var go = new GameObject("PassengerPanel");
        go.transform.SetParent(canvas, false);
        go.layer = LayerMask.NameToLayer("UI");

        // Dark background — fills top portion of screen, leaving 380px for item row
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.13f, 0.12f, 0.18f, 0.97f);
        bg.raycastTarget = true;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(0, 380);   // 380px gap at bottom for ItemRow
        rt.offsetMax = Vector2.zero;

        // ── Children ──────────────────────────────────────────────────────────

        var avatarImg = MakeImage(go.transform, "Avatar", new Color(0.35f, 0.35f, 0.4f));
        TopLeft(avatarImg.GetComponent<RectTransform>(), 24, 24, 120, 120);

        var nameTMP = MakeTMP(go.transform, "Name", 26, TextAlignmentOptions.Left);
        nameTMP.text = "Passenger Name";
        TopLeft(nameTMP.GetComponent<RectTransform>(), 160, 24, 440, 50);

        var dialogueTMP = MakeTMP(go.transform, "Dialogue", 18, TextAlignmentOptions.Left);
        dialogueTMP.enableWordWrapping = true;
        dialogueTMP.text = "Passenger dialogue will appear here...";
        TopLeft(dialogueTMP.GetComponent<RectTransform>(), 24, 165, 720, 155);

        var reqTMP = MakeTMP(go.transform, "Requirement", 22, TextAlignmentOptions.Left);
        reqTMP.text = "Looking for: <b>???</b>";
        TopLeft(reqTMP.GetComponent<RectTransform>(), 24, 340, 600, 48);

        // Drop zone highlight — visual only, raycastTarget OFF so events reach the panel
        var dropImg = MakeImage(go.transform, "DropZoneHighlight", new Color(1, 1, 1, 0.04f));
        var dropRT = dropImg.GetComponent<RectTransform>();
        dropRT.anchorMin = Vector2.zero;
        dropRT.anchorMax = Vector2.one;
        dropRT.offsetMin = Vector2.zero;
        dropRT.offsetMax = Vector2.zero;
        dropImg.raycastTarget = false;

        // Feedback — centered at the bottom of the panel
        var feedbackTMP = MakeTMP(go.transform, "Feedback", 24, TextAlignmentOptions.Center);
        feedbackTMP.text = "";
        var feedRT = feedbackTMP.GetComponent<RectTransform>();
        feedRT.anchorMin = new Vector2(0, 0);
        feedRT.anchorMax = new Vector2(1, 0);
        feedRT.pivot     = new Vector2(0.5f, 0);
        feedRT.anchoredPosition = new Vector2(0, 16);
        feedRT.sizeDelta = new Vector2(0, 56);

        // ── PassengerPanel component ──────────────────────────────────────────

        var panel = go.AddComponent<PassengerPanel>();
        var so    = new SerializedObject(panel);
        so.FindProperty("_avatar").objectReferenceValue           = avatarImg;
        so.FindProperty("_nameText").objectReferenceValue         = nameTMP;
        so.FindProperty("_dialogueText").objectReferenceValue     = dialogueTMP;
        so.FindProperty("_requirementText").objectReferenceValue  = reqTMP;
        so.FindProperty("_dropZoneHighlight").objectReferenceValue = dropImg;
        so.FindProperty("_feedbackText").objectReferenceValue     = feedbackTMP;
        so.FindProperty("_summaryRoot").objectReferenceValue      = summaryPanel;
        so.FindProperty("_summaryText").objectReferenceValue      = summaryText;
        so.ApplyModifiedProperties();
    }

    // ── SummaryPanel ──────────────────────────────────────────────────────────

    static GameObject BuildSummaryPanel(Transform canvas, out TextMeshProUGUI summaryText)
    {
        var go = new GameObject("SummaryPanel");
        go.transform.SetParent(canvas, false);
        go.layer = LayerMask.NameToLayer("UI");

        var img = go.AddComponent<Image>();
        img.color = new Color(0.08f, 0.08f, 0.12f, 0.98f);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        summaryText = MakeTMP(go.transform, "SummaryText", 40, TextAlignmentOptions.Center);
        summaryText.text = "Results";
        var textRT = summaryText.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.5f, 0.5f);
        textRT.anchorMax = new Vector2(0.5f, 0.5f);
        textRT.pivot     = new Vector2(0.5f, 0.5f);
        textRT.anchoredPosition = Vector2.zero;
        textRT.sizeDelta = new Vector2(800, 200);

        go.SetActive(false);   // PassengerPanel will enable this when phase ends
        return go;
    }

    // ── ItemRow ───────────────────────────────────────────────────────────────

    static GameObject BuildItemRow(Transform canvas)
    {
        var go = new GameObject("ItemRow");
        go.transform.SetParent(canvas, false);
        go.layer = LayerMask.NameToLayer("UI");

        var img = go.AddComponent<Image>();
        img.color = new Color(0.10f, 0.08f, 0.06f, 0.96f);

        // Anchored to the bottom 380px
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot     = new Vector2(0.5f, 0);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, 380);

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment      = TextAnchor.MiddleCenter;
        hlg.spacing             = 20;
        hlg.padding             = new RectOffset(24, 24, 20, 20);
        hlg.childControlWidth   = false;
        hlg.childControlHeight  = false;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;

        go.AddComponent<StationWorkUIManager>();
        return go;
    }

    // ── ItemCardUI Prefab ─────────────────────────────────────────────────────

    static GameObject BuildItemCardPrefab()
    {
        // Build structure in scene temporarily
        var root = new GameObject("ItemCardUI");
        root.layer = LayerMask.NameToLayer("UI");

        var rootImg = root.AddComponent<Image>();
        rootImg.color = new Color(0.18f, 0.16f, 0.14f, 0.96f);
        rootImg.raycastTarget = true;

        root.AddComponent<CanvasGroup>();

        var rootRT = root.GetComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(160, 230);

        // Icon — top-center, square
        var iconGO  = new GameObject("Icon");
        iconGO.transform.SetParent(root.transform, false);
        iconGO.layer = LayerMask.NameToLayer("UI");
        var iconImg  = iconGO.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = false;
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.5f, 1);
        iconRT.anchorMax = new Vector2(0.5f, 1);
        iconRT.pivot     = new Vector2(0.5f, 1);
        iconRT.anchoredPosition = new Vector2(0, -12);
        iconRT.sizeDelta = new Vector2(120, 120);

        // Item name — below icon
        var nameGO  = new GameObject("ItemName");
        nameGO.transform.SetParent(root.transform, false);
        nameGO.layer = LayerMask.NameToLayer("UI");
        var nameTMP  = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.fontSize          = 15;
        nameTMP.alignment         = TextAlignmentOptions.Center;
        nameTMP.enableWordWrapping = true;
        nameTMP.color             = Color.white;
        nameTMP.raycastTarget     = false;
        nameTMP.text              = "Item";
        var nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 0);
        nameRT.anchorMax = new Vector2(1, 0);
        nameRT.pivot     = new Vector2(0.5f, 0);
        nameRT.anchoredPosition = new Vector2(0, 68);
        nameRT.sizeDelta = new Vector2(0, 40);

        // Labels — bottom
        var labelsGO  = new GameObject("Labels");
        labelsGO.transform.SetParent(root.transform, false);
        labelsGO.layer = LayerMask.NameToLayer("UI");
        var labelsTMP  = labelsGO.AddComponent<TextMeshProUGUI>();
        labelsTMP.fontSize          = 11;
        labelsTMP.alignment         = TextAlignmentOptions.Center;
        labelsTMP.color             = new Color(0.75f, 0.75f, 0.75f);
        labelsTMP.enableWordWrapping = true;
        labelsTMP.raycastTarget     = false;
        labelsTMP.text              = "???\n???\n???";
        var labelsRT = labelsGO.GetComponent<RectTransform>();
        labelsRT.anchorMin = new Vector2(0, 0);
        labelsRT.anchorMax = new Vector2(1, 0);
        labelsRT.pivot     = new Vector2(0.5f, 0);
        labelsRT.anchoredPosition = new Vector2(0, 6);
        labelsRT.sizeDelta = new Vector2(0, 62);

        // Wire ItemCardUI script
        var cardUI = root.AddComponent<ItemCardUI>();
        var so     = new SerializedObject(cardUI);
        so.FindProperty("_icon").objectReferenceValue      = iconImg;
        so.FindProperty("_nameText").objectReferenceValue  = nameTMP;
        so.FindProperty("_labelsText").objectReferenceValue = labelsTMP;
        so.ApplyModifiedProperties();

        // Save to Assets/Prefabs/
        const string path = "Assets/Prefabs/ItemCardUI.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        AssetDatabase.Refresh();
        Debug.Log($"[StationWorkSceneBuilder] Prefab saved: {path}");
        return prefab;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static Image MakeImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static TextMeshProUGUI MakeTMP(Transform parent, string name, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize      = size;
        tmp.alignment     = align;
        tmp.color         = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    /// <summary>Sets anchorMin/Max to top-left and positions by pixel offset from that corner.</summary>
    static void TopLeft(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
    }

    static void DestroyChild(Transform parent, string childName)
    {
        var child = parent.Find(childName);
        if (child != null) Object.DestroyImmediate(child.gameObject);
    }
}
