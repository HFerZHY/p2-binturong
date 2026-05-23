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
        [SerializeField] private Button _button;

        // ── Runtime State ───────────────────────────────────────────────────────

        private ExhibitionTheme _theme;
        private Action<ExhibitionTheme> _onClick;

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
                _slotsText.text = $"{theme.requiredSlots} slots";

            if (_completedIcon != null)
                _completedIcon.enabled = theme.isCompleted;
        }

        // ── Private Methods ─────────────────────────────────────────────────────

        private void HandleClick()
        {
            _onClick?.Invoke(_theme);
        }
    }
}
