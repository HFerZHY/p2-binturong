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
    /// TWO SEPARATE COLLECTIONS
    ///   SpeakerTableCollection — stores speaker name strings, keyed by a
    ///     designer-chosen speaker identifier (e.g. "Guard", "Merchant").
    ///     Keys are shared across nodes; one speaker entry is reused by many nodes.
    ///
    ///   DialogueTextTableCollection — stores node dialogue text, keyed by
    ///     the node's textKey (e.g. "guard_01_text").  One entry per node.
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
    ///   GetTextEntry(locale, key)    — read from DialogueTextTableCollection
    ///   GetSpeakerEntry(locale, key) — read from SpeakerTableCollection
    ///
    /// WRITING
    ///   SetTextEntry(locale, key, value)    — write to DialogueTextTableCollection
    ///   SetSpeakerEntry(locale, key, value) — write to SpeakerTableCollection
    ///   SaveAll() — call once after all writes to flush dirty assets
    ///
    /// SPEAKER ENUMERATION
    ///   GetAllSpeakerKeys() — returns every key currently in the speaker
    ///   collection (from SharedTableData), used to populate the speaker combobox.
    /// </summary>
    public static class LocalizationTableService
    {
        // ── Collection names — change these to match your project ─────────────

        public const string SpeakerCollectionName  = "DialogueSpeakerTable";
        public const string DialogueCollectionName = "DialogueTextTable";

        // ── Locale enumeration ────────────────────────────────────────────────

        /// <summary>
        /// All locales registered in the localization settings.
        /// Both collections are expected to have the same set of locales.
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
