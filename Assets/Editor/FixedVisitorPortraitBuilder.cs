using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Exports stable story-character portraits from the same recolored templates
/// used by VisitorCharacterGenerator. Ordinary exhibition visitors remain random.
/// </summary>
public static class FixedVisitorPortraitBuilder
{
    private const string CharacterRoot = "Assets/Resources/Characters";
    private const string OutputFolder = CharacterRoot + "/PassengerPortraits";
    private const int OutputSize = 512;
    private const byte VisibleAlphaThreshold = 8;

    [MenuItem("Tools/Museum/Generate Fixed Story Passenger Portraits")]
    public static void Generate()
    {
        EnsureOutputFolder();

        var male = AssetDatabase.LoadAssetAtPath<CharacterBase>(
            CharacterRoot + "/YoungManBase.asset");
        var female = AssetDatabase.LoadAssetAtPath<CharacterBase>(
            CharacterRoot + "/YoungWomanBase.asset");

        if (!ValidateBase(male, "YoungManBase") || !ValidateBase(female, "YoungWomanBase"))
            return;

        // Palette indices intentionally stay explicit: story characters must
        // keep the same appearance even though ordinary passengers are random.
        GeneratePortrait("Hikaru", male, skinIndex: 0, hairIndex: 0, clothesIndex: 0);
        GeneratePortrait("Hachi", male, skinIndex: 0, hairIndex: 1, clothesIndex: 2);
        GeneratePortrait("Misaki", female, skinIndex: 2, hairIndex: 2, clothesIndex: 3);
        GeneratePortrait("Passenger01", female, skinIndex: 1, hairIndex: 0, clothesIndex: 1);
        GeneratePortrait("Passenger02", male, skinIndex: 2, hairIndex: 2, clothesIndex: 3);
        GeneratePortrait("Passenger03", female, skinIndex: 0, hairIndex: 1, clothesIndex: 0);
        GeneratePortrait("Passenger04", male, skinIndex: 1, hairIndex: 3, clothesIndex: 1);
        GeneratePortrait("Passenger05", female, skinIndex: 2, hairIndex: 3, clothesIndex: 2);

        AssetDatabase.Refresh();
        foreach (var portraitName in new[]
                 {
                     "Hikaru", "Hachi", "Misaki",
                     "Passenger01", "Passenger02", "Passenger03", "Passenger04", "Passenger05",
                 })
            ConfigureSpriteImport(OutputFolder + $"/{portraitName}.png");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "[FixedVisitorPortraitBuilder] Generated named characters and stable passenger portraits " +
            $"under {OutputFolder}.");
    }

    private static void GeneratePortrait(
        string characterName,
        CharacterBase characterBase,
        int skinIndex,
        int hairIndex,
        int clothesIndex)
    {
        Texture2D colorMask = ReadTexture(characterBase.colorTexture);
        Texture2D lineMask = ReadTexture(characterBase.lineArtTexture);
        var output = new Texture2D(OutputSize, OutputSize, TextureFormat.RGBA32, false);

        try
        {
            Color32 skin = GetColor(characterBase.skinColors, skinIndex, "skin", characterName);
            Color32 hair = GetColor(characterBase.hairColors, hairIndex, "hair", characterName);
            Color32 clothes = GetColor(
                characterBase.clothesColors, clothesIndex, "clothes", characterName);
            Color32 line = ResolveLineColor(characterBase.lineColor);

            Color32[] colorPixels = colorMask.GetPixels32();
            Color32[] linePixels = lineMask.GetPixels32();
            var outputPixels = new Color32[colorPixels.Length];

            for (int i = 0; i < colorPixels.Length; i++)
            {
                Color32 source = colorPixels[i];
                if (source.a <= VisibleAlphaThreshold)
                {
                    outputPixels[i] = new Color32(0, 0, 0, 0);
                    continue;
                }

                outputPixels[i] = RecolorMaskPixel(source, skin, hair, clothes);

                Color32 lineSource = linePixels[i];
                if (lineSource.a > VisibleAlphaThreshold)
                    outputPixels[i] = new Color32(line.r, line.g, line.b, lineSource.a);
            }

            output.SetPixels32(outputPixels);
            output.Apply(false);
            File.WriteAllBytes(OutputFolder + $"/{characterName}.png", output.EncodeToPNG());
        }
        finally
        {
            Object.DestroyImmediate(colorMask);
            Object.DestroyImmediate(lineMask);
            Object.DestroyImmediate(output);
        }
    }

    private static Texture2D ReadTexture(Texture source)
    {
        var temp = RenderTexture.GetTemporary(
            OutputSize, OutputSize, 0, RenderTextureFormat.ARGB32);
        var readable = new Texture2D(OutputSize, OutputSize, TextureFormat.RGBA32, false);
        RenderTexture previous = RenderTexture.active;

        try
        {
            Graphics.Blit(source, temp);
            RenderTexture.active = temp;
            readable.ReadPixels(new Rect(0, 0, OutputSize, OutputSize), 0, 0);
            readable.Apply(false);
            return readable;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temp);
        }
    }

    private static Color32 GetColor(
        ColorPalette palette,
        int index,
        string paletteName,
        string characterName)
    {
        if (palette == null || palette.colors == null || index < 0 || index >= palette.colors.Length)
        {
            Debug.LogWarning(
                $"[FixedVisitorPortraitBuilder] Invalid {paletteName} palette index {index} " +
                $"for {characterName}; using white.");
            return Color.white;
        }

        return palette.colors[index];
    }

    private static Color32 RecolorMaskPixel(
        Color32 source,
        Color32 skin,
        Color32 hair,
        Color32 clothes)
    {
        Color32 targetColor;
        if (source.b > source.r && source.b > source.g)
            targetColor = skin;
        else if (source.r > source.b && source.g > source.b)
            targetColor = hair;
        else
            targetColor = clothes;

        targetColor.a = source.a;
        return targetColor;
    }

    private static Color32 ResolveLineColor(Color lineColor)
    {
        return lineColor.a <= 0.01f
            ? new Color32(18, 18, 24, 255)
            : (Color32)lineColor;
    }

    private static bool ValidateBase(CharacterBase characterBase, string assetName)
    {
        bool valid = characterBase != null
                     && characterBase.colorTexture != null
                     && characterBase.lineArtTexture != null
                     && characterBase.skinColors != null
                     && characterBase.hairColors != null
                     && characterBase.clothesColors != null;

        if (!valid)
            Debug.LogError($"[FixedVisitorPortraitBuilder] Incomplete character base: {assetName}.");

        return valid;
    }

    private static void EnsureOutputFolder()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
            AssetDatabase.CreateFolder(CharacterRoot, "PassengerPortraits");
    }

    private static void ConfigureSpriteImport(string assetPath)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
    }
}
