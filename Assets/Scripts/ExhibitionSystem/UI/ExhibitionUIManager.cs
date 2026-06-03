using System.Collections;
using ExhibitionSystem.Data;
using UnityEngine;

namespace ExhibitionSystem.UI
{
    /// <summary>
    /// Central manager for all exhibition UI components.
    /// Provides shared references and coordinates UI behavior.
    /// </summary>
    public class ExhibitionUIManager : MonoBehaviour
    {
        // ── Singleton ───────────────────────────────────────────────────────────

        public static ExhibitionUIManager Instance { get; private set; }

        // ── Serialized Fields ───────────────────────────────────────────────────

        [Header("Panels")]
        [SerializeField] private ShelfPanel _shelfPanel;
        [SerializeField] private DisplayPanel _displayPanel;
        [SerializeField] private VisitorPanel _visitorPanel;
        [SerializeField] private SatisfactionBar _satisfactionBar;

        [Header("Controls")]
        [SerializeField] private ThemeSelector _themeSelector;
        [SerializeField] private ThemeSelectionPopup _themePopup;
        [SerializeField] private InspirationSelectionPopup _inspirationPopup;

        [Header("Shared")]
        [SerializeField] private ItemTooltip _tooltip;
        [SerializeField] private Canvas _rootCanvas;

        private Coroutine _showInspirationPopupCoroutine;

        // ── Public Properties ───────────────────────────────────────────────────

        public Canvas RootCanvas => _rootCanvas;
        public ShelfPanel ShelfPanel => _shelfPanel;
        public DisplayPanel DisplayPanel => _displayPanel;
        public VisitorPanel VisitorPanel => _visitorPanel;
        public SatisfactionBar SatisfactionBar => _satisfactionBar;
        public ThemeSelector ThemeSelector => _themeSelector;
        public ThemeSelectionPopup ThemePopup => _themePopup;
        public InspirationSelectionPopup InspirationPopup => _inspirationPopup;
        public ItemTooltip Tooltip => _tooltip;

        // ── Unity Lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[ExhibitionUIManager] Duplicate instance destroyed.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Auto-find root canvas if not assigned
            if (_rootCanvas == null)
            {
                _rootCanvas = GetComponentInParent<Canvas>();
                while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
                {
                    var parent = _rootCanvas.transform.parent;
                    _rootCanvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
                }
            }

            EnsureTooltipReady();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ── Public Methods ──────────────────────────────────────────────────────

        /// <summary>
        /// Shows the item tooltip at the specified position.
        /// </summary>
        public void ShowTooltip(ExhibitItemData item, Vector3 worldPosition)
        {
            if (_tooltip != null)
                _tooltip.Show(item, worldPosition);
        }

        /// <summary>
        /// Hides the item tooltip.
        /// </summary>
        public void HideTooltip()
        {
            if (_tooltip != null)
                _tooltip.Hide();
        }

        /// <summary>
        /// Shows the theme selection popup.
        /// </summary>
        public void ShowThemePopup()
        {
            if (_themePopup != null)
                _themePopup.Show();
        }

        /// <summary>
        /// Hides the theme selection popup.
        /// </summary>
        public void HideThemePopup()
        {
            if (_themePopup != null)
                _themePopup.Hide();
        }

        public void ShowInspirationPopup()
        {
            if (_inspirationPopup != null)
                _inspirationPopup.Show();
        }

        public void ShowInspirationPopupForSlot(int slotIndex)
        {
            if (_inspirationPopup == null)
                return;

            if (_showInspirationPopupCoroutine != null)
                StopCoroutine(_showInspirationPopupCoroutine);

            _showInspirationPopupCoroutine = StartCoroutine(ShowInspirationPopupForSlotNextFrame(slotIndex));
        }

        public void HideInspirationPopup()
        {
            if (_showInspirationPopupCoroutine != null)
            {
                StopCoroutine(_showInspirationPopupCoroutine);
                _showInspirationPopupCoroutine = null;
            }

            if (_inspirationPopup != null)
                _inspirationPopup.Hide();
        }

        private IEnumerator ShowInspirationPopupForSlotNextFrame(int slotIndex)
        {
            yield return null;

            _showInspirationPopupCoroutine = null;
            _inspirationPopup?.ShowForSlot(slotIndex);
        }

        private void EnsureTooltipReady()
        {
            if (_tooltip == null)
                _tooltip = FindFirstObjectByType<ItemTooltip>(FindObjectsInactive.Include);

            if (_tooltip == null)
                return;

            if (!_tooltip.gameObject.activeSelf)
                _tooltip.gameObject.SetActive(true);

            _tooltip.Hide();
        }
    }
}
