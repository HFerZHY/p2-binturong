using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using DialogueSystem.Data;

namespace DialogueSystem.Editor
{
    /// <summary>
    /// Custom Inspector for the Character ScriptableObject.
    ///
    /// RESPONSIBILITIES
    ///   1. Character name (key) management
    ///      characterName serves as both the human-readable identifier and the
    ///      lookup key in CharacterNameTable.  It must be unique across all
    ///      Character assets.  This inspector enforces uniqueness and handles
    ///      the rename workflow:
    ///
    ///        • On first inspect (key not yet in the table): a "Register" button
    ///          calls LocalizationTableService.AddCharacterNameKey and creates
    ///          empty per-locale rows ready to fill in.
    ///
    ///        • Rename workflow: a "Rename key" field + Apply button checks
    ///          uniqueness across all Character assets, then calls
    ///          LocalizationTableService.RenameCharacterNameKey to preserve all
    ///          existing translations, and finally writes the new value back to
    ///          character.characterName.
    ///
    ///   2. Per-locale name editing
    ///      One text field per registered locale, reading from and writing to
    ///      CharacterNameTable immediately on change.  SaveAll() is deferred to
    ///      the user pressing the "Save to disk" button at the bottom of the
    ///      inspector so multiple edits are batched.
    ///
    ///   3. Portrait list
    ///      The default inspector handles the serialized portrait list; this
    ///      inspector draws it beneath the localization section using
    ///      DrawDefaultInspector with a filtered approach, or DrawPropertiesExcluding.
    /// </summary>
    [CustomEditor(typeof(Character))]
    public class CharacterInspector : UnityEditor.Editor
    {
        private Character _character;

        // Track whether the current characterName key is present in the table,
        // so we know whether to show "Register" or "Rename".
        private bool   _keyRegistered;
        private string _pendingRenameValue;

