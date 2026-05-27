using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private const int ICON_SIZE = 128;

    [MenuItem("Tools/Museum/Generate Test Data")]
    public static void GenerateTestData()
    {
        ResetGeneratedDataFolders();
        EnsureDirectoriesExist();

        var items = GenerateItems();
        var inspirations = GenerateInspirations(items);
        var themes = GenerateThemes();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Museum Test Data] Generated {items.Count} items, {inspirations.Count} inspirations, {themes.Count} themes.");
    }

    [MenuItem("Tools/Museum/Clear Generated Assets")]
    public static void ClearGeneratedAssets()
    {
        DeleteFolderIfExists(ITEMS_PATH);
        DeleteFolderIfExists(THEMES_PATH);
        DeleteFolderIfExists(INSPIRATIONS_PATH);
        DeleteFolderIfExists(ICONS_PATH);
        AssetDatabase.Refresh();
        Debug.Log("[Museum Test Data] Generated exhibition assets cleared.");
    }

    private static List<ExhibitItemData> GenerateItems()
    {
        var itemDefs = new (string fileName, string itemName, string nameKey, string description, string[] tags)[]
        {
            ("BlueFeather", "Blue Feather", "item_blue_feather", "A soft blue feather from a rare bird in the Otowa forest.", new[] {"Birdwatching"}),
            ("Binoculars", "Binoculars", "item_binoculars", "A trusty pair of binoculars often seen with Rintaro.", new[] {"Birdwatching"}),
            ("MineralOre", "Mineral Ore", "item_mineral_ore", "A mineral ore shaped by Otowa's unusual hot spring geology.", new[] {"Hot Springs", "Yuji"}),
            ("Amulet", "Amulet", "item_amulet", "A small blessing charm for health and peace.", new[] {"Hot Springs"}),
            ("ThreeColorDango", "Three-color Dango", "item_three_color_dango", "A colorful snack often served near the springs.", new[] {"Hot Springs", "Jiro"}),
            ("Shichimi", "Seven-flavor Chili", "item_shichimi", "A spice blend that gives old recipes their signature bite.", new[] {"Jiro"}),
            ("Sake", "Sake", "item_sake", "Otowa sake, once awarded in a local specialty competition.", new[] {"Yuji"}),
            ("Herbs", "Herbs", "item_herbs", "Fragrant herbs that shape Otowa's signature flavor.", new[] {"Yuji", "Jiro"}),
            ("BirdMask", "Bird Mask", "item_bird_mask", "A bird-shaped festival mask tied to old beliefs.", new[] {"Summer Festival"}),
            ("Fireworks", "Fireworks", "item_fireworks", "Yuji's crowd-pleasing summer specialty.", new[] {"Summer Festival", "Yuji"}),
            ("TrainTicket", "Train Ticket", "item_train_ticket", "A ticket home to Otowa for wandering youths.", new[] {"Summer Festival"}),
            ("GeologyTextbook", "Geology Textbook", "item_geology_textbook", "A professor's old geology textbook filled with marginal notes.", new[] {"Rintaro", "Birdwatching", "Yuji"}),
            ("OctopusPot", "Octopus Pot", "item_octopus_pot", "A takotsubo octopus trap carrying a memory of summer dreams.", new[] {"Hot Springs"}),
            ("BrokenAcousticGuitar", "Broken Acoustic Guitar", "item_broken_acoustic_guitar", "A broken guitar left behind after a family fight.", new[] {"Jiro"}),
            ("Painting", "Painting", "item_painting", "A painting that gathers the colors of Otowa.", new string[] {}),
            ("OtowaBluesVinylRecord", "Otowa Blues Vinyl Record", "item_otowa_blues_record", "A vinyl record carrying a goodbye to Otowa town.", new string[] {})
        };

        var items = new List<ExhibitItemData>();
        for (int i = 0; i < itemDefs.Length; i++)
        {
            var def = itemDefs[i];
            var icon = GenerateNumberedIcon(i + 1);
            string iconPath = $"{ICONS_PATH}/Icon_{i + 1:D2}.png";
            SaveTextureAsPNG(icon, iconPath);
            AssetDatabase.ImportAsset(iconPath);

            var importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }

            var item = ScriptableObject.CreateInstance<ExhibitItemData>();
            item.itemName = def.itemName;
            item.nameKey = def.nameKey;
            item.sortOrder = i + 1;
            item.description = def.description;
            item.descriptionKey = $"desc_{def.nameKey.Replace("item_", string.Empty)}";
            item.isUnlocked = true;
            item.tags = def.tags.ToList();
            item.icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

            string path = $"{ITEMS_PATH}/{def.fileName}.asset";
            AssetDatabase.CreateAsset(item, path);
            items.Add(item);
        }

        return items;
    }

    private static List<InspirationData> GenerateInspirations(IReadOnlyList<ExhibitItemData> items)
    {
        ExhibitItemData Item(int oneBasedIndex) => items[oneBasedIndex - 1];

        var defs = new (int id, string text, ExhibitItemData item)[]
        {
            (1, "There are some rare bird species in the Otowa forest.", Item(1)),
            (2, "Whenever you see Rintaro, you are guaranteed to see this.", Item(2)),
            (3, "A professor moved to Otowa to enjoy life after retirement.", Item(12)),
            (4, "Color of water, color of birds, color of Otowa.", Item(15)),
            (5, "A boy who loved music had a massive fight with his father and left Otowa.", Item(14)),
            (6, "\"Octopus traps, fleeting dreams under the summer moon.\"", Item(13)),
            (7, "Legend has it that Otowa's Summer Festival originated from worshipping a bird-shaped deity.", Item(9)),
            (8, "The multitalented Yuji's business isn't limited to the pub.", Item(10)),
            (9, "\"Bye Bye, my Otowa town...\"", Item(16)),
            (10, "The source of Otowa's signature flavor.", Item(8)),
            (11, "Jiro recreated a mysterious recipe from centuries ago.", Item(6)),
            (12, "Otowa won a gold medal in a local specialty competition over a decade ago.", Item(7)),
            (13, "A blessing from Otowa: health and peace.", Item(4)),
            (14, "Otowa's hot springs are quite magical and can improve your health.", Item(3)),
            (15, "The vibrant colors of the fireworks come from this.", Item(3)),
            (16, "A type of snack often placed on floating trays in the water.", Item(5)),
            (17, "A father's silent love.", Item(5)),
            (18, "On that day, all wandering youths will return to Otowa.", Item(11))
        };

        var inspirations = new List<InspirationData>();
        foreach (var def in defs)
        {
            var inspiration = ScriptableObject.CreateInstance<InspirationData>();
            inspiration.id = def.id;
            inspiration.text = def.text;
            inspiration.mappedItem = def.item;
            inspiration.isUnlocked = true;
            inspiration.fallbackHint = "Rin: (This idea feels connected, but I need to place it in the right context.)";

            AssetDatabase.CreateAsset(inspiration, $"{INSPIRATIONS_PATH}/Idea_{def.id:D2}.asset");
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
                "Summer Festival",
                2,
                3,
                InspirationSelectionMode.AnyFromPool,
                new[] {7, 8, 18, 15},
                "A festival exhibition about belief, fireworks, and homecoming.",
                new (int, string)[]
                {
                    (7, "Rin: (Junko seemed to mention that the Summer Festival originated from some kind of belief. Maybe I'll need a prop related to that belief.)"),
                    (8, "Rin: (Mr. Yuji said that his specialty actually isn't mixing drinks. What exactly is it that has everyone looking forward to it so much?)"),
                    (18, "Rin: (The Summer Festival isn't just a ceremony, it's also the day people come home...)"),
                    (15, "Rin: (To make the fireworks look better, Yuji even went to ask Rintaro for advice.)")
                }),
            CreateTheme(
                "Yuji",
                "Yuji",
                2,
                4,
                InspirationSelectionMode.AnyFromPool,
                new[] {3, 8, 10, 12, 15},
                "A profile of Yuji's pub, inventions, and local specialty work.",
                new (int, string)[]
                {
                    (3, "Rin: (Who was the regular at Mr. Yuji's pub again? I think it was a highly knowledgeable old man.)"),
                    (8, "Rin: (Mr. Yuji said that his specialty actually isn't mixing drinks. What exactly is it that has everyone looking forward to it so much?)"),
                    (10, "Rin: (Where exactly does the unique flavor in Yuji's sake come from?)"),
                    (12, "Rin: (In that local specialty competition, the one who won the gold medal seemed to represent the side of innovation...)"),
                    (15, "Rin: (To make the fireworks look better, Yuji even went to ask Rintaro for advice.)")
                }),
            CreateTheme(
                "Birdwatching",
                "Birdwatching",
                3,
                3,
                InspirationSelectionMode.AnyFromPool,
                new[] {1, 2, 3, 7},
                "An exhibition about birds, watchers, and local belief.",
                null),
            CreateTheme(
                "ChefJiro",
                "Chef Jiro",
                3,
                4,
                InspirationSelectionMode.ExactSet,
                new[] {5, 10, 11, 17},
                "A focused exhibition about Jiro's cooking and family history.",
                null),
            CreateTheme(
                "HotSprings",
                "Hot Springs",
                3,
                4,
                InspirationSelectionMode.AnyFromPool,
                new[] {4, 6, 13, 14, 16},
                "An exhibition about Otowa's water, health, and hot spring culture.",
                null)
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
        var theme = ScriptableObject.CreateInstance<ExhibitionTheme>();
        theme.title = title;
        theme.titleKey = $"theme_{fileName.ToLowerInvariant()}";
        theme.description = description;
        theme.descriptionKey = $"desc_{theme.titleKey.Replace("theme_", string.Empty)}";
        theme.day = day;
        theme.requiredInspirations = requiredIdeas;
        theme.selectionMode = mode;
        theme.validInspirationIds = validIdeaIds.ToList();
        theme.fallbackMissingHint = "Rin: (I am missing one of the ideas that really belongs to this exhibition.)";
        theme.fallbackInvalidHint = "Rin: (One of these ideas feels out of place for this exhibition.)";
        theme.missingIdeaHints = hints == null
            ? new List<InspirationHint>()
            : hints.Select(hint => new InspirationHint { inspirationId = hint.id, hintText = hint.hint }).ToList();

        AssetDatabase.CreateAsset(theme, $"{THEMES_PATH}/{fileName}.asset");
        return theme;
    }

    private static void ResetGeneratedDataFolders()
    {
        DeleteFolderIfExists(ITEMS_PATH);
        DeleteFolderIfExists(THEMES_PATH);
        DeleteFolderIfExists(INSPIRATIONS_PATH);
        DeleteFolderIfExists(ICONS_PATH);
        AssetDatabase.Refresh();
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

    private static Texture2D GenerateNumberedIcon(int number)
    {
        var texture = new Texture2D(ICON_SIZE, ICON_SIZE, TextureFormat.RGBA32, false);
        Color bgColor = Color.HSVToRGB((number - 1) / 16f, 0.55f, 0.75f);
        Color borderColor = bgColor * 0.55f;
        borderColor.a = 1f;

        for (int y = 0; y < ICON_SIZE; y++)
        {
            for (int x = 0; x < ICON_SIZE; x++)
            {
                bool border = x < 5 || y < 5 || x >= ICON_SIZE - 5 || y >= ICON_SIZE - 5;
                texture.SetPixel(x, y, border ? borderColor : bgColor);
            }
        }

        DrawNumber(texture, number, Color.white);
        texture.Apply();
        return texture;
    }

    private static void DrawNumber(Texture2D texture, int number, Color color)
    {
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
        const int digitWidth = 5;
        const int digitHeight = 7;
        const int spacing = 2;
        const int scale = 6;

        int totalWidth = numStr.Length * (digitWidth * scale) + (numStr.Length - 1) * spacing * scale;
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
                    if (pattern[py][px] != '#') continue;

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

    private static void SaveTextureAsPNG(Texture2D texture, string path)
    {
        byte[] pngData = texture.EncodeToPNG();
        string fullPath = Path.Combine(Application.dataPath, "..", path);
        File.WriteAllBytes(fullPath, pngData);
        Object.DestroyImmediate(texture);
    }
}
