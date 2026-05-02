using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;
using DialogueSystem.Data;

namespace DialogueSystem.Editor
{
    /// <summary>
    /// Right-hand inspector panel for the selected DialogueNode.
    ///
    /// LOCALIZED CONTENT SECTION
    ///   Speaker name — rendered as a combobox: a dropdown of all existing
    ///   speaker keys from the SpeakerTableCollection plus a text field + button
    ///   to register a brand-new speaker key. Selecting or creating a speaker
    ///   sets node.speakerNameKey and immediately writes/reads the per-locale
    ///   values from the SpeakerTableCollection.
    ///
    ///   Dialogue text — one TextField per registered locale, reading from and
    ///   writing to the DialogueTextTableCollection using node.textKey as the key.
    ///
    ///   Choice labels — one TextField per locale per choice, reading from and
    ///   writing to the DialogueChoiceLabelTableCollection using choice.labelKey.
    ///
    /// KEY RENAMING
    ///   speakerNameKey — a rename field checks if the old key is present in the
    ///     SpeakerTableCollection and calls LocalizationTableService.RenameSpeakerKey.
    ///     Because speaker keys are shared, all node views referencing the same key
    ///     are notified via DialogueLocaleState.OnSpeakerValueChanged so they
    ///     update their canvas preview; the graph is also walked to update every
    ///     node.speakerNameKey that matched the old value.
    ///
    ///   textKey — checked for duplicates across all graph nodes; if a duplicate
    ///     is found the rename is rejected and a warning label appears.  Duplicate
    ///     textKeys also block Save (surfaced through DialogueGraph.Validate()).
    ///
    ///   labelKey — same duplicate-prevention logic as textKey but checked across
    ///     all choices in the graph.
    ///
    /// STRING TABLE WRITES
    ///   Every field change calls LocalizationTableService.SetSpeakerEntry /
    ///   SetTextEntry / SetChoiceLabelEntry immediately. SaveAll() is batched to
    ///   the window Save button.  After each write, _nodeView.RefreshLocalePreview()
    ///   is called so the canvas updates in real time.
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

        // Supplied by the editor window so the panel can call RefreshLocalePreview
        // on other node views when a shared speakerNameKey is renamed.
        private Func<IEnumerable<DialogueNodeView>> _getAllNodeViews;

        private readonly ScrollView    _scroll;
        private readonly VisualElement _content;
        private readonly Label         _emptyLabel;

        // ── Constructor ───────────────────────────────────────────────────────

        public DialogueNodeInspectorPanel(DialogueLocaleState localeState,
                                          Func<IEnumerable<DialogueNodeView>> getAllNodeViews = null)
        {
            _localeState      = localeState;
            _getAllNodeViews   = getAllNodeViews;

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
                    BuildSpeakerCombobox();

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

            // ── Conditions ────────────────────────────────────────────────────
            AddSection("Node Conditions");
            RebuildConditionList(_node.conditions, "Add Node Condition");
        }

        // ── Speaker combobox ──────────────────────────────────────────────────

