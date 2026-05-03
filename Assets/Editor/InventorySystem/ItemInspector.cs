using System.Linq;
using InventorySystem.Data;
using UnityEditor;
using UnityEngine;

namespace InventorySystem.Editor
{
    /// <summary>
    /// Custom Inspector for the ItemData ScriptableObject.
    ///
    /// RESPONSIBILITIES — mirrors CharacterInspector exactly:
    ///
    ///   1. nameKey management
    ///      nameKey is both the human-readable identifier and the lookup key in
    ///      itemNameTable. It must be unique across all ItemData assets.
    ///
    ///        • On first inspect (key not yet in the table): a "Register" button
    ///          calls LocalizationTableService.AddItemNameKey (and AddItemDescriptionKey)
    ///          to create empty per-locale rows.
    ///
    ///        • Rename workflow: a "Rename key" field + Apply button checks
    ///          uniqueness, then calls LocalizationTableService.RenameItemNameKey and
    ///          RenameItemDescriptionKey to preserve translations, and writes the
    ///          new value back to item.nameKey / item.descriptionKey.
    ///
    ///   2. Per-locale name and description editing
    ///      One text field per locale per table, writing immediately on change.
    ///      SaveAll() is deferred to the "Save to disk" button.
    ///
    ///   3. Remaining fields (icon, etc.)
    ///      Drawn using serializedObject property fields, skipping the managed keys.
    /// </summary>
    [CustomEditor(typeof(ItemData))]
    public class ItemInspector : UnityEditor.Editor
    {
        private ItemData _item;

        private bool   _keysRegistered;
        private string _pendingRenameValue;

