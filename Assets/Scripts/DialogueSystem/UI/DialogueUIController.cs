using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DialogueSystem.Data;
using DialogueSystem.Interfaces;

namespace DialogueSystem.UI
{
    /// <summary>
    /// Concrete implementation of IDialogueView.
    /// Controls the dialogue canvas: speaker name, portrait, body text (typewriter),
    /// choice buttons, and the interaction prompt.
    ///
    /// Hierarchy expected:
    ///   DialogueCanvas (Canvas)
    ///   └─ DialoguePanel
    ///      ├─ PortraitImage        (Image)
    ///      ├─ SpeakerNameText      (TMP_Text)
    ///      ├─ DialogueBodyText     (TMP_Text)
    ///      ├─ ContinueIndicator    (GameObject — blinking arrow)
    ///      └─ ChoicesContainer     (LayoutGroup)
    ///         └─ [ChoiceButton prefab instantiated at runtime]
    ///   InteractPromptText         (TMP_Text — outside panel, always visible)
    /// </summary>
    public class DialogueUIController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Panel")]
        [SerializeField] private GameObject dialoguePanel;

        [Header("Content")]
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private TMP_Text dialogueBodyText;
        [SerializeField] private Button continueIndicator;   // Blinking "▼" or similar

        [Header("Choices")]
        [SerializeField] private Transform choicesContainer;
        [SerializeField] private Button choiceButtonPrefab;       // Prefab with TMP_Text child

        [Header("Prompt")]
        [SerializeField] private TMP_Text interactPromptText;
        [SerializeField] private GameObject interactPromptRoot;   // Parent of the prompt (for hide/show)

        [Header("Typewriter")]
        [Tooltip("Characters per second. Overridden per-node by DialogueNode.typewriterSpeed if > 0.")]
        [SerializeField] private float defaultTypewriterSpeed = 0.03f;

        // ── IDialogueView: state ──────────────────────────────────────────────

        public bool IsTyping => _typewriterCoroutine != null;

        // ── Private state ─────────────────────────────────────────────────────

        private Coroutine _typewriterCoroutine;
        private Action _onTypingComplete;
        private string _fullText;
        private readonly List<Button> _choiceButtons = new();

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            dialoguePanel.SetActive(false);
            if (continueIndicator) continueIndicator.gameObject.SetActive(false);
            if (interactPromptRoot) interactPromptRoot.SetActive(false);
        }

        // ── IDialogueView implementation ──────────────────────────────────────

        public void ShowLine(DialogueNode node, Action onTypingComplete)
        {
            dialoguePanel.SetActive(true);
            if (continueIndicator) continueIndicator.gameObject.SetActive(false);

            // Speaker
            if (speakerNameText != null)
                speakerNameText.text = node.speakerName;

            // Portrait
            if (portraitImage != null)
            {
                portraitImage.sprite = node.speakerPortrait;
                portraitImage.gameObject.SetActive(node.speakerPortrait != null);
            }

            // Body text (typewriter)
            _fullText = node.text;
            _onTypingComplete = onTypingComplete;

            float speed = node.typewriterSpeed > 0 ? node.typewriterSpeed : defaultTypewriterSpeed;

            if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = StartCoroutine(TypewriterRoutine(_fullText, speed));
        }

        public void SkipTypewriter()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            if (dialogueBodyText != null)
                dialogueBodyText.text = _fullText;

            FinishTyping();
        }

        public void ShowChoices(IReadOnlyList<(DialogueChoice choice, bool interactable)> choices,
                                Action<DialogueChoice> onChosen)
        {
            ClearChoiceButtons();

            foreach (var (choice, isInteractable) in choices)
            {
                var btn = Instantiate(choiceButtonPrefab, choicesContainer);
                var label = btn.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = choice.label;
                btn.interactable = isInteractable;

                // Capture for lambda
                var captured = choice;
                btn.onClick.AddListener(() => onChosen?.Invoke(captured));

                _choiceButtons.Add(btn);
            }
        }

        public void HideChoices() => ClearChoiceButtons();

        public void HideUI()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            ClearChoiceButtons();
            dialoguePanel.SetActive(false);
            if (continueIndicator) continueIndicator.gameObject.SetActive(false);
        }

        public void ShowInteractPrompt(string prompt)
        {
            if (interactPromptRoot) interactPromptRoot.SetActive(true);
            if (interactPromptText) interactPromptText.text = prompt;
        }

        public void HideInteractPrompt()
        {
            if (interactPromptRoot) interactPromptRoot.SetActive(false);
        }

        // ── Typewriter ────────────────────────────────────────────────────────

        private IEnumerator TypewriterRoutine(string text, float secondsPerChar)
        {
            if (dialogueBodyText == null) yield break;

            dialogueBodyText.text = string.Empty;

            foreach (char c in text)
            {
                dialogueBodyText.text += c;
                yield return new WaitForSeconds(secondsPerChar);
            }

            _typewriterCoroutine = null;
            FinishTyping();
        }

        private void FinishTyping()
        {
            if (continueIndicator) continueIndicator.gameObject.SetActive(true);
            _onTypingComplete?.Invoke();
            _onTypingComplete = null;
        }

        // ── Choice button helpers ─────────────────────────────────────────────

        private void ClearChoiceButtons()
        {
            foreach (var btn in _choiceButtons)
                if (btn != null) Destroy(btn.gameObject);
            _choiceButtons.Clear();
        }
    }
}