        private void BuildSpeakerCombobox()
        {
            var sectionLabel = new Label("Speaker");
            sectionLabel.AddToClassList("inspector-field-label");
            sectionLabel.style.marginBottom = 4;
            _content.Add(sectionLabel);

            List<string> existingKeys = LocalizationTableService.GetAllSpeakerKeys();

            // ── Dropdown of existing speakers ─────────────────────────────────
            var dropdownRow = new VisualElement();
            dropdownRow.AddToClassList("inspector-row");

            var dropLabel = new Label("Select");
            dropLabel.AddToClassList("inspector-field-label");
            dropdownRow.Add(dropLabel);

            var choices = new List<string> { "— none —" };
            choices.AddRange(existingKeys);

            int currentIndex = existingKeys.IndexOf(_node.speakerNameKey);
            var dropdown = new DropdownField(choices, currentIndex >= 0 ? currentIndex + 1 : 0);
            dropdown.AddToClassList("inspector-field");
            dropdown.RegisterValueChangedCallback(e =>
            {
                string selected = e.newValue == "— none —" ? string.Empty : e.newValue;
                _node.speakerNameKey = selected;
                EditorUtility.SetDirty(_graph);
                _nodeView?.RefreshLocalePreview();
                Dirty();
                Rebuild();
            });
            dropdownRow.Add(dropdown);
            _content.Add(dropdownRow);

            // ── Rename the selected speaker key ───────────────────────────────
            if (!string.IsNullOrEmpty(_node.speakerNameKey))
            {
                BuildSpeakerKeyRenameRow(_node.speakerNameKey);

                // ── Per-locale values for the selected speaker key ─────────────
                var localeBlock = new VisualElement();
                localeBlock.AddToClassList("locale-block");

                foreach (var locale in _localeState.AllLocales)
                {
                    Locale capturedLocale = locale;
                    string currentValue   = LocalizationTableService
                        .GetSpeakerEntry(locale, _node.speakerNameKey);

                    var row = new VisualElement();
                    row.AddToClassList("locale-row");

                    var badge = new Label(locale.Identifier.Code.ToUpper());
                    badge.AddToClassList("locale-badge");
                    row.Add(badge);

                    var field = new TextField { value = currentValue };
                    field.AddToClassList("locale-field");
                    field.RegisterValueChangedCallback(e =>
                    {
                        LocalizationTableService.SetSpeakerEntry(
                            capturedLocale, _node.speakerNameKey, e.newValue);
                        _nodeView?.RefreshLocalePreview();
                        Dirty();

                        if (locale == _localeState.ActiveLocale)
                            _localeState.NotifySpeakerValueChanged(_node.speakerNameKey);
                    });
                    row.Add(field);
                    localeBlock.Add(row);
                }
                _content.Add(localeBlock);
            }

            // ── Add new speaker ───────────────────────────────────────────────
            var addSpeakerSection = new VisualElement();
            addSpeakerSection.AddToClassList("add-speaker-section");

            var addLabel = new Label("New speaker key");
            addLabel.AddToClassList("inspector-field-label");
            addSpeakerSection.Add(addLabel);

            var addRow = new VisualElement();
            addRow.AddToClassList("inspector-row");

            var newKeyField = new TextField {};
            newKeyField.AddToClassList("inspector-field");
            addRow.Add(newKeyField);

            var addBtn = new Button(() =>
            {
                string newKey = newKeyField.value.Trim();
                if (string.IsNullOrEmpty(newKey)) return;

                bool added = LocalizationTableService.AddSpeakerKey(newKey);
                if (added)
                {
                    _node.speakerNameKey = newKey;
                    EditorUtility.SetDirty(_graph);
                    Dirty();
                    Rebuild();
                }
                else
                {
                    Debug.LogWarning($"[DialogueEditor] Speaker key '{newKey}' already exists.");
                }
            })
            { text = "+ Add" };
            addBtn.AddToClassList("add-btn-small");
            addRow.Add(addBtn);

            addSpeakerSection.Add(addRow);
            _content.Add(addSpeakerSection);
        }

        /// <summary>
        /// Builds the "Rename key" row for the currently-assigned speaker key.
        /// On confirm: renames the key in the SpeakerTableCollection, updates
        /// node.speakerNameKey on every node in the graph that used the old key,
        /// and notifies all canvas node views to refresh their previews.
        /// </summary>
        private void BuildSpeakerKeyRenameRow(string currentKey)
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

            var newKeyField = new TextField { value = currentKey };
            newKeyField.AddToClassList("inspector-field");
            newKeyField.AddToClassList("key-field");
            renameRow.Add(newKeyField);

