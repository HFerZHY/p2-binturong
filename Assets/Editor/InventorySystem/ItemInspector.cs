using System.Linq;
using DialogueSystem.Editor;
using InventorySystem.Data;
using UnityEditor;
using UnityEngine;

namespace InventorySystem.Editor
{
    /// <summary>
    /// Custom Inspector for the ItemData ScriptableObject.
    ///
    /// RESPONSIBILITIES
    ///   1. nameKey / descriptionKey management
    ///      Before registration: both keys are freely editable text fields so the
    ///      designer can give them their initial values.  A "Register" button then
    ///      calls LocalizationTableService.AddItemNameKey / AddItemDescriptionKey
    ///      to create empty per-locale rows.
    ///
    ///      After registration: the keys become read-only labels.  A "Rename key"
    ///      field + Apply button checks uniqueness across all ItemData assets, then
    ///      calls LocalizationTableService.RenameItemNameKey /
    ///      RenameItemDescriptionKey to preserve all existing translations, and
    ///      finally writes the new values back to the asset.
    ///
    ///   2. Per-locale name and description editing
    ///      One text field per locale per table, writing immediately on change.
    ///      SaveAll() is deferred to the "Save to disk" button.
    ///
    ///   3. Remaining fields (icon, etc.)
    ///      Drawn via serializedObject property fields, excluding the managed keys.
    /// </summary>
    [CustomEditor(typeof(ItemData))]
    public class ItemInspector : UnityEditor.Editor
    {
        private ItemData _item;

        private bool   _keysRegistered;
        private string _pendingRenameValue;

        // Editable staging values used only before registration.
        private string _draftNameKey;
        private string _draftDescriptionKey;

        private void OnEnable()
        {
            _item               = (ItemData)target;
            _pendingRenameValue = _item.nameKey;
            _keysRegistered     = LocalizationTableService.ItemNameKeyExists(_item.nameKey);

            // Pre-fill drafts from the asset so existing (but unregistered) values
            // are not lost when the inspector first opens.
            _draftNameKey        = _item.nameKey        ?? string.Empty;
            _draftDescriptionKey = _item.descriptionKey ?? string.Empty;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Item Identity", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            if (!_keysRegistered)
            {
                // ── Keys not yet registered: allow free editing ───────────────
                EditorGUI.BeginChangeCheck();
                _draftNameKey        = EditorGUILayout.TextField("Name Key",        _draftNameKey);
                _draftDescriptionKey = EditorGUILayout.TextField("Description Key", _draftDescriptionKey);
                if (EditorGUI.EndChangeCheck())
                {
                    // Write drafts back to the asset immediately so the values
                    // survive domain reloads and inspector focus changes.
                    Undo.RecordObject(_item, "Edit Item Keys");
                    _item.nameKey        = _draftNameKey;
                    _item.descriptionKey = _draftDescriptionKey;
                    EditorUtility.SetDirty(_item);

                    _pendingRenameValue = _draftNameKey;
                }

                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "Set the keys above, then click Register to create the localization table entries.",
                    MessageType.Info);

                EditorGUI.BeginDisabledGroup(
                    string.IsNullOrWhiteSpace(_draftNameKey) ||
                    string.IsNullOrWhiteSpace(_draftDescriptionKey));

                if (GUILayout.Button("Register in Localization Tables"))
                    RegisterKeys();

                EditorGUI.EndDisabledGroup();
            }
            else
            {
                // ── Keys registered: show as read-only labels ─────────────────
                EditorGUILayout.LabelField("Name Key",        _item.nameKey);
                EditorGUILayout.LabelField("Description Key", _item.descriptionKey);

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

            // ── Per-locale fields (only after registration) ───────────────────
            if (_keysRegistered)
            {
                DrawLocaleFields(
                    label:     "Localized Names",
                    getEntry:  locale => LocalizationTableService.GetItemNameEntry(locale, _item.nameKey),
                    setEntry:  (locale, val) => LocalizationTableService.SetItemNameEntry(locale, _item.nameKey, val),
                    undoLabel: "Edit Item Name Translation");

                EditorGUILayout.Space(4);

                DrawLocaleFields(
                    label:     "Localized Descriptions",
                    getEntry:  locale => LocalizationTableService.GetItemDescriptionEntry(locale, _item.descriptionKey),
                    setEntry:  (locale, val) => LocalizationTableService.SetItemDescriptionEntry(locale, _item.descriptionKey, val),
                    undoLabel: "Edit Item Description Translation");

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

            DrawPropertiesExcluding(serializedObject, "m_Script", "nameKey", "descriptionKey", "icon");

            serializedObject.ApplyModifiedProperties();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void DrawLocaleFields(
            string label,
            System.Func<UnityEngine.Localization.Locale, string>   getEntry,
            System.Action<UnityEngine.Localization.Locale, string>  setEntry,
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
                EditorGUILayout.LabelField(locale.Identifier.Code.ToUpper(), GUILayout.Width(36));

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
            string nameKey = _item.nameKey;
            string descKey = _item.descriptionKey;

            if (IsNameTakenByAnotherItem(nameKey))
            {
                EditorUtility.DisplayDialog("Duplicate Key",
                    $"Another ItemData asset already uses the name key '{nameKey}'.", "OK");
                return;
            }

            bool nameAdded = LocalizationTableService.AddItemNameKey(nameKey);
            bool descAdded = LocalizationTableService.AddItemDescriptionKey(descKey);

            if (nameAdded || descAdded)
                LocalizationTableService.SaveAll();

            // Even if the keys already existed externally, treat as registered.
            _keysRegistered     = true;
            _pendingRenameValue = nameKey;

            Debug.Log($"[ItemInspector] Registered '{nameKey}' / '{descKey}' in localization tables.");
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

            _pendingRenameValue  = newKey;
            _draftNameKey        = newKey;
            _draftDescriptionKey = newDescKey;

            Debug.Log($"[ItemInspector] Renamed '{oldKey}' → '{newKey}' in item localization tables.");
        }

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