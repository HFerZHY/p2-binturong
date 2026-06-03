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
        [SerializeField] private string _missingItemsText = "Place Items";
        [SerializeField] private string _missingLabelsText = "Choose Labels";

        private bool _canRetry;
        private HoverTextPopup _selectButtonPopup;

        public Button SelectButton => _selectButton;
        public Button StartButton => _startButton;

        private void OnEnable()
        {
            ExhibitionManager.OnThemeSelected += HandleThemeSelected;
            ExhibitionManager.OnSlotInspirationChanged += HandleSlotInspirationChanged;
            ExhibitionManager.OnItemPlaced += HandleItemChanged;
            ExhibitionManager.OnItemRemoved += HandleItemRemoved;
            ExhibitionManager.OnItemsSwapped += HandleItemsSwapped;
            ExhibitionManager.OnExhibitionStarted += HandleExhibitionStarted;
            ExhibitionManager.OnExhibitionEnded += HandleExhibitionEnded;
            ExhibitionManager.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            ExhibitionManager.OnThemeSelected -= HandleThemeSelected;
            ExhibitionManager.OnSlotInspirationChanged -= HandleSlotInspirationChanged;
            ExhibitionManager.OnItemPlaced -= HandleItemChanged;
            ExhibitionManager.OnItemRemoved -= HandleItemRemoved;
            ExhibitionManager.OnItemsSwapped -= HandleItemsSwapped;
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
            OnSelectThemeClicked?.Invoke();

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
            _canRetry = ExhibitionManager.Instance?.HasValidationFeedback ?? false;
            UpdateUI();
        }

        private void HandleSlotInspirationChanged(int slotIndex, InspirationData inspiration)
        {
            _canRetry = false;
            UpdateUI();
        }

        private void HandleItemChanged(int slotIndex, ExhibitItemData item)
        {
            _canRetry = false;
            UpdateUI();
        }

        private void HandleItemRemoved(int slotIndex)
        {
            _canRetry = false;
            UpdateUI();
        }

        private void HandleItemsSwapped(int slotA, int slotB)
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
            bool allLabelsFilled = manager != null && manager.HasAllLabelsFilled;
            bool allSlotsFilled = manager != null && manager.AreAllSlotsFilled();
            bool canStart = hasTheme && !isRunning && manager.AreAllSlotsReady();

            if (_titleText != null)
            {
                if (theme == null)
                    _titleText.text = "Select a Theme";
                else
                    _titleText.text = theme.title;
            }

            if (_selectButton != null)
                _selectButton.interactable = !isRunning;

            if (_selectButtonText != null)
                _selectButtonText.text = _selectText;

            if (_selectButtonPopup != null)
            {
                _selectButtonPopup.SetMessage(_themeLockedTooltipText);
                _selectButtonPopup.SetPopupEnabled(false);
            }

            if (_startButton != null)
                _startButton.interactable = canStart;

            if (_startButtonText != null)
            {
                if (isRunning)
                    _startButtonText.text = _runningText;
                else if (_canRetry)
                    _startButtonText.text = _retryText;
                else if (!hasTheme)
                    _startButtonText.text = _lockedText;
                else if (!allSlotsFilled)
                    _startButtonText.text = _missingItemsText;
                else if (!allLabelsFilled)
                    _startButtonText.text = string.IsNullOrWhiteSpace(_missingLabelsText)
                        ? _lockedText
                        : _missingLabelsText;
                else
                    _startButtonText.text = _startText;
            }
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