            var applyBtn = new Button(() =>
            {
                string newKey = newKeyField.value.Trim();
                if (string.IsNullOrEmpty(newKey) || newKey == currentKey) return;

                // If the old key exists in the table, rename it; otherwise just
                // update the node reference (the key may not have been saved yet).
                if (LocalizationTableService.SpeakerKeyExists(currentKey))
                {
                    bool renamed = LocalizationTableService.RenameSpeakerKey(currentKey, newKey);
                    if (!renamed)
                    {
                        warningLabel.text = $"⚠ Could not rename — '{newKey}' may already exist.";
                        warningLabel.style.display = DisplayStyle.Flex;
                        return;
                    }
                }

                warningLabel.style.display = DisplayStyle.None;

                // Walk the whole graph and update every node that shared this key
                if (_graph != null)
                {
                    Undo.RecordObject(_graph, "Rename Speaker Key");
                    foreach (var n in _graph.nodes)
                    {
                        if (n.speakerNameKey == currentKey)
                            n.speakerNameKey = newKey;
                    }
                    EditorUtility.SetDirty(_graph);
                }

                // Notify all node views so their canvas previews update
                if (_getAllNodeViews != null)
                {
                    foreach (var view in _getAllNodeViews())
                        view.RefreshLocalePreview();
                }
                // Also fire the event so any other listeners update
                _localeState?.NotifySpeakerValueChanged(newKey);

                Dirty();
                Rebuild(); // rebuild to reflect the new key everywhere in the panel
            })
            { text = "Apply" };
            applyBtn.AddToClassList("add-btn-small");
            renameRow.Add(applyBtn);

