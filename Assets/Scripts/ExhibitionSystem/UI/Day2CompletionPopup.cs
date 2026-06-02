using ExhibitionSystem.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    public class Day2CompletionPopup : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _headlineText;
        [SerializeField] private TMP_Text _bodyText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private string _nextSceneName = "Day2World";

        private const string HeadlineText = "Exhibition Success";
        private const string BodyText = "Today's work is done.";

        private bool _pendingShow;
        private bool _isTransitioning;

        private void OnEnable()
        {
            ExhibitionManager.OnExhibitionEnded += HandleExhibitionEnded;
            RewardPopup.OnRewardConfirmed += HandleRewardConfirmed;
        }

        private void OnDisable()
        {
            ExhibitionManager.OnExhibitionEnded -= HandleExhibitionEnded;
            RewardPopup.OnRewardConfirmed -= HandleRewardConfirmed;
        }

        private void Start()
        {
            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(HandleConfirmClicked);

            Hide();
        }

        private void HandleExhibitionEnded(bool success, int satisfaction, int threshold)
        {
            if (!success || threshold <= 0 || satisfaction < threshold || !AreAllThemesCompleted())
                return;

            _pendingShow = true;
        }

        private void HandleRewardConfirmed()
        {
            if (!_pendingShow)
                return;

            _pendingShow = false;
            Show();
        }

        private void Show()
        {
            if (_headlineText != null)
                _headlineText.text = HeadlineText;

            if (_bodyText != null)
                _bodyText.text = BodyText;

            if (_panel != null)
                _panel.SetActive(true);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }
        }

        private void HandleConfirmClicked()
        {
            if (_isTransitioning)
                return;

            _isTransitioning = true;
            if (_confirmButton != null)
                _confirmButton.interactable = false;

            StartCoroutine(ExhibitionSceneFadeTransition.FadeOutAndLoad(_nextSceneName));
        }

        private void Hide()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            if (_panel != null && _panel != gameObject)
                _panel.SetActive(false);
        }

        private static bool AreAllThemesCompleted()
        {
            var manager = ExhibitionManager.Instance;
            if (manager == null || manager.AllThemes == null || manager.AllThemes.Count == 0)
                return false;

            foreach (var theme in manager.AllThemes)
            {
                if (theme == null || !theme.isCompleted)
                    return false;
            }

            return true;
        }
    }
}
