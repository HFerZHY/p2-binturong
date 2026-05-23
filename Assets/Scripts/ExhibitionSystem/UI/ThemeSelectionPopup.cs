using System;
using System.Collections.Generic;
using ExhibitionSystem.Core;
using ExhibitionSystem.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    /// <summary>
    /// Popup panel for selecting exhibition themes.
    /// </summary>
    public class ThemeSelectionPopup : MonoBehaviour
    {
        // ── Serialized Fields ───────────────────────────────────────────────────

        [Header("UI References")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Transform _listContainer;
        [SerializeField] private ThemeListItem _itemPrefab;
        [SerializeField] private Button _closeButton;

        [Header("Animation")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeDuration = 0.2f;

        // ── Runtime State ───────────────────────────────────────────────────────

        private readonly List<ThemeListItem> _items = new();
        private bool _isVisible;

        // ── Unity Lifecycle ─────────────────────────────────────────────────────

        private void Start()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(Hide);

            Hide();
        }

        // ── Public Methods ──────────────────────────────────────────────────────

        /// <summary>
        /// Shows the popup and populates the theme list.
        /// </summary>
        public void Show()
        {
            if (_panel != null)
                _panel.SetActive(true);

            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;

            _isVisible = true;
            PopulateList();
        }

        /// <summary>
        /// Hides the popup.
        /// </summary>
        public void Hide()
        {
            if (_panel != null)
                _panel.SetActive(false);

            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;

            _isVisible = false;
        }

        // ── Private Methods ─────────────────────────────────────────────────────

        private void PopulateList()
        {
            // Clear existing items
            foreach (var item in _items)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            _items.Clear();

            if (_itemPrefab == null || _listContainer == null) return;

            var manager = ExhibitionManager.Instance;
            if (manager == null) return;

            // Create list items for each theme
            foreach (var theme in manager.AllThemes)
            {
                var item = Instantiate(_itemPrefab, _listContainer);
                item.SetData(theme, OnThemeSelected);
                _items.Add(item);
            }
        }

        private void OnThemeSelected(ExhibitionTheme theme)
        {
            ExhibitionManager.Instance?.SelectTheme(theme);
            Hide();
        }
    }

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
