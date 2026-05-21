using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;
using DialogueSystem.Data;
using InventorySystem.Data;

namespace DialogueSystem.Editor
{
    /// <summary>
    /// Right-hand inspector panel for the selected DialogueNode.
    ///
    /// LOCALIZED CONTENT SECTION
    ///   Speaker — an ObjectField dropdown listing all Character assets found in
    ///   the project via AssetDatabase. Selecting one sets node.speaker directly.
    ///   Character name localization is managed entirely in CharacterInspector;
    ///   this panel shows the resolved name for the active locale as a read-only
    ///   preview beneath the dropdown.
    ///
    ///   Dialogue text — one TextField per registered locale, reading from and
    ///   writing to the DialogueTextTable using node.textKey as the key.
    ///
    ///   Choice labels — one TextField per locale per choice, reading from and
    ///   writing to the DialogueChoiceLabelTable using choice.labelKey.
    ///
    /// KEY RENAMING
    ///   textKey  — checked for duplicates across all graph nodes; if a duplicate
    ///     is found the rename is rejected and a warning label appears. Duplicate
    ///     textKeys also block Save (surfaced through DialogueGraph.Validate()).
    ///
    ///   labelKey — same duplicate-prevention logic as textKey but checked across
    ///     all choices in the graph.
    ///
    /// STRING TABLE WRITES
    ///   Every field change calls LocalizationTableService.SetTextEntry /
    ///   SetChoiceLabelEntry immediately. SaveAll() is batched to the window Save
    ///   button. After each write, _nodeView.RefreshLocalePreview() is called so
    ///   the canvas updates in real time.
    /// </summary>
    public class DialogueNodeInspectorPanel : VisualElement
    {
        // ── Events ────────────────────────────────────────────────────────────

        public event Action OnDataChanged;

        // ── State ─────────────────────────────────────────────────────────────

        private DialogueNodeView    _nodeView;
        private DialogueNode        _node;
        private DialogueGraph       _graph;
        private DialogueLocaleState _localeState;

        private readonly ScrollView    _scroll;
        private readonly VisualElement _content;
        private readonly Label         _emptyLabel;

        // ── Constructor ───────────────────────────────────────────────────────

