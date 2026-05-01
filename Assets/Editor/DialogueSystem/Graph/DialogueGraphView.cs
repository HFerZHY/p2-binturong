using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using DialogueSystem.Data;

namespace DialogueSystem.Editor
{
    /// <summary>
    /// The central GraphView canvas.  Renders DialogueNodeView boxes, wires
    /// edges between them (including back-edges for loops), and owns the
    /// add-node / delete-node surface-level commands.
    ///
    /// It does NOT know about the inspector panel or toolbar — those live in
    /// DialogueGraphEditorWindow and communicate via the events below.
    /// </summary>
    public class DialogueGraphView : GraphView
    {
        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Raised whenever a node is selected on the canvas.</summary>
        public event Action<DialogueNodeView> OnNodeSelected;

        /// <summary>Raised whenever the selection is cleared.</summary>
        public event Action OnSelectionCleared;

        // ── State ─────────────────────────────────────────────────────────────

        private DialogueGraph        _graph;
        private DialogueGraphEditorData _editorData;

        // Stable map: nodeId → view, used when wiring edges and syncing positions.
        private readonly Dictionary<string, DialogueNodeView> _nodeViews = new();

        // ── Constructor ───────────────────────────────────────────────────────

        public DialogueGraphView()
        {
            // Zoom
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            // Manipulation
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // Background grid
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            // Style
            styleSheets.Add(DialogueEditorResources.GraphViewStyle);

            // Context menu
            this.AddManipulator(new ContextualMenuManipulator(BuildContextMenu));

            // Track node moves to persist positions
            graphViewChanged += OnGraphViewChanged;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Populates the canvas from <paramref name="graph"/>.
        /// Clears any existing content first.
        /// </summary>
        public void Populate(DialogueGraph graph, DialogueGraphEditorData editorData)
        {
            _graph      = graph;
            _editorData = editorData;

            ClearGraph();

            if (graph == null) return;

            // Pass 1 — create all node views
            foreach (var node in graph.nodes)
            {
                Vector2 pos = editorData.GetPosition(node.id);
                CreateNodeView(node, pos);
            }

            // Pass 2 — wire edges (after all views exist so ports are ready)
            foreach (var node in graph.nodes)
                ConnectNode(node);

            FrameAll();
        }

        /// <summary>
        /// Adds a brand-new node to the canvas and the backing graph.
        /// </summary>
        public DialogueNodeView AddNode(NodeType type, Vector2 canvasPosition)
        {
            Undo.RecordObject(_graph, "Add Dialogue Node");

            var node = new DialogueNode
            {
                id          = GenerateUniqueId(),
                nodeType    = type,
                speakerName = type == NodeType.Line ? "Speaker" : string.Empty,
                text        = type == NodeType.Line ? "..." : string.Empty,
                typewriterSpeed = 0.03f
            };

            _graph.nodes.Add(node);
            _graph.BuildLookup();

            _editorData.SetPosition(node.id, canvasPosition);

            EditorUtility.SetDirty(_graph);
            EditorUtility.SetDirty(_editorData);

            var view = CreateNodeView(node, canvasPosition);
            return view;
        }

        /// <summary>
        /// Removes a node view and its backing data from the graph.
        /// </summary>
        public void DeleteNode(DialogueNodeView nodeView)
        {
            Undo.RecordObject(_graph, "Delete Dialogue Node");

            // Remove all edges connected to this node
            var edgesToRemove = edges.ToList()
                .Where(e => e.input?.node == nodeView || e.output?.node == nodeView)
                .ToList();

            foreach (var edge in edgesToRemove)
            {
                edge.input?.Disconnect(edge);
                edge.output?.Disconnect(edge);
                RemoveElement(edge);
            }

            // Clear dangling nextNodeId / targetNodeId references in other nodes
            foreach (var n in _graph.nodes)
            {
                if (n.nextNodeId == nodeView.NodeData.id) n.nextNodeId = string.Empty;
                foreach (var c in n.choices)
                    if (c.targetNodeId == nodeView.NodeData.id) c.targetNodeId = string.Empty;
            }

            _graph.nodes.Remove(nodeView.NodeData);
            _graph.BuildLookup();
            _editorData.RemovePosition(nodeView.NodeData.id);
            _nodeViews.Remove(nodeView.NodeData.id);

            RemoveElement(nodeView);

            EditorUtility.SetDirty(_graph);
            EditorUtility.SetDirty(_editorData);
        }

        /// <summary>
        /// Re-wires all edges from scratch (called after inspector edits
        /// change nextNodeId or choice targetNodeIds).
        /// </summary>
        public void RefreshEdges()
        {
            // Remove all existing edges
            foreach (var edge in edges.ToList())
            {
                edge.input?.Disconnect(edge);
                edge.output?.Disconnect(edge);
                RemoveElement(edge);
            }

            // Rebuild ports on each node view to match current data, then rewire
            foreach (var kv in _nodeViews)
                kv.Value.RebuildPorts();

            if (_graph == null) return;
            foreach (var node in _graph.nodes)
                ConnectNode(node);
        }

        /// <summary>
        /// Persists all current node canvas positions into _editorData.
        /// </summary>
        public void FlushPositions()
        {
            foreach (var kv in _nodeViews)
                _editorData.SetPosition(kv.Key, kv.Value.GetPosition().position);
        }

        /// <summary>
        /// Runs a simple BFS left-to-right auto-layout from the entry node.
        /// </summary>
        public void AutoLayout()
        {
            if (_graph == null || _graph.nodes.Count == 0) return;

            const float colWidth  = 280f;
            const float rowHeight = 180f;
            const float startX    = 80f;
            const float startY    = 80f;

            var visited   = new HashSet<string>();
            var colItems  = new Dictionary<int, int>(); // column → item count
            var queue     = new Queue<(string id, int col)>();

            string entryId = _graph.entryNodeId;
            if (string.IsNullOrEmpty(entryId) && _graph.nodes.Count > 0)
                entryId = _graph.nodes[0].id;

            queue.Enqueue((entryId, 0));
            visited.Add(entryId);

            while (queue.Count > 0)
            {
                var (id, col) = queue.Dequeue();
                if (!_nodeViews.TryGetValue(id, out var view)) continue;

                int row = colItems.TryGetValue(col, out int r) ? r : 0;
                colItems[col] = row + 1;

                var pos = new Vector2(startX + col * colWidth, startY + row * rowHeight);
                view.SetPosition(new Rect(pos, view.GetPosition().size));
                _editorData.SetPosition(id, pos);

                // Enqueue successors
                var node = view.NodeData;
                void TryEnqueue(string nextId)
                {
                    if (!string.IsNullOrEmpty(nextId) && !visited.Contains(nextId))
                    {
                        visited.Add(nextId);
                        queue.Enqueue((nextId, col + 1));
                    }
                }

                TryEnqueue(node.nextNodeId);
                foreach (var choice in node.choices) TryEnqueue(choice.targetNodeId);
            }

            // Any nodes not reached (disconnected) get placed in a trailing column
            int maxCol = colItems.Count > 0 ? colItems.Keys.Max() + 2 : 0;
            int orphanRow = 0;
            foreach (var node in _graph.nodes)
            {
                if (visited.Contains(node.id)) continue;
                if (!_nodeViews.TryGetValue(node.id, out var view)) continue;
                var pos = new Vector2(startX + maxCol * colWidth, startY + orphanRow * rowHeight);
                view.SetPosition(new Rect(pos, view.GetPosition().size));
                _editorData.SetPosition(node.id, pos);
                orphanRow++;
            }

            EditorUtility.SetDirty(_editorData);
            FrameAll();
        }

        // ── GraphView overrides ───────────────────────────────────────────────

        /// <summary>
        /// Controls which port pairs can be connected.
        /// Output ports connect to input ports only; no self-loops on same port.
        /// Back-edges (loops) are fully allowed.
        /// </summary>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(p =>
                p.direction != startPort.direction &&
                p.node      != startPort.node  // disallow same-node connections
            ).ToList();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private DialogueNodeView CreateNodeView(DialogueNode node, Vector2 position)
        {
            var view = new DialogueNodeView(node, _graph);
            view.SetPosition(new Rect(position, Vector2.zero));
            view.OnNodeDataChanged += () =>
            {
                EditorUtility.SetDirty(_graph);
                RefreshEdges();
            };
            view.RegisterCallback<MouseDownEvent>(_ =>
            {
                OnNodeSelected?.Invoke(view);
            });

            AddElement(view);
            _nodeViews[node.id] = view;
            return view;
        }

