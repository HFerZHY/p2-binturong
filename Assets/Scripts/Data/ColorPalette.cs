using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewPaletteSet", menuName = "Data/New Palette")]
public class ColorPalette : ScriptableObject
{
    public string paletteName;
    public Color[] colors;
}