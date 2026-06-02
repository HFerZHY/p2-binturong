using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using DialogueSystem.Core;
using DialogueSystem.Data;
using DialogueSystem.Interfaces;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.Serialization;
using Otowa.IndoorDialogue;

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

        [Header("Map Style")]
        [SerializeField] private TMP_FontAsset dialogueFont;

        // ── IDialogueView: state ──────────────────────────────────────────────

        public bool IsTyping => _typewriterCoroutine != null;

        // ── Private state ─────────────────────────────────────────────────────

        private Coroutine _typewriterCoroutine;
        private Action _onTypingComplete;
        private string _fullText;
        private readonly List<string> _pages = new();
        private int _pageIndex;
        private float _secondsPerChar;
        private float _paginationWidth = -1f;
        private readonly List<Button> _choiceButtons = new();
        private LocalizationManager _localizationManager;

        private const int MAX_LINES_PER_PAGE = 3;
        private const float PAGINATION_WIDTH_EPSILON = 0.5f;

        private static readonly Color PanelBg    = new Color32(0x06, 0x0e, 0x06, 0xE8);
        private static readonly Color BodyFg     = new Color32(0xc8, 0xd4, 0xc8, 0xFF);
        private static readonly Color RinFg      = new Color32(0x8f, 0xbc, 0x8f, 0xFF);
        private static readonly Color JunkoFg    = new Color32(0xd4, 0xa0, 0x60, 0xFF);
        private static readonly Color YujiFg     = new Color32(0x80, 0xb8, 0xe8, 0xFF);
        private static readonly Color JiroFg     = new Color32(0x98, 0x98, 0xa8, 0xFF);
        private static readonly Color InspectorFg = new Color32(0xa0, 0xa8, 0xc0, 0xFF);

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            ApplyMapStyle();
            dialoguePanel.SetActive(false);
            if (continueIndicator) continueIndicator.gameObject.SetActive(false);
            if (interactPromptRoot) interactPromptRoot.SetActive(false);
        }

        private void Update()
        {
            RefreshPaginationIfLayoutChanged();

            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            if (dialoguePanel == null || !dialoguePanel.activeInHierarchy) return;
            DialogueManager.Instance?.AdvanceDialogue();
        }

        // ── IDialogueView implementation ──────────────────────────────────────

        public void ShowLine(DialogueNode node, Action onTypingComplete)
        {
            dialoguePanel.SetActive(true);
            if (continueIndicator) continueIndicator.gameObject.SetActive(false);

            // Speaker
            if (speakerNameText != null && node.speaker != null)
            {
                speakerNameText.text = ResolveCharacterName(node.speaker);
                speakerNameText.color = GetSpeakerColor(node.speaker.characterName);
            }
            else if (speakerNameText != null)
            {
                speakerNameText.text = string.Empty;
            }

            // Body text (typewriter)
            _fullText = ResolveDialogueText(node);
            _onTypingComplete = onTypingComplete;
            _secondsPerChar = node.typewriterSpeed > 0 ? node.typewriterSpeed : defaultTypewriterSpeed;
            BuildPages(_fullText);
            _pageIndex = 0;

            if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
            StartCurrentPage();
        }

        public void SkipTypewriter()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            if (dialogueBodyText != null)
                dialogueBodyText.maxVisibleCharacters = int.MaxValue;

            FinishCurrentPage();
        }

        public bool TryAdvancePage()
        {
            if (_typewriterCoroutine != null)
            {
                SkipTypewriter();
                return true;
            }

            if (_pageIndex + 1 >= _pages.Count)
                return false;

            _pageIndex++;
            StartCurrentPage();
            return true;
        }

        public void ShowChoices(IReadOnlyList<(DialogueChoice choice, bool interactable)> choices,
                                Action<DialogueChoice> onChosen)
        {
            ClearChoiceButtons();
            if (choicesContainer != null)
                choicesContainer.gameObject.SetActive(true);

            foreach (var (choice, isInteractable) in choices)
            {
                var btn = Instantiate(choiceButtonPrefab, choicesContainer);
                ApplyChoiceStyle(btn);
                var label = btn.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = !string.IsNullOrEmpty(choice.literalLabel)
                        ? choice.literalLabel
                        : ResolveDialogueChoice(choice.labelKey);
                }
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
            _pages.Clear();
            _pageIndex = 0;
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

        private void BuildPages(string text)
        {
            _pages.Clear();
            string remaining = (text ?? string.Empty).Trim();
            if (dialogueBodyText == null)
            {
                _pages.Add(remaining);
                return;
            }

            Canvas.ForceUpdateCanvases();
            _paginationWidth = dialogueBodyText.rectTransform.rect.width;

            var previousOverflowMode = dialogueBodyText.overflowMode;
            int previousMaxVisibleLines = dialogueBodyText.maxVisibleLines;
            dialogueBodyText.overflowMode = TextOverflowModes.Overflow;
            dialogueBodyText.maxVisibleLines = int.MaxValue;

            while (!string.IsNullOrEmpty(remaining))
            {
                dialogueBodyText.text = remaining;
                dialogueBodyText.maxVisibleCharacters = int.MaxValue;
                dialogueBodyText.ForceMeshUpdate();

                var textInfo = dialogueBodyText.textInfo;
                if (textInfo.lineCount <= MAX_LINES_PER_PAGE)
                {
                    _pages.Add(remaining);
                    break;
                }

                int lineIndex = Mathf.Min(MAX_LINES_PER_PAGE - 1, textInfo.lineInfo.Length - 1);
                int characterIndex = textInfo.lineInfo[lineIndex].lastCharacterIndex;
                if (characterIndex < 0 || characterIndex >= textInfo.characterCount)
                {
                    _pages.Add(remaining);
                    break;
                }

                int splitIndex = textInfo.characterInfo[characterIndex].index + 1;
                if (splitIndex <= 0 || splitIndex >= remaining.Length)
                {
                    _pages.Add(remaining);
                    break;
                }

                _pages.Add(remaining.Substring(0, splitIndex).TrimEnd());
                remaining = remaining.Substring(splitIndex).TrimStart();
            }

            if (_pages.Count == 0)
                _pages.Add(string.Empty);

            dialogueBodyText.overflowMode = previousOverflowMode;
            dialogueBodyText.maxVisibleLines = previousMaxVisibleLines;
            dialogueBodyText.text = string.Empty;
        }

        private void RefreshPaginationIfLayoutChanged()
        {
            if (dialoguePanel == null
                || !dialoguePanel.activeInHierarchy
                || dialogueBodyText == null
                || string.IsNullOrEmpty(_fullText)
                || _pages.Count == 0)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            float currentWidth = dialogueBodyText.rectTransform.rect.width;
            if (Mathf.Abs(currentWidth - _paginationWidth) <= PAGINATION_WIDTH_EPSILON)
                return;

            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            BuildPages(_fullText);
            _pageIndex = 0;
            StartCurrentPage();
        }

        private void StartCurrentPage()
        {
            if (dialogueBodyText == null) return;
            if (continueIndicator) continueIndicator.gameObject.SetActive(false);

            if (_typewriterCoroutine != null)
                StopCoroutine(_typewriterCoroutine);

            _typewriterCoroutine = StartCoroutine(TypewriterRoutine(_pages[_pageIndex], _secondsPerChar));
        }

        private IEnumerator TypewriterRoutine(string text, float secondsPerChar)
        {
            if (dialogueBodyText == null) yield break;

            dialogueBodyText.text = text;
            dialogueBodyText.ForceMeshUpdate();
            int characterCount = dialogueBodyText.textInfo.characterCount;
            dialogueBodyText.maxVisibleCharacters = 0;

            for (int i = 1; i <= characterCount; i++)
            {
                dialogueBodyText.maxVisibleCharacters = i;
                yield return new WaitForSeconds(secondsPerChar);
            }

            _typewriterCoroutine = null;
            FinishCurrentPage();
        }

        private void FinishCurrentPage()
        {
            if (continueIndicator) continueIndicator.gameObject.SetActive(true);
            if (_pageIndex + 1 < _pages.Count) return;

            _onTypingComplete?.Invoke();
            _onTypingComplete = null;
        }

        // ── Choice button helpers ─────────────────────────────────────────────

        private void ApplyMapStyle()
        {
            if (dialoguePanel != null)
            {
                SetRect(dialoguePanel.transform as RectTransform,
                    new Vector2(0.25f, 0.035f), new Vector2(0.75f, 0.29f));

                var panelImage = dialoguePanel.GetComponent<Image>();
                if (panelImage != null)
                    panelImage.color = PanelBg;
            }

            if (portraitImage != null)
            {
                portraitImage.raycastTarget = false;
                portraitImage.gameObject.SetActive(false);
            }

            ConfigureText(speakerNameText, 28f, FontStyles.Bold,
                TextAlignmentOptions.Left,
                new Vector2(0.055f, 0.70f), new Vector2(0.945f, 0.93f));

            ConfigureText(dialogueBodyText, 24f, FontStyles.Normal,
                TextAlignmentOptions.Left,
                new Vector2(0.055f, 0.12f), new Vector2(0.945f, 0.69f));

            if (dialogueBodyText != null)
            {
                dialogueBodyText.color = BodyFg;
                dialogueBodyText.lineSpacing = 3f;
                dialogueBodyText.textWrappingMode = TextWrappingModes.Normal;
                dialogueBodyText.overflowMode = TextOverflowModes.Truncate;
                dialogueBodyText.maxVisibleLines = MAX_LINES_PER_PAGE;
            }

            if (interactPromptRoot != null)
            {
                SetRect(interactPromptRoot.transform as RectTransform,
                    new Vector2(0.34f, 0.72f), new Vector2(0.66f, 0.82f));
            }

            if (interactPromptText != null)
            {
                if (dialogueFont != null) interactPromptText.font = dialogueFont;
                interactPromptText.fontSize = 26f;
                interactPromptText.color = Color.black;
                interactPromptText.alignment = TextAlignmentOptions.Center;
                interactPromptText.raycastTarget = false;
            }

            if (choicesContainer == null) return;

            if (dialoguePanel != null && dialoguePanel.transform.parent != null)
                choicesContainer.SetParent(dialoguePanel.transform.parent, false);
            SetRect(choicesContainer as RectTransform,
                new Vector2(0.25f, 0.32f), new Vector2(0.75f, 0.72f));

            IndoorDialogueChoiceStyle.ConfigureContainer(choicesContainer.gameObject);

            choicesContainer.gameObject.SetActive(false);
        }

        private void ApplyChoiceStyle(Button button)
        {
            if (button == null) return;

            IndoorDialogueChoiceStyle.ApplyButton(button, dialogueFont);
        }

        private void ConfigureText(TMP_Text text, float fontSize, FontStyles style,
                                   TextAlignmentOptions alignment,
                                   Vector2 anchorMin, Vector2 anchorMax)
        {
            if (text == null) return;
            if (dialogueFont != null) text.font = dialogueFont;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.margin = Vector4.zero;
            text.raycastTarget = false;
            SetRect(text.rectTransform, anchorMin, anchorMax);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (rect == null) return;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Color GetSpeakerColor(string characterName)
        {
            return characterName?.ToLowerInvariant() switch
            {
                "rin"       => RinFg,
                "junko"     => JunkoFg,
                "yuji"      => YujiFg,
                "jiro"      => JiroFg,
                "inspector" => InspectorFg,
                _           => BodyFg,
            };
        }

        private string ResolveCharacterName(Character speaker)
        {
            if (speaker == null || string.IsNullOrEmpty(speaker.characterName))
                return string.Empty;
            if (speaker.characterName == "???")
                return "???";

            var localization = GetLocalizationManager();
            return localization != null && localization.characterNameTable != null
                ? localization.GetCharacterName(speaker.characterName)
                : speaker.characterName;
        }

        private string ResolveDialogueText(DialogueNode node)
        {
            if (!string.IsNullOrEmpty(node.literalText))
                return node.literalText;

            var localization = GetLocalizationManager();
            return localization != null && localization.dialogueTextTable != null
                ? localization.GetDialogueText(node.textKey)
                : node.textKey ?? string.Empty;
        }

        private string ResolveDialogueChoice(string labelKey)
        {
            var localization = GetLocalizationManager();
            return localization != null && localization.dialogueChoiceLabelTable != null
                ? localization.GetDialogueChoice(labelKey)
                : labelKey ?? string.Empty;
        }

        private LocalizationManager GetLocalizationManager()
        {
            if (_localizationManager == null)
            {
                _localizationManager = FindFirstObjectByType<LocalizationManager>(
                    FindObjectsInactive.Include);
            }

            return _localizationManager;
        }

        private void ClearChoiceButtons()
        {
            foreach (var btn in _choiceButtons)
                if (btn != null) Destroy(btn.gameObject);
            _choiceButtons.Clear();
            if (choicesContainer != null)
                choicesContainer.gameObject.SetActive(false);
        }
    }
}
