using System.IO;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using DialogueSystem.Data;
using DialogueSystem.Serialization;
using UnityEditor.UIElements;

namespace DialogueSystem.Editor
{
    /// <summary>
    /// The main EditorWindow for the Dialogue Graph editor.
    ///
    /// Layout:
    ///   ┌─────────────────────────────────────────────────────┐
    ///   │  [Toolbar]                                           │
    ///   ├──────────────────────────────────┬──────────────────┤
    ///   │                                  │                  │
    ///   │         GraphView canvas         │  Inspector Panel │
    ///   │         (fills remaining space)  │  (320 px wide)   │
    ///   │                                  │                  │
    ///   ├──────────────────────────────────┴──────────────────┤
    ///   │  [Validation / Status Bar]                          │
    ///   └─────────────────────────────────────────────────────┘
    ///
    /// Open via:  Tools > Dialogue System > Open Graph Editor
    ///       or:  double-click a DialogueGraph asset
    /// </summary>
    public class DialogueGraphEditorWindow : EditorWindow
    {
        // ── Menu items ────────────────────────────────────────────────────────

        [MenuItem("Tools/Dialogue System/Open Graph Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<DialogueGraphEditorWindow>();
            window.titleContent = new GUIContent("Dialogue Graph", EditorGUIUtility.IconContent("d_NetworkAnimator Icon").image);
            window.minSize = new Vector2(900, 600);
            window.Show();
        }

        /// <summary>
        /// Called by the custom DialogueGraph inspector's "Open in Editor" button.
        /// </summary>
        public static void OpenWithGraph(DialogueGraph graph)
        {
            var window = GetWindow<DialogueGraphEditorWindow>();
            window.titleContent = new GUIContent("Dialogue Graph");
            window.minSize = new Vector2(900, 600);
            window.LoadGraph(graph);
            window.Show();
        }

        // ── State ─────────────────────────────────────────────────────────────

        private DialogueGraph            _graph;
        private DialogueGraphEditorData  _editorData;

        private DialogueGraphView         _graphView;
        private DialogueNodeInspectorPanel _inspectorPanel;
        private DialogueValidationPanel    _validationPanel;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            BuildUI();

            // If we already had a graph open (after recompile), restore it
            if (_graph != null)
                LoadGraph(_graph);
        }

        private void OnDisable()
        {
            SaveAsset();
        }

        // ── UI Construction ───────────────────────────────────────────────────

        private void BuildUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.styleSheets.Add(DialogueEditorResources.WindowStyle);
            rootVisualElement.AddToClassList("editor-window-root");

            // Toolbar
            rootVisualElement.Add(BuildToolbar());

            // Main area (canvas + inspector side-panel)
            var mainArea = new TwoPaneSplitView(0, 320f, TwoPaneSplitViewOrientation.Horizontal);
            mainArea.AddToClassList("main-area");

            // IMPORTANT: TwoPaneSplitView renders first child as the "fixed" pane.
            // We add the inspector first so it is the fixed 320 px pane on the right,
            // and the graph view fills remaining space on the left.
            // (We reverse the order by using the second overload that anchors on the right.)
            var mainAreaRev = new TwoPaneSplitView(1, 320f, TwoPaneSplitViewOrientation.Horizontal);
            mainAreaRev.AddToClassList("main-area");

            _graphView = new DialogueGraphView();
            _graphView.AddToClassList("graph-view-fill");
            _graphView.OnNodeSelected    += OnNodeSelected;
            _graphView.OnSelectionCleared += OnSelectionCleared;

            _inspectorPanel = new DialogueNodeInspectorPanel();
            _inspectorPanel.OnDataChanged += OnInspectorDataChanged;

            mainAreaRev.Add(_graphView);
            mainAreaRev.Add(_inspectorPanel);
            rootVisualElement.Add(mainAreaRev);

            // Validation bar at the bottom
            _validationPanel = new DialogueValidationPanel();
            rootVisualElement.Add(_validationPanel);
        }

