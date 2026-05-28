using ExhibitionSystem.Core;
using ExhibitionSystem.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    public class ThemeSelector : MonoBehaviour
    {
        public static event System.Action OnSelectThemeClicked;

        [Header("UI References")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private Button _selectButton;
        [SerializeField] private TMP_Text _selectButtonText;
        [SerializeField] private Button _startButton;
        [SerializeField] private TMP_Text _startButtonText;
        [SerializeField] private ThemeSelectionPopup _popup;

        [Header("Button Text")]
        [SerializeField] private string _selectText = "Select Theme";
        [SerializeField] private string _themeSelectedText = "Theme Selected";
        [SerializeField] private string _themeLockedTooltipText = "Please complete this theme's exhibition first.";
        [SerializeField] private string _startText = "Start Exhibition";
        [SerializeField] private string _runningText = "In Progress...";
        [SerializeField] private string _retryText = "Retry";
        [SerializeField] private string _lockedText = "Start Exhibition";

        private bool _canRetry;
        private HoverTextPopup _selectButtonPopup;

        private void OnEnable()
        {
            ExhibitionManager.OnThemeSelected += HandleThemeSelected;
            ExhibitionManager.OnInspirationsConfirmed += HandleInspirationsConfirmed;
            ExhibitionManager.OnItemPlaced += HandleItemChanged;
            ExhibitionManager.OnItemRemoved += HandleItemRemoved;
            ExhibitionManager.OnExhibitionStarted += HandleExhibitionStarted;
            ExhibitionManager.OnExhibitionEnded += HandleExhibitionEnded;
            ExhibitionManager.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            ExhibitionManager.OnThemeSelected -= HandleThemeSelected;
            ExhibitionManager.OnInspirationsConfirmed -= HandleInspirationsConfirmed;
            ExhibitionManager.OnItemPlaced -= HandleItemChanged;
            ExhibitionManager.OnItemRemoved -= HandleItemRemoved;
            ExhibitionManager.OnExhibitionStarted -= HandleExhibitionStarted;
            ExhibitionManager.OnExhibitionEnded -= HandleExhibitionEnded;
            ExhibitionManager.OnStateChanged -= HandleStateChanged;
        }

        private void Start()
        {
            EnsureSelectButtonReferences();

            if (_selectButton != null)
                _selectButton.onClick.AddListener(OnSelectClicked);

            if (_startButton != null)
                _startButton.onClick.AddListener(OnStartClicked);

            UpdateUI();
        }

        private void OnSelectClicked()
        {
            if (IsThemeLocked())
                return;

            OnSelectThemeClicked?.Invoke();

            var manager = ExhibitionManager.Instance;
            manager?.ClearCompletedCurationForThemeSelection();

            if (_popup != null)
                _popup.Show();
        }

        private void OnStartClicked()
        {
            var manager = ExhibitionManager.Instance;
            if (manager == null) return;

            if (_canRetry)
                manager.RetryExhibition();
            else
                manager.StartExhibition();
        }

        private void HandleThemeSelected(ExhibitionTheme theme)
        {
            _canRetry = false;
            UpdateUI();
        }

        private void HandleInspirationsConfirmed(System.Collections.Generic.IReadOnlyList<InspirationData> inspirations)
        {
            _canRetry = false;
            UpdateUI();
        }

        private void HandleItemChanged(int slotIndex, ExhibitItemData item)
        {
            UpdateUI();
        }

        private void HandleItemRemoved(int slotIndex)
        {
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

        private void HandleStateChanged(ExhibitionState state)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            EnsureSelectButtonReferences();

            var manager = ExhibitionManager.Instance;
            var theme = manager?.CurrentTheme;
            bool isRunning = manager != null && manager.IsRunning;
            bool hasTheme = theme != null;
            bool hasIdeas = manager != null && manager.HasConfirmedInspirations;
            bool canStart = hasTheme && hasIdeas && !isRunning && manager.AreAllSlotsFilled();
            bool themeLocked = IsThemeLocked();

            if (_titleText != null)
            {
                if (theme == null)
                    _titleText.text = "Select a Theme";
                else if (!hasIdeas)
                    _titleText.text = theme.title;
                else
                    _titleText.text = theme.title;
            }

            if (_selectButton != null)
                _selectButton.interactable = !themeLocked && !isRunning;

            if (_selectButtonText != null)
                _selectButtonText.text = themeLocked ? _themeSelectedText : _selectText;

            if (_selectButtonPopup != null)
            {
                _selectButtonPopup.SetMessage(_themeLockedTooltipText);
                _selectButtonPopup.SetPopupEnabled(themeLocked);
            }

            if (_startButton != null)
                _startButton.interactable = _canRetry || canStart;

            if (_startButtonText != null)
            {
                if (isRunning)
                    _startButtonText.text = _runningText;
                else if (_canRetry)
                    _startButtonText.text = _retryText;
                else if (!hasIdeas)
                    _startButtonText.text = _lockedText;
                else
                    _startButtonText.text = _startText;
            }
        }

        private static bool IsThemeLocked()
        {
            var manager = ExhibitionManager.Instance;
            var theme = manager?.CurrentTheme;
            return theme != null && manager.HasConfirmedInspirations && !theme.isCompleted;
        }

        private void EnsureSelectButtonReferences()
        {
            if (_selectButton == null)
                return;

            if (_selectButtonText == null)
                _selectButtonText = _selectButton.GetComponentInChildren<TMP_Text>();

            if (_selectButtonPopup == null)
            {
                _selectButtonPopup = _selectButton.GetComponent<HoverTextPopup>();
                if (_selectButtonPopup == null)
                    _selectButtonPopup = _selectButton.gameObject.AddComponent<HoverTextPopup>();
            }

            _selectButtonPopup.SetMessage(_themeLockedTooltipText);
        }
    }
}
