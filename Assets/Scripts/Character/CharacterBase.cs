using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCharacterBase", menuName = "Data/New Character Base")]
public class CharacterBase : ScriptableObject
{
    public Texture2D lineArtTexture;
    public Texture2D colorTexture;
    public ColorPalette skinColors;
    public ColorPalette hairColors;
    public ColorPalette clothesColors;
    public Color lineColor;
    public Material baseMaterial;
}
