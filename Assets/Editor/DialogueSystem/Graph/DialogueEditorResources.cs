using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueSystem.Editor
{
    /// <summary>
    /// Central access point for editor-only style sheets.
    /// Loads USS assets from the package and returns empty stubs if not found
    /// so the editor still opens gracefully when assets are missing.
    /// </summary>
    public static class DialogueEditorResources
    {
        // Expected paths relative to the project root.
        private const string GraphViewStylePath   = "Assets/Editor/DialogueSystem/Styles/DialogueGraphView.uss";
        private const string WindowStylePath      = "Assets/Editor/DialogueSystem/Styles/DialogueEditorWindow.uss";

        private static StyleSheet _graphViewStyle;
        private static StyleSheet _windowStyle;

        public static StyleSheet GraphViewStyle =>
            _graphViewStyle ??= LoadStyle(GraphViewStylePath);

        public static StyleSheet WindowStyle =>
            _windowStyle ??= LoadStyle(WindowStylePath);

        private static StyleSheet LoadStyle(string path)
        {
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (sheet == null)
            {
                Debug.LogWarning(
                    $"[DialogueEditorResources] StyleSheet not found at '{path}'. " +
                    "Place the .uss file there or update the path in DialogueEditorResources.cs.");
                // Return a blank StyleSheet so callers don't null-reference
                sheet = ScriptableObject.CreateInstance<StyleSheet>();
            }
            return sheet;
        }
    }
}
