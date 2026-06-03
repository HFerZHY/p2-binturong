using System.Collections.Generic;
using Otowa.Audio;
using Otowa.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    public class ExhibitionDay1TutorialController : MonoBehaviour
    {
        [SerializeField] private string _nextSceneName = "Intro-4";

        private static readonly string[] TutorialMessages =
        {
            "Oh, so this is what being a stationmaster feels like.",
            "The items on the shelf must be the ones Hikaru meant to use for the exhibition.",
            "I can place exhibits here... but without a theme, there's no way to curate.",
            "There are exhibit labels here, too. But I don't know a thing about these items yet, so I've no idea what to write...",
            "If only I had a notebook to jot down inspirations...",
            "Huh!?"
        };

        private static readonly Color LabelHighlightColor = new(1f, 0.72f, 0.25f, 1f);

        private RectTransform _tutorialPanel;
        private TextMeshProUGUI _tutorialText;
        private GameObject _tutorialRoot;
        private GameObject _rewardRoot;
        private readonly List<Image> _labelImages = new();
        private readonly List<Color> _labelDefaultColors = new();
        private Button _advanceButton;
        private Button _confirmButton;
        private int _messageIndex;
        private bool _isTransitioning;

        private void Awake()
        {
            CacheLabelImages();
            BuildTutorialUi();
            ShowTutorialMessage(0);
        }

        private void BuildTutorialUi()
        {
            var canvasObject = new GameObject(
                "Day1ExhibitionTutorialCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _tutorialRoot = CreateImage("TutorialRoot", canvasObject.transform, new Color(0f, 0f, 0f, 0.08f));
            Stretch(_tutorialRoot.GetComponent<RectTransform>());
            _advanceButton = _tutorialRoot.AddComponent<Button>();
            _advanceButton.transition = Selectable.Transition.None;
            _advanceButton.onClick.AddListener(AdvanceTutorial);

            var tutorialPanelObject = TutorialPopup.CreateRuntimeVisual(
                _tutorialRoot.transform,
                out _,
                out _,
                out _tutorialText,
                out _);
            _tutorialPanel = tutorialPanelObject.GetComponent<RectTransform>();

            BuildRewardPopup(canvasObject.transform);
        }

        private void BuildRewardPopup(Transform parent)
        {
            _rewardRoot = CreateImage("JournalRewardPopup", parent, new Color(0f, 0f, 0f, 0.72f));
            Stretch(_rewardRoot.GetComponent<RectTransform>());
            _rewardRoot.SetActive(false);

            var panel = CreateImage("Window", _rewardRoot.transform, new Color(0.91f, 0.80f, 0.58f, 0.98f));
            SetRect(panel.GetComponent<RectTransform>(), new Vector2(0.27f, 0.20f), new Vector2(0.73f, 0.80f));

            var title = CreateText("Title", panel.transform, "Item obtained", 52f, TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.80f), new Vector2(0.92f, 0.95f));
            title.color = new Color(0.24f, 0.13f, 0.06f, 1f);

            var iconObject = CreateImage("JournalIcon", panel.transform, Color.white);
            SetRect(iconObject.GetComponent<RectTransform>(), new Vector2(0.38f, 0.53f), new Vector2(0.62f, 0.78f));
            var icon = iconObject.GetComponent<Image>();
            icon.sprite = LoadJournalIcon();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var body = CreateText(
                "Body",
                panel.transform,
                "Obtained Hikaru's journal! Hikaru left this notebook behind, with pictures of the items pasted inside. I'll keep filling it in with the inspirations I find!",
                32f,
                TextAlignmentOptions.Center);
            SetRect(body.rectTransform, new Vector2(0.09f, 0.23f), new Vector2(0.91f, 0.51f));
            body.color = new Color(0.24f, 0.13f, 0.06f, 1f);

            var confirmObject = CreateImage("ConfirmButton", panel.transform, new Color(0.64f, 0.45f, 0.28f, 1f));
            SetRect(confirmObject.GetComponent<RectTransform>(), new Vector2(0.35f, 0.06f), new Vector2(0.65f, 0.18f));
            _confirmButton = confirmObject.AddComponent<Button>();
            _confirmButton.onClick.AddListener(ConfirmReward);

            var confirmLabel = CreateText("Text", confirmObject.transform, "Continue", 30f, TextAlignmentOptions.Center);
            Stretch(confirmLabel.rectTransform);
            confirmLabel.color = new Color(0.98f, 0.92f, 0.78f, 1f);
        }

        private void AdvanceTutorial()
        {
            if (_isTransitioning)
                return;

            _messageIndex++;
            if (_messageIndex < TutorialMessages.Length)
            {
                ShowTutorialMessage(_messageIndex);
                return;
            }

            _tutorialRoot.SetActive(false);
            _rewardRoot.SetActive(true);
            GameAudioManager.Instance.PlaySfxOnce(AudioId.Jingle);
        }

        private void ConfirmReward()
        {
            if (_isTransitioning)
                return;

            _isTransitioning = true;
            _confirmButton.interactable = false;
            StartCoroutine(ExhibitionSceneFadeTransition.FadeOutAndLoad(_nextSceneName));
        }

        private void ShowTutorialMessage(int index)
        {
            _messageIndex = index;
            _tutorialText.text = TutorialMessages[index];
            SetLabelsHighlighted(index == 3);

            switch (index)
            {
                case 0:
                    PlaceTutorialPanel(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
                    break;
                case 1:
                    PlaceTutorialPanel(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(60f, 125f));
                    break;
                case 2:
                case 3:
                    PlaceTutorialPanel(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-60f, -50f));
                    break;
                default:
                    PlaceTutorialPanel(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
                    break;
            }
        }

        private void CacheLabelImages()
        {
            var layoutRoot = GameObject.Find("LayoutRoot");
            var slotContainer = layoutRoot?.transform.Find("DisplayPanel/SlotContainer");
            if (slotContainer == null)
                return;

            for (var index = 0; index < slotContainer.childCount; index++)
            {
                var label = slotContainer.GetChild(index).Find("LabelStrip");
                var image = label != null ? label.GetComponent<Image>() : null;
                if (image == null)
                    continue;

                _labelImages.Add(image);
                _labelDefaultColors.Add(image.color);
            }
        }

        private void SetLabelsHighlighted(bool highlighted)
        {
            for (var index = 0; index < _labelImages.Count; index++)
            {
                var image = _labelImages[index];
                if (image != null)
                    image.color = highlighted ? LabelHighlightColor : _labelDefaultColors[index];
            }
        }

        private void PlaceTutorialPanel(Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition)
        {
            _tutorialPanel.anchorMin = anchor;
            _tutorialPanel.anchorMax = anchor;
            _tutorialPanel.pivot = pivot;
            _tutorialPanel.anchoredPosition = anchoredPosition;
            _tutorialPanel.sizeDelta = new Vector2(660f, 126f);
        }

        private static GameObject CreateImage(string name, Transform parent, Color color)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            imageObject.GetComponent<Image>().color = color;
            return imageObject;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string text,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            var label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.enableWordWrapping = true;
            label.raycastTarget = false;
            RuntimeFontLibrary.ApplyBreeSerif(label);
            return label;
        }

        private static Sprite LoadJournalIcon()
        {
            var sprites = Resources.LoadAll<Sprite>("Map/journal");
            if (sprites.Length > 0)
                return sprites[0];

            var texture = Resources.Load<Texture2D>("Map/journal");
            return texture != null
                ? Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f))
                : null;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rect)
        {
            Stretch(rect, Vector2.zero, Vector2.zero);
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
