using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "NewCharacter", menuName = "Dialogue System/New Character")]
public class Character : ScriptableObject
{
    [Tooltip("Name of the character. Also used as the key to the localization string table. Must be unique.")]
    public string characterName;
    public Dictionary<string, Sprite> portraits;
}
