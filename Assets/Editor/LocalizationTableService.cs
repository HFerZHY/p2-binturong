using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;


/// <summary>
/// All StringTable read/write operations for the dialogue editor.
///
/// THREE SEPARATE COLLECTIONS
///   CharacterNameTable — stores localized character display names, keyed by
///     Character.characterName (e.g. "Guard", "Merchant").
///     One entry per Character asset; the key equals the asset's characterName.
///     Managed exclusively through CharacterInspector — not editable in the
///     node inspector panel.
///
///   DialogueTextTable — stores node dialogue text, keyed by
///     DialogueNode.textKey (e.g. "guard_01_text").  One entry per node.
///
///   DialogueChoiceLabelTable — stores player choice button labels, keyed by
///     DialogueChoice.labelKey (e.g. "guard_01_choice_a").
///     One entry per choice; keys are unique across a graph.
///
/// HOW KEYS WORK (per the Unity docs)
///   - A StringTableCollection has one StringTable per locale.
///   - Each StringTable shares its key list via SharedTableData.
///   - To add/update an entry: call table.AddEntry(key, value) on each
///     per-locale StringTable, then mark both the table AND its SharedData dirty.
///   - Key names (strings) are stored in SharedTableData. The per-locale
///     StringTable maps those key IDs to localized values.
///
/// READING
///   GetCharacterNameEntry(locale, key) — read from CharacterNameTable
///   GetTextEntry(locale, key)          — read from DialogueTextTable
///   GetChoiceLabelEntry(locale, key)   — read from DialogueChoiceLabelTable
///
/// WRITING
///   SetCharacterNameEntry(locale, key, value) — write to CharacterNameTable
///   SetTextEntry(locale, key, value)          — write to DialogueTextTable
///   SetChoiceLabelEntry(locale, key, value)   — write to DialogueChoiceLabelTable
///   SaveAll() — call once after all writes to flush dirty assets
///
/// RENAMING KEYS
///   RenameCharacterNameKey(oldKey, newKey) — renames in CharacterNameTable
///   RenameTextKey(oldKey, newKey)          — renames in DialogueTextTable
///   RenameChoiceLabelKey(oldKey, newKey)   — renames in DialogueChoiceLabelTable
///   Each returns false if the old key does not exist or the new key already exists.
///
/// KEY EXISTENCE CHECKS
///   CharacterNameKeyExists(key) — checks CharacterNameTable
///   TextKeyExists(key)          — checks DialogueTextTable
///   ChoiceLabelKeyExists(key)   — checks DialogueChoiceLabelTable
/// </summary>
public static class LocalizationTableService
{
    // ── Locale enumeration ────────────────────────────────────────────────

    /// <summary>
    /// All locales registered in the localization settings.
    /// All three collections are expected to have the same set of locales.
    /// </summary>
    public static List<Locale> GetAllLocales()
        => LocalizationEditorSettings.GetLocales().ToList();

    // ── Read ──────────────────────────────────────────────────────────────

    /// <summary>Reads a localized character name for the given locale and characterName key.</summary>
    public static string GetCharacterNameEntry(Locale locale, string key)
        => ReadEntry(LocalizationManager.Instance.characterNameTable, locale, key);

    /// <summary>Reads a dialogue text entry for the given locale and key.</summary>
    public static string GetTextEntry(Locale locale, string key)
        => ReadEntry(LocalizationManager.Instance.dialogueTextTable, locale, key);

    /// <summary>Reads a choice label entry for the given locale and key.</summary>
    public static string GetChoiceLabelEntry(Locale locale, string key)
        => ReadEntry(LocalizationManager.Instance.dialogueChoiceLabelTable, locale, key);

    private static string ReadEntry(StringTableCollection collection, Locale locale, string key)
    {
        if (locale == null || string.IsNullOrEmpty(key)) return string.Empty;
        var table = GetTable(collection, locale);
        if (table == null) return string.Empty;
        var entry = table.GetEntry(key);
        return entry?.LocalizedValue ?? string.Empty;
    }

