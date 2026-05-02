using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using DialogueSystem.Data;

namespace DialogueSystem.Editor
{
    /// <summary>
    /// A single node box on the dialogue graph canvas.
    ///
    /// The canvas body is read-only for localized text — it shows:
    ///   • The raw key pill (speakerNameKey / textKey) stored on DialogueNode
    ///   • A resolved preview label for the currently selected locale
    ///
    /// All editing of localized strings happens in DialogueNodeInspectorPanel.
    /// Flow fields (nextNodeId, choices) and the entry-node badge are set here
    /// and in the inspector panel.
    ///
    /// Public surface for the inspector panel:
    ///   TriggerNodeDataChange() — signals graph view to refresh edges
    ///   RefreshLocalePreview()  — re-resolves preview labels from StringTable
    /// </summary>
    public class DialogueNodeView : Node
    {
        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Raised when flow data changes (nextNodeId, choice list) requiring edge refresh.</summary>
        public event Action OnNodeDataChanged;

        // ── Data ──────────────────────────────────────────────────────────────

        public DialogueNode        NodeData    { get; private set; }
        public DialogueGraph       GraphData   { get; private set; }
        public DialogueLocaleState LocaleState { get; private set; }

        // ── Ports ─────────────────────────────────────────────────────────────

        public Port       InputPort         { get; private set; }
        public Port       OutputPort        { get; private set; }
        public List<Port> ChoiceOutputPorts { get; private set; } = new();

        // ── Internal UI refs (updated on locale switch) ───────────────────────

        private Label _speakerPreviewLabel;
        private Label _textPreviewLabel;
        private Label _entryBadge;          // shown only when this is the entry node

        // Choice port preview labels — one per choice, in order.
        // Updated by RefreshLocalePreview so port names show the resolved label.
        private readonly List<Label> _choicePortLabels = new();

        // ── Constructor ───────────────────────────────────────────────────────

        public DialogueNodeView(DialogueNode node, DialogueGraph graph, DialogueLocaleState localeState)
        {
            NodeData    = node;
            GraphData   = graph;
            LocaleState = localeState;

            Build();

            if (localeState != null)
            {
                localeState.OnLocaleChanged += RefreshLocalePreview;
                localeState.OnSpeakerValueChanged += RefreshIfSpeakerChanged;
            }

            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                if (localeState != null)
                {
                    localeState.OnLocaleChanged -= RefreshLocalePreview;
                    localeState.OnSpeakerValueChanged -= RefreshIfSpeakerChanged;
                }
            });
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Called by DialogueNodeInspectorPanel when flow data changes.
        /// Raises OnNodeDataChanged so DialogueGraphView can refresh edges.
        /// </summary>
        public void TriggerNodeDataChange() => OnNodeDataChanged?.Invoke();

        /// <summary>
        /// Re-resolves preview labels from the StringTable for the active locale.
        /// Does NOT rebuild ports or layout — O(1) label updates only.
        /// For Branch nodes, updates each choice port's display name from the
        /// resolved choice label rather than the raw labelKey.
        /// </summary>
        public void RefreshLocalePreview()
        {
            UpdatePreview(_speakerPreviewLabel,
                LocaleState?.ResolveSpeaker(NodeData.speakerNameKey));
            UpdatePreview(_textPreviewLabel,
                LocaleState?.ResolveText(NodeData.textKey));

            // Refresh choice port labels with the resolved locale string
            for (int i = 0; i < _choicePortLabels.Count && i < NodeData.choices.Count; i++)
            {
                var choice = NodeData.choices[i];
                string resolved = string.IsNullOrEmpty(choice.labelKey)
                    ? null
                    : LocaleState?.ResolveChoiceLabel(choice.labelKey);
                string display = !string.IsNullOrEmpty(resolved)
                    ? Truncate(resolved, 28)
                    : (!string.IsNullOrEmpty(choice.labelKey)
                        ? Truncate(choice.labelKey, 28)   // fallback: show raw key
                        : $"Choice {i + 1}");

                var label = _choicePortLabels[i];
                if (label != null)
                    label.text = display;
            }
        }