        private void ConnectNode(DialogueNode node)
        {
            if (!_nodeViews.TryGetValue(node.id, out var srcView)) return;

            if (node.nodeType == NodeType.Line && !string.IsNullOrEmpty(node.nextNodeId))
            {
                ConnectPorts(srcView.OutputPort, node.nextNodeId);
            }
            else if (node.nodeType == NodeType.Branch)
            {
                var choicePorts = srcView.ChoiceOutputPorts;
                for (int i = 0; i < node.choices.Count && i < choicePorts.Count; i++)
                {
                    string targetId = node.choices[i].targetNodeId;
                    if (!string.IsNullOrEmpty(targetId))
                        ConnectPorts(choicePorts[i], targetId);
                }
            }
        }

        private void ConnectPorts(Port outputPort, string targetNodeId)
        {
            if (outputPort == null) return;
            if (!_nodeViews.TryGetValue(targetNodeId, out var targetView)) return;

            Port inputPort = targetView.InputPort;
            if (inputPort == null) return;

            var edge = outputPort.ConnectTo(inputPort);
            AddElement(edge);
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            // Sync positions when nodes are moved
            if (change.movedElements != null)
            {
                foreach (var el in change.movedElements)
                {
                    if (el is DialogueNodeView nv)
                        _editorData?.SetPosition(nv.NodeData.id, nv.GetPosition().position);
                }
                if (_editorData != null)
                    EditorUtility.SetDirty(_editorData);
            }

            // Handle edge connections — update backing data
            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                    ApplyEdgeConnection(edge, connect: true);
            }

