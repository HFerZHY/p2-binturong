using ExhibitionSystem.Core;
using ExhibitionSystem.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    public class RewardPopup : MonoBehaviour
    {
        public static event System.Action OnRewardConfirmed;

        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _headlineText;
        [SerializeField] private TMP_Text _themeTitleText;
        [SerializeField] private TMP_Text _bodyText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _windowImage;
        [SerializeField] private TMP_FontAsset _popupFont;

        private const string SuccessHeadline = "Exhibition Complete!";
        private const string ConfirmText = "yayyy";
        private const string SuccessBackgroundResource = "Exhibitions/Icons/success";
        private const string PopupFontResource = "Fonts/BreeSerif-Regular";
        private static TMP_FontAsset _cachedPopupFont;
        private const string YujiRewardText =
            "He is the trendy boss of the pub, and the magician under the night sky. Whether it's the gentle buzz on the tip of the tongue or the passionate bloom in the night sky, both are the most romantic blues Yuji has dedicated to this village.";
        private const string SummerFestivalRewardText =
            "The Otowa Summer Festival is not only a reverence for the ancient bird deity, but also a ceremony of \"homecoming.\"\nWhen the fireworks light up all of Otowa, even the migratory birds that flew the furthest will follow the railway tracks back to their original nest on this day.";
        private const string ChefJiroRewardText =
            "He is an artisan devoted to tradition, who revived the century-old recipe for shichimi pepper.\nHe is also a clumsy father, who could only watch his son's retreating figure carried away by the current of time.\nMr. Jiro, surely you know it too: the only thing that endures across the years, ever renewed, is that quiet, unspoken love.";
        private const string HotSpringsRewardText =
            "Hot spring in the mountains:\n" +
            "high above the naked bathers\n" +
            "the River of Heaven.\n\n" +
            "Octopus traps,\n" +
            "fleeting dreams\n" +
            "under the summer moon.";
        private const string BirdwatchingRewardText =
            "\"This place is a birdwatcher's paradise, plain and simple! These forests teem with rare birds, and if luck's on your side, you might even catch sight of that deep-blue migrant. So if birds hold even the slightest interest for you, don't just sit there, grab your binoculars and buy yourself a train ticket to Otowa!\"\n\n" +
            "-- A retired professor who'd rather not give his name";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        internal static void EnsureRewardPopupExists()
        {
            if (FindFirstObjectByType<RewardPopup>(FindObjectsInactive.Include) != null)
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
            ExhibitionManager.OnExhibitionEnded += HandleExhibitionEnded;
        }

        private void OnDisable()
        {
            ExhibitionManager.OnExhibitionEnded -= HandleExhibitionEnded;
        }

        private void Start()
        {
            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(HandleConfirmClicked);

            ConfigureLayout();
            Hide();
        }

        private void HandleConfirmClicked()
        {
            Hide();
            OnRewardConfirmed?.Invoke();
        }

        private void HandleExhibitionEnded(bool success, int satisfaction, int threshold)
        {
            if (!success || threshold <= 0 || satisfaction < threshold)
                return;

            var theme = ExhibitionManager.Instance != null ? ExhibitionManager.Instance.CurrentTheme : null;
            Show(theme);
        }

        private void Show(ExhibitionTheme theme)
        {
            ConfigureLayout();

            if (_headlineText != null)
                _headlineText.text = SuccessHeadline;

            if (_themeTitleText != null)
                _themeTitleText.text = theme != null ? theme.title : string.Empty;

            if (_bodyText != null)
            {
                _bodyText.enableAutoSizing = true;
                _bodyText.fontSizeMin = 17f;
                _bodyText.fontSizeMax = 23f;
                _bodyText.text = GetRewardText(theme);
            }

            if (_panel != null)
                _panel.SetActive(true);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }
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

        private void ConfigureLayout()
        {
            EnsureRuntimeReferences();
            ApplyPopupFont();
            ApplySuccessBackground();

            var windowRect = _windowImage != null
                ? _windowImage.GetComponent<RectTransform>()
                : _themeTitleText != null
                    ? _themeTitleText.transform.parent as RectTransform
                    : null;
            if (windowRect != null)
            {
                windowRect.sizeDelta = new Vector2(880f, 635f);
                windowRect.anchoredPosition = Vector2.zero;
            }

            if (_headlineText != null && _headlineText.transform is RectTransform headlineRect)
            {
                _headlineText.text = SuccessHeadline;
                _headlineText.alignment = TextAlignmentOptions.Center;
                _headlineText.fontStyle = FontStyles.Bold;
                _headlineText.fontSize = 43f;
                _headlineText.enableAutoSizing = true;
                _headlineText.fontSizeMin = 34f;
                _headlineText.fontSizeMax = 43f;
                _headlineText.color = new Color(0.30f, 0.13f, 0.06f, 1f);
                headlineRect.anchorMin = new Vector2(0f, 1f);
                headlineRect.anchorMax = new Vector2(1f, 1f);
                headlineRect.pivot = new Vector2(0.5f, 1f);
                headlineRect.anchoredPosition = new Vector2(0f, -98f);
                headlineRect.sizeDelta = new Vector2(-150f, 62f);
            }

            if (_themeTitleText != null)
            {
                _themeTitleText.alignment = TextAlignmentOptions.Center;
                _themeTitleText.fontStyle = FontStyles.Bold;
                _themeTitleText.fontSize = 25f;
                _themeTitleText.enableAutoSizing = true;
                _themeTitleText.fontSizeMin = 18f;
                _themeTitleText.fontSizeMax = 25f;
                _themeTitleText.color = new Color(0.45f, 0.20f, 0.07f, 1f);
                _themeTitleText.textWrappingMode = TextWrappingModes.NoWrap;
                _themeTitleText.overflowMode = TextOverflowModes.Ellipsis;

                if (_themeTitleText.transform is RectTransform titleRect)
                {
                    titleRect.anchorMin = new Vector2(0f, 1f);
                    titleRect.anchorMax = new Vector2(1f, 1f);
                    titleRect.pivot = new Vector2(0.5f, 1f);
                    titleRect.anchoredPosition = new Vector2(0f, -168f);
                    titleRect.sizeDelta = new Vector2(-150f, 44f);
                }
            }

            if (_bodyText != null)
            {
                _bodyText.alignment = TextAlignmentOptions.Center;
                _bodyText.fontStyle = FontStyles.Normal;
                _bodyText.enableAutoSizing = true;
                _bodyText.fontSizeMin = 17f;
                _bodyText.fontSizeMax = 23f;
                _bodyText.lineSpacing = 10f;
                _bodyText.paragraphSpacing = 8f;
                _bodyText.color = new Color(0.28f, 0.14f, 0.07f, 1f);
                _bodyText.textWrappingMode = TextWrappingModes.Normal;
                _bodyText.overflowMode = TextOverflowModes.Ellipsis;

                if (_bodyText.transform is RectTransform bodyRect)
                {
                    bodyRect.anchorMin = Vector2.zero;
                    bodyRect.anchorMax = Vector2.one;
                    bodyRect.offsetMin = new Vector2(110f, 145f);
                    bodyRect.offsetMax = new Vector2(-110f, -205f);
                }
            }

            if (_confirmButton != null)
            {
                var buttonImage = _confirmButton.targetGraphic as Image;
                if (buttonImage == null)
                    buttonImage = _confirmButton.GetComponent<Image>();

                if (buttonImage != null)
                    buttonImage.color = new Color(0.48f, 0.28f, 0.13f, 1f);

                if (_confirmButton.transform is RectTransform buttonRect)
                {
                    buttonRect.anchorMin = new Vector2(0.5f, 0f);
                    buttonRect.anchorMax = new Vector2(0.5f, 0f);
                    buttonRect.pivot = new Vector2(0.5f, 0f);
                    buttonRect.anchoredPosition = new Vector2(0f, 72f);
                    buttonRect.sizeDelta = new Vector2(190f, 56f);
                }

                if (_confirmButton.GetComponentInChildren<TMP_Text>(true) is TMP_Text buttonText)
                {
                    buttonText.text = ConfirmText;
                    buttonText.alignment = TextAlignmentOptions.Center;
                    buttonText.fontStyle = FontStyles.Bold;
                    buttonText.fontSize = 27f;
                    buttonText.color = new Color(1f, 0.91f, 0.72f, 1f);
                    var sharedButtonFont = GetSharedButtonFont();
                    if (sharedButtonFont != null)
                        buttonText.font = sharedButtonFont;
                }
            }
        }

        private void EnsureRuntimeReferences()
        {
            if (_windowImage == null)
            {
                var windowTransform = _themeTitleText != null
                    ? _themeTitleText.transform.parent
                    : transform.Find("Window");
                if (windowTransform != null)
                    _windowImage = windowTransform.GetComponent<Image>();
            }
        }

        private void ApplyPopupFont()
        {
            if (_popupFont == null)
                _popupFont = LoadPopupFont();

            if (_popupFont == null)
                return;

            if (_headlineText != null)
                _headlineText.font = _popupFont;
            if (_themeTitleText != null)
                _themeTitleText.font = _popupFont;
            if (_bodyText != null)
                _bodyText.font = _popupFont;
        }

        private void ApplySuccessBackground()
        {
            if (_windowImage == null)
                return;

            var sprite = LoadSuccessBackground();
            if (sprite != null)
            {
                _windowImage.sprite = sprite;
                _windowImage.type = Image.Type.Simple;
                _windowImage.preserveAspect = true;
                _windowImage.color = Color.white;
            }
            else
            {
                _windowImage.color = new Color(0.91f, 0.80f, 0.58f, 0.98f);
            }
        }

        private static TMP_FontAsset LoadPopupFont()
        {
            if (_cachedPopupFont != null)
                return _cachedPopupFont;

            var font = Resources.Load<Font>(PopupFontResource);
            if (font == null)
                return null;

            _cachedPopupFont = TMP_FontAsset.CreateFontAsset(font);
            _cachedPopupFont.name = "Bree Serif Runtime SDF";
            return _cachedPopupFont;
        }

        private static Sprite LoadSuccessBackground()
        {
            var sprites = Resources.LoadAll<Sprite>(SuccessBackgroundResource);
            if (sprites != null && sprites.Length > 0)
                return sprites[0];

            return Resources.Load<Sprite>(SuccessBackgroundResource);
        }

        private static string GetRewardText(ExhibitionTheme theme)
        {
            if (theme == null)
                return string.Empty;

            switch (theme.name)
            {
                case "Yuji":
                    return YujiRewardText;
                case "SummerFestival":
                    return SummerFestivalRewardText;
                case "ChefJiro":
                    return ChefJiroRewardText;
                case "HotSprings":
                    return HotSpringsRewardText;
                case "Birdwatching":
                    return BirdwatchingRewardText;
                default:
                    return string.Empty;
            }
        }

        private static RewardPopup CreateRuntimePopup(Transform parent)
        {
            var panelObj = CreateRuntimeChild(parent, "RewardPopup");
            StretchRuntime(panelObj.GetComponent<RectTransform>(), 0f);

            var overlay = panelObj.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.62f);
            overlay.raycastTarget = true;

            var cg = panelObj.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            var windowObj = CreateRuntimeChild(panelObj.transform, "Window");
            var windowBg = windowObj.AddComponent<Image>();
            windowBg.sprite = LoadSuccessBackground();
            windowBg.color = windowBg.sprite != null ? Color.white : new Color(0.91f, 0.80f, 0.58f, 0.98f);
            windowBg.type = Image.Type.Simple;
            windowBg.preserveAspect = true;
            windowBg.raycastTarget = true;
            var windowRt = windowObj.GetComponent<RectTransform>();
            windowRt.anchorMin = new Vector2(0.5f, 0.5f);
            windowRt.anchorMax = new Vector2(0.5f, 0.5f);
            windowRt.pivot = new Vector2(0.5f, 0.5f);
            windowRt.sizeDelta = new Vector2(880f, 635f);
            windowRt.anchoredPosition = Vector2.zero;

            var headline = CreateRuntimeText(windowObj.transform, "Headline", SuccessHeadline, 43f, FontStyles.Bold, TextAlignmentOptions.Center);
            headline.color = new Color(0.30f, 0.13f, 0.06f, 1f);
            var headlineRt = headline.GetComponent<RectTransform>();
            headlineRt.anchorMin = new Vector2(0f, 1f);
            headlineRt.anchorMax = new Vector2(1f, 1f);
            headlineRt.pivot = new Vector2(0.5f, 1f);
            headlineRt.anchoredPosition = new Vector2(0f, -98f);
            headlineRt.sizeDelta = new Vector2(-150f, 62f);

            var themeTitle = CreateRuntimeText(windowObj.transform, "ThemeTitle", "Theme Title", 25f, FontStyles.Bold, TextAlignmentOptions.Center);
            themeTitle.color = new Color(0.45f, 0.20f, 0.07f, 1f);
            themeTitle.textWrappingMode = TextWrappingModes.NoWrap;
            themeTitle.overflowMode = TextOverflowModes.Ellipsis;
            var themeRt = themeTitle.GetComponent<RectTransform>();
            themeRt.anchorMin = new Vector2(0f, 1f);
            themeRt.anchorMax = new Vector2(1f, 1f);
            themeRt.pivot = new Vector2(0.5f, 1f);
            themeRt.anchoredPosition = new Vector2(0f, -168f);
            themeRt.sizeDelta = new Vector2(-150f, 44f);

            var body = CreateRuntimeText(windowObj.transform, "Body", string.Empty, 23f, FontStyles.Normal, TextAlignmentOptions.Center);
            body.color = new Color(0.28f, 0.14f, 0.07f, 1f);
            body.enableAutoSizing = true;
            body.fontSizeMin = 17f;
            body.fontSizeMax = 23f;
            body.lineSpacing = 10f;
            body.paragraphSpacing = 8f;
            body.textWrappingMode = TextWrappingModes.Normal;
            body.overflowMode = TextOverflowModes.Ellipsis;
            var bodyRt = body.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0f, 0f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.offsetMin = new Vector2(110f, 145f);
            bodyRt.offsetMax = new Vector2(-110f, -205f);

            var buttonObj = CreateRuntimeChild(windowObj.transform, "ConfirmButton");
            var buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.48f, 0.28f, 0.13f, 1f);
            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            var buttonRt = buttonObj.GetComponent<RectTransform>();
            buttonRt.anchorMin = new Vector2(0.5f, 0f);
            buttonRt.anchorMax = new Vector2(0.5f, 0f);
            buttonRt.pivot = new Vector2(0.5f, 0f);
            buttonRt.anchoredPosition = new Vector2(0f, 72f);
            buttonRt.sizeDelta = new Vector2(190f, 56f);

            var buttonText = CreateRuntimeText(buttonObj.transform, "Text", ConfirmText, 27f, FontStyles.Bold, TextAlignmentOptions.Center, false);
            buttonText.color = new Color(1f, 0.91f, 0.72f, 1f);
            var sharedButtonFont = GetSharedButtonFont();
            if (sharedButtonFont != null)
                buttonText.font = sharedButtonFont;
            StretchRuntime(buttonText.GetComponent<RectTransform>(), 8f);

            var popup = panelObj.AddComponent<RewardPopup>();
            popup._panel = panelObj;
            popup._headlineText = headline;
            popup._themeTitleText = themeTitle;
            popup._bodyText = body;
            popup._confirmButton = button;
            popup._canvasGroup = cg;
            popup._windowImage = windowBg;
            popup._popupFont = LoadPopupFont();
            return popup;
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
            TextAlignmentOptions alignment,
            bool usePopupFont = true)
        {
            var obj = CreateRuntimeChild(parent, name);
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            if (usePopupFont)
            {
                var popupFont = LoadPopupFont();
                if (popupFont != null)
                    tmp.font = popupFont;
            }
            return tmp;
        }

        private static TMP_FontAsset GetSharedButtonFont()
        {
            var selector = FindFirstObjectByType<ThemeSelector>(FindObjectsInactive.Include);
            var startText = selector != null && selector.StartButton != null
                ? selector.StartButton.GetComponentInChildren<TMP_Text>(true)
                : null;
            if (startText != null && startText.font != null)
                return startText.font;

            var selectText = selector != null && selector.SelectButton != null
                ? selector.SelectButton.GetComponentInChildren<TMP_Text>(true)
                : null;
            if (selectText != null && selectText.font != null)
                return selectText.font;

            return TMP_Settings.defaultFontAsset;
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