    // ── Write ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Writes a localized character name entry for the given locale and key.
    /// Creates the entry if it does not exist.
    /// </summary>
    public static void SetCharacterNameEntry(Locale locale, string key, string value)
        => WriteEntry(LocalizationManager.Instance.characterNameTable, locale, key, value);

    /// <summary>
    /// Writes a dialogue text entry for the given locale and key.
    /// Creates the entry if it does not exist.
    /// Per the Unity docs, marks both the StringTable and its SharedData dirty.
    /// </summary>
    public static void SetTextEntry(Locale locale, string key, string value)
        => WriteEntry(LocalizationManager.Instance.dialogueTextTable, locale, key, value);

    /// <summary>
    /// Writes a choice label entry for the given locale and key.
    /// Creates the entry if it does not exist.
    /// </summary>
    public static void SetChoiceLabelEntry(Locale locale, string key, string value)
        => WriteEntry(LocalizationManager.Instance.dialogueChoiceLabelTable, locale, key, value);

    private static void WriteEntry(StringTableCollection collection, Locale locale, string key, string value)
    {
        if (locale == null || string.IsNullOrEmpty(key)) return;

        var table = GetTable(collection, locale);
        if (table == null)
        {
            Debug.LogWarning(
                $"[LocalizationTableService] No StringTable for locale '{locale.LocaleName}' " +
                $"in collection '{collection.name}'.");
            return;
        }

        table.AddEntry(key, value);
        EditorUtility.SetDirty(table);
        EditorUtility.SetDirty(table.SharedData);
    }

    /// <summary>
    /// Flushes all dirty table assets to disk.
    /// Call once per save action after all WriteEntry calls complete.
    /// </summary>
    public static void SaveAll()
    {
        MarkAllDirty(LocalizationManager.Instance.characterNameTable);
        MarkAllDirty(LocalizationManager.Instance.dialogueTextTable);
        MarkAllDirty(LocalizationManager.Instance.dialogueChoiceLabelTable);
        MarkAllDirty(LocalizationManager.Instance.itemNameTable);
        MarkAllDirty(LocalizationManager.Instance.itemDescriptionTable);
        AssetDatabase.SaveAssets();
    }

    private static void MarkAllDirty(StringTableCollection collection)
    {
        if (collection == null) return;
        foreach (var table in collection.StringTables)
        {
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(table.SharedData);
        }
    }

    // ── Key existence checks ──────────────────────────────────────────────

    /// <summary>Returns true if <paramref name="key"/> exists in the character name collection.</summary>
    public static bool CharacterNameKeyExists(string key) => KeyExists(LocalizationManager.Instance.characterNameTable, key);

    /// <summary>Returns true if <paramref name="key"/> exists in the text collection.</summary>
    public static bool TextKeyExists(string key) => KeyExists(LocalizationManager.Instance.dialogueTextTable, key);

    /// <summary>Returns true if <paramref name="key"/> exists in the choice label collection.</summary>
    public static bool ChoiceLabelKeyExists(string key) => KeyExists(LocalizationManager.Instance.dialogueChoiceLabelTable, key);

    private static bool KeyExists(StringTableCollection collection, string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        return collection != null && collection.SharedData.Contains(key);
    }

    // ── Key rename ────────────────────────────────────────────────────────

    /// <summary>
    /// Renames <paramref name="oldKey"/> to <paramref name="newKey"/> in the character name collection.
    /// All per-locale values are preserved under the new key.
    /// Returns false if oldKey does not exist or newKey already exists.
    /// </summary>
    public static bool RenameCharacterNameKey(string oldKey, string newKey)
        => RenameKey(LocalizationManager.Instance.characterNameTable, oldKey, newKey);

    /// <summary>
    /// Renames <paramref name="oldKey"/> to <paramref name="newKey"/> in the text collection.
    /// All per-locale values are preserved under the new key.
    /// Returns false if oldKey does not exist or newKey already exists.
    /// </summary>
    public static bool RenameTextKey(string oldKey, string newKey)
        => RenameKey(LocalizationManager.Instance.dialogueTextTable, oldKey, newKey);

