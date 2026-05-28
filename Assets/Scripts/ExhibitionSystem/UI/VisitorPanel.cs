using System.Collections;
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
        [SerializeField] private RawImage _characterRawImage;
        [SerializeField] private CanvasGroup _characterCanvasGroup;
        [SerializeField] private TMP_Text _dialogueText;
        [SerializeField] private CanvasGroup _dialoguePanel;

        [Header("Character Generation")]
        [SerializeField] private VisitorCharacterGenerator _characterGenerator;
        [SerializeField] private float _characterFadeDuration = 0.25f;

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
        [SerializeField] private string _waitingMessage = "";

        [Header("Animation")]
        [SerializeField] private float _dialogueFadeDuration = 0.3f;

        // ── Runtime State ───────────────────────────────────────────────────────

        private bool _isShowingDialogue;
        private float _dialogueTargetAlpha;
        private float _dialogueCurrentAlpha;
        private string _pendingDialogueText;
        private bool _isFadingOut;

        // Character transition state
        private bool _isCharacterVisible;
        private Coroutine _characterTransitionCoroutine;

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
            // Initialize dialogue state
            _dialogueCurrentAlpha = 0f;
            _dialogueTargetAlpha = 0f;
            if (_dialoguePanel != null)
                _dialoguePanel.alpha = 0f;

            // Initialize character state (hidden)
            _isCharacterVisible = false;
            if (_characterCanvasGroup != null)
                _characterCanvasGroup.alpha = 0f;

            if (_dialogueText != null)
                _dialogueText.text = string.Empty;
        }

        private void Update()
        {
            if (_dialoguePanel == null) return;

            // Only animate if there's a difference
            if (Mathf.Abs(_dialogueCurrentAlpha - _dialogueTargetAlpha) > 0.01f)
            {
                float fadeSpeed = 1f / _dialogueFadeDuration;
                _dialogueCurrentAlpha = Mathf.MoveTowards(_dialogueCurrentAlpha, _dialogueTargetAlpha, fadeSpeed * Time.deltaTime);
                _dialoguePanel.alpha = _dialogueCurrentAlpha;

                // Fade out completed, now set new text and fade in
                if (_isFadingOut && _dialogueCurrentAlpha <= 0.01f && _pendingDialogueText != null)
                {
                    if (_dialogueText != null)
                        _dialogueText.text = _pendingDialogueText;

                    _pendingDialogueText = null;
                    _isFadingOut = false;
                    _dialogueTargetAlpha = 1f;
                }
            }
        }

        // ── Public Methods ──────────────────────────────────────────────────────

        /// <summary>
        /// Shows dialogue text with fade animation.
        /// If dialogue is already showing, fades out first then fades in with new text.
        /// </summary>
        public void ShowDialogue(string text)
        {
            if (_dialoguePanel == null) return;

            // If currently showing dialogue, fade out first then show new text
            if (_isShowingDialogue && _dialogueCurrentAlpha > 0.5f)
            {
                _pendingDialogueText = text;
                _dialogueTargetAlpha = 0f;
                _isFadingOut = true;
            }
            else
            {
                // Set text immediately and fade in
                if (_dialogueText != null)
                    _dialogueText.text = text;

                _pendingDialogueText = null;
                _isFadingOut = false;
                _dialogueTargetAlpha = 1f;
            }

            _isShowingDialogue = true;
        }

        /// <summary>
        /// Hides the dialogue panel with fade out animation.
        /// </summary>
        public void HideDialogue()
        {
            _dialogueTargetAlpha = 0f;
            _isShowingDialogue = false;
            _pendingDialogueText = null;
            _isFadingOut = false;
        }

        // ── Event Handlers ──────────────────────────────────────────────────────

        private void HandleThemeSelected(ExhibitionTheme theme)
        {
            if (theme != null)
                ShowDialogue($"Prepare the {theme.title}!");
            else
                HideDialogue();
        }

        private void HandleExhibitionStarted()
        {
            ShowDialogue("The visitors are arriving...");

            // Reset character area (hidden at start)
            if (_characterCanvasGroup != null)
                _characterCanvasGroup.alpha = 0f;
            _isCharacterVisible = false;
        }

        private void HandleVisitorReacted(int slotIndex, InspirationData inspiration, ExhibitItemData item, bool isCorrect, int satisfaction)
        {
            var manager = ExhibitionManager.Instance;
            if (manager == null) return;

            bool hasItem = item != null;

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

            // Trigger character transition
            if (_characterGenerator != null && _characterGenerator.IsConfigured)
            {
                if (_characterTransitionCoroutine != null)
                    StopCoroutine(_characterTransitionCoroutine);
                _characterTransitionCoroutine = StartCoroutine(TransitionToNewCharacter());
            }
        }

        private void HandleExhibitionEnded(bool success, int satisfaction, int threshold)
        {
            string message = success
                ? $"{_successMessage}\nMatches: {satisfaction}/{threshold}"
                : $"{_failureMessage}\nMatches: {satisfaction}/{threshold}. Rearrange the items and try again.";

            ShowDialogue(message);

            // Fade out character
            if (_characterCanvasGroup != null && _isCharacterVisible)
            {
                if (_characterTransitionCoroutine != null)
                    StopCoroutine(_characterTransitionCoroutine);
                _characterTransitionCoroutine = StartCoroutine(FadeOutCharacter());
            }
        }

        // ── Private Methods ─────────────────────────────────────────────────────

        private string GetRandomComment(string[] comments)
        {
            if (comments == null || comments.Length == 0)
                return "";

            return comments[Random.Range(0, comments.Length)];
        }

        // ── Character Transition Methods ────────────────────────────────────────

        private IEnumerator TransitionToNewCharacter()
        {
            if (_characterCanvasGroup == null) yield break;

            // Fade out (if currently visible)
            if (_isCharacterVisible)
            {
                yield return FadeCharacterAlpha(0f);
            }

            // Generate new character (renders to RenderTexture)
            if (_characterGenerator != null)
            {
                _characterGenerator.GenerateRandomVisitor();
            }

            // Fade in
            yield return FadeCharacterAlpha(1f);
            _isCharacterVisible = true;
        }

        private IEnumerator FadeOutCharacter()
        {
            if (_characterCanvasGroup == null) yield break;

            yield return FadeCharacterAlpha(0f);
            _isCharacterVisible = false;
        }

        private IEnumerator FadeCharacterAlpha(float targetAlpha)
        {
            if (_characterCanvasGroup == null) yield break;

            float startAlpha = _characterCanvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < _characterFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _characterFadeDuration;
                _characterCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            _characterCanvasGroup.alpha = targetAlpha;
        }
    }
}
