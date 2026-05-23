using ExhibitionSystem.Core;
using ExhibitionSystem.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    /// <summary>
    /// Bottom control panel for theme selection and exhibition start.
    /// </summary>
    public class ThemeSelector : MonoBehaviour
    {
        // ── Serialized Fields ───────────────────────────────────────────────────

        [Header("UI References")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private Button _selectButton;
        [SerializeField] private Button _startButton;
        [SerializeField] private TMP_Text _startButtonText;
        [SerializeField] private ThemeSelectionPopup _popup;

        [Header("Button Text")]
        [SerializeField] private string _startText = "Start Exhibition";
        [SerializeField] private string _runningText = "In Progress...";
        [SerializeField] private string _retryText = "Retry";

        // ── Runtime State ───────────────────────────────────────────────────────

        private bool _canRetry;

        // ── Unity Lifecycle ─────────────────────────────────────────────────────

        private void OnEnable()
        {
            ExhibitionManager.OnThemeSelected += HandleThemeSelected;
            ExhibitionManager.OnExhibitionStarted += HandleExhibitionStarted;
            ExhibitionManager.OnExhibitionEnded += HandleExhibitionEnded;
        }

        private void OnDisable()
        {
            ExhibitionManager.OnThemeSelected -= HandleThemeSelected;
            ExhibitionManager.OnExhibitionStarted -= HandleExhibitionStarted;
            ExhibitionManager.OnExhibitionEnded -= HandleExhibitionEnded;
        }

        private void Start()
        {
            // Wire up buttons
            if (_selectButton != null)
                _selectButton.onClick.AddListener(OnSelectClicked);

            if (_startButton != null)
                _startButton.onClick.AddListener(OnStartClicked);

            UpdateUI();
        }

        // ── Button Handlers ─────────────────────────────────────────────────────

        private void OnSelectClicked()
        {
            if (_popup != null)
                _popup.Show();
        }

        private void OnStartClicked()
        {
            var manager = ExhibitionManager.Instance;
            if (manager == null) return;

            if (_canRetry)
            {
                manager.RetryExhibition();
            }
            else
            {
                manager.StartExhibition();
            }
        }

        // ── Event Handlers ──────────────────────────────────────────────────────

        private void HandleThemeSelected(ExhibitionTheme theme)
        {
            _canRetry = false;
            UpdateUI();
        }

        private void HandleExhibitionStarted()
        {
            _canRetry = false;
            UpdateUI();
        }

        private void HandleExhibitionEnded(bool success, int satisfaction, int threshold)
        {
            _canRetry = !success;
            UpdateUI();
        }

        // ── Private Methods ─────────────────────────────────────────────────────

        private void UpdateUI()
        {
            var manager = ExhibitionManager.Instance;
            var theme = manager?.CurrentTheme;

            // Update title
            if (_titleText != null)
            {
                _titleText.text = theme != null ? theme.title : "Select a Theme";
            }

            // Update buttons based on state
            bool isRunning = manager != null && manager.IsRunning;
            bool hasTheme = theme != null;

            if (_selectButton != null)
                _selectButton.interactable = !isRunning;

            if (_startButton != null)
            {
                _startButton.interactable = hasTheme && !isRunning;
            }

            if (_startButtonText != null)
            {
                if (isRunning)
                    _startButtonText.text = _runningText;
                else if (_canRetry)
                    _startButtonText.text = _retryText;
                else
                    _startButtonText.text = _startText;
            }
        }
    }
}
