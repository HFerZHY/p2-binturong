using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace DialogueSystem.Editor
{
    /// <summary>
    /// All StringTable read/write operations for the dialogue editor.
    ///
    /// THREE SEPARATE COLLECTIONS
    ///   SpeakerTableCollection — stores speaker name strings, keyed by a
    ///     designer-chosen speaker identifier (e.g. "Guard", "Merchant").
    ///     Keys are shared across nodes; one speaker entry is reused by many nodes.
    ///
    ///   DialogueTextTableCollection — stores node dialogue text, keyed by
    ///     the node's textKey (e.g. "guard_01_text").  One entry per node.
    ///
    ///   DialogueChoiceLabelTableCollection — stores player choice button labels,
    ///     keyed by the choice's labelKey (e.g. "guard_01_choice_a").
    ///     One entry per choice; keys are unique across the graph.
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
    ///   GetTextEntry(locale, key)        — read from DialogueTextTableCollection
    ///   GetSpeakerEntry(locale, key)     — read from SpeakerTableCollection
    ///   GetChoiceLabelEntry(locale, key) — read from DialogueChoiceLabelTableCollection
    ///
    /// WRITING
    ///   SetTextEntry(locale, key, value)        — write to DialogueTextTableCollection
    ///   SetSpeakerEntry(locale, key, value)     — write to SpeakerTableCollection
    ///   SetChoiceLabelEntry(locale, key, value) — write to DialogueChoiceLabelTableCollection
    ///   SaveAll() — call once after all writes to flush dirty assets
    ///
    /// RENAMING KEYS
    ///   RenameTextKey(oldKey, newKey)        — renames in DialogueTextTableCollection
    ///   RenameSpeakerKey(oldKey, newKey)     — renames in SpeakerTableCollection
    ///   RenameChoiceLabelKey(oldKey, newKey) — renames in DialogueChoiceLabelTableCollection
    ///   Each returns false if the old key does not exist or the new key already exists.
    ///
    /// KEY EXISTENCE CHECKS
    ///   TextKeyExists(key)        — checks DialogueTextTableCollection
    ///   SpeakerKeyExists(key)     — checks SpeakerTableCollection
    ///   ChoiceLabelKeyExists(key) — checks DialogueChoiceLabelTableCollection
    ///
    /// SPEAKER ENUMERATION
    ///   GetAllSpeakerKeys() — returns every key currently in the speaker
    ///   collection (from SharedTableData), used to populate the speaker combobox.
    /// </summary>
    public static class LocalizationTableService
    {
        // ── Collection names — change these to match your project ─────────────

        public const string SpeakerCollectionName     = "CharacterNameTable";
        public const string DialogueCollectionName    = "DialogueTextTable";
        public const string ChoiceLabelCollectionName = "DialogueChoiceLabelTable";

        // ── Locale enumeration ────────────────────────────────────────────────

        /// <summary>
        /// All locales registered in the localization settings.
        /// All three collections are expected to have the same set of locales.
        /// </summary>
        public static List<Locale> GetAllLocales()
            => LocalizationEditorSettings.GetLocales().ToList();

        // ── Read ──────────────────────────────────────────────────────────────

        /// <summary>Reads a dialogue text entry for the given locale and key.</summary>
        public static string GetTextEntry(Locale locale, string key)
            => ReadEntry(DialogueCollectionName, locale, key);

        /// <summary>Reads a speaker name entry for the given locale and key.</summary>
        public static string GetSpeakerEntry(Locale locale, string key)
            => ReadEntry(SpeakerCollectionName, locale, key);

        /// <summary>Reads a choice label entry for the given locale and key.</summary>
        public static string GetChoiceLabelEntry(Locale locale, string key)
            => ReadEntry(ChoiceLabelCollectionName, locale, key);

        private static string ReadEntry(string collectionName, Locale locale, string key)
        {
            if (locale == null || string.IsNullOrEmpty(key)) return string.Empty;
            var table = GetTable(collectionName, locale);
            if (table == null) return string.Empty;
            var entry = table.GetEntry(key);
            return entry?.LocalizedValue ?? string.Empty;
        }

        // ── Write ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Writes a dialogue text entry for the given locale and key.
        /// Creates the entry if it does not exist.
        /// Per the Unity docs, marks both the StringTable and its SharedData dirty.
        /// </summary>
        public static void SetTextEntry(Locale locale, string key, string value)
            => WriteEntry(DialogueCollectionName, locale, key, value);

        /// <summary>
        /// Writes a speaker name entry for the given locale and key.
        /// Creates the entry if it does not exist.
        /// </summary>
        public static void SetSpeakerEntry(Locale locale, string key, string value)
            => WriteEntry(SpeakerCollectionName, locale, key, value);

        /// <summary>
        /// Writes a choice label entry for the given locale and key.
        /// Creates the entry if it does not exist.
        /// </summary>
        public static void SetChoiceLabelEntry(Locale locale, string key, string value)
            => WriteEntry(ChoiceLabelCollectionName, locale, key, value);

        private static void WriteEntry(string collectionName, Locale locale, string key, string value)
        {
            if (locale == null || string.IsNullOrEmpty(key)) return;

            var table = GetTable(collectionName, locale);
            if (table == null)
            {
                Debug.LogWarning(
                    $"[LocalizationTableService] No StringTable for locale '{locale.LocaleName}' " +
                    $"in collection '{collectionName}'.");
                return;
            }

            // AddEntry creates the entry if missing, or updates its value if it exists.
            // Per the Unity Localization 1.5 docs, we must mark both the table
            // AND its SharedData dirty after modifications.
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
            MarkAllDirty(SpeakerCollectionName);
            MarkAllDirty(DialogueCollectionName);
            MarkAllDirty(ChoiceLabelCollectionName);
            AssetDatabase.SaveAssets();
        }

        private static void MarkAllDirty(string collectionName)
        {
            var collection = GetCollection(collectionName);
            if (collection == null) return;
            foreach (var table in collection.StringTables)
            {
                EditorUtility.SetDirty(table);
                EditorUtility.SetDirty(table.SharedData);
            }
        }

        // ── Key existence checks ──────────────────────────────────────────────

        /// <summary>Returns true if <paramref name="key"/> exists in the text collection's SharedTableData.</summary>
        public static bool TextKeyExists(string key) => KeyExists(DialogueCollectionName, key);

        /// <summary>Returns true if <paramref name="key"/> exists in the speaker collection's SharedTableData.</summary>
        public static bool SpeakerKeyExists(string key) => KeyExists(SpeakerCollectionName, key);

        /// <summary>Returns true if <paramref name="key"/> exists in the choice label collection's SharedTableData.</summary>
        public static bool ChoiceLabelKeyExists(string key) => KeyExists(ChoiceLabelCollectionName, key);

        private static bool KeyExists(string collectionName, string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            var collection = GetCollection(collectionName);
            return collection != null && collection.SharedData.Contains(key);
        }

        // ── Key rename ────────────────────────────────────────────────────────

        /// <summary>
        /// Renames <paramref name="oldKey"/> to <paramref name="newKey"/> in the text collection.
        /// All per-locale values are preserved under the new key.
        /// Returns false if oldKey does not exist or newKey already exists.
        /// </summary>
        public static bool RenameTextKey(string oldKey, string newKey)
            => RenameKey(DialogueCollectionName, oldKey, newKey);

        /// <summary>
        /// Renames <paramref name="oldKey"/> to <paramref name="newKey"/> in the speaker collection.
        /// All per-locale values are preserved under the new key.
        /// Returns false if oldKey does not exist or newKey already exists.
        /// </summary>
        public static bool RenameSpeakerKey(string oldKey, string newKey)
            => RenameKey(SpeakerCollectionName, oldKey, newKey);

        /// <summary>
        /// Renames <paramref name="oldKey"/> to <paramref name="newKey"/> in the choice label collection.
        /// All per-locale values are preserved under the new key.
        /// Returns false if oldKey does not exist or newKey already exists.
        /// </summary>
        public static bool RenameChoiceLabelKey(string oldKey, string newKey)
            => RenameKey(ChoiceLabelCollectionName, oldKey, newKey);

        /// <summary>
        /// Renames a key inside a given collection, preserving all per-locale values.
        ///
        /// Unity's StringTableCollection does not expose a direct rename API, so the
        /// approach is:
        ///   1. Read all per-locale values for the old key.
        ///   2. Remove the old key from SharedTableData (removes it from all tables).
        ///   3. AddEntry with the new key and restored values to each per-locale table.
        /// </summary>
        private static bool RenameKey(string collectionName, string oldKey, string newKey)
        {
            if (string.IsNullOrEmpty(oldKey) || string.IsNullOrEmpty(newKey)) return false;
            if (oldKey == newKey) return true; // no-op

            var collection = GetCollection(collectionName);
            if (collection == null) return false;

            var sharedData = collection.SharedData;

            if (!sharedData.Contains(oldKey))
            {
                Debug.LogWarning(
                    $"[LocalizationTableService] RenameKey: old key '{oldKey}' not found in '{collectionName}'.");
                return false;
            }

            if (sharedData.Contains(newKey))
            {
                Debug.LogWarning(
                    $"[LocalizationTableService] RenameKey: new key '{newKey}' already exists in '{collectionName}'.");
                return false;
            }

            // Step 1 — Snapshot per-locale values before we remove the old key
            var values = new Dictionary<StringTable, string>();
            foreach (var table in collection.StringTables)
            {
                var entry = table.GetEntry(oldKey);
                values[table] = entry?.LocalizedValue ?? string.Empty;
            }

            // Step 2 — Remove the old key from every locale table and SharedTableData.
            // StringTableCollection.RemoveEntry removes the shared key + all per-locale rows.
            collection.RemoveEntry(oldKey);
            EditorUtility.SetDirty(sharedData);

            // Step 3 — Add the new key with the preserved values
            foreach (var (table, value) in values)
            {
                table.AddEntry(newKey, value);
                EditorUtility.SetDirty(table);
                EditorUtility.SetDirty(table.SharedData);
            }

            return true;
        }

        // ── Speaker enumeration ───────────────────────────────────────────────

        /// <summary>
        /// Returns all key names currently registered in the speaker collection's
        /// SharedTableData. These are the stable key strings (e.g. "Guard",
        /// "Merchant") that designers assign to nodes.
        /// Order matches insertion order in the SharedTableData.
        /// </summary>
        public static List<string> GetAllSpeakerKeys()
        {
            var collection = GetCollection(SpeakerCollectionName);
            if (collection == null) return new List<string>();

            // SharedTableData.Entries is the canonical key list — same across all
            // per-locale StringTables in the collection.
            return collection.SharedData.Entries
                .Select(e => e.Key)
                .OrderBy(k => k)
                .ToList();
        }

        /// <summary>
        /// Adds a new key to the speaker collection for every locale, with an
        /// empty string as the initial value. The caller should immediately call
        /// SetSpeakerEntry per locale to populate the values.
        /// Returns false if the key already exists.
        /// </summary>
        public static bool AddSpeakerKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;

            var collection = GetCollection(SpeakerCollectionName);
            if (collection == null) return false;

            // Check for duplicates via SharedTableData
            if (collection.SharedData.Contains(key))
            {
                Debug.LogWarning($"[LocalizationTableService] Speaker key '{key}' already exists.");
                return false;
            }

            // AddEntry on each per-locale table creates the shared key once and
            // stores a locale-specific empty string value.
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

            var collection = GetCollection(ChoiceLabelCollectionName);
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

        private static StringTableCollection GetCollection(string name)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(name);
            if (collection == null)
                Debug.LogWarning(
                    $"[LocalizationTableService] StringTableCollection '{name}' not found.");
            return collection;
        }

        private static StringTable GetTable(string collectionName, Locale locale)
        {
            var collection = GetCollection(collectionName);
            return collection?.StringTables
                .FirstOrDefault(t => t.LocaleIdentifier == locale.Identifier);
        }
    }
}
