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
        private float _targetAlpha;
        private float _currentAlpha;
        private bool _isAnimating;

        // ── Unity Lifecycle ─────────────────────────────────────────────────────

        private void Start()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(Hide);

            // Load prefab from Resources if not configured in Inspector
            if (_itemPrefab == null)
            {
                _itemPrefab = Resources.Load<ThemeListItem>("Exhibitions/Prefabs/ThemeListItem");
                if (_itemPrefab == null)
                    Debug.LogError("[ThemeSelectionPopup] Failed to load ThemeListItem prefab from Resources.");
            }

            // Initialize in hidden state without animation
            if (_panel != null)
                _panel.SetActive(false);

            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;

            _currentAlpha = 0f;
            _targetAlpha = 0f;
            _isVisible = false;
            _isAnimating = false;
        }

        private void Update()
        {
            if (!_isAnimating) return;

            float fadeSpeed = 1f / _fadeDuration;
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha, fadeSpeed * Time.deltaTime);

            if (_canvasGroup != null)
                _canvasGroup.alpha = _currentAlpha;

            // Check if animation completed
            if (Mathf.Abs(_currentAlpha - _targetAlpha) < 0.01f)
            {
                _currentAlpha = _targetAlpha;
                if (_canvasGroup != null)
                    _canvasGroup.alpha = _currentAlpha;

                _isAnimating = false;

                // Deactivate panel after fade out
                if (_targetAlpha < 0.01f && _panel != null)
                    _panel.SetActive(false);
            }
        }

        // ── Public Methods ──────────────────────────────────────────────────────

        /// <summary>
        /// Shows the popup and populates the theme list.
        /// </summary>
        public void Show()
        {
            if (_panel != null)
                _panel.SetActive(true);

            // Start fade in animation
            _currentAlpha = 0f;
            _targetAlpha = 1f;
            _isAnimating = true;

            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;

            _isVisible = true;
            PopulateList();
        }

        /// <summary>
        /// Hides the popup.
        /// </summary>
        public void Hide()
        {
            // Start fade out animation
            _targetAlpha = 0f;
            _isAnimating = true;
            _isVisible = false;
            // Panel will be deactivated in Update when fade completes
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