        private void OnEnable()
        {
            _item               = (ItemData)target;
            _pendingRenameValue = _item.nameKey;
            _keysRegistered     = LocalizationTableService.ItemNameKeyExists(_item.nameKey);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // ── Key / identity section ────────────────────────────────────────
            EditorGUILayout.LabelField("Item Identity", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("Name key",        _item.nameKey        ?? "(not set)");
            EditorGUILayout.LabelField("Description key", _item.descriptionKey ?? "(not set)");

            if (!_keysRegistered)
            {
                EditorGUILayout.HelpBox(
                    $"Key '{_item.nameKey}' is not yet registered in ItemNameTable / ItemDescriptionTable. " +
                    "Click Register to create the table entries.",
                    MessageType.Warning);

                if (GUILayout.Button("Register in Localization Tables"))
                    RegisterKeys();
            }
            else
            {
                // ── Rename workflow ───────────────────────────────────────────
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Rename key (both tables)", EditorStyles.miniBoldLabel);

                EditorGUILayout.BeginHorizontal();
                _pendingRenameValue = EditorGUILayout.TextField(_pendingRenameValue);

                EditorGUI.BeginDisabledGroup(
                    string.IsNullOrWhiteSpace(_pendingRenameValue) ||
                    _pendingRenameValue == _item.nameKey);

                if (GUILayout.Button("Apply", GUILayout.Width(60)))
                    ApplyRename();

                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                if (!string.IsNullOrWhiteSpace(_pendingRenameValue) &&
                    _pendingRenameValue != _item.nameKey &&
                    IsNameTakenByAnotherItem(_pendingRenameValue))
                {
                    EditorGUILayout.HelpBox(
                        $"'{_pendingRenameValue}' is already used by another ItemData asset.",
                        MessageType.Error);
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(8);

            // ── Per-locale name fields ────────────────────────────────────────
            if (_keysRegistered)
            {
                DrawLocaleFields(
                    label:      "Localized Names",
                    getEntry:   (locale) => LocalizationTableService.GetItemNameEntry(locale, _item.nameKey),
                    setEntry:   (locale, val) => LocalizationTableService.SetItemNameEntry(locale, _item.nameKey, val),
                    undoLabel:  "Edit Item Name Translation");

                EditorGUILayout.Space(4);

                DrawLocaleFields(
                    label:      "Localized Descriptions",
                    getEntry:   (locale) => LocalizationTableService.GetItemDescriptionEntry(locale, _item.descriptionKey),
                    setEntry:   (locale, val) => LocalizationTableService.SetItemDescriptionEntry(locale, _item.descriptionKey, val),
                    undoLabel:  "Edit Item Description Translation");

                EditorGUILayout.Space(4);

                if (GUILayout.Button("Save localization to disk"))
                {
                    LocalizationTableService.SaveAll();
                    Debug.Log($"[ItemInspector] Saved localization tables for '{_item.nameKey}'.");
                }

                EditorGUILayout.Space(8);
            }

            // ── Remaining serialized fields (icon, etc.) ──────────────────────
            EditorGUILayout.LabelField("Item Properties", EditorStyles.boldLabel);

            var iconProp = serializedObject.FindProperty("icon");
            if (iconProp != null)
                EditorGUILayout.PropertyField(iconProp);

            // Draw any future fields that are not the managed string keys.
            DrawPropertiesExcluding(serializedObject, "m_Script", "nameKey", "descriptionKey", "icon");

            serializedObject.ApplyModifiedProperties();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>Draws one text field per locale for a given table entry.</summary>
        private void DrawLocaleFields(
            string label,
            System.Func<UnityEngine.Localization.Locale, string>         getEntry,
            System.Action<UnityEngine.Localization.Locale, string>        setEntry,
            string undoLabel)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            var locales = LocalizationTableService.GetAllLocales();
            if (locales.Count == 0)
            {
                EditorGUILayout.HelpBox("No locales found in Localization Settings.", MessageType.Warning);
                EditorGUI.indentLevel--;
                return;
            }

            foreach (var locale in locales)
            {
                string current = getEntry(locale);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    locale.Identifier.Code.ToUpper(), GUILayout.Width(36));

                EditorGUI.BeginChangeCheck();
                string newVal = EditorGUILayout.TextField(current);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_item, undoLabel);
                    setEntry(locale, newVal);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }

        private void RegisterKeys()
        {
            string key = _item.nameKey;

            if (string.IsNullOrWhiteSpace(key))
            {
                EditorUtility.DisplayDialog("Invalid Key",
                    "nameKey must not be empty before registering.", "OK");
                return;
            }

            if (IsNameTakenByAnotherItem(key))
            {
                EditorUtility.DisplayDialog("Duplicate Key",
                    $"Another ItemData asset already uses the name key '{key}'.", "OK");
                return;
            }

            // Register in both tables; generate a description key if not set.
            if (string.IsNullOrWhiteSpace(_item.descriptionKey))
            {
                Undo.RecordObject(_item, "Set Item Description Key");
                _item.descriptionKey = key + "_desc";
                EditorUtility.SetDirty(_item);
            }

            bool nameAdded = LocalizationTableService.AddItemNameKey(key);
            bool descAdded = LocalizationTableService.AddItemDescriptionKey(_item.descriptionKey);

            if (nameAdded || descAdded)
            {
                _keysRegistered = true;
                LocalizationTableService.SaveAll();
                Debug.Log($"[ItemInspector] Registered '{key}' in ItemNameTable and ItemDescriptionTable.");
            }
            else
            {
                // Keys existed already (external modification); treat as registered.
                _keysRegistered = true;
            }
        }

        private void ApplyRename()
        {
            string oldKey = _item.nameKey;
            string newKey = _pendingRenameValue.Trim();

            if (string.IsNullOrEmpty(newKey) || newKey == oldKey) return;

            if (IsNameTakenByAnotherItem(newKey))
            {
                EditorUtility.DisplayDialog("Duplicate Key",
                    $"Another ItemData asset already uses the name key '{newKey}'.", "OK");
                return;
            }

            // Derive the new description key by replacing the old name prefix.
            string oldDescKey = _item.descriptionKey;
            string newDescKey = string.IsNullOrEmpty(oldDescKey)
                ? newKey + "_desc"
                : oldDescKey.Replace(oldKey, newKey);

            bool nameRenamed = LocalizationTableService.RenameItemNameKey(oldKey, newKey);
            bool descRenamed = LocalizationTableService.RenameItemDescriptionKey(oldDescKey, newDescKey);

            if (!nameRenamed || !descRenamed)
            {
                EditorUtility.DisplayDialog("Rename Failed",
                    $"Could not rename '{oldKey}' → '{newKey}' in one or both item tables. " +
                    "The new key may already exist.", "OK");
                return;
            }

            Undo.RecordObject(_item, "Rename Item");
            _item.nameKey        = newKey;
            _item.descriptionKey = newDescKey;
            EditorUtility.SetDirty(_item);

            LocalizationTableService.SaveAll();
            AssetDatabase.SaveAssets();

            _pendingRenameValue = newKey;
            Debug.Log($"[ItemInspector] Renamed '{oldKey}' → '{newKey}' in item localization tables.");
        }

        /// <summary>
        /// Returns true if any other ItemData asset in the project already uses
        /// <paramref name="nameKey"/> as its nameKey.
        /// Excludes the currently inspected asset.
        /// </summary>
        private bool IsNameTakenByAnotherItem(string nameKey)
        {
            return AssetDatabase.FindAssets("t:ItemData")
                .Select(guid => AssetDatabase.LoadAssetAtPath<ItemData>(
                    AssetDatabase.GUIDToAssetPath(guid)))
                .Where(i => i != null && i != _item)
                .Any(i => i.nameKey == nameKey);
        }
    }
}
