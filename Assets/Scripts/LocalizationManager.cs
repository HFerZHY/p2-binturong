using System;
using Base;
using DialogueSystem.Core;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Localization;
#endif
using UnityEngine;
using UnityEngine.Localization.Settings;

[ExecuteAlways]
public class LocalizationManager : MonoSingleton<LocalizationManager>
{
#if UNITY_EDITOR
    public StringTableCollection characterNameTable => FindStringTableCollection(GetCharacterNameTableName());
    public StringTableCollection dialogueChoiceLabelTable => FindStringTableCollection(GetDialogueChoiceLabelTableName());
    public StringTableCollection dialogueTextTable => FindStringTableCollection(GetDialogueTextTableName());
    public StringTableCollection itemNameTable => FindStringTableCollection(GetItemNameTableName());
    public StringTableCollection itemDescriptionTable => FindStringTableCollection(GetItemDescriptionTableName());
#endif

    [SerializeField] private string _characterNameTableName = "CharacterNameTable";
    [SerializeField] private string _dialogueChoiceLabelTableName = "DialogueChoiceLabelTable";
    [SerializeField] private string _dialogueTextTableName = "DialogueTextTable";
    [SerializeField] private string _itemNameTableName = "ItemNameTable";
    [SerializeField] private string _itemDescriptionTableName = "ItemDescriptionTable";

    public bool HasCharacterNameTable => HasTable(GetCharacterNameTableName());
    public bool HasDialogueChoiceLabelTable => HasTable(GetDialogueChoiceLabelTableName());
    public bool HasDialogueTextTable => HasTable(GetDialogueTextTableName());
    public bool HasItemNameTable => HasTable(GetItemNameTableName());
    public bool HasItemDescriptionTable => HasTable(GetItemDescriptionTableName());

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
        return GetLocalizedValue(GetCharacterNameTableName(), characterKey);
    }

    public string GetDialogueText(string dialogueTextKey)
    {
        return GetLocalizedValue(GetDialogueTextTableName(), dialogueTextKey);
    }

    public string GetDialogueChoice(string dialogueChoiceKey)
    {
        return GetLocalizedValue(GetDialogueChoiceLabelTableName(), dialogueChoiceKey);
    }

    public string GetItemName(string itemName)
    {
        return GetLocalizedValue(GetItemNameTableName(), itemName);
    }

    public string GetItemDescription(string itemDescriptionKey)
    {
        return GetLocalizedValue(GetItemDescriptionTableName(), itemDescriptionKey);
    }

    private string GetCharacterNameTableName()
    {
        return _characterNameTableName;
    }

    private string GetDialogueChoiceLabelTableName()
    {
        return _dialogueChoiceLabelTableName;
    }

    private string GetDialogueTextTableName()
    {
        return _dialogueTextTableName;
    }

    private string GetItemNameTableName()
    {
        return _itemNameTableName;
    }

    private string GetItemDescriptionTableName()
    {
        return _itemDescriptionTableName;
    }

#if UNITY_EDITOR
    private static StringTableCollection FindStringTableCollection(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return null;

        var guids = AssetDatabase.FindAssets($"{tableName} t:StringTableCollection");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var collection = AssetDatabase.LoadAssetAtPath<StringTableCollection>(path);
            if (collection != null && collection.name == tableName)
                return collection;
        }

        return null;
    }
#endif

    private static bool HasTable(string tableName)
    {
        return !string.IsNullOrWhiteSpace(tableName);
    }

    private static string GetLocalizedValue(string tableName, string key)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        if (string.IsNullOrWhiteSpace(tableName))
            return key;

        try
        {
            var value = LocalizationSettings.StringDatabase.GetLocalizedString(tableName, key);
            return string.IsNullOrEmpty(value) ? key : value;
        }
        catch (Exception)
        {
            return key;
        }
    }
}
