using ExhibitionSystem.Data;
using UnityEditor;
using UnityEngine;

public static class ItemIconUpdater
{
    private const string ICONS_PATH = "Assets/Resources/Exhibitions/Icons";
    private const string ITEMS_PATH = "Assets/Resources/Exhibitions/Items";

    [MenuItem("Tools/Museum/Update Item Icons From Images")]
    public static void UpdateItemIcons()
    {
        var iconMappings = new (string iconFile, string itemFile, float iconScale)[]
        {
            ("bluefeather-1.png", "BlueFeather.asset", 1f),
            ("binoculars-2.png", "Binoculars.asset", 1f),
            ("Mineral-3.png", "MineralOre.asset", 1f),
            ("amulet-4.png", "Amulet.asset", 1f),
            ("dango-5.png", "ThreeColorDango.asset", 1f),
            ("shichimi-6.png", "Shichimi.asset", 0.85f),
            ("sake-7.png", "Sake.asset", 1.15f),
            ("herb-8.png", "Herbs.asset", 1f),
            ("fan-9.png", "BirdMask.asset", 1f),
            ("firework-10.png", "Fireworks.asset", 0.65f),
            ("ticket-11.png", "TrainTicket.asset", 1f),
            ("book-12.png", "GeologyTextbook.asset", 1f),
            ("pot-13.png", "OctopusPot.asset", 0.85f),
            ("guitar-14.png", "BrokenAcousticGuitar.asset", 1f),
            ("painting-15.png", "Painting.asset", 1f),
            ("blues-16.png", "OtowaBluesVinylRecord.asset", 1f),
        };

        // Configure sprite imports
        foreach (var (iconFile, _, _) in iconMappings)
        {
            ConfigureSpriteImport($"{ICONS_PATH}/{iconFile}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Update item references
        int updated = 0;
        foreach (var (iconFile, itemFile, iconScale) in iconMappings)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ICONS_PATH}/{iconFile}");
            var item = AssetDatabase.LoadAssetAtPath<ExhibitItemData>($"{ITEMS_PATH}/{itemFile}");

            if (item != null && sprite != null)
            {
                item.icon = sprite;
                item.iconScale = iconScale;
                EditorUtility.SetDirty(item);
                updated++;
                Debug.Log($"[ItemIconUpdater] Updated {itemFile} icon: {sprite.name}, scale: {iconScale}");
            }
            else
            {
                Debug.LogWarning($"[ItemIconUpdater] Failed to update {itemFile} - Item: {item != null}, Sprite: {sprite != null}");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[ItemIconUpdater] Updated {updated} item icons.");
    }

    private static void ConfigureSpriteImport(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[ItemIconUpdater] Could not find texture at: {path}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 100;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        Debug.Log($"[ItemIconUpdater] Configured sprite import: {path}");
    }
}