        private VisualElement BuildToolbar()
        {
            var toolbar = new UnityEditor.UIElements.Toolbar();
            toolbar.AddToClassList("editor-toolbar");

            // Graph object picker
            var graphField = new UnityEditor.UIElements.ObjectField("Graph")
            {
                objectType   = typeof(DialogueGraph),
                allowSceneObjects = false,
                value        = _graph
            };
            graphField.AddToClassList("toolbar-graph-field");
            graphField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue is DialogueGraph g) LoadGraph(g);
                else UnloadGraph();
            });
            toolbar.Add(graphField);

            toolbar.Add(new UnityEditor.UIElements.ToolbarSpacer());

            // Add node dropdown
            var addMenu = new ToolbarMenu { text = "+ Add Node" };
            addMenu.AddToClassList("toolbar-btn");
            addMenu.menu.AppendAction("Line",     _ => AddNode(NodeType.Line));
            addMenu.menu.AppendAction("Branch",   _ => AddNode(NodeType.Branch));
            addMenu.menu.AppendAction("Terminal", _ => AddNode(NodeType.Terminal));
            toolbar.Add(addMenu);

            toolbar.Add(new UnityEditor.UIElements.ToolbarSpacer());

            // Auto layout
            AddToolbarButton(toolbar, "Auto Layout", () => _graphView?.AutoLayout());

            // Validate
            AddToolbarButton(toolbar, "Validate", Validate);

            toolbar.Add(new UnityEditor.UIElements.ToolbarSpacer());

            // Import JSON
            AddToolbarButton(toolbar, "Import JSON", ImportJson);

            // Export JSON
            AddToolbarButton(toolbar, "Export JSON", ExportJson);

            toolbar.Add(new UnityEditor.UIElements.ToolbarSpacer());

            // Save
            AddToolbarButton(toolbar, "💾 Save", SaveAsset);

            return toolbar;
        }

        private static void AddToolbarButton(UnityEditor.UIElements.Toolbar toolbar, string label, System.Action onClick)
        {
            var btn = new UnityEditor.UIElements.ToolbarButton(onClick) { text = label };
            btn.AddToClassList("toolbar-btn");
            toolbar.Add(btn);
        }

        // ── Load / Unload ─────────────────────────────────────────────────────

        private void LoadGraph(DialogueGraph graph)
        {
            _graph = graph;

            if (_graph == null) { UnloadGraph(); return; }

            _editorData = LoadOrCreateEditorData(graph);
            _graphView.Populate(_graph, _editorData);
            _validationPanel.Refresh(_graph);
        }

        private void UnloadGraph()
        {
            _graph      = null;
            _editorData = null;
            _graphView.Populate(null, null);
            _inspectorPanel.Clear();
            _validationPanel.Refresh(null);
        }

        private static DialogueGraphEditorData LoadOrCreateEditorData(DialogueGraph graph)
        {
            string graphPath  = AssetDatabase.GetAssetPath(graph);
            string dir        = Path.GetDirectoryName(graphPath) ?? "Assets";
            string editorPath = Path.Combine(dir, graph.name + "_EditorData.asset")
                                    .Replace("\\", "/");

            var existing = AssetDatabase.LoadAssetAtPath<DialogueGraphEditorData>(editorPath);
            if (existing != null) return existing;

            var data = ScriptableObject.CreateInstance<DialogueGraphEditorData>();
            AssetDatabase.CreateAsset(data, editorPath);
            AssetDatabase.SaveAssets();
            return data;
        }

        // ── Node operations ───────────────────────────────────────────────────

        private void AddNode(NodeType type)
        {
            if (_graph == null)
            {
                EditorUtility.DisplayDialog("No Graph", "Open or create a DialogueGraph asset first.", "OK");
                return;
            }

            // Place new node near the centre of the visible canvas
            Vector2 centre = _graphView.contentRect.center;
            _graphView.AddNode(type, centre);
            _validationPanel.Refresh(_graph);
        }

        // ── Inspector callbacks ───────────────────────────────────────────────

        private void OnNodeSelected(DialogueNodeView nodeView)
        {
            _inspectorPanel.Inspect(nodeView);
        }

        private void OnSelectionCleared()
        {
            _inspectorPanel.Clear();
        }

        private void OnInspectorDataChanged()
        {
            if (_graph == null) return;
            EditorUtility.SetDirty(_graph);
            _graphView.RefreshEdges();
            _validationPanel.Refresh(_graph);
        }

        // ── Toolbar actions ───────────────────────────────────────────────────

        private void Validate()
        {
            if (_graph == null) return;
            _validationPanel.Refresh(_graph);
#if UNITY_EDITOR
            var errors = _graph.Validate();
            if (errors.Count == 0)
                EditorUtility.DisplayDialog("Validation", "✓ Graph is valid!", "Great");
            else
                EditorUtility.DisplayDialog("Validation Issues",
                    string.Join("\n• ", errors), "OK");
#endif
        }

        private void SaveAsset()
        {
            if (_graph == null) return;
            _graphView.FlushPositions();
            EditorUtility.SetDirty(_graph);
            if (_editorData != null) EditorUtility.SetDirty(_editorData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[DialogueEditor] Saved '{_graph.name}'.");
        }

        private void ImportJson()
        {
            string absolutePath = EditorUtility.OpenFilePanel("Import Dialogue JSON", "", "json");
            if (string.IsNullOrEmpty(absolutePath)) return;

            string json = File.ReadAllText(absolutePath);
            var graph = DialogueJsonLoader.ParseJson(json, Path.GetFileNameWithoutExtension(absolutePath));
            if (graph == null)
            {
                EditorUtility.DisplayDialog("Import Failed", "Could not parse the JSON file. Check the console for details.", "OK");
                return;
            }

            string savePath = EditorUtility.SaveFilePanelInProject(
                "Save Imported Graph", graph.name, "asset", "Choose location for the new DialogueGraph asset");
            if (string.IsNullOrEmpty(savePath)) return;

            AssetDatabase.CreateAsset(graph, savePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            LoadGraph(graph);

            // Auto-layout since there are no stored positions
            _graphView.AutoLayout();
            SaveAsset();
        }

        private void ExportJson()
        {
            if (_graph == null)
            {
                EditorUtility.DisplayDialog("No Graph", "Open a DialogueGraph asset first.", "OK");
                return;
            }

            string defaultName = _graph.name + ".json";
            string absolutePath = EditorUtility.SaveFilePanel("Export Dialogue JSON", "", defaultName, "json");
            if (string.IsNullOrEmpty(absolutePath)) return;

            DialogueJsonExporter.WriteToFile(_graph, absolutePath, prettyPrint: true);
            EditorUtility.RevealInFinder(absolutePath);
        }
    }
}
