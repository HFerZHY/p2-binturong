using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using DialogueSystem.Data;

namespace DialogueSystem.Editor
{
    /// <summary>
    /// Right-hand side panel drawn inside the DialogueGraphEditorWindow.
    /// Displays all fields of the currently-selected DialogueNode that
    /// don't fit neatly inside the canvas node box: conditions, events,
    /// audio, animation trigger, typewriter speed, and choice management.
    ///
    /// Communicates changes back via the OnDataChanged event so the
    /// parent window can call SetDirty and RefreshEdges as needed.
    /// </summary>
    public class DialogueNodeInspectorPanel : VisualElement
    {
        // ── Events ────────────────────────────────────────────────────────────

        public event Action OnDataChanged;

        // ── State ─────────────────────────────────────────────────────────────

        private DialogueNodeView _nodeView;
        private DialogueNode     _node;
        private DialogueGraph    _graph;

        // Scroll container so long content doesn't overflow
        private readonly ScrollView    _scroll;
        private readonly VisualElement _content;
        private readonly Label         _emptyLabel;

        // ── Constructor ───────────────────────────────────────────────────────

        public DialogueNodeInspectorPanel()
        {
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

            // ── ID (read-only for safety) ─────────────────────────────────────
            AddSection("Identity");
            AddReadOnlyField("Node ID", _node.id);
            AddReadOnlyField("Type",    _node.nodeType.ToString());

            // ── Content ───────────────────────────────────────────────────────
            if (_node.nodeType != NodeType.Terminal)
            {
                AddSection("Content");
                AddTextField("Speaker Name", _node.speakerName, v =>
                {
                    _node.speakerName = v;
                    Dirty();
                });

                if (_node.nodeType == NodeType.Line)
                {
                    AddTextAreaField("Dialogue Text", _node.text, v =>
                    {
                        _node.text = v;
                        Dirty();
                    });
                }
            }

            // ── Flow ──────────────────────────────────────────────────────────
            if (_node.nodeType == NodeType.Line)
            {
                AddSection("Flow");
                AddTextField("Next Node ID", _node.nextNodeId, v =>
                {
                    _node.nextNodeId = v;
                    Dirty(refreshEdges: true);
                });
            }

            // ── Choices (Branch) ──────────────────────────────────────────────
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

        // ── Choices ───────────────────────────────────────────────────────────

        private void RebuildChoices()
        {
            var container = new VisualElement();
            container.AddToClassList("choice-list");

            for (int i = 0; i < _node.choices.Count; i++)
            {
                int idx = i; // capture
                var choice = _node.choices[i];

                var card = new VisualElement();
                card.AddToClassList("choice-card");

                // Header row: "Choice N" + remove button
                var cardHeader = new VisualElement();
                cardHeader.AddToClassList("choice-card-header");
                var cardTitle = new Label($"Choice {i + 1}");
                cardTitle.AddToClassList("choice-card-title");
                var removeBtn = new Button(() => RemoveChoice(idx)) { text = "✕" };
                removeBtn.AddToClassList("remove-btn");
                cardHeader.Add(cardTitle);
                cardHeader.Add(removeBtn);
                card.Add(cardHeader);

                // Label
                AddInlineTextField(card, "Label", choice.label, v =>
                {
                    choice.label = v;
                    Dirty(refreshEdges: true);
                });

                // Target node ID
                AddInlineTextField(card, "Target Node ID", choice.targetNodeId, v =>
                {
                    choice.targetNodeId = v;
                    Dirty(refreshEdges: true);
                });

                // Show if failed toggle
                var toggle = new Toggle("Show if conditions fail") { value = choice.showIfFailed };
                toggle.AddToClassList("inspector-toggle");
                toggle.RegisterValueChangedCallback(e =>
                {
                    choice.showIfFailed = e.newValue;
                    Dirty();
                });
                card.Add(toggle);

                // Choice conditions (inline)
                var condHeader = new Label("Conditions");
                condHeader.AddToClassList("subsection-label");
                card.Add(condHeader);
                RebuildConditionList(choice.conditions, "Add Condition", card);

                container.Add(card);
            }

            _content.Add(container);

            // Add choice button
            var addBtn = new Button(() => AddChoice()) { text = "+ Add Choice" };
            addBtn.AddToClassList("add-btn");
            _content.Add(addBtn);
        }

        private void AddChoice()
        {
            _node.choices.Add(new DialogueChoice { label = "New choice..." });
            Dirty(refreshEdges: true);
            Rebuild();
        }

        private void RemoveChoice(int index)
        {
            if (index < 0 || index >= _node.choices.Count) return;
            _node.choices.RemoveAt(index);
            Dirty(refreshEdges: true);
            Rebuild();
        }

        // ── Conditions ────────────────────────────────────────────────────────

        private void RebuildConditionList(List<DialogueCondition> conditions,
                                          string addLabel,
                                          VisualElement parent = null)
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
                int  idx  = i;
                var  cond = conditions[i];
                var  row  = new VisualElement();
                row.AddToClassList("condition-row");

                // Type dropdown
                var typeEnum = new EnumField(cond.type);
                typeEnum.AddToClassList("condition-type");
                typeEnum.RegisterValueChangedCallback(e =>
                {
                    cond.type = (ConditionType)e.newValue;
                    Dirty();
                });
                row.Add(typeEnum);

                // Key
                // var keyField = new TextField { value = cond.key, placeholderText = "key" };
                var keyField = new TextField { value = cond.key};
                keyField.AddToClassList("condition-field");
                keyField.RegisterValueChangedCallback(e => { cond.key = e.newValue; Dirty(); });
                row.Add(keyField);

                // Value
                // var valField = new TextField { value = cond.value, placeholderText = "value" };
                var valField = new TextField { value = cond.value};
                valField.AddToClassList("condition-field");
                valField.RegisterValueChangedCallback(e => { cond.value = e.newValue; Dirty(); });
                row.Add(valField);

                // Negate
                var negateToggle = new Toggle("¬") { value = cond.negate };
                negateToggle.AddToClassList("condition-negate");
                negateToggle.RegisterValueChangedCallback(e => { cond.negate = e.newValue; Dirty(); });
                row.Add(negateToggle);

                // Remove
                var removeBtn = new Button(() =>
                {
                    conditions.RemoveAt(idx);
                    Dirty();
                    Rebuild();
                }) { text = "✕" };
                removeBtn.AddToClassList("remove-btn");
                row.Add(removeBtn);

                parent.Add(row);
            }

