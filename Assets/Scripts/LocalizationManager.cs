using Base;
using DialogueSystem.Core;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Settings;



[ExecuteAlways]
public class LocalizationManager : MonoSingleton<LocalizationManager>
{
    public StringTableCollection characterNameTable;
    public StringTableCollection dialogueChoiceLabelTable;
    public StringTableCollection dialogueTextTable;
    public StringTableCollection itemNameTable;
    public StringTableCollection itemDescriptionTable;

    protected override void DestroyDuplicate(GameObject go)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(go);
        else
#endif
            Destroy(go);
    }
    
    public string GetCharacterName(string characterKey)
    {
        return LocalizationSettings.StringDatabase.GetLocalizedString(characterNameTable.name, characterKey);
    }

    public string GetDialogueText(string dialogueTextKey)
    {
        return LocalizationSettings.StringDatabase.GetLocalizedString(dialogueTextTable.name, dialogueTextKey);
    }

    public string GetDialogueChoice(string dialogueChoiceKey)
    {
        return LocalizationSettings.StringDatabase.GetLocalizedString(dialogueChoiceLabelTable.name, dialogueChoiceKey);
    }

    public string GetItemName(string itemName)
    {
        return LocalizationSettings.StringDatabase.GetLocalizedString(itemNameTable.name, itemName);
    }

    public string GetItemDescription(string itemDescriptionKey)
    {
        return LocalizationSettings.StringDatabase.GetLocalizedString(itemDescriptionTable.name, itemDescriptionKey);
    }
}