        public DialogueNodeInspectorPanel(DialogueLocaleState localeState)
        {
            _localeState = localeState;

            AddToClassList("inspector-panel");

            var header = new Label("NODE INSPECTOR");
            header.AddToClassList("inspector-header");
            Add(header);

            _emptyLabel = new Label("Select a node\nto inspect it.");
            _emptyLabel.AddToClassList("inspector-empty-label");
            Add(_emptyLabel);

            _scroll = new ScrollView(ScrollViewMode.Vertical);
            _scroll.AddToClassList("inspector-scroll");
            _content = new VisualElement();
            _content.AddToClassList("inspector-content");
            _scroll.Add(_content);
            Add(_scroll);

            ShowEmpty();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Inspect(DialogueNodeView nodeView)
        {
            _nodeView = nodeView;
            _node     = nodeView?.NodeData;
            _graph    = nodeView?.GraphData;
            Rebuild();
        }

        public void Clear()
        {
            _nodeView = null;
            _node     = null;
            _graph    = null;
            ShowEmpty();
        }

        // ── Rebuild ───────────────────────────────────────────────────────────

        private void Rebuild()
        {
            _content.Clear();

            if (_node == null) { ShowEmpty(); return; }
            _emptyLabel.style.display = DisplayStyle.None;
            _scroll.style.display     = DisplayStyle.Flex;

            // ── Identity ──────────────────────────────────────────────────────
            AddSection("Identity");
            AddReadOnlyField("Node ID", _node.id);
            AddReadOnlyField("Type",    _node.nodeType.ToString());

            // ── Entry node ────────────────────────────────────────────────────
            if (_graph != null)
            {
                AddSection("Graph Role");
                bool isEntry = _graph.entryNodeId == _node.id;

                if (isEntry)
                {
                    var entryLabel = new Label("✓ This is the entry node.");
                    entryLabel.AddToClassList("entry-node-active-label");
                    _content.Add(entryLabel);
                }
                else
                {
                    var setEntryBtn = new Button(() =>
                    {
                        Undo.RecordObject(_graph, "Set Entry Node");
                        _graph.entryNodeId = _node.id;
                        EditorUtility.SetDirty(_graph);
                        OnDataChanged?.Invoke();
                        Rebuild();
                    })
                    { text = "★  Set as Entry Node" };
                    setEntryBtn.AddToClassList("set-entry-btn");
                    _content.Add(setEntryBtn);
                }
            }

            // ── Localized Content ─────────────────────────────────────────────
            if (_node.nodeType != NodeType.Terminal)
            {
                AddSection("Localized Content");

                if (_node.nodeType == NodeType.Line || _node.nodeType == NodeType.Branch)
                    BuildCharacterDropdown();

                if (_node.nodeType == NodeType.Line)
                {
                    var spacer = new VisualElement();
                    spacer.style.height = 8;
                    _content.Add(spacer);
                    BuildTextKeyField();
                    BuildPerLocaleTextFields();
                }
            }

            // ── Flow ──────────────────────────────────────────────────────────
            if (_node.nodeType == NodeType.Line)
            {
                AddSection("Flow");
                AddTextField("Next Node ID", _node.nextNodeId, v =>
                {
                    _node.nextNodeId = v;
                    DirtyFlow();
                });
            }

            // ── Choices ───────────────────────────────────────────────────────
            if (_node.nodeType == NodeType.Branch)
            {
                AddSection("Choices");
                RebuildChoices();
            }

            // ── Presentation ──────────────────────────────────────────────────
            if (_node.nodeType == NodeType.Line)
            {
                AddSection("Presentation");
                AddFloatField("Typewriter Speed", _node.typewriterSpeed, v =>
                {
                    _node.typewriterSpeed = v;
                    Dirty();
                });
                AddTextField("NPC Animator Trigger", _node.npcAnimatorTrigger, v =>
                {
                    _node.npcAnimatorTrigger = v;
                    Dirty();
                });
            }
            
            // ── Item Rewards ──────────────────────────────────────────────────
            if (_node.nodeType == NodeType.Line)
            {
                AddSection("Item Rewards");

                var onceToggle = new Toggle("Grant rewards only once")
                {
                    value = _node.grantItemRewardsOnlyOnce
                };
                onceToggle.AddToClassList("inspector-toggle");
                onceToggle.RegisterValueChangedCallback(e =>
                {
                    _node.grantItemRewardsOnlyOnce = e.newValue;
                    Dirty();
                });
                _content.Add(onceToggle);

                RebuildItemRewardList(_node.itemRewards);
            }

            // ── Conditions ────────────────────────────────────────────────────
            AddSection("Node Conditions");
            RebuildConditionList(_node.conditions, "Add Node Condition");
        }

        // ── Character dropdown ────────────────────────────────────────────────

        /// <summary>
        /// Builds a dropdown populated with all Character assets found in the project.
        /// Selecting one assigns node.speaker and refreshes the canvas preview.
        /// A read-only label below shows the character's localized name for the
        /// active locale, resolved from CharacterNameTable.
        /// Designers edit the localized name in the CharacterInspector, not here.
        /// </summary>
        private void BuildCharacterDropdown()
        {
            var sectionLabel = new Label("Speaker");
            sectionLabel.AddToClassList("inspector-field-label");
            sectionLabel.style.marginBottom = 4;
            _content.Add(sectionLabel);

            // Load every Character asset in the project
            var allCharacters = LoadAllCharacters();

            // Build choice strings: "— none —" + one entry per Character asset,
            // displaying the asset name. Index 0 is always "— none —".
            var displayNames = new List<string> { "— none —" };
            displayNames.AddRange(allCharacters.Select(c => c.name));

            int currentIdx = _node.speaker != null
                ? allCharacters.IndexOf(_node.speaker) + 1  // +1 for "— none —"
                : 0;

            var dropdownRow = new VisualElement();
            dropdownRow.AddToClassList("inspector-row");

            var dropLabel = new Label("Character");
            dropLabel.AddToClassList("inspector-field-label");
            dropdownRow.Add(dropLabel);

            var dropdown = new DropdownField(displayNames, currentIdx);
            dropdown.AddToClassList("inspector-field");
            dropdown.RegisterValueChangedCallback(e =>
            {
                if (e.newValue == "— none —")
                {
                    _node.speaker = null;
                }
                else
                {
                    // Re-find by name in case the list order shifts between repaints
                    _node.speaker = allCharacters.FirstOrDefault(c => c.name == e.newValue);
                }

                EditorUtility.SetDirty(_graph);
                _nodeView?.RefreshLocalePreview();
                Dirty();
                // Rebuild so the locale preview label beneath updates immediately
                Rebuild();
            });
            dropdownRow.Add(dropdown);
            _content.Add(dropdownRow);

            // ── Portrait key field ────────────────────────────────────────────
            if (_node.speaker != null)
            {
                BuildPortraitKeyDropdown();

                // ── Resolved name preview (read-only) ─────────────────────────
                // This tells the designer what name will be shown at runtime for
                // the currently active preview locale.
                string charKey = string.IsNullOrEmpty(_node.speaker.characterName)
                    ? _node.speaker.name
                    : _node.speaker.characterName;
                string resolvedName = _localeState?.ResolveCharacterName(charKey) ?? string.Empty;

                var previewRow = MakeRow("Name preview");
                var previewLabel = new Label(
                    string.IsNullOrEmpty(resolvedName)
                        ? $"(no entry in CharacterNameTable for '{charKey}')"
                        : resolvedName);
                previewLabel.AddToClassList("readonly-value");
                if (string.IsNullOrEmpty(resolvedName))
                    previewLabel.AddToClassList("key-rename-warning");
                previewRow.Add(previewLabel);
                _content.Add(previewRow);

                // Hint to open the Character asset inspector
                var hint = new Label("Edit localized names in the Character asset inspector.");
                hint.AddToClassList("locale-warning");
                _content.Add(hint);
            }
        }

        /// <summary>
        /// Builds a dropdown of portrait keys declared on the assigned Character asset.
        /// Selecting one sets node.speakerPortraitKey.
        /// </summary>
        private void BuildPortraitKeyDropdown()
        {
            if (_node.speaker == null || _node.speaker.portraits == null
                || _node.speaker.portraits.Count == 0)
                return;

            var portraitKeys = new List<string> { "— default —" };
            portraitKeys.AddRange(_node.speaker.portraits
                .Where(p => !string.IsNullOrEmpty(p.Key))
                .Select(p => p.Key));

            int idx = string.IsNullOrEmpty(_node.speakerPortraitKey)
                ? 0
                : portraitKeys.IndexOf(_node.speakerPortraitKey);
            if (idx < 0) idx = 0;

            var row = MakeRow("Portrait");
            var dropdown = new DropdownField(portraitKeys, idx);
            dropdown.AddToClassList("inspector-field");
            dropdown.RegisterValueChangedCallback(e =>
            {
                _node.speakerPortraitKey = e.newValue == "— default —" ? string.Empty : e.newValue;
                EditorUtility.SetDirty(_graph);
                Dirty();
            });
            row.Add(dropdown);
            _content.Add(row);
        }

        /// <summary>
        /// Returns all Character ScriptableObject assets in the project, sorted by name.
        /// Uses AssetDatabase so it works regardless of whether assets are in a Resources folder.
        /// </summary>
        private static List<Character> LoadAllCharacters()
        {
            return AssetDatabase.FindAssets("t:Character")
                .Select(guid => AssetDatabase.LoadAssetAtPath<Character>(
                    AssetDatabase.GUIDToAssetPath(guid)))
                .Where(c => c != null)
                .OrderBy(c => c.name)
                .ToList();
        }

        // ── Dialogue text fields ──────────────────────────────────────────────

        private void BuildTextKeyField()
        {
            var keyHeaderRow = MakeRow("Text Key");
            var keyValueLabel = new Label(_node.textKey ?? "(not set)");
            keyValueLabel.AddToClassList("readonly-value");
            keyHeaderRow.Add(keyValueLabel);
            _content.Add(keyHeaderRow);

            BuildTextKeyRenameRow();
        }

        /// <summary>
        /// Builds the rename row for textKey.
        /// Validates that the new key is not already used by another node in the graph.
        /// Duplicate textKeys also surface as validation errors blocking Save.
        /// </summary>
        private void BuildTextKeyRenameRow()
        {
            var renameContainer = new VisualElement();
            renameContainer.AddToClassList("key-rename-container");

            var renameLabel = new Label("Rename key");
            renameLabel.AddToClassList("inspector-field-label");
            renameContainer.Add(renameLabel);

            var renameRow = new VisualElement();
            renameRow.AddToClassList("inspector-row");

            var warningLabel = new Label();
            warningLabel.AddToClassList("key-rename-warning");
            warningLabel.style.display = DisplayStyle.None;

            var newKeyField = new TextField { value = _node.textKey ?? string.Empty };
            newKeyField.AddToClassList("inspector-field");
            newKeyField.AddToClassList("key-field");
            renameRow.Add(newKeyField);

            var applyBtn = new Button(() =>
            {
                string oldKey = _node.textKey ?? string.Empty;
                string newKey = newKeyField.value.Trim();
                if (string.IsNullOrEmpty(newKey) || newKey == oldKey) return;

                if (_graph != null)
                {
                    bool duplicateInGraph = _graph.nodes
                        .Any(n => n != _node && n.textKey == newKey);
                    if (duplicateInGraph)
                    {
                        warningLabel.text = $"⚠ textKey '{newKey}' is already used by another node.";
                        warningLabel.style.display = DisplayStyle.Flex;
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(oldKey) && LocalizationTableService.TextKeyExists(oldKey))
                {
                    bool renamed = LocalizationTableService.RenameTextKey(oldKey, newKey);
                    if (!renamed)
                    {
                        warningLabel.text = $"⚠ Could not rename — '{newKey}' may already exist in the table.";
                        warningLabel.style.display = DisplayStyle.Flex;
                        return;
                    }
                }

                warningLabel.style.display = DisplayStyle.None;
                Undo.RecordObject(_graph, "Rename Text Key");
                _node.textKey = newKey;
                EditorUtility.SetDirty(_graph);
                _nodeView?.RefreshLocalePreview();
                Dirty();
                Rebuild();
            })
            { text = "Apply" };
            applyBtn.AddToClassList("add-btn-small");
            renameRow.Add(applyBtn);

            renameContainer.Add(renameRow);
            renameContainer.Add(warningLabel);
            _content.Add(renameContainer);
        }

        private void BuildPerLocaleTextFields()
        {
            var locales = _localeState?.AllLocales;
            if (locales == null || locales.Count == 0)
            {
                _content.Add(MakeWarning("⚠ No locales found."));
                return;
            }

            if (string.IsNullOrEmpty(_node.textKey))
            {
                _content.Add(MakeWarning("Set a Text Key above to edit translations."));
                return;
            }

            var localeBlock = new VisualElement();
            localeBlock.AddToClassList("locale-block");

            foreach (var locale in locales)
            {
                Locale capturedLocale = locale;
                string currentValue = LocalizationTableService.GetTextEntry(locale, _node.textKey);

                var row = new VisualElement();
                row.AddToClassList("locale-row");

                var badge = new Label(locale.Identifier.Code.ToUpper());
                badge.AddToClassList("locale-badge");
                row.Add(badge);

                var field = new TextField { value = currentValue, multiline = true };
                field.AddToClassList("locale-textarea");
                field.RegisterValueChangedCallback(e =>
                {
                    LocalizationTableService.SetTextEntry(capturedLocale, _node.textKey, e.newValue);
                    _nodeView?.RefreshLocalePreview();
                    Dirty();
                });
                row.Add(field);
                localeBlock.Add(row);
            }

            _content.Add(localeBlock);
        }

        // ── Choices ───────────────────────────────────────────────────────────

        private void RebuildChoices()
        {
            var container = new VisualElement();
            container.AddToClassList("choice-list");

            for (int i = 0; i < _node.choices.Count; i++)
            {
                int idx    = i;
                var choice = _node.choices[i];
                var card   = new VisualElement();
                card.AddToClassList("choice-card");

                var cardHeader = new VisualElement();
                cardHeader.AddToClassList("choice-card-header");
                var cardTitle  = new Label($"Choice {i + 1}");
                cardTitle.AddToClassList("choice-card-title");
                var removeBtn  = new Button(() => RemoveChoice(idx)) { text = "✕" };
                removeBtn.AddToClassList("remove-btn");
                cardHeader.Add(cardTitle);
                cardHeader.Add(removeBtn);
                card.Add(cardHeader);

                // Label key display + rename
                var labelKeyRow = MakeRow("Label Key");
                var labelKeyValue = new Label(
                    string.IsNullOrEmpty(choice.labelKey) ? "(not set)" : choice.labelKey);
                labelKeyValue.AddToClassList("readonly-value");
                labelKeyRow.Add(labelKeyValue);
                card.Add(labelKeyRow);

                BuildChoiceLabelKeyRenameRow(card, choice, idx);
                BuildPerLocaleChoiceLabelFields(card, choice);

                AddInlineTextField(card, "Target Node ID", choice.targetNodeId, v =>
                {
                    choice.targetNodeId = v;
                    DirtyFlow();
                });

                var toggle = new Toggle("Show if conditions fail") { value = choice.showIfFailed };
                toggle.AddToClassList("inspector-toggle");
                toggle.RegisterValueChangedCallback(e => { choice.showIfFailed = e.newValue; Dirty(); });
                card.Add(toggle);

                var condHeader = new Label("Conditions");
                condHeader.AddToClassList("subsection-label");
                card.Add(condHeader);
                RebuildConditionList(choice.conditions, "Add Condition", card);

                var costHeader = new Label("Item Costs");
                costHeader.AddToClassList("subsection-label");
                card.Add(costHeader);
                RebuildItemCostList(choice.itemCosts, card);
                container.Add(card);
            }

            _content.Add(container);

            var addBtn = new Button(AddChoice) { text = "+ Add Choice" };
            addBtn.AddToClassList("add-btn");
            _content.Add(addBtn);
        }

        private void BuildChoiceLabelKeyRenameRow(VisualElement parent,
                                                   DialogueChoice choice, int choiceIndex)
        {
            var renameContainer = new VisualElement();
            renameContainer.AddToClassList("key-rename-container");

            var renameLabel = new Label("Rename key");
            renameLabel.AddToClassList("inspector-field-label");
            renameContainer.Add(renameLabel);

            var renameRow = new VisualElement();
            renameRow.AddToClassList("inspector-row");

            var warningLabel = new Label();
            warningLabel.AddToClassList("key-rename-warning");
            warningLabel.style.display = DisplayStyle.None;

            var newKeyField = new TextField { value = choice.labelKey ?? string.Empty };
            newKeyField.AddToClassList("inspector-field");
            newKeyField.AddToClassList("key-field");
            renameRow.Add(newKeyField);

            var applyBtn = new Button(() =>
            {
                string oldKey = choice.labelKey ?? string.Empty;
                string newKey = newKeyField.value.Trim();
                if (string.IsNullOrEmpty(newKey) || newKey == oldKey) return;

                if (_graph != null)
                {
                    bool duplicateInGraph = _graph.nodes
                        .SelectMany(n => n.choices)
                        .Any(c => c != choice && c.labelKey == newKey);
                    if (duplicateInGraph)
                    {
                        warningLabel.text = $"⚠ labelKey '{newKey}' is already used by another choice.";
                        warningLabel.style.display = DisplayStyle.Flex;
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(oldKey) && LocalizationTableService.ChoiceLabelKeyExists(oldKey))
                {
                    bool renamed = LocalizationTableService.RenameChoiceLabelKey(oldKey, newKey);
                    if (!renamed)
                    {
                        warningLabel.text = $"⚠ Could not rename — '{newKey}' may already exist in the table.";
                        warningLabel.style.display = DisplayStyle.Flex;
                        return;
                    }
                }

                warningLabel.style.display = DisplayStyle.None;
                Undo.RecordObject(_graph, "Rename Choice Label Key");
                choice.labelKey = newKey;
                EditorUtility.SetDirty(_graph);
                _nodeView?.RefreshLocalePreview();
                DirtyFlow();
                Rebuild();
            })
            { text = "Apply" };
            applyBtn.AddToClassList("add-btn-small");
            renameRow.Add(applyBtn);

            renameContainer.Add(renameRow);
            renameContainer.Add(warningLabel);
            parent.Add(renameContainer);
        }

        private void BuildPerLocaleChoiceLabelFields(VisualElement parent, DialogueChoice choice)
        {
            var locales = _localeState?.AllLocales;
            if (locales == null || locales.Count == 0) return;

            if (string.IsNullOrEmpty(choice.labelKey))
            {
                parent.Add(MakeWarning("Set a Label Key above to edit translations."));
                return;
            }

            var localeBlock = new VisualElement();
            localeBlock.AddToClassList("locale-block");

            foreach (var locale in locales)
            {
                Locale capturedLocale = locale;
                DialogueChoice capturedChoice = choice;

                string currentValue = LocalizationTableService
                    .GetChoiceLabelEntry(locale, choice.labelKey);

                var row = new VisualElement();
                row.AddToClassList("locale-row");

                var badge = new Label(locale.Identifier.Code.ToUpper());
                badge.AddToClassList("locale-badge");
                row.Add(badge);

                var field = new TextField { value = currentValue };
                field.AddToClassList("locale-field");
                field.RegisterValueChangedCallback(e =>
                {
                    LocalizationTableService.SetChoiceLabelEntry(
                        capturedLocale, capturedChoice.labelKey, e.newValue);
                    _nodeView?.RefreshLocalePreview();
                    Dirty();
                });
                row.Add(field);
                localeBlock.Add(row);
            }

            parent.Add(localeBlock);
        }

        private void AddChoice()
        {
            string baseKey  = $"{_node.id}_choice";
            int    suffix   = _node.choices.Count + 1;
            var    usedKeys = _graph?.nodes
                .SelectMany(n => n.choices)
                .Select(c => c.labelKey)
                .Where(k => !string.IsNullOrEmpty(k))
                .ToHashSet() ?? new HashSet<string>();

            string candidateKey = $"{baseKey}_{suffix:D2}";
            while (usedKeys.Contains(candidateKey))
                candidateKey = $"{baseKey}_{++suffix:D2}";

            _node.choices.Add(new DialogueChoice { labelKey = candidateKey });
            DirtyFlow();
            Rebuild();
        }

        private void RemoveChoice(int index)
        {
            if (index < 0 || index >= _node.choices.Count) return;
            _node.choices.RemoveAt(index);
            DirtyFlow();
            Rebuild();
        }

        // ── Conditions ────────────────────────────────────────────────────────

        private void RebuildConditionList(List<DialogueCondition> conditions,
                                          string addLabel, VisualElement parent = null)
        {
            parent ??= _content;

            if (conditions.Count == 0)
            {
                var none = new Label("No conditions.");
                none.AddToClassList("inspector-empty-small");
                parent.Add(none);
            }

            for (int i = 0; i < conditions.Count; i++)
            {
                int idx  = i;
                var cond = conditions[i];
                var row  = new VisualElement();
                row.AddToClassList("condition-row");

                var typeEnum = new EnumField(cond.type);
                typeEnum.AddToClassList("condition-type");
                typeEnum.RegisterValueChangedCallback(e => { cond.type = (ConditionType)e.newValue; Dirty(); });
                row.Add(typeEnum);

                var keyField = new TextField { value = cond.key };
                keyField.AddToClassList("condition-field");
                keyField.RegisterValueChangedCallback(e => { cond.key = e.newValue; Dirty(); });
                row.Add(keyField);

                var valField = new TextField { value = cond.value };
                valField.AddToClassList("condition-field");
                valField.RegisterValueChangedCallback(e => { cond.value = e.newValue; Dirty(); });
                row.Add(valField);

                var negateToggle = new Toggle("¬") { value = cond.negate };
                negateToggle.AddToClassList("condition-negate");
                negateToggle.RegisterValueChangedCallback(e => { cond.negate = e.newValue; Dirty(); });
                row.Add(negateToggle);

                var removeBtn = new Button(() => { conditions.RemoveAt(idx); Dirty(); Rebuild(); })
                    { text = "✕" };
                removeBtn.AddToClassList("remove-btn");
                row.Add(removeBtn);

                parent.Add(row);
            }

            var addCondBtn = new Button(() => { conditions.Add(new DialogueCondition()); Dirty(); Rebuild(); })
                { text = $"+ {addLabel}" };
            addCondBtn.AddToClassList("add-btn-small");
            parent.Add(addCondBtn);
        }

        // ── Item Rewards / Costs ───────────────────────────────────────────

        private void RebuildItemRewardList(List<DialogueItemReward> rewards,
                                        VisualElement parent = null)
        {
            parent ??= _content;

            if (rewards.Count == 0)
            {
                var none = new Label("No item rewards.");
                none.AddToClassList("inspector-empty-small");
                parent.Add(none);
            }

            for (int i = 0; i < rewards.Count; i++)
            {
                int idx = i;
                var reward = rewards[i];

                var row = new VisualElement();
                row.AddToClassList("condition-row");

                var itemField = new ObjectField("Item")
                {
                    objectType = typeof(ItemData),
                    value = reward.item,
                    allowSceneObjects = false
                };
                itemField.AddToClassList("condition-field");
                itemField.RegisterValueChangedCallback(e =>
                {
                    reward.item = e.newValue as ItemData;
                    Dirty();
                });
                row.Add(itemField);

                var amountField = new IntegerField("Amount")
                {
                    value = reward.amount
                };
                amountField.AddToClassList("condition-field");
                amountField.RegisterValueChangedCallback(e =>
                {
                    reward.amount = Mathf.Max(1, e.newValue);
                    Dirty();
                });
                row.Add(amountField);

                var removeBtn = new Button(() =>
                {
                    rewards.RemoveAt(idx);
                    Dirty();
                    Rebuild();
                })
                { text = "✕" };
                removeBtn.AddToClassList("remove-btn");
                row.Add(removeBtn);

                parent.Add(row);
            }

            var addBtn = new Button(() =>
            {
                rewards.Add(new DialogueItemReward());
                Dirty();
                Rebuild();
            })
            { text = "+ Add Item Reward" };
            addBtn.AddToClassList("add-btn-small");
            parent.Add(addBtn);
        }

        private void RebuildItemCostList(List<DialogueItemCost> costs,
                                        VisualElement parent = null)
        {
            parent ??= _content;

            if (costs.Count == 0)
            {
                var none = new Label("No item costs.");
                none.AddToClassList("inspector-empty-small");
                parent.Add(none);
            }

            for (int i = 0; i < costs.Count; i++)
            {
                int idx = i;
                var cost = costs[i];

                var row = new VisualElement();
                row.AddToClassList("condition-row");

                var itemField = new ObjectField("Item")
                {
                    objectType = typeof(ItemData),
                    value = cost.item,
                    allowSceneObjects = false
                };
                itemField.AddToClassList("condition-field");
                itemField.RegisterValueChangedCallback(e =>
                {
                    cost.item = e.newValue as ItemData;
                    Dirty();
                });
                row.Add(itemField);

                var amountField = new IntegerField("Amount")
                {
                    value = cost.amount
                };
                amountField.AddToClassList("condition-field");
                amountField.RegisterValueChangedCallback(e =>
                {
                    cost.amount = Mathf.Max(1, e.newValue);
                    Dirty();
                });
                row.Add(amountField);

                var removeBtn = new Button(() =>
                {
                    costs.RemoveAt(idx);
                    Dirty();
                    Rebuild();
                })
                { text = "✕" };
                removeBtn.AddToClassList("remove-btn");
                row.Add(removeBtn);

                parent.Add(row);
            }

            var addBtn = new Button(() =>
            {
                costs.Add(new DialogueItemCost());
                Dirty();
                Rebuild();
            })
            { text = "+ Add Item Cost" };
            addBtn.AddToClassList("add-btn-small");
            parent.Add(addBtn);
        }

        // ── Field helpers ─────────────────────────────────────────────────────

        private void AddSection(string title)
        {
            var lbl = new Label(title);
            lbl.AddToClassList("inspector-section");
            _content.Add(lbl);
        }

        private void AddReadOnlyField(string label, string value)
        {
            var row = MakeRow(label);
            var lbl = new Label(value ?? "—");
            lbl.AddToClassList("readonly-value");
            row.Add(lbl);
            _content.Add(row);
        }

        private void AddTextField(string label, string value, Action<string> onChange)
        {
            var row   = MakeRow(label);
            var field = new TextField { value = value ?? string.Empty };
            field.AddToClassList("inspector-field");
            field.RegisterValueChangedCallback(e => onChange(e.newValue));
            row.Add(field);
            _content.Add(row);
        }

        private void AddFloatField(string label, float value, Action<float> onChange)
        {
            var row   = MakeRow(label);
            var field = new FloatField { value = value };
            field.AddToClassList("inspector-field");
            field.RegisterValueChangedCallback(e => onChange(e.newValue));
            row.Add(field);
            _content.Add(row);
        }

        private void AddInlineTextField(VisualElement parent, string label,
                                         string value, Action<string> onChange)
        {
            var row   = MakeRow(label);
            var field = new TextField { value = value ?? string.Empty };
            field.AddToClassList("inspector-field");
            field.RegisterValueChangedCallback(e => onChange(e.newValue));
            row.Add(field);
            parent.Add(row);
        }

        private VisualElement MakeRow(string label)
        {
            var row = new VisualElement();
            row.AddToClassList("inspector-row");
            var lbl = new Label(label);
            lbl.AddToClassList("inspector-field-label");
            row.Add(lbl);
            return row;
        }

        private static Label MakeWarning(string text)
        {
            var lbl = new Label(text);
            lbl.AddToClassList("locale-warning");
            return lbl;
        }

        // ── Dirty helpers ─────────────────────────────────────────────────────

        private void ShowEmpty()
        {
            _emptyLabel.style.display = DisplayStyle.Flex;
            _scroll.style.display     = DisplayStyle.None;
        }

        private void Dirty()
        {
            if (_graph != null) EditorUtility.SetDirty(_graph);
            OnDataChanged?.Invoke();
        }

        private void DirtyFlow()
        {
            Dirty();
            _nodeView?.TriggerNodeDataChange();
        }
    }
}
