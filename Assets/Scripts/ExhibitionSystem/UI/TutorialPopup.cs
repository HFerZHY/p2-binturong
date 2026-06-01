using System.Collections.Generic;
using ExhibitionSystem.Core;
using ExhibitionSystem.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    public class TutorialPopup : MonoBehaviour
    {
        private const string RIN_SPRITE_RESOURCE = "Characters/WorldSprite/rin";
        private const string RIN_HEAD_SPRITE_NAME = "spritesheet_template_0";

        [Header("UI References")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Image _portraitImage;
        [SerializeField] private TMP_Text _speakerText;
        [SerializeField] private TMP_Text _bodyText;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Portrait")]
        [SerializeField] private Sprite _rinHeadSprite;

        [Header("Messages")]
        [SerializeField] private string _speakerName = "Rin";
        [SerializeField] private string _selectThemeMessage =
            "I came up with a few exhibition themes yesterday. For now, I should choose one first.";
        [SerializeField] private string _chooseInspirationsMessage =
            "I need to pick a few inspirations that fit this theme...";
        [SerializeField] private string _arrangeItemsMessage =
            "Next, I should drag the exhibits into the positions that match these inspirations.";
        [SerializeField] private string _startExhibitionMessage =
            "Great! Now I can let the passengers visit the exhibition!";
        [SerializeField] private string _tryAnotherThemeMessage =
            "Great job! To finish today's work, try another theme.";

        private bool _selectThemeDismissed;
        private bool _inspirationHintDismissed;
        private bool _arrangementHintDismissed;
        private bool _startHintDismissed;
        private bool _startHintShown;
        private bool _tryAnotherThemeHintShown;
        private bool _tryAnotherThemeHintDismissed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        internal static void EnsureTutorialPopupExists()
        {
            if (FindFirstObjectByType<TutorialPopup>(FindObjectsInactive.Include) != null)
                return;

            if (FindFirstObjectByType<ExhibitionManager>(FindObjectsInactive.Include) == null)
                return;

            var layoutRoot = GameObject.Find("LayoutRoot");
            if (layoutRoot == null)
                return;

            CreateRuntimePopup(layoutRoot.transform);
        }

        private void OnEnable()
        {
            ThemeSelector.OnSelectThemeClicked += HandleSelectThemeClicked;
            ExhibitionManager.OnThemeSelected += HandleThemeSelected;
            ExhibitionManager.OnInspirationValidationAttempted += HandleInspirationValidationAttempted;
            InspirationSuccessPopup.OnSuccessConfirmed += HandleInspirationSuccessConfirmed;
            InspirationSelectionPopup.OnInspirationClicked += HandleInspirationClicked;
            ExhibitionManager.OnItemPlaced += HandleItemPlaced;
            ExhibitionManager.OnItemRemoved += HandleItemChanged;
            ExhibitionManager.OnItemsSwapped += HandleItemsSwapped;
            ExhibitionManager.OnExhibitionStarted += HandleExhibitionStarted;
            ExhibitionManager.OnCurationCleared += HandleCurationCleared;
            RewardPopup.OnRewardConfirmed += HandleRewardConfirmed;
        }

        private void OnDisable()
        {
            ThemeSelector.OnSelectThemeClicked -= HandleSelectThemeClicked;
            ExhibitionManager.OnThemeSelected -= HandleThemeSelected;
            ExhibitionManager.OnInspirationValidationAttempted -= HandleInspirationValidationAttempted;
            InspirationSuccessPopup.OnSuccessConfirmed -= HandleInspirationSuccessConfirmed;
            InspirationSelectionPopup.OnInspirationClicked -= HandleInspirationClicked;
            ExhibitionManager.OnItemPlaced -= HandleItemPlaced;
            ExhibitionManager.OnItemRemoved -= HandleItemChanged;
            ExhibitionManager.OnItemsSwapped -= HandleItemsSwapped;
            ExhibitionManager.OnExhibitionStarted -= HandleExhibitionStarted;
            ExhibitionManager.OnCurationCleared -= HandleCurationCleared;
            RewardPopup.OnRewardConfirmed -= HandleRewardConfirmed;
        }

        private void Start()
        {
            ConfigurePortrait();

            var manager = ExhibitionManager.Instance;
            if (!_selectThemeDismissed && (manager == null || manager.CurrentTheme == null))
                Show(_selectThemeMessage);
            else
                Hide();
        }

        private void HandleSelectThemeClicked()
        {
            if (_tryAnotherThemeHintShown)
            {
                _tryAnotherThemeHintShown = false;
                _tryAnotherThemeHintDismissed = true;
                Hide();
                return;
            }

            if (_selectThemeDismissed)
                return;

            _selectThemeDismissed = true;
            Hide();
        }

        private void HandleThemeSelected(ExhibitionTheme theme)
        {
            if (theme == null || _inspirationHintDismissed)
                return;

            Show(_chooseInspirationsMessage);
        }

        private void HandleInspirationValidationAttempted()
        {
            DismissInspirationHint();
        }

        private void HandleInspirationClicked()
        {
            DismissInspirationHint();
        }

        private void DismissInspirationHint()
        {
            if (_inspirationHintDismissed)
                return;

            _inspirationHintDismissed = true;
            Hide();
        }

        private void HandleInspirationSuccessConfirmed()
        {
            if (_arrangementHintDismissed)
                return;

            Show(_arrangeItemsMessage);
        }

        private void HandleItemPlaced(int slotIndex, ExhibitItemData item)
        {
            if (!_arrangementHintDismissed)
            {
                _arrangementHintDismissed = true;
                Hide();
            }

            TryShowStartHint();
        }

        private void HandleItemChanged(int slotIndex)
        {
            TryShowStartHint();
        }

        private void HandleItemsSwapped(int slotA, int slotB)
        {
            TryShowStartHint();
        }

        private void HandleExhibitionStarted()
        {
            if (_startHintDismissed)
                return;

            _startHintDismissed = true;
            Hide();
        }

        private void HandleCurationCleared()
        {
            Hide();
        }

        private void HandleRewardConfirmed()
        {
            if (_tryAnotherThemeHintDismissed || AreAllThemesCompleted())
                return;

            _tryAnotherThemeHintShown = true;
            Show(_tryAnotherThemeMessage);
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

        private void TryShowStartHint()
        {
            if (_startHintDismissed)
                return;

            var manager = ExhibitionManager.Instance;
            bool canStart = manager != null &&
                !manager.IsRunning &&
                manager.HasConfirmedInspirations &&
                manager.AreAllSlotsFilled();

            if (!canStart)
            {
                if (_startHintShown)
                {
                    _startHintShown = false;
                    Hide();
                }
                return;
            }

            if (_startHintShown)
                return;

            _startHintShown = true;
            Show(_startExhibitionMessage);
        }

        private void Show(string message)
        {
            ConfigurePortrait();

            if (_speakerText != null)
                _speakerText.text = _speakerName;

            if (_bodyText != null)
                _bodyText.text = message;

            if (_panel != null)
                _panel.SetActive(true);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }
        }

        private void Hide()
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;

            if (_panel != null && _panel != gameObject)
                _panel.SetActive(false);
        }

        private void ConfigurePortrait()
        {
            if (_portraitImage == null)
                return;

            if (_rinHeadSprite == null)
            {
                foreach (var sprite in Resources.LoadAll<Sprite>(RIN_SPRITE_RESOURCE))
                {
                    if (sprite != null && sprite.name == RIN_HEAD_SPRITE_NAME)
                    {
                        _rinHeadSprite = sprite;
                        break;
                    }

                    if (_rinHeadSprite == null)
                        _rinHeadSprite = sprite;
                }
            }

            _portraitImage.sprite = _rinHeadSprite;
            _portraitImage.preserveAspect = true;
            _portraitImage.raycastTarget = false;
        }

        private static void CreateRuntimePopup(Transform parent)
        {
            var panelObj = CreateRuntimeChild(parent, "TutorialPopup");
            var panelRt = panelObj.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0f);
            panelRt.anchorMax = new Vector2(0.5f, 0f);
            panelRt.pivot = new Vector2(0.5f, 0f);
            panelRt.anchoredPosition = new Vector2(0f, 130f);
            panelRt.sizeDelta = new Vector2(660f, 126f);

            var bg = panelObj.AddComponent<Image>();
            bg.color = new Color(0.91f, 0.80f, 0.58f, 0.97f);
            bg.raycastTarget = false;

            var cg = panelObj.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            var portraitFrame = CreateRuntimeChild(panelObj.transform, "PortraitFrame");
            var portraitFrameBg = portraitFrame.AddComponent<Image>();
            portraitFrameBg.color = new Color(0.64f, 0.45f, 0.28f, 0.95f);
            portraitFrameBg.raycastTarget = false;
            var portraitFrameRt = portraitFrame.GetComponent<RectTransform>();
            portraitFrameRt.anchorMin = new Vector2(0f, 0.5f);
            portraitFrameRt.anchorMax = new Vector2(0f, 0.5f);
            portraitFrameRt.pivot = new Vector2(0f, 0.5f);
            portraitFrameRt.anchoredPosition = new Vector2(18f, 0f);
            portraitFrameRt.sizeDelta = new Vector2(86f, 86f);

            var portraitObj = CreateRuntimeChild(portraitFrame.transform, "RinPortrait");
            var portrait = portraitObj.AddComponent<Image>();
            portrait.sprite = LoadRinHeadSprite();
            portrait.color = Color.white;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            StretchRuntime(portraitObj.GetComponent<RectTransform>(), 5f);

            var speakerText = CreateRuntimeText(panelObj.transform, "SpeakerText", "Rin", 22f, FontStyles.Bold, TextAlignmentOptions.Left);
            speakerText.color = new Color(0.24f, 0.13f, 0.06f, 1f);
            var speakerRt = speakerText.GetComponent<RectTransform>();
            speakerRt.anchorMin = new Vector2(0f, 1f);
            speakerRt.anchorMax = new Vector2(1f, 1f);
            speakerRt.pivot = new Vector2(0f, 1f);
            speakerRt.offsetMin = new Vector2(122f, -42f);
            speakerRt.offsetMax = new Vector2(-24f, -14f);

            var bodyText = CreateRuntimeText(
                panelObj.transform,
                "BodyText",
                "I came up with a few exhibition themes yesterday. For now, I should choose one first.",
                24f,
                FontStyles.Bold,
                TextAlignmentOptions.Left);
            bodyText.color = new Color(0.24f, 0.13f, 0.06f, 1f);
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            var bodyRt = bodyText.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0f, 0f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.offsetMin = new Vector2(122f, 18f);
            bodyRt.offsetMax = new Vector2(-24f, -44f);

            var popup = panelObj.AddComponent<TutorialPopup>();
            popup._panel = panelObj;
            popup._portraitImage = portrait;
            popup._speakerText = speakerText;
            popup._bodyText = bodyText;
            popup._canvasGroup = cg;
            popup._rinHeadSprite = portrait.sprite;
        }

        private static Sprite LoadRinHeadSprite()
        {
            Sprite fallback = null;
            foreach (var sprite in Resources.LoadAll<Sprite>(RIN_SPRITE_RESOURCE))
            {
                if (sprite == null)
                    continue;

                if (sprite.name == RIN_HEAD_SPRITE_NAME)
                    return sprite;

                if (fallback == null)
                    fallback = sprite;
            }

            return fallback;
        }

        private static GameObject CreateRuntimeChild(Transform parent, string name)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static TextMeshProUGUI CreateRuntimeText(
            Transform parent,
            string name,
            string text,
            float fontSize,
            FontStyles style,
            TextAlignmentOptions alignment)
        {
            var obj = CreateRuntimeChild(parent, name);
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return tmp;
        }

        private static void StretchRuntime(RectTransform rt, float padding)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }
    }
}
