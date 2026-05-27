using System;
using ExhibitionSystem.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    /// <summary>
    /// Individual list item for theme selection.
    /// </summary>
    public class ThemeListItem : MonoBehaviour
    {
        // ── Serialized Fields ───────────────────────────────────────────────────

        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private TMP_Text _slotsText;
        [SerializeField] private Image _completedIcon;
        [SerializeField] private Image _selectionFrame;
        [SerializeField] private Image _background;
        [SerializeField] private Button _button;
        [SerializeField] private Color _normalColor = new(0.78f, 0.66f, 0.46f, 0.98f);
        [SerializeField] private Color _selectedColor = new(0.92f, 0.76f, 0.42f, 1f);

        // ── Runtime State ───────────────────────────────────────────────────────

        private ExhibitionTheme _theme;
        private Action<ExhibitionTheme> _onClick;
        private bool _selected;

        // ── Unity Lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            if (_button != null)
                _button.onClick.AddListener(HandleClick);
        }

        // ── Public Methods ──────────────────────────────────────────────────────

        /// <summary>
        /// Sets the theme data and click callback.
        /// </summary>
        public void SetData(ExhibitionTheme theme, Action<ExhibitionTheme> onClick)
        {
            _theme = theme;
            _onClick = onClick;

            if (_titleText != null)
                _titleText.text = theme.title;

            if (_descriptionText != null)
                _descriptionText.text = theme.description;

            if (_slotsText != null)
                _slotsText.text = $"Day {theme.day} • {theme.requiredInspirations} ideas";

            if (_completedIcon != null)
                _completedIcon.gameObject.SetActive(theme.isCompleted);

            UpdateVisual();
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            UpdateVisual();
        }

        // ── Private Methods ─────────────────────────────────────────────────────

        private void HandleClick()
        {
            _onClick?.Invoke(_theme);
        }

        private void UpdateVisual()
        {
            if (_background != null)
                _background.color = _selected ? _selectedColor : _normalColor;

            if (_selectionFrame != null)
                _selectionFrame.enabled = _selected;
        }
    }
}