            renameContainer.Add(renameRow);
            renameContainer.Add(warningLabel);
            _content.Add(renameContainer);
        }

        // ── Dialogue text fields ──────────────────────────────────────────────

        private void BuildTextKeyField()
        {
            // ── Current key display + rename UI ───────────────────────────────
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
        /// If a duplicate is found, shows a warning and does not apply.
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

            string capturedOldKey = _node.textKey ?? string.Empty;

            var newKeyField = new TextField { value = capturedOldKey };
            newKeyField.AddToClassList("inspector-field");
            newKeyField.AddToClassList("key-field");
            renameRow.Add(newKeyField);

            var applyBtn = new Button(() =>
            {
                string oldKey = _node.textKey ?? string.Empty;
                string newKey = newKeyField.value.Trim();
                if (string.IsNullOrEmpty(newKey) || newKey == oldKey) return;

                // Check for duplicate textKey in the graph (must be unique per node)
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

                // Rename in the table if the old key exists there
                if (!string.IsNullOrEmpty(oldKey) &&
                    LocalizationTableService.TextKeyExists(oldKey))
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
                var warn = new Label("⚠ No locales found.");
                warn.AddToClassList("locale-warning");
                _content.Add(warn);
                return;
            }

            if (string.IsNullOrEmpty(_node.textKey))
            {
                var hint = new Label("Set a Text Key above to edit translations.");
                hint.AddToClassList("locale-warning");
                _content.Add(hint);
                return;
            }

            var localeBlock = new VisualElement();
            localeBlock.AddToClassList("locale-block");

            foreach (var locale in locales)
            {
                Locale capturedLocale = locale;
                string currentValue = LocalizationTableService
                    .GetTextEntry(locale, _node.textKey);

                var row = new VisualElement();
                row.AddToClassList("locale-row");

                var badge = new Label(locale.Identifier.Code.ToUpper());
                badge.AddToClassList("locale-badge");
                row.Add(badge);

                var field = new TextField { value = currentValue, multiline = true };
                field.AddToClassList("locale-textarea");
                field.RegisterValueChangedCallback(e =>
                {
                    LocalizationTableService.SetTextEntry(
                        capturedLocale, _node.textKey, e.newValue);
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

                // ── Card header ───────────────────────────────────────────────
                var cardHeader = new VisualElement();
                cardHeader.AddToClassList("choice-card-header");
                var cardTitle  = new Label($"Choice {i + 1}");
                cardTitle.AddToClassList("choice-card-title");
                var removeBtn  = new Button(() => RemoveChoice(idx)) { text = "✕" };
                removeBtn.AddToClassList("remove-btn");
                cardHeader.Add(cardTitle);
                cardHeader.Add(removeBtn);
                card.Add(cardHeader);

                // ── Label key (read-only display) ─────────────────────────────
                var labelKeyRow = MakeRow("Label Key");
                var labelKeyValue = new Label(string.IsNullOrEmpty(choice.labelKey)
                    ? "(not set)" : choice.labelKey);
                labelKeyValue.AddToClassList("readonly-value");
                labelKeyRow.Add(labelKeyValue);
                card.Add(labelKeyRow);

                // ── Label key rename ──────────────────────────────────────────
                BuildChoiceLabelKeyRenameRow(card, choice, idx);

                // ── Per-locale label fields ────────────────────────────────────
                BuildPerLocaleChoiceLabelFields(card, choice);

                // ── Target node ───────────────────────────────────────────────
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

                container.Add(card);
            }

            _content.Add(container);

            var addBtn = new Button(AddChoice) { text = "+ Add Choice" };
            addBtn.AddToClassList("add-btn");
            _content.Add(addBtn);
        }

        /// <summary>
        /// Builds the rename row for a single choice's labelKey.
        /// Validates uniqueness across all choices in the graph before applying.
        /// Duplicate labelKeys are also surfaced by DialogueGraph.Validate().
        /// </summary>
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

                // Check for duplicate labelKey across all choices in the graph
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

                // Rename in the table if the old key exists
                if (!string.IsNullOrEmpty(oldKey) &&
                    LocalizationTableService.ChoiceLabelKeyExists(oldKey))
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
                DirtyFlow(); // ports need refresh so their labels update
                Rebuild();
            })
            { text = "Apply" };
            applyBtn.AddToClassList("add-btn-small");
            renameRow.Add(applyBtn);

            renameContainer.Add(renameRow);
            renameContainer.Add(warningLabel);
            parent.Add(renameContainer);
        }

        /// <summary>
        /// Builds per-locale label fields for a single choice inside a card.
        /// Reads from and writes to the DialogueChoiceLabelTableCollection.
        /// If choice.labelKey is empty, shows a helper to set one first.
        /// </summary>
        private void BuildPerLocaleChoiceLabelFields(VisualElement parent, DialogueChoice choice)
        {
            var locales = _localeState?.AllLocales;
            if (locales == null || locales.Count == 0) return;

            if (string.IsNullOrEmpty(choice.labelKey))
            {
                var hint = new Label("Set a Label Key above to edit translations.");
                hint.AddToClassList("locale-warning");
                parent.Add(hint);
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
                    // Refresh canvas so port names show the updated resolved label
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
            // Generate a default labelKey that is unique within this graph's choices
            string baseKey    = $"{_node.id}_choice";
            int    suffix     = _node.choices.Count + 1;
            var    usedKeys   = _graph?.nodes
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

                var keyField = new TextField { value = cond.key};
                keyField.AddToClassList("condition-field");
                keyField.RegisterValueChangedCallback(e => { cond.key = e.newValue; Dirty(); });
                row.Add(keyField);

                var valField = new TextField { value = cond.value};
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

        // ── Dirty helpers ─────────────────────────────────────────────────────

        private void ShowEmpty()
        {
            _emptyLabel.style.display = DisplayStyle.Flex;
            _scroll.style.display     = DisplayStyle.None;
        }

        /// <summary>Marks graph dirty and notifies the window.</summary>
        private void Dirty()
        {
            if (_graph != null) EditorUtility.SetDirty(_graph);
            OnDataChanged?.Invoke();
        }

        /// <summary>Marks graph dirty, notifies window, AND triggers edge refresh.</summary>
        private void DirtyFlow()
        {
            Dirty();
            _nodeView?.TriggerNodeDataChange();
        }
    }
}