    /// <summary>
    /// Renames <paramref name="oldKey"/> to <paramref name="newKey"/> in the choice label collection.
    /// All per-locale values are preserved under the new key.
    /// Returns false if oldKey does not exist or newKey already exists.
    /// </summary>
    public static bool RenameChoiceLabelKey(string oldKey, string newKey)
        => RenameKey(LocalizationManager.Instance.dialogueChoiceLabelTable, oldKey, newKey);

    /// <summary>
    /// Renames a key inside a given collection, preserving all per-locale values.
    ///
    /// Unity's StringTableCollection does not expose a direct rename API, so the
    /// approach is:
    ///   1. Read all per-locale values for the old key.
    ///   2. Remove the old key via StringTableCollection.RemoveEntry (clears all locales).
    ///   3. AddEntry with the new key and restored values to each per-locale table.
    /// </summary>
    private static bool RenameKey(StringTableCollection collection, string oldKey, string newKey)
    {
        if (string.IsNullOrEmpty(oldKey) || string.IsNullOrEmpty(newKey)) return false;
        if (oldKey == newKey) return true;

        // var collection = GetCollection(collectionName);
        if (collection == null) return false;

        var sharedData = collection.SharedData;

        if (!sharedData.Contains(oldKey))
        {
            Debug.LogWarning(
                $"[LocalizationTableService] RenameKey: old key '{oldKey}' not found in '{collection.name}'.");
            return false;
        }

        if (sharedData.Contains(newKey))
        {
            Debug.LogWarning(
                $"[LocalizationTableService] RenameKey: new key '{newKey}' already exists in '{collection.name}'.");
            return false;
        }

        // Step 1 — snapshot per-locale values
        var values = new Dictionary<StringTable, string>();
        foreach (var table in collection.StringTables)
        {
            var entry = table.GetEntry(oldKey);
            values[table] = entry?.LocalizedValue ?? string.Empty;
        }

        // Step 2 — remove the old key from all tables and SharedTableData
        collection.RemoveEntry(oldKey);
        EditorUtility.SetDirty(sharedData);

        // Step 3 — add the new key with the preserved values
        foreach (var (table, value) in values)
        {
            table.AddEntry(newKey, value);
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(table.SharedData);
        }

        return true;
    }

    // ── Character name key management ─────────────────────────────────────

    /// <summary>
    /// Adds a new key to the character name collection for every locale with an
    /// empty initial value.  Returns false if the key already exists.
    /// Called by CharacterInspector when a new Character asset is first inspected
    /// or when a rename is committed.
    /// </summary>
    public static bool AddCharacterNameKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;

        var collection = LocalizationManager.Instance.characterNameTable;
        if (collection == null) return false;

        if (collection.SharedData.Contains(key))
        {
            Debug.LogWarning($"[LocalizationTableService] Character name key '{key}' already exists.");
            return false;
        }

        foreach (var table in collection.StringTables)
        {
            table.AddEntry(key, string.Empty);
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(table.SharedData);
        }