            // Handle edge deletions — clear backing data
            if (change.elementsToRemove != null)
            {
                foreach (var el in change.elementsToRemove)
                {
                    if (el is Edge edge)
                        ApplyEdgeConnection(edge, connect: false);

                    if (el is DialogueNodeView nv)
                    {
                        // Deletion from the keyboard Delete key
                        DeleteNode(nv);
                    }
                }
            }

            return change;
        }

        private void ApplyEdgeConnection(Edge edge, bool connect)
        {
            if (edge.output?.node is not DialogueNodeView srcView) return;
            if (edge.input?.node  is not DialogueNodeView dstView) return;

            string targetId = connect ? dstView.NodeData.id : string.Empty;

            // Line node — simple nextNodeId
            if (srcView.NodeData.nodeType == NodeType.Line)
            {
                Undo.RecordObject(_graph, connect ? "Connect Edge" : "Disconnect Edge");
                srcView.NodeData.nextNodeId = targetId;
                EditorUtility.SetDirty(_graph);
                return;
            }

            // Branch node — find the choice whose port this is
            if (srcView.NodeData.nodeType == NodeType.Branch)
            {
                var ports = srcView.ChoiceOutputPorts;
                for (int i = 0; i < ports.Count; i++)
                {
                    if (ports[i] == edge.output)
                    {
                        Undo.RecordObject(_graph, connect ? "Connect Choice Edge" : "Disconnect Choice Edge");
                        if (i < srcView.NodeData.choices.Count)
                            srcView.NodeData.choices[i].targetNodeId = targetId;
                        EditorUtility.SetDirty(_graph);
                        return;
                    }
                }
            }
        }

        private void ClearGraph()
        {
            foreach (var edge in edges.ToList())   RemoveElement(edge);
            foreach (var node in nodes.ToList())   RemoveElement(node);
            _nodeViews.Clear();
        }

        private void BuildContextMenu(ContextualMenuPopulateEvent evt)
        {
            Vector2 mousePos = contentViewContainer.WorldToLocal(evt.mousePosition);
            evt.menu.AppendAction("Add Node/Line",     _ => AddNode(NodeType.Line,     mousePos));
            evt.menu.AppendAction("Add Node/Branch",   _ => AddNode(NodeType.Branch,   mousePos));
            evt.menu.AppendAction("Add Node/Terminal", _ => AddNode(NodeType.Terminal, mousePos));
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Auto Layout", _ => AutoLayout());
        }

        private static int _idCounter = 0;

        private string GenerateUniqueId()
        {
            string candidate;
            var existing = new HashSet<string>(_graph.nodes.Select(n => n.id));
            do { candidate = $"node_{++_idCounter:D4}"; }
            while (existing.Contains(candidate));
            return candidate;
        }
    }
}
