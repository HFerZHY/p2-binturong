using System.Collections.Generic;
using System.IO;
using ExhibitionSystem.Data;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool that generates test data for the museum exhibition system.
/// Creates 16 exhibit items and 3 exhibition themes.
/// Run via Tools > Museum > Generate Test Data.
/// </summary>
public static class ExhibitionTestDataBuilder
{
    private const string ITEMS_PATH = "Assets/Resources/Exhibitions/Items";
    private const string THEMES_PATH = "Assets/Resources/Exhibitions/Themes";
    private const string ICONS_PATH = "Assets/Resources/Exhibitions/Icons";
    private const int ICON_SIZE = 128;

    [MenuItem("Tools/Museum/Generate Test Data")]
    public static void GenerateTestData()
    {
        EnsureDirectoriesExist();

        var items = GenerateItems();
        var themes = GenerateThemes(items);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Museum Test Data",
            $"Generated:\n" +
            $"- {items.Count} exhibit items\n" +
            $"- {themes.Count} exhibition themes\n\n" +
            $"Location:\n" +
            $"- {ITEMS_PATH}\n" +
            $"- {THEMES_PATH}",
            "OK");
    }

    [MenuItem("Tools/Museum/Clear Generated Assets")]
    public static void ClearGeneratedAssets()
    {
        if (!EditorUtility.DisplayDialog("Clear Generated Assets",
            "This will delete all generated exhibition items, themes, and icons.\n\nContinue?",
            "Delete", "Cancel"))
            return;

        if (AssetDatabase.IsValidFolder(ITEMS_PATH))
            AssetDatabase.DeleteAsset(ITEMS_PATH);
        if (AssetDatabase.IsValidFolder(THEMES_PATH))
            AssetDatabase.DeleteAsset(THEMES_PATH);
        if (AssetDatabase.IsValidFolder(ICONS_PATH))
            AssetDatabase.DeleteAsset(ICONS_PATH);

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Clear Complete",
            "All generated exhibition assets have been deleted.",
            "OK");
    }

    // ── Item Generation ──────────────────────────────────────────────────────────

    private static List<ExhibitItemData> GenerateItems()
    {
        var items = new List<ExhibitItemData>();

        // Item definitions: (fileName, itemName, nameKey, description, themeGroup)
        // themeGroup: R=Railway, C=Culinary, G=General
        var itemDefs = new (string fileName, string itemName, string nameKey, string description, char group)[]
        {
            // Railway Theme Items (6 items)
            ("SignalLamp", "Signal Lamp", "item_signal_lamp",
                "A kerosene lamp used by railway workers to signal trains at night or in poor visibility.", 'R'),
            ("ConductorCap", "Conductor Cap", "item_conductor_cap",
                "The distinctive cap worn by train conductors, symbolizing authority and professionalism.", 'R'),
            ("PunchTicket", "Punch Ticket", "item_punch_ticket",
                "A vintage cardboard ticket with characteristic punch holes marking the journey.", 'R'),
            ("PocketWatch", "Pocket Watch", "item_pocket_watch",
                "A precision timepiece essential for maintaining railway schedules.", 'R'),
            ("StationBell", "Station Bell", "item_station_bell",
                "A brass bell used to announce train arrivals and departures.", 'R'),
            ("TrackSwitch", "Track Switch", "item_track_switch",
                "A mechanical lever used to change the direction of railway tracks.", 'R'),

            // Culinary Theme Items (6 items)
            ("Sake", "Sake Bottle", "item_sake",
                "Traditional Japanese rice wine, aged in ceramic bottles for refined flavor.", 'C'),
            ("RiceBowl", "Rice Bowl", "item_rice_bowl",
                "A hand-crafted ceramic bowl used for serving perfectly steamed rice.", 'C'),
            ("SushiKnife", "Sushi Knife", "item_sushi_knife",
                "A single-beveled blade forged for precise fish cutting.", 'C'),
            ("ChopsticksLacquer", "Lacquer Chopsticks", "item_chopsticks",
                "Elegant chopsticks with traditional urushi lacquer coating.", 'C'),
            ("TeaPot", "Clay Teapot", "item_teapot",
                "An unglazed clay teapot that absorbs tea oils to enhance flavor over time.", 'C'),
            ("MisoBucket", "Miso Bucket", "item_miso_bucket",
                "A wooden barrel used for fermenting soybeans into rich, savory miso paste.", 'C'),

            // Nature/General Items (4 items - can fit multiple themes)
            ("Compass", "Brass Compass", "item_compass",
                "A finely crafted navigation instrument with a polished brass casing.", 'G'),
            ("Lantern", "Paper Lantern", "item_lantern",
                "A traditional paper lantern that casts warm, gentle light.", 'G'),
            ("FoldingFan", "Folding Fan", "item_fan",
                "A painted silk fan depicting scenes of nature and seasonal beauty.", 'G'),
            ("Inkstone", "Inkstone", "item_inkstone",
                "A stone slab for grinding solid ink sticks into liquid ink for calligraphy.", 'G')
        };

        for (int i = 0; i < itemDefs.Length; i++)
        {
            var def = itemDefs[i];
            var item = CreateItem(def.fileName, def.itemName, def.nameKey, def.description, i + 1, def.group);
            items.Add(item);
        }

        return items;
    }

    private static ExhibitItemData CreateItem(string fileName, string itemName,
        string nameKey, string description, int index, char themeGroup)
    {
        string path = $"{ITEMS_PATH}/{fileName}.asset";

        // Generate icon with number
        var icon = GenerateNumberedIcon(index, themeGroup);
        string iconPath = $"{ICONS_PATH}/Icon_{index:D2}.png";
        SaveTextureAsPNG(icon, iconPath);
        AssetDatabase.Refresh();

        // Import as sprite
        var importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

        // Check for existing asset
        var existing = AssetDatabase.LoadAssetAtPath<ExhibitItemData>(path);
        if (existing != null)
        {
            // Update existing
            existing.itemName = itemName;
            existing.nameKey = nameKey;
            existing.description = description;
            existing.descriptionKey = $"desc_{nameKey.Replace("item_", "")}";
            existing.isUnlocked = true;
            existing.icon = sprite;
            existing.usedInExhibitions.Clear();
            EditorUtility.SetDirty(existing);
            return existing;
        }

        // Create new
        var item = ScriptableObject.CreateInstance<ExhibitItemData>();
        item.itemName = itemName;
        item.nameKey = nameKey;
        item.description = description;
        item.descriptionKey = $"desc_{nameKey.Replace("item_", "")}";
        item.isUnlocked = true;
        item.icon = sprite;

        AssetDatabase.CreateAsset(item, path);
        return item;
    }

    // ── Theme Generation ─────────────────────────────────────────────────────────

    private static List<ExhibitionTheme> GenerateThemes(List<ExhibitItemData> allItems)
    {
        var themes = new List<ExhibitionTheme>();

        // Helper to find items by name
        ExhibitItemData FindItem(string name) =>
            allItems.Find(i => i.itemName == name);

        // Theme 1: Railway Heritage (4 slots, 6 correct items)
        themes.Add(CreateTheme(
            "RailwayHeritage",
            "Railway Heritage Exhibition",
            "theme_railway",
            "Celebrating a century of railway history and the people who built the iron roads.",
            4,
            new List<ExhibitItemData>
            {
                FindItem("Signal Lamp"),
                FindItem("Conductor Cap"),
                FindItem("Punch Ticket"),
                FindItem("Pocket Watch"),
                FindItem("Station Bell"),
                FindItem("Track Switch")
            }
        ));

        // Theme 2: Culinary Masters (5 slots, 6 correct items)
        themes.Add(CreateTheme(
            "CulinaryMasters",
            "Culinary Masters Exhibition",
            "theme_culinary",
            "Honoring the artisans who transform simple ingredients into culinary masterpieces.",
            5,
            new List<ExhibitItemData>
            {
                FindItem("Sake Bottle"),
                FindItem("Rice Bowl"),
                FindItem("Sushi Knife"),
                FindItem("Lacquer Chopsticks"),
                FindItem("Clay Teapot"),
                FindItem("Miso Bucket")
            }
        ));

        // Theme 3: Traditional Arts (6 slots, 8 correct items - mixed)
        themes.Add(CreateTheme(
            "TraditionalArts",
            "Traditional Arts Exhibition",
            "theme_arts",
            "A showcase of craftsmanship and artistic traditions passed down through generations.",
            6,
            new List<ExhibitItemData>
            {
                FindItem("Lacquer Chopsticks"),
                FindItem("Clay Teapot"),
                FindItem("Brass Compass"),
                FindItem("Paper Lantern"),
                FindItem("Folding Fan"),
                FindItem("Inkstone"),
                FindItem("Rice Bowl"),
                FindItem("Pocket Watch")
            }
        ));

        // Theme 4: Mixed Collection (4 slots, 4 correct items)
        themes.Add(CreateTheme(
            "MixedCollection",
            "Mixed Collection Exhibition",
            "theme_mixed",
            "A curated collection blending railway heritage with culinary traditions.",
            4,
            new List<ExhibitItemData>
            {
                FindItem("Clay Teapot"),    // 11
                FindItem("Station Bell"),   // 5
                FindItem("Sake Bottle"),    // 7
                FindItem("Rice Bowl")       // 8
            }
        ));

        return themes;
    }

    private static ExhibitionTheme CreateTheme(string fileName, string title,
        string titleKey, string description, int slots, List<ExhibitItemData> correctItems)
    {
        string path = $"{THEMES_PATH}/{fileName}.asset";

        // Remove any null items from the list
        correctItems.RemoveAll(item => item == null);

        // Check for existing asset
        var existing = AssetDatabase.LoadAssetAtPath<ExhibitionTheme>(path);
        if (existing != null)
        {
            existing.title = title;
            existing.titleKey = titleKey;
            existing.description = description;
            existing.descriptionKey = $"desc_{titleKey.Replace("theme_", "")}";
            existing.requiredSlots = slots;
            existing.correctItems = correctItems;
            existing.isCompleted = false;
            EditorUtility.SetDirty(existing);
            return existing;
        }

        // Create new
        var theme = ScriptableObject.CreateInstance<ExhibitionTheme>();
        theme.title = title;
        theme.titleKey = titleKey;
        theme.description = description;
        theme.descriptionKey = $"desc_{titleKey.Replace("theme_", "")}";
        theme.requiredSlots = slots;
        theme.correctItems = correctItems;

        AssetDatabase.CreateAsset(theme, path);
        return theme;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static void EnsureDirectoriesExist()
    {
        // Ensure parent directories exist
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        if (!AssetDatabase.IsValidFolder("Assets/Resources/Exhibitions"))
            AssetDatabase.CreateFolder("Assets/Resources", "Exhibitions");

        if (!AssetDatabase.IsValidFolder(ITEMS_PATH))
            AssetDatabase.CreateFolder("Assets/Resources/Exhibitions", "Items");

        if (!AssetDatabase.IsValidFolder(THEMES_PATH))
            AssetDatabase.CreateFolder("Assets/Resources/Exhibitions", "Themes");

        if (!AssetDatabase.IsValidFolder(ICONS_PATH))
            AssetDatabase.CreateFolder("Assets/Resources/Exhibitions", "Icons");
    }

    // ── Icon Generation ─────────────────────────────────────────────────────────

    private static Texture2D GenerateNumberedIcon(int number, char themeGroup)
    {
        var texture = new Texture2D(ICON_SIZE, ICON_SIZE, TextureFormat.RGBA32, false);

        // Choose colors based on theme group
        Color bgColor, fgColor, borderColor;
        switch (themeGroup)
        {
            case 'R': // Railway - Blue
                bgColor = new Color(0.2f, 0.4f, 0.7f, 1f);
                fgColor = Color.white;
                borderColor = new Color(0.1f, 0.2f, 0.4f, 1f);
                break;
            case 'C': // Culinary - Orange
                bgColor = new Color(0.9f, 0.5f, 0.2f, 1f);
                fgColor = Color.white;
                borderColor = new Color(0.5f, 0.3f, 0.1f, 1f);
                break;
            case 'G': // General - Green
            default:
                bgColor = new Color(0.3f, 0.7f, 0.4f, 1f);
                fgColor = Color.white;
                borderColor = new Color(0.15f, 0.35f, 0.2f, 1f);
                break;
        }

        // Fill background with rounded rectangle
        int borderWidth = 4;
        int cornerRadius = 16;

        for (int y = 0; y < ICON_SIZE; y++)
        {
            for (int x = 0; x < ICON_SIZE; x++)
            {
                bool isInside = IsInsideRoundedRect(x, y, ICON_SIZE, ICON_SIZE, cornerRadius);
                bool isBorder = isInside && !IsInsideRoundedRect(
                    x - borderWidth, y - borderWidth,
                    ICON_SIZE - borderWidth * 2, ICON_SIZE - borderWidth * 2,
                    cornerRadius - borderWidth);

                if (isBorder)
                    texture.SetPixel(x, y, borderColor);
                else if (isInside)
                    texture.SetPixel(x, y, bgColor);
                else
                    texture.SetPixel(x, y, Color.clear);
            }
        }

        // Draw number
        DrawNumber(texture, number, fgColor);

        texture.Apply();
        return texture;
    }

    private static bool IsInsideRoundedRect(int x, int y, int width, int height, int radius)
    {
        if (x < 0 || y < 0 || x >= width || y >= height)
            return false;

        // Check corners
        int left = radius;
        int right = width - radius;
        int bottom = radius;
        int top = height - radius;

        // Inside main rectangle (excluding corners)
        if (x >= left && x < right) return true;
        if (y >= bottom && y < top) return true;

        // Check corner circles
        int dx, dy;

        // Bottom-left corner
        if (x < left && y < bottom)
        {
            dx = x - left;
            dy = y - bottom;
            return dx * dx + dy * dy <= radius * radius;
        }

        // Bottom-right corner
        if (x >= right && y < bottom)
        {
            dx = x - right + 1;
            dy = y - bottom;
            return dx * dx + dy * dy <= radius * radius;
        }

        // Top-left corner
        if (x < left && y >= top)
        {
            dx = x - left;
            dy = y - top + 1;
            return dx * dx + dy * dy <= radius * radius;
        }

        // Top-right corner
        if (x >= right && y >= top)
        {
            dx = x - right + 1;
            dy = y - top + 1;
            return dx * dx + dy * dy <= radius * radius;
        }

        return true;
    }

    private static void DrawNumber(Texture2D texture, int number, Color color)
    {
        // Simple 5x7 pixel font for digits
        var digits = new Dictionary<char, string[]>
        {
            {'0', new[] {" ### ", "#   #", "#   #", "#   #", "#   #", "#   #", " ### "}},
            {'1', new[] {"  #  ", " ##  ", "  #  ", "  #  ", "  #  ", "  #  ", " ### "}},
            {'2', new[] {" ### ", "#   #", "    #", "  ## ", " #   ", "#    ", "#####"}},
            {'3', new[] {" ### ", "#   #", "    #", "  ## ", "    #", "#   #", " ### "}},
            {'4', new[] {"#   #", "#   #", "#   #", "#####", "    #", "    #", "    #"}},
            {'5', new[] {"#####", "#    ", "#### ", "    #", "    #", "#   #", " ### "}},
            {'6', new[] {" ### ", "#    ", "#### ", "#   #", "#   #", "#   #", " ### "}},
            {'7', new[] {"#####", "    #", "   # ", "  #  ", "  #  ", "  #  ", "  #  "}},
            {'8', new[] {" ### ", "#   #", "#   #", " ### ", "#   #", "#   #", " ### "}},
            {'9', new[] {" ### ", "#   #", "#   #", " ####", "    #", "    #", " ### "}},
        };

        string numStr = number.ToString();
        int digitWidth = 5;
        int digitHeight = 7;
        int spacing = 2;
        int scale = 6; // Scale up the digits

        int totalWidth = numStr.Length * (digitWidth * scale) + (numStr.Length - 1) * (spacing * scale);
        int startX = (ICON_SIZE - totalWidth) / 2;
        int startY = (ICON_SIZE - digitHeight * scale) / 2;

        for (int d = 0; d < numStr.Length; d++)
        {
            char digit = numStr[d];
            if (!digits.ContainsKey(digit)) continue;

            var pattern = digits[digit];
            int offsetX = startX + d * (digitWidth + spacing) * scale;

            for (int py = 0; py < digitHeight; py++)
            {
                for (int px = 0; px < digitWidth; px++)
                {
                    if (px < pattern[py].Length && pattern[py][px] == '#')
                    {
                        // Draw scaled pixel
                        for (int sy = 0; sy < scale; sy++)
                        {
                            for (int sx = 0; sx < scale; sx++)
                            {
                                int x = offsetX + px * scale + sx;
                                int y = startY + (digitHeight - 1 - py) * scale + sy;
                                if (x >= 0 && x < ICON_SIZE && y >= 0 && y < ICON_SIZE)
                                    texture.SetPixel(x, y, color);
                            }
                        }
                    }
                }
            }
        }
    }

    private static void SaveTextureAsPNG(Texture2D texture, string path)
    {
        byte[] pngData = texture.EncodeToPNG();
        string fullPath = Path.Combine(Application.dataPath, "..", path);
        File.WriteAllBytes(fullPath, pngData);
        Object.DestroyImmediate(texture);
    }
}
