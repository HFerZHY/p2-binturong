using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using DialogueSystem.Data;
using DialogueSystem.Serialization;

namespace DialogueSystem.Editor
{
    /// <summary>
    /// The main EditorWindow for the Dialogue Graph editor.
    ///
    /// Layout:
    ///   ┌─────────────────────────────────────────────────────┐
    ///   │  [Toolbar — graph picker | locale dropdown | ...]   │
    ///   ├──────────────────────────────────┬──────────────────┤
    ///   │                                  │                  │
    ///   │         GraphView canvas         │  Inspector Panel │
    ///   │         (all nodes show text     │  (all locales    │
    ///   │          for selected locale)    │   stacked)       │
    ///   │                                  │                  │
    ///   ├──────────────────────────────────┴──────────────────┤
    ///   │  [Validation / Status Bar]                          │
    ///   └─────────────────────────────────────────────────────┘
    ///
    /// Locale state is owned here and passed down to the graph view
    /// and inspector panel on construction. The dropdown in the toolbar
    /// mutates DialogueLocaleState.ActiveLocale, which fires an event
    /// that every DialogueNodeView subscribes to for live preview refresh.
    ///
    /// On Save:
    ///   1. Graph asset and editor data are marked dirty and saved.
    ///   2. LocalizationTableService.SaveAll() flushes any StringTable
    ///      edits made in the inspector panel to disk.
    /// </summary>
    public class DialogueGraphEditorWindow : EditorWindow
    {
        // ── Menu items ────────────────────────────────────────────────────────

        [MenuItem("Tools/Dialogue System/Open Graph Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<DialogueGraphEditorWindow>();
            window.titleContent = new GUIContent("Dialogue Graph",
                EditorGUIUtility.IconContent("d_NetworkAnimator Icon").image);
            window.minSize = new Vector2(960, 600);
            window.Show();
        }

        public static void OpenWithGraph(DialogueGraph graph)
        {
            var window = GetWindow<DialogueGraphEditorWindow>();
            window.titleContent = new GUIContent("Dialogue Graph");
            window.minSize = new Vector2(960, 600);
            window.LoadGraph(graph);
            window.Show();
        }

        // ── State ─────────────────────────────────────────────────────────────

        private DialogueGraph           _graph;
        private DialogueGraphEditorData _editorData;

        // Locale state is owned by the window; passed as a reference to children.
        private DialogueLocaleState _localeState;

        private DialogueGraphView          _graphView;
        private DialogueNodeInspectorPanel _inspectorPanel;
        private DialogueValidationPanel    _validationPanel;

        // Toolbar dropdown reference — rebuilt when locales change
        private DropdownField _localeDropdown;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            // Create locale state first — children need it during UI construction
            _localeState = new DialogueLocaleState();

            BuildUI();

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

            rootVisualElement.Add(BuildToolbar());

            // Graph view + inspector split (inspector anchored right at 320 px)
            var split = new TwoPaneSplitView(1, 320f, TwoPaneSplitViewOrientation.Horizontal);
            split.AddToClassList("main-area");

            _graphView = new DialogueGraphView();
            _graphView.AddToClassList("graph-view-fill");
            _graphView.OnNodeSelected     += OnNodeSelected;
            _graphView.OnSelectionCleared += OnSelectionCleared;

            // Inspector panel receives the locale state so it can enumerate locales
            // and resolve previews. Character management is handled by CharacterInspector.
            _inspectorPanel = new DialogueNodeInspectorPanel(_localeState);
            _inspectorPanel.OnDataChanged += OnInspectorDataChanged;

            split.Add(_graphView);
            split.Add(_inspectorPanel);
            rootVisualElement.Add(split);

            _validationPanel = new DialogueValidationPanel();
            rootVisualElement.Add(_validationPanel);
        }