        private void OnEnable()
        {
            _character          = (Character)target;
            _pendingRenameValue = _character.characterName;
            _keyRegistered      = LocalizationTableService
                .CharacterNameKeyExists(_character.characterName);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // ── Character name / key section ──────────────────────────────────
            EditorGUILayout.LabelField("Character Identity", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            // Show the current characterName as a read-only label so it is clear
            // that editing the name field is done through the rename workflow below,
            // not by typing directly (which would orphan the table key).
            EditorGUILayout.LabelField("Name key", _character.characterName ?? "(not set)");

            if (!_keyRegistered)
            {
                // Key is not yet in the table — let the designer register it.
                EditorGUILayout.HelpBox(
                    $"Key '{_character.characterName}' is not yet registered in CharacterNameTable. " +
                    "Click Register to create the table entry.",
                    MessageType.Warning);

                if (GUILayout.Button("Register in CharacterNameTable"))
                    RegisterKey();
            }
            else
            {
                // ── Rename workflow ───────────────────────────────────────────
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Rename key", EditorStyles.miniBoldLabel);

                EditorGUILayout.BeginHorizontal();
                _pendingRenameValue = EditorGUILayout.TextField(_pendingRenameValue);

                EditorGUI.BeginDisabledGroup(
                    string.IsNullOrWhiteSpace(_pendingRenameValue) ||
                    _pendingRenameValue == _character.characterName);

                if (GUILayout.Button("Apply", GUILayout.Width(60)))
                    ApplyRename();

                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                // Uniqueness hint
                if (!string.IsNullOrWhiteSpace(_pendingRenameValue) &&
                    _pendingRenameValue != _character.characterName &&
                    IsNameTakenByAnotherCharacter(_pendingRenameValue))
                {
                    EditorGUILayout.HelpBox(
                        $"'{_pendingRenameValue}' is already used by another Character asset.",
                        MessageType.Error);
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(8);

            // ── Per-locale name fields ─────────────────────────────────────────
            if (_keyRegistered)
            {
                EditorGUILayout.LabelField("Localized Names", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                var locales = LocalizationTableService.GetAllLocales();
                if (locales.Count == 0)
                {
                    EditorGUILayout.HelpBox("No locales found in Localization Settings.", MessageType.Warning);
                }
                else
                {
                    foreach (var locale in locales)
                    {
                        string currentValue = LocalizationTableService
                            .GetCharacterNameEntry(locale, _character.characterName);

                        EditorGUILayout.BeginHorizontal();
                        // Locale badge
                        EditorGUILayout.LabelField(
                            locale.Identifier.Code.ToUpper(),
                            GUILayout.Width(36));

                        EditorGUI.BeginChangeCheck();
                        string newValue = EditorGUILayout.TextField(currentValue);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(_character, "Edit Character Name Translation");
                            LocalizationTableService.SetCharacterNameEntry(
                                locale, _character.characterName, newValue);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.Space(4);

                if (GUILayout.Button("Save localization to disk"))
                {
                    LocalizationTableService.SaveAll();
                    Debug.Log($"[CharacterInspector] Saved CharacterNameTable for '{_character.characterName}'.");
                }

                EditorGUILayout.Space(8);
            }

            // ── Portraits ─────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Portraits", EditorStyles.boldLabel);
            // Draw only the portraits list from the serialized object, skipping
            // characterName which is managed above.
            var portraitsProp = serializedObject.FindProperty("portraits");
            if (portraitsProp is not null)
                EditorGUILayout.PropertyField(portraitsProp, includeChildren: true);

            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void RegisterKey()
        {
            string key = _character.characterName;

            if (string.IsNullOrWhiteSpace(key))
            {
                EditorUtility.DisplayDialog("Invalid Key",
                    "characterName must not be empty before registering.", "OK");
                return;
            }

            if (IsNameTakenByAnotherCharacter(key))
            {
                EditorUtility.DisplayDialog("Duplicate Key",
                    $"Another Character asset already uses the name '{key}'.", "OK");
                return;
            }

            bool added = LocalizationTableService.AddCharacterNameKey(key);
            if (added)
            {
                _keyRegistered = true;
                LocalizationTableService.SaveAll();
                Debug.Log($"[CharacterInspector] Registered '{key}' in CharacterNameTable.");
            }
            else
            {
                // Key already existed (e.g. table was modified externally); treat as registered.
                _keyRegistered = true;
            }
        }

        private void ApplyRename()
        {
            string oldKey = _character.characterName;
            string newKey = _pendingRenameValue.Trim();

            if (string.IsNullOrEmpty(newKey) || newKey == oldKey) return;

            if (IsNameTakenByAnotherCharacter(newKey))
            {
                EditorUtility.DisplayDialog("Duplicate Key",
                    $"Another Character asset already uses the name '{newKey}'.", "OK");
                return;
            }

            // Rename in the localization table, preserving all per-locale values
            bool renamed = LocalizationTableService.RenameCharacterNameKey(oldKey, newKey);
            if (!renamed)
            {
                EditorUtility.DisplayDialog("Rename Failed",
                    $"Could not rename '{oldKey}' → '{newKey}' in CharacterNameTable. " +
                    "The new key may already exist.", "OK");
                return;
            }

            // Update the asset itself
            Undo.RecordObject(_character, "Rename Character");
            _character.characterName = newKey;
            EditorUtility.SetDirty(_character);

            // Flush to disk immediately so other inspectors see the updated key
            LocalizationTableService.SaveAll();
            AssetDatabase.SaveAssets();

            _pendingRenameValue = newKey;
            Debug.Log($"[CharacterInspector] Renamed '{oldKey}' → '{newKey}' in CharacterNameTable.");
        }

        /// <summary>
        /// Returns true if any other Character asset in the project already uses
        /// <paramref name="name"/> as its characterName.
        /// Excludes the currently inspected asset from the check.
        /// </summary>
        private bool IsNameTakenByAnotherCharacter(string name)
        {
            return AssetDatabase.FindAssets("t:Character")
                .Select(guid => AssetDatabase.LoadAssetAtPath<Character>(
                    AssetDatabase.GUIDToAssetPath(guid)))
                .Where(c => c != null && c != _character)
                .Any(c => c.characterName == name);
        }
    }
}
