using ExhibitionSystem.Core;
using ExhibitionSystem.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    /// <summary>
    /// Displays the visitor area with character and dialogue.
    /// Shows visitor reactions during exhibition evaluation.
    /// </summary>
    public class VisitorPanel : MonoBehaviour
    {
        // ── Serialized Fields ───────────────────────────────────────────────────

        [Header("UI References")]
        [SerializeField] private Image _characterImage;
        [SerializeField] private TMP_Text _dialogueText;
        [SerializeField] private CanvasGroup _dialoguePanel;

        [Header("Default Comments")]
        [SerializeField] private string[] _positiveComments =
        {
            "Wonderful! This piece fits perfectly!",
            "Excellent choice for this exhibition!",
            "This is exactly what I was hoping to see!",
            "Marvelous! A perfect addition!"
        };

        [SerializeField] private string[] _negativeComments =
        {
            "Hmm, this doesn't quite fit the theme...",
            "I'm not sure this belongs here.",
            "This seems out of place.",
            "Interesting, but not what I expected."
        };

        [SerializeField] private string[] _emptySlotComments =
        {
            "There's nothing here to see...",
            "This display case is empty.",
            "Was something supposed to be here?"
        };

        [Header("Result Messages")]
        [SerializeField] private string _successMessage = "The exhibition was a great success!";
        [SerializeField] private string _failureMessage = "The exhibition didn't quite meet expectations...";
        [SerializeField] private string _waitingMessage = "Place items in the display slots to begin.";

        [Header("Animation")]
        [SerializeField] private float _dialogueFadeDuration = 0.3f;

        // ── Runtime State ───────────────────────────────────────────────────────

        private bool _isShowingDialogue;

        // ── Unity Lifecycle ─────────────────────────────────────────────────────

        private void OnEnable()
        {
            ExhibitionManager.OnThemeSelected += HandleThemeSelected;
            ExhibitionManager.OnExhibitionStarted += HandleExhibitionStarted;
            ExhibitionManager.OnVisitorReacted += HandleVisitorReacted;
            ExhibitionManager.OnExhibitionEnded += HandleExhibitionEnded;
        }

        private void OnDisable()
        {
            ExhibitionManager.OnThemeSelected -= HandleThemeSelected;
            ExhibitionManager.OnExhibitionStarted -= HandleExhibitionStarted;
            ExhibitionManager.OnVisitorReacted -= HandleVisitorReacted;
            ExhibitionManager.OnExhibitionEnded -= HandleExhibitionEnded;
        }

        private void Start()
        {
            ShowDialogue(_waitingMessage);
        }

        // ── Public Methods ──────────────────────────────────────────────────────

        /// <summary>
        /// Shows dialogue text.
        /// </summary>
        public void ShowDialogue(string text)
        {
            if (_dialogueText != null)
                _dialogueText.text = text;

            if (_dialoguePanel != null)
            {
                _dialoguePanel.alpha = 1f;
                _isShowingDialogue = true;
            }
        }

        /// <summary>
        /// Hides the dialogue panel.
        /// </summary>
        public void HideDialogue()
        {
            if (_dialoguePanel != null)
            {
                _dialoguePanel.alpha = 0f;
                _isShowingDialogue = false;
            }
        }

        // ── Event Handlers ──────────────────────────────────────────────────────

        private void HandleThemeSelected(ExhibitionTheme theme)
        {
            if (theme != null)
                ShowDialogue($"Prepare the {theme.title}!");
            else
                ShowDialogue(_waitingMessage);
        }

        private void HandleExhibitionStarted()
        {
            ShowDialogue("The visitors are arriving...");
        }

        private void HandleVisitorReacted(int slotIndex, bool isCorrect, int satisfaction)
        {
            var manager = ExhibitionManager.Instance;
            if (manager == null) return;

            var displaySlots = manager.DisplaySlots;
            bool hasItem = slotIndex < displaySlots.Count && displaySlots[slotIndex] != null;

            string comment;
            if (!hasItem)
            {
                comment = GetRandomComment(_emptySlotComments);
            }
            else if (isCorrect)
            {
                comment = GetRandomComment(_positiveComments);
            }
            else
            {
                comment = GetRandomComment(_negativeComments);
            }

            ShowDialogue(comment);
        }

        private void HandleExhibitionEnded(bool success, int satisfaction, int threshold)
        {
            string message = success
                ? $"{_successMessage}\nSatisfaction: {satisfaction}/{threshold} required"
                : $"{_failureMessage}\nSatisfaction: {satisfaction}/{threshold} required";

            ShowDialogue(message);
        }

        // ── Private Methods ─────────────────────────────────────────────────────

        private string GetRandomComment(string[] comments)
        {
            if (comments == null || comments.Length == 0)
                return "";

            return comments[Random.Range(0, comments.Length)];
        }
    }
}
