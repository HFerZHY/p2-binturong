using System.Collections.Generic;
using System.Linq;
using ExhibitionSystem.Core;
using ExhibitionSystem.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    public class InspirationSelectionPopup : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _themeText;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private TMP_Text _hintText;
        [SerializeField] private Transform _selectedContainer;
        [SerializeField] private Transform _libraryContainer;
        [SerializeField] private Transform _progressContainer;
        [SerializeField] private Transform _listContainer;
        [SerializeField] private InspirationListItem _itemPrefab;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private CanvasGroup _canvasGroup;

        private readonly List<InspirationListItem> _libraryItems = new();
        private readonly List<InspirationListItem> _selectedItems = new();
        private readonly List<GameObject> _progressDots = new();
        private readonly List<GameObject> _emptyPlaceholders = new();
        private readonly List<InspirationData> _selectedInspirations = new();
        private readonly HashSet<int> _invalidSelectionIds = new();
        private bool _animateInvalidFeedback;
        private bool _isVisible;
        private bool _hasInitialized;

        private void OnEnable()
        {
            ExhibitionManager.OnThemeSelected += HandleThemeSelected;
            ExhibitionManager.OnPlayerHint += HandlePlayerHint;
        }

        private void OnDisable()
        {
            ExhibitionManager.OnThemeSelected -= HandleThemeSelected;
            ExhibitionManager.OnPlayerHint -= HandlePlayerHint;
        }

        private void Start()
        {
            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(ConfirmSelection);

            if (_closeButton != null)
                _closeButton.onClick.AddListener(Hide);

            // Only hide if not already being shown (avoids race condition on first Show() call)
            if (!_isVisible)
                HideImmediate();

            _hasInitialized = true;
        }

        public void Show()
        {
            var manager = ExhibitionManager.Instance;
            if (manager == null || manager.CurrentTheme == null) return;

            _isVisible = true;
            _selectedInspirations.Clear();
            _invalidSelectionIds.Clear();
            _animateInvalidFeedback = false;

            if (_panel != null)
                _panel.SetActive(true);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }

            if (_titleText != null)
                _titleText.text = "Inspiration Selection";

            if (_themeText != null)
                _themeText.text = $"Theme: {manager.CurrentTheme.title}";

            if (_hintText != null)
                _hintText.text = "Hmm... which ones should I choose?";

            PopulateList();
            PopulateSelectedList();
            UpdateProgress();
            UpdateConfirmState();
        }

        public void Hide()
        {
            HideImmediate();
        }

        private void HideImmediate()
        {
            _isVisible = false;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            if (_panel != null)
                _panel.SetActive(false);
        }

        private void PopulateList()
        {
            foreach (var item in _libraryItems)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            _libraryItems.Clear();

            var manager = ExhibitionManager.Instance;
            var container = _libraryContainer != null ? _libraryContainer : _listContainer;
            if (manager == null || _itemPrefab == null || container == null) return;

            foreach (var inspiration in manager.AllInspirations.Where(inspiration => inspiration.isUnlocked))
            {
                var item = Instantiate(_itemPrefab, container);
                bool selected = _selectedInspirations.Contains(inspiration);
                bool invalid = selected && _invalidSelectionIds.Contains(inspiration.id);
                item.SetData(inspiration, selected, ToggleSelection, invalid, invalid && _animateInvalidFeedback);
                _libraryItems.Add(item);
            }

            _animateInvalidFeedback = false;
        }

        private void PopulateSelectedList()
        {
            foreach (var item in _selectedItems)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            _selectedItems.Clear();

            foreach (var placeholder in _emptyPlaceholders)
            {
                if (placeholder != null)
                    Destroy(placeholder);
            }
            _emptyPlaceholders.Clear();

            var manager = ExhibitionManager.Instance;
            if (manager == null || manager.CurrentTheme == null || _selectedContainer == null) return;

            foreach (var inspiration in _selectedInspirations)
            {
                var item = Instantiate(_itemPrefab, _selectedContainer);
                bool invalid = _invalidSelectionIds.Contains(inspiration.id);
                item.SetData(inspiration, true, ToggleSelection, invalid, invalid && _animateInvalidFeedback);
                _selectedItems.Add(item);
            }

            _animateInvalidFeedback = false;

            int emptyCount = Mathf.Max(0, manager.CurrentTheme.requiredInspirations - _selectedInspirations.Count);
            for (int i = 0; i < emptyCount; i++)
                _emptyPlaceholders.Add(CreateEmptyPlaceholder(_selectedContainer));
        }

        private void ToggleSelection(InspirationData inspiration)
        {
            var manager = ExhibitionManager.Instance;
            if (manager == null || manager.CurrentTheme == null || inspiration == null) return;

            if (_selectedInspirations.Contains(inspiration))
            {
                _selectedInspirations.Remove(inspiration);
            }
            else
            {
                if (_selectedInspirations.Count >= manager.CurrentTheme.requiredInspirations)
                    _selectedInspirations.RemoveAt(0);

                _selectedInspirations.Add(inspiration);
            }

            _invalidSelectionIds.Clear();
            _animateInvalidFeedback = false;
            RefreshSelectionVisuals();
            PopulateSelectedList();
            UpdateProgress();
            UpdateConfirmState();
        }

        private void ConfirmSelection()
        {
            var manager = ExhibitionManager.Instance;
            if (manager == null) return;

            if (manager.TryConfirmInspirations(_selectedInspirations, out string hint))
            {
                Hide();
                return;
            }

            if (_hintText != null)
                _hintText.text = hint;

            MarkInvalidSelections(manager.CurrentTheme);
        }

        private void HandleThemeSelected(ExhibitionTheme theme)
        {
            if (_isVisible)
                Show();
        }

        private void HandlePlayerHint(string hint)
        {
            if (_hintText != null && !string.IsNullOrWhiteSpace(hint))
                _hintText.text = hint;
        }

        private void RefreshSelectionVisuals()
        {
            PopulateList();
        }

        private void MarkInvalidSelections(ExhibitionTheme theme)
        {
            _invalidSelectionIds.Clear();

            if (theme == null) return;

            foreach (var inspiration in _selectedInspirations)
            {
                if (inspiration == null || !theme.IsInspirationValid(inspiration.id))
                    _invalidSelectionIds.Add(inspiration != null ? inspiration.id : -1);
            }

            if (_invalidSelectionIds.Count == 0) return;

            _animateInvalidFeedback = true;
            PopulateList();
            _animateInvalidFeedback = true;
            PopulateSelectedList();
        }

        private void UpdateProgress()
        {
            foreach (var dot in _progressDots)
            {
                if (dot != null)
                    Destroy(dot);
            }
            _progressDots.Clear();

            var manager = ExhibitionManager.Instance;
            if (manager == null || manager.CurrentTheme == null) return;

            int requiredCount = manager.CurrentTheme.requiredInspirations;
            if (_progressText != null)
                _progressText.text = $"Select {requiredCount} Inspirations";

            if (_progressContainer == null) return;

            for (int i = 0; i < requiredCount; i++)
            {
                var dot = new GameObject($"ProgressDot_{i + 1}");
                dot.transform.SetParent(_progressContainer, false);
                var image = dot.AddComponent<Image>();
                image.color = i < _selectedInspirations.Count
                    ? new Color(0.78f, 0.25f, 0.2f, 1f)
                    : new Color(0.64f, 0.57f, 0.45f, 0.55f);

                var rt = dot.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(28, 28);

                var layoutElement = dot.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = 28;
                layoutElement.preferredHeight = 28;

                _progressDots.Add(dot);
            }
        }

        private void UpdateConfirmState()
        {
            var manager = ExhibitionManager.Instance;
            bool hasRequiredCount = manager != null &&
                manager.CurrentTheme != null &&
                _selectedInspirations.Count == manager.CurrentTheme.requiredInspirations;

            if (_confirmButton != null)
                _confirmButton.interactable = hasRequiredCount;
        }

        private GameObject CreateEmptyPlaceholder(Transform parent)
        {
            var obj = new GameObject("EmptyInspirationSlot");
            obj.transform.SetParent(parent, false);

            var image = obj.AddComponent<Image>();
            image.color = new Color(0.64f, 0.52f, 0.36f, 0.42f);

            var layout = obj.AddComponent<LayoutElement>();
            layout.preferredHeight = 82;

            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(obj.transform, false);
            var label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = "Empty Slot";
            label.fontSize = 18;
            label.fontStyle = FontStyles.Italic;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.30f, 0.20f, 0.12f, 0.82f);

            var labelRt = labelObj.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            return obj;
        }
    }
}
