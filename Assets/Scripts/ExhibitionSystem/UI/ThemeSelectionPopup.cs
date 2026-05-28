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
        [SerializeField] private Button _enterButton;
        [SerializeField] private TMP_Text _hintText;

        [Header("List Layout")]
        [SerializeField] private int _maxVisibleItems = 5;
        [SerializeField] private float _minimumItemHeight = 82f;

        [Header("Animation")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeDuration = 0.2f;

        // ── Runtime State ───────────────────────────────────────────────────────

        private readonly List<ThemeListItem> _items = new();
        private ExhibitionTheme _selectedTheme;
        private float _targetAlpha;
        private float _currentAlpha;
        private bool _isAnimating;
        private bool _isVisible;

        // ── Unity Lifecycle ─────────────────────────────────────────────────────

        private void Start()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(Hide);

            if (_enterButton != null)
                _enterButton.onClick.AddListener(EnterSelectedTheme);

            // Load prefab from Resources if not configured in Inspector
            if (_itemPrefab == null)
            {
                var prefabObject = Resources.Load<GameObject>("Exhibitions/Prefabs/ThemeListItem");
                if (prefabObject != null)
                    _itemPrefab = prefabObject.GetComponent<ThemeListItem>();

                if (_itemPrefab == null)
                    Debug.LogError("[ThemeSelectionPopup] Failed to load ThemeListItem prefab from Resources.");
            }

            // Only hide if not already being shown (avoids race condition on first Show() call).
            if (!_isVisible)
                HideImmediate();
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
            _isVisible = true;

            if (_panel != null)
                _panel.SetActive(true);

            // Start fade in animation
            _currentAlpha = 0f;
            _targetAlpha = 1f;
            _isAnimating = true;

            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;

            _selectedTheme = ExhibitionManager.Instance?.CurrentTheme;
            if (_selectedTheme != null && _selectedTheme.isCompleted)
                _selectedTheme = null;

            PopulateList();
            UpdateSelectionVisuals();
        }

        /// <summary>
        /// Hides the popup.
        /// </summary>
        public void Hide()
        {
            _isVisible = false;

            // Start fade out animation
            _targetAlpha = 0f;
            _isAnimating = true;
            // Panel will be deactivated in Update when fade completes
        }

        private void HideImmediate()
        {
            _isVisible = false;
            _currentAlpha = 0f;
            _targetAlpha = 0f;
            _isAnimating = false;

            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;

            if (_panel != null)
                _panel.SetActive(false);
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

            // Create list items for each unlocked test theme
            foreach (var theme in manager.AllThemes)
            {
                var item = Instantiate(_itemPrefab, _listContainer);
                item.SetData(theme, OnThemeSelected);
                _items.Add(item);
            }

            FitItemsToViewport();
        }

        private void FitItemsToViewport()
        {
            if (_items.Count == 0 || _listContainer == null) return;

            Canvas.ForceUpdateCanvases();

            var listRect = _listContainer as RectTransform;
            var viewportRect = _listContainer.parent as RectTransform;
            var layout = _listContainer.GetComponent<VerticalLayoutGroup>();

            int visibleCount = Mathf.Clamp(_items.Count, 1, Mathf.Max(1, _maxVisibleItems));
            float viewportHeight = viewportRect != null ? viewportRect.rect.height : 0f;
            float verticalPadding = layout != null ? layout.padding.top + layout.padding.bottom : 0f;
            float totalSpacing = layout != null ? layout.spacing * Mathf.Max(0, visibleCount - 1) : 0f;
            float itemHeight = _minimumItemHeight;

            if (viewportHeight > 0f)
                itemHeight = Mathf.Max(_minimumItemHeight, (viewportHeight - verticalPadding - totalSpacing) / visibleCount);

            foreach (var item in _items)
            {
                if (item == null) continue;

                var layoutElement = item.GetComponent<LayoutElement>();
                if (layoutElement == null)
                    layoutElement = item.gameObject.AddComponent<LayoutElement>();

                layoutElement.minHeight = itemHeight;
                layoutElement.preferredHeight = itemHeight;
                layoutElement.flexibleHeight = 0f;

                if (item.transform is RectTransform itemRect)
                    itemRect.sizeDelta = new Vector2(itemRect.sizeDelta.x, itemHeight);
            }

            if (listRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(listRect);
        }

        private void OnThemeSelected(ExhibitionTheme theme)
        {
            if (theme == null || theme.isCompleted)
                return;

            _selectedTheme = theme;
            UpdateSelectionVisuals();
        }

        private void EnterSelectedTheme()
        {
            if (_selectedTheme == null || _selectedTheme.isCompleted) return;

            ExhibitionManager.Instance?.SelectTheme(_selectedTheme);
            Hide();
            ExhibitionUIManager.Instance?.ShowInspirationPopup();
        }

        private void UpdateSelectionVisuals()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item == null) continue;

                var theme = ExhibitionManager.Instance != null && i < ExhibitionManager.Instance.AllThemes.Count
                    ? ExhibitionManager.Instance.AllThemes[i]
                    : null;
                item.SetSelected(theme != null && !theme.isCompleted && theme == _selectedTheme);
            }

            if (_enterButton != null)
                _enterButton.interactable = _selectedTheme != null && !_selectedTheme.isCompleted;

            if (_hintText != null)
            {
                _hintText.text = _selectedTheme != null
                    ? $"Selected: {_selectedTheme.title}"
                    : "Choose an exhibition theme.";
            }
        }
    }
}
