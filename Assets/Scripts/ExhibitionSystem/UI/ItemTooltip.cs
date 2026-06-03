using ExhibitionSystem.Core;
using ExhibitionSystem.Data;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    /// <summary>
    /// Tooltip panel that displays item information on hover.
    /// Shows the item name and a description once its matching inspiration is unlocked.
    /// </summary>
    public class ItemTooltip : MonoBehaviour
    {
        private const string DAY2_EXHIBITION_SCENE = "ExhibitionDay2Scene";
        private const string DAY3_EXHIBITION_SCENE = "ExhibitionDay3Scene";

        // ── Singleton ───────────────────────────────────────────────────────────

        public static ItemTooltip Instance { get; private set; }

        // ── Serialized Fields ───────────────────────────────────────────────────

        [Header("Components")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _panel;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _descriptionText;

        [Header("Positioning")]
        [SerializeField] private Vector2 _offset = new(16f, 0f);
        [SerializeField] private float _padding = 10f;

        [Header("Text Style")]
        [SerializeField] private float _titleFontSize = 22f;
        [SerializeField] private float _descriptionFontSize = 22f;

        // ── Runtime State ───────────────────────────────────────────────────────

        private Canvas _rootCanvas;
        private RectTransform _canvasRect;
        private RectTransform _anchorRect;
        private Vector3 _anchorWorldPos;
        private bool _hasAnchorWorldPos;
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

            ConfigurePanelStyle();
            ConfigureTextStyle();
            HideLegacyChildren();

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
        public void Show(ExhibitItemData item, RectTransform anchorRect)
        {
            _anchorRect = anchorRect;
            _hasAnchorWorldPos = false;
            Show(item);
        }

        public void Show(ExhibitItemData item, Vector3 anchorWorldPos)
        {
            _anchorRect = null;
            _anchorWorldPos = anchorWorldPos;
            _hasAnchorWorldPos = true;
            Show(item);
        }

        private void Show(ExhibitItemData item)
        {
            if (item == null) return;

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            transform.SetAsLastSibling();

            // Update content
            if (_nameText != null)
                _nameText.text = item.itemName;

            if (_descriptionText != null)
                _descriptionText.text = IsDescriptionUnlocked(item) ? item.description : "???";

            HideLegacyChildren();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_panel);

            // Show
            _isVisible = true;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            UpdatePosition();
        }

        /// <summary>
        /// Hides the tooltip.
        /// </summary>
        public void Hide()
        {
            _isVisible = false;
            _anchorRect = null;
            _hasAnchorWorldPos = false;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }
        }

        // ── Private Methods ─────────────────────────────────────────────────────

        private void ConfigurePanelStyle()
        {
            _offset = new Vector2(16f, 0f);

            var background = GetComponent<Image>();
            if (background != null)
            {
                background.color = new Color(0.88f, 0.78f, 0.58f, 0.98f);
                background.raycastTarget = false;
            }

            if (_panel != null)
            {
                _panel.pivot = new Vector2(0f, 0.5f);
                _panel.sizeDelta = new Vector2(372f, 104f);
            }

            var layout = GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.padding = new RectOffset(14, 14, 12, 12);
                layout.spacing = 4f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
            }
        }

        private void ConfigureTextStyle()
        {
            if (_nameText != null)
            {
                _nameText.fontSize = _titleFontSize;
                _nameText.fontStyle = FontStyles.Bold;
                _nameText.alignment = TextAlignmentOptions.Center;
                _nameText.textWrappingMode = TextWrappingModes.NoWrap;
                _nameText.overflowMode = TextOverflowModes.Ellipsis;
                _nameText.color = new Color(0.22f, 0.13f, 0.07f, 1f);
                _nameText.raycastTarget = false;
            }

            if (_descriptionText != null)
            {
                _descriptionText.fontSize = _descriptionFontSize;
                _descriptionText.fontStyle = FontStyles.Normal;
                _descriptionText.alignment = TextAlignmentOptions.Center;
                _descriptionText.textWrappingMode = TextWrappingModes.Normal;
                _descriptionText.overflowMode = TextOverflowModes.Ellipsis;
                _descriptionText.maxVisibleLines = 2;
                _descriptionText.color = new Color(0.22f, 0.13f, 0.07f, 1f);
                _descriptionText.raycastTarget = false;
            }
        }

        private void HideLegacyChildren()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                bool isContent =
                    (_nameText != null && child == _nameText.transform) ||
                    (_descriptionText != null && child == _descriptionText.transform);

                if (!isContent)
                    child.gameObject.SetActive(false);
            }
        }

        private static bool IsDescriptionUnlocked(ExhibitItemData item)
        {
            if (item == null)
                return false;

            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == DAY3_EXHIBITION_SCENE)
                return item.isUnlocked;

            var manager = ExhibitionManager.Instance;
            if (manager == null)
                return item.isUnlocked;

            if (sceneName != DAY2_EXHIBITION_SCENE)
                return item.isUnlocked;

            foreach (var inspiration in manager.AllInspirations)
            {
                if (inspiration != null && inspiration.mappedItem == item)
                    return inspiration.isUnlocked;
            }

            return false;
        }

        private void UpdatePosition()
        {
            if (_panel == null || _canvasRect == null) return;

            var camera = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera;
            Vector2 screenPoint = GetTooltipScreenPoint(camera);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                screenPoint,
                camera,
                out Vector2 localPoint);

            _panel.pivot = new Vector2(0f, 0.5f);
            localPoint += _offset;

            Vector2 panelSize = GetPanelSize();
            Vector2 canvasSize = _canvasRect.sizeDelta;
            float left = -canvasSize.x / 2f + _padding;
            float right = canvasSize.x / 2f - _padding;
            float bottom = -canvasSize.y / 2f + _padding;
            float top = canvasSize.y / 2f - _padding;

            if (localPoint.x + panelSize.x > right && TryGetAnchorLeftPoint(camera, out Vector2 leftLocalPoint))
            {
                _panel.pivot = new Vector2(1f, 0.5f);
                localPoint = leftLocalPoint - _offset;
            }

            if (_panel.pivot.x == 0f)
                localPoint.x = Mathf.Clamp(localPoint.x, left, right - panelSize.x);
            else
                localPoint.x = Mathf.Clamp(localPoint.x, left + panelSize.x, right);

            localPoint.y = Mathf.Clamp(localPoint.y, bottom + panelSize.y / 2f, top - panelSize.y / 2f);

            _panel.localPosition = localPoint;
        }

        private Vector2 GetTooltipScreenPoint(Camera camera)
        {
            if (_anchorRect != null)
            {
                var corners = new Vector3[4];
                _anchorRect.GetWorldCorners(corners);
                return RectTransformUtility.WorldToScreenPoint(camera, (corners[2] + corners[3]) * 0.5f);
            }

            if (_hasAnchorWorldPos)
                return RectTransformUtility.WorldToScreenPoint(camera, _anchorWorldPos);

            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        }

        private bool TryGetAnchorLeftPoint(Camera camera, out Vector2 localPoint)
        {
            localPoint = Vector2.zero;
            if (_anchorRect == null)
                return false;

            var corners = new Vector3[4];
            _anchorRect.GetWorldCorners(corners);
            var screenPoint = RectTransformUtility.WorldToScreenPoint(camera, (corners[0] + corners[1]) * 0.5f);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, camera, out localPoint);
        }

        private Vector2 GetPanelSize()
        {
            if (_panel == null)
                return Vector2.zero;

            Vector2 rectSize = _panel.rect.size;
            float width = rectSize.x > 0f ? rectSize.x : _panel.sizeDelta.x;
            float height = LayoutUtility.GetPreferredHeight(_panel);
            if (height <= 0f)
                height = rectSize.y > 0f ? rectSize.y : _panel.sizeDelta.y;

            return new Vector2(
                width,
                height);
        }
    }
}