        private VisualElement BuildToolbar()
        {
            var toolbar = new UnityEditor.UIElements.Toolbar();
            toolbar.AddToClassList("editor-toolbar");

            // Graph picker
            var graphField = new UnityEditor.UIElements.ObjectField("Graph")
            {
                objectType        = typeof(DialogueGraph),
                allowSceneObjects = false,
                value             = _graph
            };
            graphField.AddToClassList("toolbar-graph-field");
            graphField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue is DialogueGraph g) LoadGraph(g);
                else UnloadGraph();
            });
            toolbar.Add(graphField);

            toolbar.Add(new UnityEditor.UIElements.ToolbarSpacer());

            // ── Locale dropdown ───────────────────────────────────────────────
            BuildLocaleDropdown(toolbar);

            toolbar.Add(new UnityEditor.UIElements.ToolbarSpacer());

            // Add node — ToolbarMenu is in UnityEditor.UIElements
            var addMenu = new ToolbarMenu { text = "+ Add Node" };
            addMenu.AddToClassList("toolbar-btn");
            addMenu.menu.AppendAction("Line",     _ => AddNode(NodeType.Line));
            addMenu.menu.AppendAction("Branch",   _ => AddNode(NodeType.Branch));
            addMenu.menu.AppendAction("Terminal", _ => AddNode(NodeType.Terminal));
            toolbar.Add(addMenu);

            toolbar.Add(new UnityEditor.UIElements.ToolbarSpacer());

            AddToolbarButton(toolbar, "Auto Layout", () => _graphView?.AutoLayout());
            AddToolbarButton(toolbar, "Validate",    Validate);

            toolbar.Add(new UnityEditor.UIElements.ToolbarSpacer());

            AddToolbarButton(toolbar, "Import JSON", ImportJson);
            AddToolbarButton(toolbar, "Export JSON", ExportJson);

            toolbar.Add(new UnityEditor.UIElements.ToolbarSpacer());

            AddToolbarButton(toolbar, "💾 Save", SaveAsset);

            return toolbar;
        }

        private void BuildLocaleDropdown(UnityEditor.UIElements.Toolbar toolbar)
        {
            var localeNames = _localeState.AllLocales
                .Select(l => l.LocaleName)
                .ToList();

            if (localeNames.Count == 0)
            {
                var warn = new UnityEditor.UIElements.ToolbarButton(() => { }) { text = "⚠ No locales" };
                warn.AddToClassList("toolbar-btn");
                warn.AddToClassList("toolbar-btn--warn");
                toolbar.Add(warn);
                return;
            }

            // Label
            var localeLabel = new Label("Preview:");
            localeLabel.AddToClassList("toolbar-locale-label");
            toolbar.Add(localeLabel);

            // Dropdown — value is locale display name
            string initialName = _localeState.ActiveLocale?.LocaleName ?? localeNames[0];
            _localeDropdown = new DropdownField(localeNames, localeNames.IndexOf(initialName));
            _localeDropdown.AddToClassList("toolbar-locale-dropdown");
            _localeDropdown.RegisterValueChangedCallback(e =>
            {
                // Find the Locale object matching the chosen display name and set it
                var chosen = _localeState.AllLocales
                    .FirstOrDefault(l => l.LocaleName == e.newValue);
                if (chosen != null)
                    _localeState.ActiveLocale = chosen; // fires OnLocaleChanged → all node views refresh
            });
            toolbar.Add(_localeDropdown);
        }

        private static void AddToolbarButton(UnityEditor.UIElements.Toolbar toolbar, string label,
                                              System.Action onClick)
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
            _graphView.Populate(_graph, _editorData, _localeState);
            _validationPanel.Refresh(_graph);
        }

        private void UnloadGraph()
        {
            _graph      = null;
            _editorData = null;
            _graphView.Populate(null, null, null);
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
                EditorUtility.DisplayDialog("No Graph",
                    "Open or create a DialogueGraph asset first.", "OK");
                return;
            }
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
            _graphView.RefreshAllEntryBadges();
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

            // Flush all StringTable edits made in the inspector panel
            LocalizationTableService.SaveAll();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[DialogueEditor] Saved '{_graph.name}' and flushed StringTable.");
        }

        private void ImportJson()
        {
            string absolutePath = EditorUtility.OpenFilePanel("Import Dialogue JSON", "", "json");
            if (string.IsNullOrEmpty(absolutePath)) return;

            string json  = File.ReadAllText(absolutePath);
            var    graph = DialogueJsonLoader.ParseJson(json,
                               Path.GetFileNameWithoutExtension(absolutePath));
            if (graph == null)
            {
                EditorUtility.DisplayDialog("Import Failed",
                    "Could not parse the JSON file. Check the console for details.", "OK");
                return;
            }

            string savePath = EditorUtility.SaveFilePanelInProject(
                "Save Imported Graph", graph.name, "asset",
                "Choose location for the new DialogueGraph asset");
            if (string.IsNullOrEmpty(savePath)) return;

            AssetDatabase.CreateAsset(graph, savePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            LoadGraph(graph);
            _graphView.AutoLayout();
            SaveAsset();
        }

        private void ExportJson()
        {
            if (_graph == null)
            {
                EditorUtility.DisplayDialog("No Graph",
                    "Open a DialogueGraph asset first.", "OK");
                return;
            }
            string defaultName  = _graph.name + ".json";
            string absolutePath = EditorUtility.SaveFilePanel(
                "Export Dialogue JSON", "", defaultName, "json");
            if (string.IsNullOrEmpty(absolutePath)) return;

            DialogueJsonExporter.WriteToFile(_graph, absolutePath, prettyPrint: true);
            EditorUtility.RevealInFinder(absolutePath);
        }
    }
}
