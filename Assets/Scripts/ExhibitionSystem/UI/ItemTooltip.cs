using System.Collections.Generic;
using ExhibitionSystem.Data;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    /// <summary>
    /// Tooltip panel that displays item information on hover.
    /// Shows item name, description, and usage history.
    /// </summary>
    public class ItemTooltip : MonoBehaviour
    {
        // ── Singleton ───────────────────────────────────────────────────────────

        public static ItemTooltip Instance { get; private set; }

        // ── Serialized Fields ───────────────────────────────────────────────────

        [Header("Components")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _panel;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _descriptionText;

        [Header("History")]
        [SerializeField] private Transform _historyContainer;
        [SerializeField] private GameObject _historyEntryPrefab;
        [SerializeField] private string _historyPrefix = "Used in: ";

        [Header("Positioning")]
        [SerializeField] private Vector2 _offset = new(20, -20);
        [SerializeField] private float _padding = 10f;

        // ── Runtime State ───────────────────────────────────────────────────────

        private Canvas _rootCanvas;
        private RectTransform _canvasRect;
        private readonly List<GameObject> _historyEntries = new();
        private bool _isVisible;

        // ── Unity Lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _rootCanvas = GetComponentInParent<Canvas>();
            while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
            {
                var parent = _rootCanvas.transform.parent;
                _rootCanvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
            }

            if (_rootCanvas != null)
                _canvasRect = _rootCanvas.GetComponent<RectTransform>();

            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (_isVisible)
                UpdatePosition();
        }

        // ── Public Methods ──────────────────────────────────────────────────────

        /// <summary>
        /// Shows the tooltip with the given item data.
        /// </summary>
        public void Show(ExhibitItemData item, Vector3 anchorWorldPos)
        {
            if (item == null) return;

            // Update content
            if (_nameText != null)
                _nameText.text = item.itemName;

            if (_descriptionText != null)
                _descriptionText.text = item.description;

            PopulateHistory(item);

            // Show
            _isVisible = true;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            gameObject.SetActive(true);
            UpdatePosition();
        }

        /// <summary>
        /// Hides the tooltip.
        /// </summary>
        public void Hide()
        {
            _isVisible = false;

            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;

            gameObject.SetActive(false);
        }

        // ── Private Methods ─────────────────────────────────────────────────────

        private void UpdatePosition()
        {
            if (_panel == null || _canvasRect == null) return;

            // Convert mouse position to canvas local position
            Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                mousePos,
                _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera,
                out Vector2 localPoint);

            // Apply offset
            localPoint += _offset;

            // Clamp to canvas bounds
            Vector2 panelSize = _panel.sizeDelta;
            Vector2 canvasSize = _canvasRect.sizeDelta;

            // Right edge
            if (localPoint.x + panelSize.x > canvasSize.x / 2 - _padding)
                localPoint.x = canvasSize.x / 2 - panelSize.x - _padding;

            // Bottom edge
            if (localPoint.y - panelSize.y < -canvasSize.y / 2 + _padding)
                localPoint.y = -canvasSize.y / 2 + panelSize.y + _padding;

            // Left edge
            if (localPoint.x < -canvasSize.x / 2 + _padding)
                localPoint.x = -canvasSize.x / 2 + _padding;

            // Top edge
            if (localPoint.y > canvasSize.y / 2 - _padding)
                localPoint.y = canvasSize.y / 2 - _padding;

            _panel.localPosition = localPoint;
        }

        private void PopulateHistory(ExhibitItemData item)
        {
            // Clear existing entries
            foreach (var entry in _historyEntries)
            {
                if (entry != null)
                    Destroy(entry);
            }
            _historyEntries.Clear();

            if (_historyContainer == null || _historyEntryPrefab == null) return;
            if (item.usedInExhibitions == null || item.usedInExhibitions.Count == 0) return;

            // Create entries for each exhibition
            foreach (var exhibitionTitle in item.usedInExhibitions)
            {
                var entry = Instantiate(_historyEntryPrefab, _historyContainer);
                var text = entry.GetComponentInChildren<TMP_Text>();
                if (text != null)
                    text.text = $"{_historyPrefix}{exhibitionTitle}";

                _historyEntries.Add(entry);
            }
        }
    }
}
