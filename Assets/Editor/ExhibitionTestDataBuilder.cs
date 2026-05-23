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
            "This will delete all generated exhibition items and themes.\n\nContinue?",
            "Delete", "Cancel"))
            return;

        if (AssetDatabase.IsValidFolder(ITEMS_PATH))
            AssetDatabase.DeleteAsset(ITEMS_PATH);
        if (AssetDatabase.IsValidFolder(THEMES_PATH))
            AssetDatabase.DeleteAsset(THEMES_PATH);

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Clear Complete",
            "All generated exhibition assets have been deleted.",
            "OK");
    }

    // ── Item Generation ──────────────────────────────────────────────────────────

    private static List<ExhibitItemData> GenerateItems()
    {
        var items = new List<ExhibitItemData>();

        // Item definitions: (fileName, itemName, nameKey, description)
        var itemDefs = new (string fileName, string itemName, string nameKey, string description)[]
        {
            // Railway Theme Items (6 items)
            ("SignalLamp", "Signal Lamp", "item_signal_lamp",
                "A kerosene lamp used by railway workers to signal trains at night or in poor visibility."),
            ("ConductorCap", "Conductor Cap", "item_conductor_cap",
                "The distinctive cap worn by train conductors, symbolizing authority and professionalism."),
            ("PunchTicket", "Punch Ticket", "item_punch_ticket",
                "A vintage cardboard ticket with characteristic punch holes marking the journey."),
            ("PocketWatch", "Pocket Watch", "item_pocket_watch",
                "A precision timepiece essential for maintaining railway schedules."),
            ("StationBell", "Station Bell", "item_station_bell",
                "A brass bell used to announce train arrivals and departures."),
            ("TrackSwitch", "Track Switch", "item_track_switch",
                "A mechanical lever used to change the direction of railway tracks."),

            // Culinary Theme Items (6 items)
            ("Sake", "Sake Bottle", "item_sake",
                "Traditional Japanese rice wine, aged in ceramic bottles for refined flavor."),
            ("RiceBowl", "Rice Bowl", "item_rice_bowl",
                "A hand-crafted ceramic bowl used for serving perfectly steamed rice."),
            ("SushiKnife", "Sushi Knife", "item_sushi_knife",
                "A single-beveled blade forged for precise fish cutting."),
            ("ChopsticksLacquer", "Lacquer Chopsticks", "item_chopsticks",
                "Elegant chopsticks with traditional urushi lacquer coating."),
            ("TeaPot", "Clay Teapot", "item_teapot",
                "An unglazed clay teapot that absorbs tea oils to enhance flavor over time."),
            ("MisoBucket", "Miso Bucket", "item_miso_bucket",
                "A wooden barrel used for fermenting soybeans into rich, savory miso paste."),

            // Nature/General Items (4 items - can fit multiple themes)
            ("Compass", "Brass Compass", "item_compass",
                "A finely crafted navigation instrument with a polished brass casing."),
            ("Lantern", "Paper Lantern", "item_lantern",
                "A traditional paper lantern that casts warm, gentle light."),
            ("FoldingFan", "Folding Fan", "item_fan",
                "A painted silk fan depicting scenes of nature and seasonal beauty."),
            ("Inkstone", "Inkstone", "item_inkstone",
                "A stone slab for grinding solid ink sticks into liquid ink for calligraphy.")
        };

        foreach (var def in itemDefs)
        {
            var item = CreateItem(def.fileName, def.itemName, def.nameKey, def.description);
            items.Add(item);
        }

        return items;
    }

    private static ExhibitItemData CreateItem(string fileName, string itemName,
        string nameKey, string description)
    {
        string path = $"{ITEMS_PATH}/{fileName}.asset";

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
        item.icon = null; // Placeholder - will be generated or assigned later

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
    }
}