        return true;
    }

    /// <summary>
    /// Adds a new key to the choice label collection for every locale with
    /// an empty initial value.  Returns false if the key already exists.
    /// </summary>
    public static bool AddChoiceLabelKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;

        var collection = LocalizationManager.Instance.dialogueChoiceLabelTable;
        if (collection == null) return false;

        if (collection.SharedData.Contains(key))
        {
            Debug.LogWarning($"[LocalizationTableService] Choice label key '{key}' already exists.");
            return false;
        }

        foreach (var table in collection.StringTables)
        {
            table.AddEntry(key, string.Empty);
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(table.SharedData);
        }

        return true;
    }

    // ── Private helpers ───────────────────────────────────────────────────
    
    private static StringTable GetTable(StringTableCollection collection, Locale locale)
    {
        return collection?.StringTables
            .FirstOrDefault(t => t.LocaleIdentifier == locale.Identifier);
    }
    
    // ────────────────────────────────────────────────────────────────────────────
    // Append these members to your existing LocalizationTableService class.
    //
    // They follow the identical pattern already used for characterNameTable /
    // dialogueTextTable / dialogueChoiceLabelTable, but target:
    //   LocalizationManager.Instance.itemNameTable
    //   LocalizationManager.Instance.itemDescriptionTable
    // ────────────────────────────────────────────────────────────────────────────

    // ── Read ──────────────────────────────────────────────────────────────────────

    /// <summary>Reads a localized item name for the given locale and nameKey.</summary>
    public static string GetItemNameEntry(Locale locale, string key)
        => ReadEntry(LocalizationManager.Instance.itemNameTable, locale, key);

    /// <summary>Reads a localized item description for the given locale and descriptionKey.</summary>
    public static string GetItemDescriptionEntry(Locale locale, string key)
        => ReadEntry(LocalizationManager.Instance.itemDescriptionTable, locale, key);

    // ── Write ─────────────────────────────────────────────────────────────────────

    /// <summary>Writes a localized item name entry. Creates the entry if absent.</summary>
    public static void SetItemNameEntry(Locale locale, string key, string value)
        => WriteEntry(LocalizationManager.Instance.itemNameTable, locale, key, value);

    /// <summary>Writes a localized item description entry. Creates the entry if absent.</summary>
    public static void SetItemDescriptionEntry(Locale locale, string key, string value)
        => WriteEntry(LocalizationManager.Instance.itemDescriptionTable, locale, key, value);

    // ── Key existence checks ──────────────────────────────────────────────────────

    /// <summary>Returns true if <paramref name="key"/> exists in the item name collection.</summary>
    public static bool ItemNameKeyExists(string key)
        => KeyExists(LocalizationManager.Instance.itemNameTable, key);

    /// <summary>Returns true if <paramref name="key"/> exists in the item description collection.</summary>
    public static bool ItemDescriptionKeyExists(string key)
        => KeyExists(LocalizationManager.Instance.itemDescriptionTable, key);

    // ── Key rename ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Renames <paramref name="oldKey"/> to <paramref name="newKey"/> in the item name collection.
    /// Preserves all per-locale values. Returns false if oldKey does not exist or newKey already exists.
    /// </summary>
    public static bool RenameItemNameKey(string oldKey, string newKey)
        => RenameKey(LocalizationManager.Instance.itemNameTable, oldKey, newKey);

    /// <summary>
    /// Renames <paramref name="oldKey"/> to <paramref name="newKey"/> in the item description collection.
    /// Preserves all per-locale values. Returns false if oldKey does not exist or newKey already exists.
    /// </summary>
    public static bool RenameItemDescriptionKey(string oldKey, string newKey)
        => RenameKey(LocalizationManager.Instance.itemDescriptionTable, oldKey, newKey);

    // ── Key creation ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds a new key to the item name collection for every locale with an empty initial value.
    /// Returns false if the key already exists.
    /// </summary>
    public static bool AddItemNameKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;

        var collection = LocalizationManager.Instance.itemNameTable;
        if (collection == null) return false;

        if (collection.SharedData.Contains(key))
        {
            Debug.LogWarning($"[LocalizationTableService] Item name key '{key}' already exists.");
            return false;
        }

        foreach (var table in collection.StringTables)
        {
            table.AddEntry(key, string.Empty);
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(table.SharedData);
        }

        return true;
    }

    /// <summary>
    /// Adds a new key to the item description collection for every locale with an empty initial value.
    /// Returns false if the key already exists.
    /// </summary>
    public static bool AddItemDescriptionKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;

        var collection = LocalizationManager.Instance.itemDescriptionTable;
        if (collection == null) return false;

        if (collection.SharedData.Contains(key))
        {
            Debug.LogWarning($"[LocalizationTableService] Item description key '{key}' already exists.");
            return false;
        }

        foreach (var table in collection.StringTables)
        {
            table.AddEntry(key, string.Empty);
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(table.SharedData);
        }

        return true;
    }
}
