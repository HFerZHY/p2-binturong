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

        private const string SuccessHeadline = "Exhibition Success";
        private const string ConfirmText = "Yayyyy!";
        private const string YujiRewardText =
            "He is the trendy boss of the pub, and the magician under the night sky. Whether it's the gentle buzz on the tip of the tongue or the passionate bloom in the night sky, both are the most romantic blues Yuji has dedicated to this village.";
        private const string SummerFestivalRewardText =
            "The Otowa Summer Festival is not only a reverence for the ancient bird deity, but also a ceremony of \"homecoming.\" When the fireworks light up all of Otowa, even the migratory birds that flew the furthest will follow the railway tracks back to their original nest on this day.";
        private const string ChefJiroRewardText =
            "He is an artisan devoted to tradition, who revived the century-old recipe for shichimi pepper. He is also a clumsy father, who could only watch his son's retreating figure carried away by the current of time. Mr. Jiro, surely you know it too: the only thing that endures across the years, ever renewed, is that quiet, unspoken love.";
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
            if (_headlineText != null)
                _headlineText.text = SuccessHeadline;

            if (_themeTitleText != null)
                _themeTitleText.text = theme != null ? theme.title : string.Empty;

            if (_bodyText != null)
            {
                _bodyText.enableAutoSizing = true;
                _bodyText.fontSizeMin = 18f;
                _bodyText.fontSizeMax = 24f;
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
            windowBg.color = new Color(0.91f, 0.80f, 0.58f, 0.98f);
            windowBg.raycastTarget = true;
            var windowRt = windowObj.GetComponent<RectTransform>();
            windowRt.anchorMin = new Vector2(0.5f, 0.5f);
            windowRt.anchorMax = new Vector2(0.5f, 0.5f);
            windowRt.pivot = new Vector2(0.5f, 0.5f);
            windowRt.sizeDelta = new Vector2(760f, 500f);
            windowRt.anchoredPosition = Vector2.zero;

            var headline = CreateRuntimeText(windowObj.transform, "Headline", SuccessHeadline, 38f, FontStyles.Bold, TextAlignmentOptions.Center);
            headline.color = new Color(0.25f, 0.13f, 0.06f, 1f);
            var headlineRt = headline.GetComponent<RectTransform>();
            headlineRt.anchorMin = new Vector2(0f, 1f);
            headlineRt.anchorMax = new Vector2(1f, 1f);
            headlineRt.pivot = new Vector2(0.5f, 1f);
            headlineRt.anchoredPosition = new Vector2(0f, -34f);
            headlineRt.sizeDelta = new Vector2(-80f, 52f);

            var themeTitle = CreateRuntimeText(windowObj.transform, "ThemeTitle", "Theme Title", 26f, FontStyles.Bold, TextAlignmentOptions.Center);
            themeTitle.color = new Color(0.48f, 0.23f, 0.08f, 1f);
            themeTitle.textWrappingMode = TextWrappingModes.Normal;
            var themeRt = themeTitle.GetComponent<RectTransform>();
            themeRt.anchorMin = new Vector2(0f, 1f);
            themeRt.anchorMax = new Vector2(1f, 1f);
            themeRt.pivot = new Vector2(0.5f, 1f);
            themeRt.anchoredPosition = new Vector2(0f, -92f);
            themeRt.sizeDelta = new Vector2(-96f, 68f);

            var body = CreateRuntimeText(windowObj.transform, "Body", string.Empty, 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            body.color = new Color(0.24f, 0.13f, 0.06f, 1f);
            body.textWrappingMode = TextWrappingModes.Normal;
            var bodyRt = body.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0f, 0f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.offsetMin = new Vector2(62f, 116f);
            bodyRt.offsetMax = new Vector2(-62f, -178f);

            var buttonObj = CreateRuntimeChild(windowObj.transform, "ConfirmButton");
            var buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.34f, 0.46f, 0.20f, 1f);
            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            var buttonRt = buttonObj.GetComponent<RectTransform>();
            buttonRt.anchorMin = new Vector2(0.5f, 0f);
            buttonRt.anchorMax = new Vector2(0.5f, 0f);
            buttonRt.pivot = new Vector2(0.5f, 0f);
            buttonRt.anchoredPosition = new Vector2(0f, 34f);
            buttonRt.sizeDelta = new Vector2(230f, 58f);

            var buttonText = CreateRuntimeText(buttonObj.transform, "Text", ConfirmText, 24f, FontStyles.Bold, TextAlignmentOptions.Center);
            buttonText.color = new Color(0.98f, 0.90f, 0.72f, 1f);
            StretchRuntime(buttonText.GetComponent<RectTransform>(), 8f);

            var popup = panelObj.AddComponent<RewardPopup>();
            popup._panel = panelObj;
            popup._headlineText = headline;
            popup._themeTitleText = themeTitle;
            popup._bodyText = body;
            popup._confirmButton = button;
            popup._canvasGroup = cg;
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
