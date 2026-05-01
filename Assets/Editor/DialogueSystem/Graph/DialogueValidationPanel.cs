using System.Collections.Generic;
using UnityEngine.UIElements;
using DialogueSystem.Data;

namespace DialogueSystem.Editor
{
    /// <summary>
    /// A collapsible status bar rendered at the bottom of the editor window.
    /// Displays errors/warnings returned by DialogueGraph.Validate().
    /// Green "✓ Valid" when no errors; red error list when issues are found.
    /// </summary>
    public class DialogueValidationPanel : VisualElement
    {
        private readonly Label         _statusLabel;
        private readonly VisualElement _errorList;
        private readonly Button        _toggleBtn;
        private bool                   _expanded = true;

        public DialogueValidationPanel()
        {
            AddToClassList("validation-panel");

            // Top bar: status label + collapse toggle
            var bar = new VisualElement();
            bar.AddToClassList("validation-bar");

            _statusLabel = new Label("No graph loaded.");
            _statusLabel.AddToClassList("validation-status");
            bar.Add(_statusLabel);

            _toggleBtn = new Button(ToggleExpanded) { text = "▼" };
            _toggleBtn.AddToClassList("validation-toggle-btn");
            bar.Add(_toggleBtn);

            Add(bar);

            // Error list (shown when expanded & errors exist)
            _errorList = new VisualElement();
            _errorList.AddToClassList("validation-error-list");
            Add(_errorList);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Refresh(DialogueGraph graph)
        {
            _errorList.Clear();

            if (graph == null)
            {
                _statusLabel.text = "No graph loaded.";
                _statusLabel.RemoveFromClassList("validation-ok");
                _statusLabel.RemoveFromClassList("validation-error");
                return;
            }

#if UNITY_EDITOR
            List<string> errors = graph.Validate();

            if (errors.Count == 0)
            {
                _statusLabel.text = $"✓  '{graph.name}' is valid.";
                _statusLabel.EnableInClassList("validation-ok",    true);
                _statusLabel.EnableInClassList("validation-error", false);
            }
            else
            {
                _statusLabel.text = $"⚠  {errors.Count} issue{(errors.Count == 1 ? "" : "s")} found.";
                _statusLabel.EnableInClassList("validation-ok",    false);
                _statusLabel.EnableInClassList("validation-error", true);

                if (_expanded)
                {
                    foreach (var err in errors)
                    {
                        var row = new Label($"• {err}");
                        row.AddToClassList("validation-error-item");
                        _errorList.Add(row);
                    }
                }
            }
#endif
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void ToggleExpanded()
        {
            _expanded = !_expanded;
            _toggleBtn.text = _expanded ? "▼" : "▲";
            // Re-render will happen next Refresh call (called on every save/edit)
        }
    }
}
