using System.Collections;
using StationWork.Core;
using StationWork.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StationWork.UI
{
    /// <summary>
    /// Shows the current passenger and acts as the drop target for item cards.
    ///
    /// PREFAB / SCENE STRUCTURE (example)
    ///   PassengerPanel  ← this component + Image (background)
    ///   ├── Avatar      ← Image  →  _avatar
    ///   ├── Name        ← TextMeshProUGUI  →  _nameText
    ///   ├── Dialogue    ← TextMeshProUGUI  →  _dialogueText
    ///   ├── Requirement ← TextMeshProUGUI  →  _requirementText  ("Looking for: Bird watching")
    ///   ├── DropZone    ← child GameObject + Image  →  _dropZoneHighlight (tinted when hovered)
    ///   └── Feedback    ← TextMeshProUGUI  →  _feedbackText
    ///
    /// SETUP
    ///   StationWorkManager must be in the scene and Begin() called to start.
    /// </summary>
    public class PassengerPanel : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Passenger Display")]
        [SerializeField] private Image            _avatar;
        [SerializeField] private TextMeshProUGUI  _nameText;
        [SerializeField] private TextMeshProUGUI  _dialogueText;
        [SerializeField] private TextMeshProUGUI  _requirementText;

        [Header("Drop Zone")]
        [SerializeField] private Image            _dropZoneHighlight;
        [SerializeField] private Color            _dropZoneNormal   = new Color(1, 1, 1, 0.1f);
        [SerializeField] private Color            _dropZoneHovered  = new Color(1, 1, 0.3f, 0.4f);

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI  _feedbackText;
        [SerializeField] private float            _feedbackDuration = 1.5f;

        [Header("Summary (shown after all passengers)")]
        [SerializeField] private GameObject       _summaryRoot;
        [SerializeField] private TextMeshProUGUI  _summaryText;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void OnEnable()
        {
            StationWorkManager.OnPassengerArrived += HandlePassengerArrived;
            StationWorkManager.OnItemSubmitted    += HandleItemSubmitted;
            StationWorkManager.OnPhaseComplete    += HandlePhaseComplete;
        }

        private void OnDisable()
        {
            StationWorkManager.OnPassengerArrived -= HandlePassengerArrived;
            StationWorkManager.OnItemSubmitted    -= HandleItemSubmitted;
            StationWorkManager.OnPhaseComplete    -= HandlePhaseComplete;
        }

        private void Start()
        {
            SetDropZoneColor(_dropZoneNormal);
            SetFeedback(string.Empty);
            if (_summaryRoot != null) _summaryRoot.SetActive(false);
        }

        // ── IDropHandler ───────────────────────────────────────────────────────

        public void OnDrop(PointerEventData eventData)
        {
            SetDropZoneColor(_dropZoneNormal);
            var item = ItemCardUI.CurrentlyDragging;
            if (item == null) return;

            StationWorkManager.Instance?.SubmitItem(item);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (ItemCardUI.CurrentlyDragging != null)
                SetDropZoneColor(_dropZoneHovered);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetDropZoneColor(_dropZoneNormal);
        }

        // ── Event handlers ─────────────────────────────────────────────────────

        private void HandlePassengerArrived(PassengerData passenger)
        {
            SetFeedback(string.Empty);
            gameObject.SetActive(true);

            if (_avatar != null)
            {
                _avatar.sprite  = passenger.avatar;
                _avatar.enabled = passenger.avatar != null;
            }

            if (_nameText       != null) _nameText.text       = passenger.passengerName;
            if (_dialogueText   != null) _dialogueText.text   = passenger.dialogue;
            if (_requirementText != null)
                _requirementText.text = $"Looking for: <b>{passenger.requiredLabel}</b>";
        }

        private void HandleItemSubmitted(bool correct, bool passengerLeaving)
        {
            if (correct)
                SetFeedback("The passenger is delighted and decides to stay!");
            else if (passengerLeaving)
                SetFeedback("The passenger left disappointed...");
            else
                SetFeedback("Not quite... Try again!");

            if (passengerLeaving)
                StartCoroutine(ClearAfterDelay(_feedbackDuration));
        }

        private void HandlePhaseComplete(int score, int total)
        {
            gameObject.SetActive(false);

            if (_summaryRoot != null)
            {
                _summaryRoot.SetActive(true);
                if (_summaryText != null)
                    _summaryText.text = $"{score}/{total} passengers decided to stay.";
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private void SetFeedback(string msg)
        {
            if (_feedbackText != null) _feedbackText.text = msg;
        }

        private void SetDropZoneColor(Color c)
        {
            if (_dropZoneHighlight != null) _dropZoneHighlight.color = c;
        }

        private IEnumerator ClearAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            SetFeedback(string.Empty);
        }
    }
}
