using System.Collections.Generic;
using System.Linq;
using ExhibitionSystem.Core;
using ExhibitionSystem.Data;
using UnityEditor;
using UnityEngine;

public static class ExhibitionTestDataBuilder
{
    private const string ROOT_PATH = "Assets/Resources/Exhibitions";
    private const string ITEMS_PATH = ROOT_PATH + "/Items";
    private const string THEMES_PATH = ROOT_PATH + "/Themes";
    private const string INSPIRATIONS_PATH = ROOT_PATH + "/Inspirations";
    private const string ICONS_PATH = ROOT_PATH + "/Icons";

    [MenuItem("Tools/Museum/Generate Test Data")]
    public static void GenerateTestData()
    {
        EnsureDirectoriesExist();

        var items = GenerateItems();
        var inspirations = GenerateInspirations(items);
        var themes = GenerateThemes();

        RebindLoadedManagers(items, inspirations, themes);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Museum Test Data] Updated {items.Count} items, {inspirations.Count} inspirations, {themes.Count} themes.");
    }

    [MenuItem("Tools/Museum/Reset Exhibition Progress")]
    public static void ResetExhibitionProgress()
    {
        EnsureDirectoriesExist();

        int resetCount = 0;
        foreach (var theme in Resources.LoadAll<ExhibitionTheme>("Exhibitions/Themes"))
        {
            if (theme == null)
                continue;

            theme.ResetCompletion();
            EditorUtility.SetDirty(theme);
            resetCount++;
        }

        ExhibitionManager.ResetKnownInspirationMatches();
        RebindLoadedManagers(
            Resources.LoadAll<ExhibitItemData>("Exhibitions/Items").OrderBy(item => item.sortOrder).ToList(),
            Resources.LoadAll<InspirationData>("Exhibitions/Inspirations").OrderBy(inspiration => inspiration.id).ToList(),
            Resources.LoadAll<ExhibitionTheme>("Exhibitions/Themes").OrderBy(theme => theme.day).ThenBy(theme => theme.title).ToList());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Museum Test Data] Reset exhibition progress for {resetCount} themes.");
    }

    [MenuItem("Tools/Museum/Clear Generated Assets")]
    public static void ClearGeneratedAssets()
    {
        DeleteFolderIfExists(ITEMS_PATH);
        DeleteFolderIfExists(THEMES_PATH);
        DeleteFolderIfExists(INSPIRATIONS_PATH);
        AssetDatabase.Refresh();
        Debug.Log("[Museum Test Data] Generated exhibition data cleared. Icon artwork was preserved.");
    }

    private static List<ExhibitItemData> GenerateItems()
    {
        var itemDefs = new (string fileName, string itemName, string nameKey, string description, string[] tags, string iconFile, float iconScale)[]
        {
            ("BlueFeather", "Blue Feather", "item_blue_feather", "A soft blue feather from a rare bird in the Otowa forest.", new[] {"Birdwatching"}, "bluefeather-1.png", 1f),
            ("Binoculars", "Binoculars", "item_binoculars", "A trusty pair of binoculars often seen with Rintaro.", new[] {"Birdwatching"}, "binoculars-2.png", 1f),
            ("MineralOre", "Mineral Ore", "item_mineral_ore", "A mineral ore shaped by Otowa's unusual hot spring geology.", new[] {"Hot Springs", "Yuji"}, "Mineral-3.png", 1f),
            ("Amulet", "Amulet", "item_amulet", "A small blessing charm for health and peace.", new[] {"Hot Springs"}, "amulet-4.png", 1f),
            ("ThreeColorDango", "Three-color Dango", "item_three_color_dango", "A colorful snack often served near the springs.", new[] {"Hot Springs", "Jiro"}, "dango-5.png", 1f),
            ("Shichimi", "Seven-flavor Chili", "item_shichimi", "A spice blend that gives old recipes their signature bite.", new[] {"Jiro"}, "shichimi-6.png", 0.85f),
            ("Sake", "Sake", "item_sake", "Otowa sake, once awarded in a local specialty competition.", new[] {"Yuji"}, "sake-7.png", 1.15f),
            ("Herbs", "Herbs", "item_herbs", "Fragrant herbs that shape Otowa's signature flavor.", new[] {"Yuji", "Jiro"}, "herb-8.png", 1f),
            ("BirdMask", "Bird Mask", "item_bird_mask", "A bird-shaped festival mask tied to old beliefs.", new[] {"Summer Festival"}, "fan-9.png", 1f),
            ("Fireworks", "Fireworks", "item_fireworks", "Yuji's crowd-pleasing summer specialty.", new[] {"Summer Festival", "Yuji"}, "firework-10.png", 0.65f),
            ("TrainTicket", "Train Ticket", "item_train_ticket", "A ticket home to Otowa for wandering youths.", new[] {"Summer Festival"}, "ticket-11.png", 1f),
            ("GeologyTextbook", "Geology Textbook", "item_geology_textbook", "A professor's old geology textbook filled with marginal notes.", new[] {"Rintaro", "Birdwatching", "Yuji"}, "book-12.png", 1f),
            ("OctopusPot", "Octopus Pot", "item_octopus_pot", "A takotsubo octopus trap carrying a memory of summer dreams.", new[] {"Hot Springs"}, "pot-13.png", 0.85f),
            ("BrokenAcousticGuitar", "Broken Acoustic Guitar", "item_broken_acoustic_guitar", "A broken guitar left behind after a family fight.", new[] {"Jiro"}, "guitar-14.png", 1f),
            ("Painting", "Painting", "item_painting", "A painting that gathers the colors of Otowa.", new string[] {}, "painting-15.png", 1f),
            ("OtowaBluesVinylRecord", "Otowa Blues Vinyl Record", "item_otowa_blues_record", "A vinyl record carrying a goodbye to Otowa town.", new string[] {}, "blues-16.png", 1f)
        };

        var items = new List<ExhibitItemData>();
        for (int i = 0; i < itemDefs.Length; i++)
        {
            var def = itemDefs[i];
            var item = GetOrCreateAsset<ExhibitItemData>($"{ITEMS_PATH}/{def.fileName}.asset");
            item.itemName = def.itemName;
            item.nameKey = def.nameKey;
            item.sortOrder = i + 1;
            item.description = def.description;
            item.descriptionKey = $"desc_{def.nameKey.Replace("item_", string.Empty)}";
            item.isUnlocked = true;
            item.tags = def.tags.ToList();
            item.icon = LoadConfiguredSprite($"{ICONS_PATH}/{def.iconFile}");
            item.iconScale = def.iconScale;

            EditorUtility.SetDirty(item);
            items.Add(item);
        }

        return items;
    }

    private static List<InspirationData> GenerateInspirations(IReadOnlyList<ExhibitItemData> items)
    {
        ExhibitItemData Item(int oneBasedIndex) => items[oneBasedIndex - 1];

        var defs = new (int id, string text, ExhibitItemData item)[]
        {
            (1, "Rare creatures dwell within the forests of Otowa.", Item(1)),
            (2, "Wherever Rintaro goes, this is never far behind.", Item(2)),
            (3, "A professor retired to Otowa to savor the quiet life.", Item(12)),
            (4, "The color of the water, the color of the birds, the color of Otowa.", Item(15)),
            (5, "A music boy left Otowa after a bitter quarrel with his father.", Item(14)),
            (6, "Octopus traps, fleeting dreams under the summer moon.", Item(13)),
            (7, "Legend speaks of an indigenous Otowa belief in an avian deity.", Item(9)),
            (8, "When it blossoms in the sky, it marks the most beautiful night of summer.", Item(10)),
            (9, "Bye Bye, my Otowa town.", Item(16)),
            (10, "The source of Otowa's signature flavor, found in sake and local cuisine.", Item(8)),
            (11, "A mysterious recipe dating back centuries.", Item(6)),
            (12, "It won Otowa a gold medal at the regional specialty competition over a decade ago.", Item(7)),
            (13, "A blessing from Otowa: health and peace.", Item(4)),
            (14, "The healing properties of Otowa's hot springs.", Item(3)),
            (15, "A father's silent love.", Item(5)),
            (16, "On that day, all wandering souls journey back to Otowa.", Item(11))
        };

        var inspirations = new List<InspirationData>();
        foreach (var def in defs)
        {
            var inspiration = GetOrCreateAsset<InspirationData>($"{INSPIRATIONS_PATH}/Idea_{def.id:D2}.asset");
            inspiration.id = def.id;
            inspiration.text = def.text;
            inspiration.mappedItem = def.item;
            inspiration.isUnlocked = true;
            inspiration.fallbackHint = "This idea feels connected, but I need to place it in the right context.";

            EditorUtility.SetDirty(inspiration);
            inspirations.Add(inspiration);
        }

        return inspirations;
    }

    private static List<ExhibitionTheme> GenerateThemes()
    {
        var themes = new List<ExhibitionTheme>
        {
            CreateTheme(
                "SummerFestival",
                "Summer Festival: An Introduction to Otowa Folklore",
                2,
                3,
                InspirationSelectionMode.AnyFromPool,
                new[] {7, 8, 16},
                "A festival exhibition about belief, fireworks, and homecoming.",
                new (int, string)[]
                {
                    (7, "Junko seemed to mention that the origin of the Summer Festival is related to birds."),
                    (8, "Summer's most beautiful night seems to bloom above the town."),
                    (16, "The Summer Festival isn't just a ceremony, it is also tied to family bonds.")
                }),
            CreateTheme(
                "Yuji",
                "Sake & Sparks: Yuji, Artisan of Two Worlds",
                2,
                3,
                InspirationSelectionMode.AnyFromPool,
                new[] {8, 10, 12},
                "A profile of Yuji's pub, inventions, and local specialty work.",
                new (int, string)[]
                {
                    (8, "Yuji said running the pub was just a hobby. What was his main profession again?"),
                    (10, "Where exactly does the unique flavor in Yuji's sake come from?"),
                    (12, "I remember Yuji once brought honor to Otowa in a competition.")
                }),
            CreateTheme(
                "Birdwatching",
                "Wings Over Otowa: A Birdwatcher's Paradise",
                3,
                3,
                InspirationSelectionMode.AnyFromPool,
                new[] {1, 2, 3, 7},
                "An exhibition about birds, watchers, and local belief.",
                new (int, string)[]
                {
                    (1, "Professor Rintaro was after some rare bird. What color was it again...?"),
                    (2, "You absolutely can't go birdwatching without one of these..."),
                    (3, "From geology professor to birdwatching devotee... let's put that turning point into the exhibition!"),
                    (7, "Junko seemed to mention that the origin of the Summer Festival is related to birds.")
                }),
            CreateTheme(
                "ChefJiro",
                "Master Jiro: Culinary Devotion and Hidden Sorrows",
                3,
                4,
                InspirationSelectionMode.ExactSet,
                new[] {5, 10, 11, 15},
                "A focused exhibition about Jiro's cooking and family history.",
                new (int, string)[]
                {
                    (5, "I should find an exhibit that captures the rift between Jiro and his son."),
                    (10, "What could possibly capture the taste of Otowa...?"),
                    (11, "This exhibition definitely needs Jiro's signature creation."),
                    (15, "I remember... Jiro was secretly making Hachi's favorite treat.")
                }),
            CreateTheme(
                "HotSprings",
                "The Mountain Springs: A Soak Beneath the Milky Way",
                3,
                4,
                InspirationSelectionMode.AnyFromPool,
                new[] {4, 6, 13, 14},
                "An exhibition about Otowa's water, health, and hot spring culture.",
                new (int, string)[]
                {
                    (4, "This piece embodies Mizuki's dream, I promised her I'd show it to the world."),
                    (6, "Let's pick something that belongs to the seaside... it even comes with a haiku."),
                    (13, "This is the blessing Mizuki gave me, let's share it with the travelers, too."),
                    (14, "They say Otowa's hot spring holds a wondrous energy... that's exactly what I should put into the exhibition.")
                })
        };

        return themes;
    }

    private static ExhibitionTheme CreateTheme(
        string fileName,
        string title,
        int day,
        int requiredIdeas,
        InspirationSelectionMode mode,
        int[] validIdeaIds,
        string description,
        (int id, string hint)[] hints)
    {
        var theme = GetOrCreateAsset<ExhibitionTheme>($"{THEMES_PATH}/{fileName}.asset");
        theme.title = title;
        theme.titleKey = $"theme_{fileName.ToLowerInvariant()}";
        theme.description = description;
        theme.descriptionKey = $"desc_{theme.titleKey.Replace("theme_", string.Empty)}";
        theme.day = day;
        theme.requiredInspirations = requiredIdeas;
        theme.selectionMode = mode;
        theme.validInspirationIds = validIdeaIds.ToList();
        theme.fallbackMissingHint = "I am missing one of the ideas that really belongs to this exhibition.";
        theme.fallbackInvalidHint = "One of these ideas feels out of place for this exhibition.";
        theme.missingIdeaHints = hints == null
            ? new List<InspirationHint>()
            : hints.Select(hint => new InspirationHint { inspirationId = hint.id, hintText = hint.hint }).ToList();
        theme.ResetCompletion();

        EditorUtility.SetDirty(theme);
        return theme;
    }

    private static void DeleteFolderIfExists(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            AssetDatabase.DeleteAsset(path);
    }

    private static void EnsureDirectoriesExist()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        if (!AssetDatabase.IsValidFolder(ROOT_PATH))
            AssetDatabase.CreateFolder("Assets/Resources", "Exhibitions");

        if (!AssetDatabase.IsValidFolder(ITEMS_PATH))
            AssetDatabase.CreateFolder(ROOT_PATH, "Items");

        if (!AssetDatabase.IsValidFolder(THEMES_PATH))
            AssetDatabase.CreateFolder(ROOT_PATH, "Themes");

        if (!AssetDatabase.IsValidFolder(INSPIRATIONS_PATH))
            AssetDatabase.CreateFolder(ROOT_PATH, "Inspirations");

        if (!AssetDatabase.IsValidFolder(ICONS_PATH))
            AssetDatabase.CreateFolder(ROOT_PATH, "Icons");
    }

    private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
            return asset;

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static Sprite LoadConfiguredSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            bool needsImport = importer.textureType != TextureImporterType.Sprite ||
                importer.spritePixelsPerUnit != 100 ||
                importer.filterMode != FilterMode.Bilinear ||
                importer.mipmapEnabled;

            if (needsImport)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning($"[Museum Test Data] Missing item icon: {path}");

        return sprite;
    }

    private static void RebindLoadedManagers(
        IReadOnlyList<ExhibitItemData> items,
        IReadOnlyList<InspirationData> inspirations,
        IReadOnlyList<ExhibitionTheme> themes)
    {
        foreach (var manager in Object.FindObjectsByType<ExhibitionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var serializedObject = new SerializedObject(manager);
            AssignObjectArray(serializedObject.FindProperty("_allItems"), items);
            AssignObjectArray(serializedObject.FindProperty("_allInspirations"), inspirations);
            AssignObjectArray(serializedObject.FindProperty("_allThemes"), themes);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
        }
    }

    private static void AssignObjectArray<T>(SerializedProperty property, IReadOnlyList<T> values) where T : Object
    {
        if (property == null || !property.isArray)
            return;

        property.arraySize = values != null ? values.Count : 0;
        for (int i = 0; i < property.arraySize; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
