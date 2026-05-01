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
    /// Visual structure:
    ///   ┌─ [input port] ── [Header: id · type badge] ──────────────┐
    ///   │  Speaker: ___________                                      │
    ///   │  ┌─────────────────────────────────┐                       │
    ///   │  │ dialogue text (textarea)        │ ── [output port]      │
    ///   │  └─────────────────────────────────┘                       │
    ///   │  (Branch) ┌── choice label ──┐ ── [choice output port]    │
    ///   └───────────────────────────────────────────────────────────┘
    ///
    /// Ports:
    ///   - One INPUT port on the left  (all node types)
    ///   - Line:     one OUTPUT port on the right
    ///   - Branch:   one OUTPUT port per choice on the right
    ///   - Terminal: no output ports
    /// </summary>
    public class DialogueNodeView : Node
    {
        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Raised when any field in NodeData is mutated via this view.</summary>
        public event Action OnNodeDataChanged;

        // ── Data ──────────────────────────────────────────────────────────────

        public DialogueNode  NodeData  { get; private set; }
        public DialogueGraph GraphData { get; private set; }

        // ── Ports ─────────────────────────────────────────────────────────────

        public Port       InputPort        { get; private set; }
        public Port       OutputPort       { get; private set; }    // Line only
        public List<Port> ChoiceOutputPorts { get; private set; } = new();

        // ── Constructor ───────────────────────────────────────────────────────

        public DialogueNodeView(DialogueNode node, DialogueGraph graph)
        {
            NodeData  = node;
            GraphData = graph;
            Build();
        }

        // ── Build ─────────────────────────────────────────────────────────────

        private void Build()
        {
            // Title bar
            title = string.IsNullOrEmpty(NodeData.id) ? "(no id)" : NodeData.id;

            // Color-coded stripe via USS class
            AddToClassList(NodeData.nodeType switch
            {
                NodeType.Line     => "node-line",
                NodeType.Branch   => "node-branch",
                NodeType.Terminal => "node-terminal",
                _                 => "node-line"
            });

            // Type badge in title
            var badge = new Label(NodeData.nodeType.ToString().ToUpper());
            badge.AddToClassList("node-type-badge");
            titleContainer.Add(badge);

            // Input port (all types)
            InputPort = CreateInputPort();
            inputContainer.Add(InputPort);

            // Body fields
            BuildBody();

            // Output ports
            RebuildOutputPorts();

            RefreshExpandedState();
            RefreshPorts();
        }

        // ── Body ──────────────────────────────────────────────────────────────

        private void BuildBody()
        {
            extensionContainer.Clear();

            if (NodeData.nodeType == NodeType.Terminal)
            {
                var lbl = new Label("[ End of conversation ]");
                lbl.AddToClassList("terminal-label");
                extensionContainer.Add(lbl);
                return;
            }

            // Speaker name
            if (NodeData.nodeType == NodeType.Line)
            {
                var speakerRow = new VisualElement();
                speakerRow.AddToClassList("node-row");

                var speakerLabel = new Label("Speaker");
                speakerLabel.AddToClassList("node-field-label");

                var speakerField = new TextField { value = NodeData.speakerName };
                speakerField.AddToClassList("node-field");
                speakerField.RegisterValueChangedCallback(e =>
                {
                    NodeData.speakerName = e.newValue;
                    OnNodeDataChanged?.Invoke();
                });

                speakerRow.Add(speakerLabel);
                speakerRow.Add(speakerField);
                extensionContainer.Add(speakerRow);

                // Dialogue text
                var textField = new TextField
                {
                    value     = NodeData.text,
                    multiline = true
                };
                textField.AddToClassList("node-text-field");
                textField.RegisterValueChangedCallback(e =>
                {
                    NodeData.text = e.newValue;
                    OnNodeDataChanged?.Invoke();
                });
                extensionContainer.Add(textField);
            }

            if (NodeData.nodeType == NodeType.Branch)
            {
                var lbl = new Label("Branch — connect choices →");
                lbl.AddToClassList("branch-hint-label");
                extensionContainer.Add(lbl);
            }
        }

        // ── Ports ─────────────────────────────────────────────────────────────

        /// <summary>Recreates output ports to match current NodeData.choices. Called by GraphView after edits.</summary>
        public void RebuildPorts()
        {
            // Disconnect and remove existing output ports/edges
            foreach (var port in ChoiceOutputPorts)
            {
                port.DisconnectAll();
                outputContainer.Remove(port);
            }
            ChoiceOutputPorts.Clear();

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
                        string label = string.IsNullOrEmpty(NodeData.choices[i].label)
                            ? $"Choice {i + 1}"
                            : Truncate(NodeData.choices[i].label, 28);

                        var choicePort = CreateOutputPort(label);
                        choicePort.AddToClassList("choice-port");
                        outputContainer.Add(choicePort);
                        ChoiceOutputPorts.Add(choicePort);
                    }
                    break;

                case NodeType.Terminal:
                    // No output ports
                    break;
            }
        }

        private Port CreateInputPort()
        {
            var port = Port.Create<Edge>(
                Orientation.Horizontal,
                Direction.Input,
                Port.Capacity.Multi,   // multiple edges can enter (for loops / merges)
                typeof(bool));
            port.portName = "In";
            port.AddToClassList("dialogue-port");
            return port;
        }

        private Port CreateOutputPort(string portName)
        {
            var port = Port.Create<Edge>(
                Orientation.Horizontal,
                Direction.Output,
                Port.Capacity.Single,
                typeof(bool));
            port.portName = portName;
            port.AddToClassList("dialogue-port");
            return port;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string Truncate(string s, int maxLen) =>
            s.Length <= maxLen ? s : s[..maxLen] + "…";

        public void TriggerNodeDataChange()
        {
            OnNodeDataChanged.Invoke();
        }
    }
}
