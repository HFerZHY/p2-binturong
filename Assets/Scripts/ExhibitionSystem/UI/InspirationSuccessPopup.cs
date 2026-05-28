using System.Collections.Generic;
using ExhibitionSystem.Core;
using ExhibitionSystem.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    public class InspirationSuccessPopup : MonoBehaviour
    {
        public static event System.Action OnSuccessConfirmed;

        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _headlineText;
        [SerializeField] private TMP_Text _bodyText;
        [SerializeField] private TMP_Text _nextText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private CanvasGroup _canvasGroup;

        private const string HeadlineText = "Success!";
        private const string BodyText = "These inspirations all fit the exhibition theme.";
        private const string NextText = "Next, choose the exhibits that match each inspiration.";
        private const string ConfirmText = "OK";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePopupExists()
        {
            if (FindFirstObjectByType<InspirationSuccessPopup>(FindObjectsInactive.Include) != null)
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
            ExhibitionManager.OnInspirationsConfirmed += HandleInspirationsConfirmed;
        }

        private void OnDisable()
        {
            ExhibitionManager.OnInspirationsConfirmed -= HandleInspirationsConfirmed;
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
            OnSuccessConfirmed?.Invoke();
        }

        private void HandleInspirationsConfirmed(IReadOnlyList<InspirationData> inspirations)
        {
            Show();
        }

        private void Show()
        {
            if (_headlineText != null)
                _headlineText.text = HeadlineText;

            if (_bodyText != null)
                _bodyText.text = BodyText;

            if (_nextText != null)
                _nextText.text = NextText;

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

        private static InspirationSuccessPopup CreateRuntimePopup(Transform parent)
        {
            var panelObj = CreateRuntimeChild(parent, "InspirationSuccessPopup");
            StretchRuntime(panelObj.GetComponent<RectTransform>(), 0f);

            var overlay = panelObj.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.58f);
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
            windowRt.sizeDelta = new Vector2(680f, 360f);
            windowRt.anchoredPosition = Vector2.zero;

            var headline = CreateRuntimeText(windowObj.transform, "Headline", HeadlineText, 40f, FontStyles.Bold, TextAlignmentOptions.Center);
            headline.color = new Color(0.25f, 0.13f, 0.06f, 1f);
            var headlineRt = headline.GetComponent<RectTransform>();
            headlineRt.anchorMin = new Vector2(0f, 1f);
            headlineRt.anchorMax = new Vector2(1f, 1f);
            headlineRt.pivot = new Vector2(0.5f, 1f);
            headlineRt.anchoredPosition = new Vector2(0f, -42f);
            headlineRt.sizeDelta = new Vector2(-80f, 56f);

            var body = CreateRuntimeText(windowObj.transform, "Body", BodyText, 26f, FontStyles.Bold, TextAlignmentOptions.Center);
            body.color = new Color(0.31f, 0.16f, 0.06f, 1f);
            body.textWrappingMode = TextWrappingModes.Normal;
            var bodyRt = body.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0f, 1f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.pivot = new Vector2(0.5f, 1f);
            bodyRt.anchoredPosition = new Vector2(0f, -118f);
            bodyRt.sizeDelta = new Vector2(-92f, 82f);

            var next = CreateRuntimeText(windowObj.transform, "Next", NextText, 24f, FontStyles.Normal, TextAlignmentOptions.Center);
            next.color = new Color(0.24f, 0.13f, 0.06f, 1f);
            next.textWrappingMode = TextWrappingModes.Normal;
            var nextRt = next.GetComponent<RectTransform>();
            nextRt.anchorMin = new Vector2(0f, 1f);
            nextRt.anchorMax = new Vector2(1f, 1f);
            nextRt.pivot = new Vector2(0.5f, 1f);
            nextRt.anchoredPosition = new Vector2(0f, -204f);
            nextRt.sizeDelta = new Vector2(-92f, 66f);

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
            buttonRt.sizeDelta = new Vector2(180f, 56f);

            var buttonText = CreateRuntimeText(buttonObj.transform, "Text", ConfirmText, 24f, FontStyles.Bold, TextAlignmentOptions.Center);
            buttonText.color = new Color(0.98f, 0.90f, 0.72f, 1f);
            StretchRuntime(buttonText.GetComponent<RectTransform>(), 8f);

            var popup = panelObj.AddComponent<InspirationSuccessPopup>();
            popup._panel = panelObj;
            popup._headlineText = headline;
            popup._bodyText = body;
            popup._nextText = next;
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