            var addCondBtn = new Button(() =>
            {
                conditions.Add(new DialogueCondition());
                Dirty();
                Rebuild();
            }) { text = $"+ {addLabel}" };
            addCondBtn.AddToClassList("add-btn-small");
            parent.Add(addCondBtn);
        }

        // ── Field helpers ─────────────────────────────────────────────────────

        private void AddSection(string title)
        {
            var sectionLabel = new Label(title);
            sectionLabel.AddToClassList("inspector-section");
            _content.Add(sectionLabel);
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

        private void AddTextAreaField(string label, string value, Action<string> onChange)
        {
            var lbl = new Label(label);
            lbl.AddToClassList("inspector-field-label");
            _content.Add(lbl);

            var field = new TextField { value = value ?? string.Empty, multiline = true };
            field.AddToClassList("inspector-textarea");
            field.RegisterValueChangedCallback(e => onChange(e.newValue));
            _content.Add(field);
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

        private void AddInlineTextField(VisualElement parent, string label, string value, Action<string> onChange)
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

        // ── State helpers ─────────────────────────────────────────────────────

        private void ShowEmpty()
        {
            _emptyLabel.style.display = DisplayStyle.Flex;
            _scroll.style.display     = DisplayStyle.None;
        }

        private void Dirty(bool refreshEdges = false)
        {
            OnDataChanged?.Invoke();
            // if (refreshEdges) _nodeView?.OnNodeDataChanged?.Invoke();
            if (refreshEdges) _nodeView?.TriggerNodeDataChange();
        }
    }
}
