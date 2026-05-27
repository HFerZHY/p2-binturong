using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor tool to fix ThemeListItem prefab and ThemeSelectionPopup layout.
/// </summary>
public static class ThemeListItemFixer
{
    [MenuItem("Tools/Museum/Fix ThemeListItem Prefab")]
    public static void FixThemeListItemPrefab()
    {
        // First, fix sakura.png import settings
        FixSakuraImportSettings();

        // Load the sakura sprite
        var sakuraSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Exhibitions/Icons/sakura.png");
        if (sakuraSprite == null)
        {
            Debug.LogError("[ThemeListItemFixer] Failed to load sakura.png sprite!");
            return;
        }

        Debug.Log($"[ThemeListItemFixer] Loaded sakura sprite: {sakuraSprite.rect.width}x{sakuraSprite.rect.height}");

        // Load the prefab
        string prefabPath = "Assets/Resources/Exhibitions/Prefabs/ThemeListItem.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[ThemeListItemFixer] Failed to load prefab at {prefabPath}");
            return;
        }

        var prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);

        try
        {
            // 1. Find and modify Icon
            var icon = prefabContents.transform.Find("Icon");
            if (icon == null)
                icon = prefabContents.transform.Find("StatusFlower");

            if (icon != null)
            {
                var image = icon.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = sakuraSprite;
                    image.color = Color.white;
                    image.preserveAspect = true;
                    image.type = Image.Type.Simple;
                    // Reset native size to ensure proper display
                    image.SetNativeSize();
                }

                // Small icon like reference (28x28)
                var layoutElement = icon.GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    layoutElement.preferredWidth = 28;
                    layoutElement.preferredHeight = 28;
                    layoutElement.minWidth = 28;
                    layoutElement.minHeight = 28;
                }

                var iconRect = icon.GetComponent<RectTransform>();
                if (iconRect != null)
                {
                    iconRect.sizeDelta = new Vector2(28, 28);
                }

                icon.name = "Icon";
                Debug.Log("[ThemeListItemFixer] Icon configured (28x28)");
            }

            // 2. Fix Content area
            var content = prefabContents.transform.Find("Content");
            if (content != null)
            {
                var vlg = content.GetComponent<VerticalLayoutGroup>();
                if (vlg != null)
                {
                    vlg.childControlHeight = true;
                    vlg.childForceExpandHeight = false;
                }

                // Remove Description and Slots if they exist
                var description = content.Find("Description");
                if (description != null)
                    Object.DestroyImmediate(description.gameObject);

                var slots = content.Find("Slots");
                if (slots != null)
                    Object.DestroyImmediate(slots.gameObject);

                // Adjust Title - larger font
                var title = content.Find("Title");
                if (title != null)
                {
                    var titleText = title.GetComponent<TMP_Text>();
                    if (titleText != null)
                    {
                        titleText.verticalAlignment = VerticalAlignmentOptions.Middle;
                        titleText.fontSize = 28; // Larger font
                    }
                }
            }

            // 3. Item dimensions - MUCH wider, fill container
            var rectTransform = prefabContents.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // Use stretch anchors to fill parent width
                rectTransform.anchorMin = new Vector2(0, 0.5f);
                rectTransform.anchorMax = new Vector2(1, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = new Vector2(-20, 52); // -20 for small margin, height 52
            }

            // 4. HorizontalLayoutGroup settings
            var hlg = prefabContents.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.padding = new RectOffset(16, 16, 8, 8);
                hlg.spacing = 12;
                hlg.childAlignment = TextAnchor.MiddleLeft;
            }

            PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
            Debug.Log("[ThemeListItemFixer] ThemeListItem prefab saved!");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }

        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Museum/Fix Sakura Import Settings")]
    public static void FixSakuraImportSettings()
    {
        string path = "Assets/Resources/Exhibitions/Icons/sakura.png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
        {
            Debug.LogError($"[ThemeListItemFixer] Cannot find texture importer for {path}");
            return;
        }

        bool needsReimport = false;

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            needsReimport = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            needsReimport = true;
        }

        // Ensure full rect is used
        importer.spritePivot = new Vector2(0.5f, 0.5f);

        // Set proper compression for UI
        var settings = importer.GetDefaultPlatformTextureSettings();
        settings.format = TextureImporterFormat.RGBA32;
        importer.SetPlatformTextureSettings(settings);

        if (needsReimport)
        {
            importer.SaveAndReimport();
            Debug.Log("[ThemeListItemFixer] Sakura texture reimported as Sprite");
        }
        else
        {
            Debug.Log("[ThemeListItemFixer] Sakura texture settings already correct");
        }
    }

    [MenuItem("Tools/Museum/Fix ThemeSelectionPopup Layout")]
    public static void FixThemeSelectionPopupLayout()
    {
        // Find ThemeSelectionPopup in scene (including inactive objects)
        GameObject popup = null;

        var canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            var popupTransform = canvas.transform.Find("ThemeSelectionPopup");
            if (popupTransform != null)
                popup = popupTransform.gameObject;
        }

        if (popup == null)
        {
            var allPopups = Resources.FindObjectsOfTypeAll<ExhibitionSystem.UI.ThemeSelectionPopup>();
            if (allPopups.Length > 0)
                popup = allPopups[0].gameObject;
        }

        if (popup == null)
        {
            Debug.LogError("[ThemeListItemFixer] ThemeSelectionPopup not found in scene!");
            return;
        }

        var window = popup.transform.Find("Window");
        if (window == null)
        {
            Debug.LogError("[ThemeListItemFixer] Window not found!");
            return;
        }

        // 1. REMOVE any VerticalLayoutGroup on Window (this was breaking the layout!)
        var badVlg = window.GetComponent<VerticalLayoutGroup>();
        if (badVlg != null)
        {
            Object.DestroyImmediate(badVlg);
            Debug.Log("[ThemeListItemFixer] Removed broken VerticalLayoutGroup from Window");
        }

        // 2. Remove LayoutElements that were incorrectly added
        foreach (Transform child in window)
        {
            var le = child.GetComponent<LayoutElement>();
            if (le != null)
            {
                Object.DestroyImmediate(le);
            }
        }

        // 3. Resize Window to 80% of screen (larger as user requested)
        var windowRect = window.GetComponent<RectTransform>();
        if (windowRect != null)
        {
            windowRect.anchorMin = new Vector2(0.1f, 0.1f);  // 10% margin = 80% size
            windowRect.anchorMax = new Vector2(0.9f, 0.9f);
            windowRect.offsetMin = Vector2.zero;
            windowRect.offsetMax = Vector2.zero;
            Debug.Log("[ThemeListItemFixer] Window resized to 80% of screen");
        }

        // 4. Reposition elements using anchors (manual layout, not LayoutGroup)
        // Title at top
        var title = window.Find("Title");
        if (title != null)
        {
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -20);
            titleRect.sizeDelta = new Vector2(-60, 50);
        }

        // HintText below title (with more spacing)
        var hintText = window.Find("HintText");
        if (hintText != null)
        {
            var hintRect = hintText.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0, 1);
            hintRect.anchorMax = new Vector2(1, 1);
            hintRect.pivot = new Vector2(0.5f, 1);
            hintRect.anchoredPosition = new Vector2(0, -80);  // More space from title
            hintRect.sizeDelta = new Vector2(-60, 30);
        }

        // ListScroll in middle (main content area)
        var listScroll = window.Find("ListScroll");
        if (listScroll != null)
        {
            var listRect = listScroll.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0, 0);
            listRect.anchorMax = new Vector2(1, 1);
            listRect.pivot = new Vector2(0.5f, 0.5f);
            listRect.offsetMin = new Vector2(30, 80);   // Left, Bottom padding
            listRect.offsetMax = new Vector2(-30, -130); // Right, Top padding (more top for title+hint)
        }

        // Buttons at bottom - horizontal layout
        var closeButton = window.Find("CloseButton");
        if (closeButton != null)
        {
            var closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.2f, 0);
            closeRect.anchorMax = new Vector2(0.45f, 0);
            closeRect.pivot = new Vector2(0.5f, 0);
            closeRect.anchoredPosition = new Vector2(0, 20);
            closeRect.sizeDelta = new Vector2(0, 50);
        }

        var enterButton = window.Find("EnterButton");
        if (enterButton != null)
        {
            var enterRect = enterButton.GetComponent<RectTransform>();
            enterRect.anchorMin = new Vector2(0.55f, 0);
            enterRect.anchorMax = new Vector2(0.8f, 0);
            enterRect.pivot = new Vector2(0.5f, 0);
            enterRect.anchoredPosition = new Vector2(0, 20);
            enterRect.sizeDelta = new Vector2(0, 50);
        }

        Debug.Log("[ThemeListItemFixer] Window layout fixed with manual positioning");

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[ThemeListItemFixer] ThemeSelectionPopup layout fixed! Save the scene.");
    }

    [MenuItem("Tools/Museum/Fix All Theme UI")]
    public static void FixAllThemeUI()
    {
        FixThemeListItemPrefab();
        FixThemeSelectionPopupLayout();
        Debug.Log("[ThemeListItemFixer] All Theme UI fixes applied!");
    }
}
