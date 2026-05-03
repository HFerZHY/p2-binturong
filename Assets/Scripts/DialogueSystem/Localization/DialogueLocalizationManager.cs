using Base;
using DialogueSystem.Core;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Settings;


namespace DialogueSystem.Localization
{
    [ExecuteAlways]
    public class DialogueLocalizationManager : MonoSingleton<DialogueLocalizationManager>
    {
        public StringTableCollection characterNameTable;
        public StringTableCollection dialogueChoiceLabelTable;
        public StringTableCollection dialogueTextTable;

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
    }
}