        /// <summary>
        /// Shows or hides the "ENTRY" badge. Called by DialogueGraphView
        /// after the entry node changes.
        /// </summary>
        public void RefreshEntryBadge()
        {
            bool isEntry = GraphData != null && GraphData.entryNodeId == NodeData.id;
            if (_entryBadge != null)
                _entryBadge.style.display = isEntry ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RefreshIfSpeakerChanged(string speakerKey)
        {
            if (speakerKey == NodeData.speakerNameKey)
                RefreshLocalePreview();
        }

        // ── Build ─────────────────────────────────────────────────────────────

        private void Build()
        {
            title = string.IsNullOrEmpty(NodeData.id) ? "(no id)" : NodeData.id;

            AddToClassList(NodeData.nodeType switch
            {
                NodeType.Line     => "node-line",
                NodeType.Branch   => "node-branch",
                NodeType.Terminal => "node-terminal",
                _                 => "node-line"
            });

            // Type badge
            var typeBadge = new Label(NodeData.nodeType.ToString().ToUpper());
            typeBadge.AddToClassList("node-type-badge");
            titleContainer.Add(typeBadge);

            // Entry node badge — visible only when this is the graph entry
            _entryBadge = new Label("ENTRY");
            _entryBadge.AddToClassList("node-entry-badge");
            titleContainer.Add(_entryBadge);
            RefreshEntryBadge();

            InputPort = CreateInputPort();
            inputContainer.Add(InputPort);

            BuildBody();
            RebuildOutputPorts();

            RefreshExpandedState();
            RefreshPorts();
        }

        // ── Body ──────────────────────────────────────────────────────────────

        private void BuildBody()
        {
            extensionContainer.Clear();
            _speakerPreviewLabel = null;
            _textPreviewLabel    = null;

            if (NodeData.nodeType == NodeType.Terminal)
            {
                var lbl = new Label("[ End of conversation ]");
                lbl.AddToClassList("terminal-label");
                extensionContainer.Add(lbl);
                return;
            }

            if (NodeData.nodeType == NodeType.Line)
            {
                // ── Speaker ───────────────────────────────────────────────────
                var speakerRow = MakeKeyRow("Speaker", NodeData.speakerNameKey);
                extensionContainer.Add(speakerRow);

                _speakerPreviewLabel = new Label();
                _speakerPreviewLabel.AddToClassList("node-locale-preview");
                _speakerPreviewLabel.AddToClassList("node-speaker-preview");
                extensionContainer.Add(_speakerPreviewLabel);

                var divider = new VisualElement();
                divider.AddToClassList("node-divider");
                extensionContainer.Add(divider);

                // ── Text ──────────────────────────────────────────────────────
                var textRow = MakeKeyRow("Text", NodeData.textKey);
                extensionContainer.Add(textRow);

                _textPreviewLabel = new Label();
                _textPreviewLabel.AddToClassList("node-locale-preview");
                _textPreviewLabel.AddToClassList("node-text-preview");
                extensionContainer.Add(_textPreviewLabel);

                RefreshLocalePreview();
            }

            if (NodeData.nodeType == NodeType.Branch)
            {
                var lbl = new Label("Branch — connect choices →");
                lbl.AddToClassList("branch-hint-label");
                extensionContainer.Add(lbl);
            }
        }

        private static VisualElement MakeKeyRow(string fieldLabel, string key)
        {
            var row = new VisualElement();
            row.AddToClassList("node-row");
            var label = new Label(fieldLabel);
            label.AddToClassList("node-field-label");
            row.Add(label);
            var pill = new Label(string.IsNullOrEmpty(key) ? "(no key)" : key);
            pill.AddToClassList("node-key-pill");
            row.Add(pill);
            return row;
        }

        private static void UpdatePreview(Label label, string resolved)
        {
            if (label == null) return;
            bool missing = string.IsNullOrEmpty(resolved);
            label.text = missing ? "(no translation)" : resolved;
            label.EnableInClassList("node-locale-preview--missing", missing);
        }

        // ── Ports ─────────────────────────────────────────────────────────────

        public void RebuildPorts()
        {
            foreach (var port in ChoiceOutputPorts)
            {
                port.DisconnectAll();
                outputContainer.Remove(port);
            }
            ChoiceOutputPorts.Clear();
            _choicePortLabels.Clear();

            if (OutputPort != null)
            {
                OutputPort.DisconnectAll();
                outputContainer.Remove(OutputPort);
                OutputPort = null;
            }

            RebuildOutputPorts();
            RefreshPorts();
            RefreshExpandedState();
        }

        private void RebuildOutputPorts()
        {
            switch (NodeData.nodeType)
            {
                case NodeType.Line:
                    OutputPort = CreateOutputPort("▶");
                    outputContainer.Add(OutputPort);
                    break;

                case NodeType.Branch:
                    for (int i = 0; i < NodeData.choices.Count; i++)
                    {
                        var choice = NodeData.choices[i];

                        // Resolve the localized label for the port name.
                        // If the table returns nothing yet, fall back to the raw labelKey,
                        // then to the ordinal "Choice N".
                        string resolved = string.IsNullOrEmpty(choice.labelKey)
                            ? null
                            : LocaleState?.ResolveChoiceLabel(choice.labelKey);
                        string portName = !string.IsNullOrEmpty(resolved)
                            ? Truncate(resolved, 28)
                            : (!string.IsNullOrEmpty(choice.labelKey)
                                ? Truncate(choice.labelKey, 28)
                                : $"Choice {i + 1}");

                        var port = CreateOutputPort(portName);
                        port.AddToClassList("choice-port");
                        outputContainer.Add(port);
                        ChoiceOutputPorts.Add(port);

                        // Keep a reference to the portName label so RefreshLocalePreview
                        // can update it without a full port rebuild.
                        // The portName Label is the first Label child of the port.
                        Label portLabel = port.Q<Label>();
                        _choicePortLabels.Add(portLabel);
                    }
                    break;
            }
        }

        private Port CreateInputPort()
        {
            var port = Port.Create<Edge>(
                Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            port.portName = "In";
            port.AddToClassList("dialogue-port");
            return port;
        }

        private Port CreateOutputPort(string portName)
        {
            var port = Port.Create<Edge>(
                Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            port.portName = portName;
            port.AddToClassList("dialogue-port");
            return port;
        }

        private static string Truncate(string s, int maxLen) =>
            s.Length <= maxLen ? s : s[..maxLen] + "…";
    }
}
