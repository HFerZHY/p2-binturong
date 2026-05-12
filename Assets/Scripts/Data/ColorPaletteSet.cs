using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewPaletteSet", menuName = "Data/New Palette Set")]
public class ColorPaletteSet : ScriptableObject
{
    [Min(1)]
    public int paletteSize = 2;

    public ColorPalette[] palettes;
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        for (int i = 0; i < palettes.Length; i++)
        {
            if (palettes[i].colors == null) continue;

            if (palettes[i].colors.Length != paletteSize)
            {
                Debug.LogWarning(
                    $"[ColorPaletteSet] '{name}': palette '{palettes[i].paletteName}' " +
                    $"(index {i}) has {palettes[i].colors.Length} colors but expected {paletteSize}.",
                    this
                );
            }
        }
    }
#endif
}
