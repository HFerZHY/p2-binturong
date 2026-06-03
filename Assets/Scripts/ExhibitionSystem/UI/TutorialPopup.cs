using ExhibitionSystem.Core;
using ExhibitionSystem.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    public class TutorialPopup : MonoBehaviour
    {
        private const string RIN_SPRITE_RESOURCE = "Characters/WorldSprite/rin";
        private const string RIN_HEAD_SPRITE_NAME = "spritesheet_template_0";
        private const string DAY2_EXHIBITION_SCENE = "ExhibitionDay2Scene";
        private const string DAY3_EXHIBITION_SCENE = "ExhibitionDay3Scene";

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
            "I should click the label above the exhibit and choose a matching inspiration.";
        [SerializeField] private string _inspirationFirstMessage =
            "I can also choose inspirations first, then drag items in afterward.";
        [SerializeField] private string _matchedItemMessage =
            "This item has already been matched with an inspiration. I don't need to match it again. I should try another one.";
        [SerializeField] private string _arrangeItemsMessage =
            "Next, I should choose items that fit this theme and drag them into the empty display slots.";
        [SerializeField] private string _startExhibitionMessage =
            "Great! Now I can let the passengers visit the exhibition!";
        [SerializeField] private string _tryAnotherThemeMessage =
            "Great job! To finish today's work, try another theme.";
        [SerializeField] private GameObject _clickDismissOverlay;

        [Header("Button Highlights")]
        [SerializeField] private Color _buttonHighlightColor = new(1f, 0.86f, 0.18f, 1f);
        [SerializeField] private Color _buttonHighlightFillColor = new(1f, 0.80f, 0.20f, 0.38f);
        [SerializeField] private Vector2 _buttonHighlightWidth = new(12f, 12f);

        private bool _selectThemeDismissed;
        private bool _inspirationHintDismissed;
        private bool _inspirationFirstHintShown;
        private bool _inspirationFirstHintActive;
        private bool _arrangementHintDismissed;
        private bool _startHintDismissed;
        private bool _startHintShown;
        private bool _tryAnotherThemeHintShown;
        private bool _tryAnotherThemeHintDismissed;
        private Button _clickDismissButton;
        private ButtonHighlightState _activeButtonHighlight;

        public static bool IsInspirationEditingBlocked { get; private set; }
        public static event System.Action<bool> OnInspirationEditingBlockChanged;

        private enum ButtonHighlightTarget
        {
            None,
            SelectTheme,
            StartExhibition
        }

        private sealed class ButtonHighlightState
        {
            public Outline Outline;
            public bool AddedOutline;
            public bool WasEnabled;
            public Color PreviousColor;
            public Vector2 PreviousDistance;
            public Image FillImage;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        internal static void EnsureTutorialPopupExists()
        {
            if (SceneManager.GetActiveScene().name == DAY3_EXHIBITION_SCENE)
                return;

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
            InspirationSelectionPopup.OnPopupOpened += HandleInspirationPopupOpened;
            InspirationSelectionPopup.OnInspirationClicked += HandleInspirationClicked;
            ExhibitionManager.OnSlotInspirationChanged += HandleSlotInspirationChanged;
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
            InspirationSelectionPopup.OnPopupOpened -= HandleInspirationPopupOpened;
            InspirationSelectionPopup.OnInspirationClicked -= HandleInspirationClicked;
            ExhibitionManager.OnSlotInspirationChanged -= HandleSlotInspirationChanged;
            ExhibitionManager.OnItemPlaced -= HandleItemPlaced;
            ExhibitionManager.OnItemRemoved -= HandleItemChanged;
            ExhibitionManager.OnItemsSwapped -= HandleItemsSwapped;
            ExhibitionManager.OnExhibitionStarted -= HandleExhibitionStarted;
            ExhibitionManager.OnCurationCleared -= HandleCurationCleared;
            RewardPopup.OnRewardConfirmed -= HandleRewardConfirmed;
            SetInspirationEditingBlocked(false);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            IsInspirationEditingBlocked = false;
            OnInspirationEditingBlockChanged = null;
        }

        private void Start()
        {
            ConfigurePortrait();

            var manager = ExhibitionManager.Instance;
            if (!_selectThemeDismissed && (manager == null || manager.CurrentTheme == null))
                Show(_selectThemeMessage, ButtonHighlightTarget.SelectTheme);
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
            if (theme == null || _arrangementHintDismissed)
                return;

            SetInspirationEditingBlocked(SceneManager.GetActiveScene().name == DAY2_EXHIBITION_SCENE);
            Show(_arrangeItemsMessage);
        }

        private void HandleInspirationClicked()
        {
            DismissInspirationHint();
        }

        private void HandleInspirationPopupOpened()
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

        private void HandleItemPlaced(int slotIndex, ExhibitItemData item)
        {
            if (!_arrangementHintDismissed)
            {
                _arrangementHintDismissed = true;
                SetInspirationEditingBlocked(false);
                Hide();
            }

            if (!_inspirationHintDismissed)
            {
                var manager = ExhibitionManager.Instance;
                bool isFixedMatch = manager != null && manager.IsSlotInspirationFixed(slotIndex);
                Show(isFixedMatch ? _matchedItemMessage : _chooseInspirationsMessage);
            }

            TryShowStartHint();
        }

        private void HandleSlotInspirationChanged(int slotIndex, InspirationData inspiration)
        {
            var manager = ExhibitionManager.Instance;
            bool isFixedMatch = manager != null && manager.IsSlotInspirationFixed(slotIndex);
            if (inspiration != null && !isFixedMatch)
            {
                DismissInspirationHint();
                if (TryShowInspirationFirstHint())
                    return;
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
            SetInspirationEditingBlocked(false);
            Hide();
        }

        private void HandleCurationCleared()
        {
            _inspirationFirstHintActive = false;
            SetInspirationEditingBlocked(false);
            SetClickDismissOverlayActive(false);
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
            if (_inspirationFirstHintActive)
                return;

            if (_startHintDismissed)
                return;

            var manager = ExhibitionManager.Instance;
            bool canStart = manager != null &&
                !manager.IsRunning &&
                manager.AreAllSlotsReady();

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
            Show(_startExhibitionMessage, ButtonHighlightTarget.StartExhibition);
        }

        private bool TryShowInspirationFirstHint()
        {
            if (_inspirationFirstHintShown ||
                SceneManager.GetActiveScene().name != DAY2_EXHIBITION_SCENE)
            {
                return false;
            }

            _inspirationFirstHintShown = true;
            _inspirationFirstHintActive = true;
            Show(_inspirationFirstMessage);
            SetClickDismissOverlayActive(true);
            return true;
        }

        private void HandleClickDismissOverlayClicked()
        {
            if (!_inspirationFirstHintActive)
                return;

            _inspirationFirstHintActive = false;
            SetClickDismissOverlayActive(false);
            Hide();
            TryShowStartHint();
        }

        private void Show(string message, ButtonHighlightTarget highlightTarget = ButtonHighlightTarget.None)
        {
            ConfigurePortrait();
            SetButtonHighlight(highlightTarget);

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
            _inspirationFirstHintActive = false;
            SetClickDismissOverlayActive(false);
            ClearButtonHighlight();

            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;

            if (_panel != null && _panel != gameObject)
                _panel.SetActive(false);
        }

        private void SetButtonHighlight(ButtonHighlightTarget target)
        {
            ClearButtonHighlight();

            if (target == ButtonHighlightTarget.None ||
                SceneManager.GetActiveScene().name != DAY2_EXHIBITION_SCENE)
            {
                return;
            }

            var selector = FindFirstObjectByType<ThemeSelector>(FindObjectsInactive.Include);
            var button = target == ButtonHighlightTarget.SelectTheme
                ? selector?.SelectButton
                : selector?.StartButton;
            if (button == null)
                return;

            var outline = button.GetComponent<Outline>();
            bool addedOutline = outline == null;
            if (addedOutline)
                outline = button.gameObject.AddComponent<Outline>();

            _activeButtonHighlight = new ButtonHighlightState
            {
                Outline = outline,
                AddedOutline = addedOutline,
                WasEnabled = outline.enabled,
                PreviousColor = outline.effectColor,
                PreviousDistance = outline.effectDistance,
                FillImage = CreateButtonHighlightFill(button)
            };

            outline.effectColor = _buttonHighlightColor;
            outline.effectDistance = _buttonHighlightWidth;
            outline.enabled = true;
        }

        private void ClearButtonHighlight()
        {
            if (_activeButtonHighlight == null)
                return;

            var outline = _activeButtonHighlight.Outline;
            if (outline != null)
            {
                if (_activeButtonHighlight.AddedOutline)
                {
                    outline.enabled = false;
                }
                else
                {
                    outline.effectColor = _activeButtonHighlight.PreviousColor;
                    outline.effectDistance = _activeButtonHighlight.PreviousDistance;
                    outline.enabled = _activeButtonHighlight.WasEnabled;
                }
            }

            if (_activeButtonHighlight.FillImage != null)
                Destroy(_activeButtonHighlight.FillImage.gameObject);

            _activeButtonHighlight = null;
        }

        private Image CreateButtonHighlightFill(Button button)
        {
            if (button == null)
                return null;

            var fillObject = new GameObject("TutorialButtonHighlightFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.transform.SetParent(button.transform, false);
            fillObject.transform.SetAsFirstSibling();

            var fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(-10f, -10f);
            fillRect.offsetMax = new Vector2(10f, 10f);

            var fillImage = fillObject.GetComponent<Image>();
            fillImage.color = _buttonHighlightFillColor;
            fillImage.raycastTarget = false;
            return fillImage;
        }

        private static void SetInspirationEditingBlocked(bool blocked)
        {
            if (IsInspirationEditingBlocked == blocked)
                return;

            IsInspirationEditingBlocked = blocked;
            OnInspirationEditingBlockChanged?.Invoke(blocked);
        }

        private void SetClickDismissOverlayActive(bool active)
        {
            EnsureClickDismissOverlay();

            if (_clickDismissOverlay != null)
                _clickDismissOverlay.SetActive(active);
        }

        private void EnsureClickDismissOverlay()
        {
            if (_clickDismissOverlay != null)
            {
                if (_clickDismissButton == null)
                    _clickDismissButton = _clickDismissOverlay.GetComponent<Button>();

                if (_clickDismissButton != null)
                {
                    _clickDismissButton.onClick.RemoveListener(HandleClickDismissOverlayClicked);
                    _clickDismissButton.onClick.AddListener(HandleClickDismissOverlayClicked);
                }

                return;
            }

            var parent = _panel != null ? _panel.transform.parent : transform.parent;
            if (parent == null)
                return;

            _clickDismissOverlay = CreateClickDismissOverlay(parent);
            _clickDismissButton = _clickDismissOverlay.GetComponent<Button>();
            if (_clickDismissButton != null)
                _clickDismissButton.onClick.AddListener(HandleClickDismissOverlayClicked);

            if (_panel != null)
            {
                _clickDismissOverlay.transform.SetSiblingIndex(_panel.transform.GetSiblingIndex());
                _panel.transform.SetAsLastSibling();
            }
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
            var clickDismissOverlay = CreateClickDismissOverlay(parent);

            var panelObj = CreateRuntimeVisual(
                parent,
                out var portrait,
                out var speakerText,
                out var bodyText,
                out var cg);

            var popup = panelObj.AddComponent<TutorialPopup>();
            popup._panel = panelObj;
            popup._portraitImage = portrait;
            popup._speakerText = speakerText;
            popup._bodyText = bodyText;
            popup._canvasGroup = cg;
            popup._rinHeadSprite = portrait.sprite;
            popup._clickDismissOverlay = clickDismissOverlay;
            popup._clickDismissButton = clickDismissOverlay.GetComponent<Button>();
            if (popup._clickDismissButton != null)
                popup._clickDismissButton.onClick.AddListener(popup.HandleClickDismissOverlayClicked);
        }

        internal static GameObject CreateRuntimeVisual(
            Transform parent,
            out Image portrait,
            out TextMeshProUGUI speakerText,
            out TextMeshProUGUI bodyText,
            out CanvasGroup canvasGroup)
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

            canvasGroup = panelObj.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

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
            portrait = portraitObj.AddComponent<Image>();
            portrait.sprite = LoadRinHeadSprite();
            portrait.color = Color.white;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            StretchRuntime(portraitObj.GetComponent<RectTransform>(), 5f);

            speakerText = CreateRuntimeText(panelObj.transform, "SpeakerText", "Rin", 22f, FontStyles.Bold, TextAlignmentOptions.Left);
            speakerText.color = new Color(0.24f, 0.13f, 0.06f, 1f);
            var speakerRt = speakerText.GetComponent<RectTransform>();
            speakerRt.anchorMin = new Vector2(0f, 1f);
            speakerRt.anchorMax = new Vector2(1f, 1f);
            speakerRt.pivot = new Vector2(0f, 1f);
            speakerRt.offsetMin = new Vector2(122f, -42f);
            speakerRt.offsetMax = new Vector2(-24f, -14f);

            bodyText = CreateRuntimeText(
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

            return panelObj;
        }

        private static GameObject CreateClickDismissOverlay(Transform parent)
        {
            var overlayObject = CreateRuntimeChild(parent, "TutorialClickDismissOverlay");
            StretchRuntime(overlayObject.GetComponent<RectTransform>(), 0f);

            var image = overlayObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;

            var button = overlayObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;

            overlayObject.SetActive(false);
            return overlayObject;
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
            tmp.raycastTarget = false;
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
