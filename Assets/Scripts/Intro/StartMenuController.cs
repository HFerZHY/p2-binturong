using System.Collections;
using TMPro;
using Otowa.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Otowa.Intro
{
    public class StartMenuController : MonoBehaviour
    {
        [SerializeField] private string _nextSceneName = "Intro-1  (START)";
        [SerializeField] private TMP_FontAsset _serifFont;
        [SerializeField] private float _fadeDuration = 0.65f;

        private static readonly Color Background = new Color32(0x05, 0x18, 0x20, 0xFF);
        private static readonly Color Panel = new Color32(0x21, 0x9b, 0xd8, 0xF2);
        private static readonly Color PanelHover = new Color32(0x46, 0xc7, 0xff, 0xFF);
        private static readonly Color PanelPressed = new Color32(0x12, 0x69, 0xa4, 0xFF);
        private static readonly Color Title = new Color32(0xc8, 0xdc, 0xda, 0xFF);
        private static readonly Color ButtonText = new Color32(0xf0, 0xfb, 0xff, 0xFF);

        private CanvasGroup _fade;
        private Button _startButton;
        private bool _loading;

        private void Awake()
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            EnsureEventSystem();
            BuildUi();
        }

        private void Start()
        {
            _fade.alpha = 0f;
            GameAudioManager.Instance.PlayBgm(AudioId.OtowaBlues, fadeIn: 0.45f);
            StartCoroutine(FadeTo(1f));
        }

        private void StartGame()
        {
            if (_loading)
                return;

            _loading = true;
            _startButton.interactable = false;
            StartCoroutine(FadeAndLoad());
        }

        private IEnumerator FadeAndLoad()
        {
            GameAudioManager.Instance.StopBgm(0.35f);
            yield return FadeTo(0f);
            SceneManager.LoadScene(_nextSceneName);
        }

        private IEnumerator FadeTo(float target)
        {
            float start = _fade.alpha;
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _fade.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / _fadeDuration));
                yield return null;
            }

            _fade.alpha = target;
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("StartMenuCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            _fade = canvasObject.GetComponent<CanvasGroup>();

            MakeImage(canvasObject.transform, "Background", Background, Vector2.zero, Vector2.one);

            var station = MakeImage(canvasObject.transform, "Station", Color.white,
                new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.88f));
            station.sprite = LoadSprite("Map/trainStation");
            station.preserveAspect = true;
            station.color = new Color(0.72f, 0.90f, 0.93f, 0.22f);
            station.raycastTarget = false;

            var title = MakeText(canvasObject.transform, "Title", "OTOWA BLUES",
                116f, Title, TextAlignmentOptions.Center,
                new Vector2(0.08f, 0.64f), new Vector2(0.92f, 0.86f));
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 17f;

            var buttonObject = MakeRect(canvasObject.transform, "StartButton",
                new Vector2(0.38f, 0.075f), new Vector2(0.62f, 0.185f));
            var buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = Panel;

            _startButton = buttonObject.AddComponent<Button>();
            _startButton.targetGraphic = buttonImage;
            _startButton.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = PanelHover,
                pressedColor = PanelPressed,
                selectedColor = Color.white,
                disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f),
                colorMultiplier = 1f,
                fadeDuration = 0.12f,
            };
            _startButton.onClick.AddListener(StartGame);

            var label = MakeText(buttonObject.transform, "Label", "START GAME",
                34f, ButtonText, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 6f;
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
                return;

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        private Sprite LoadSprite(string resourcePath)
        {
            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
                return sprite;

            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
                return null;

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f);
        }

        private TextMeshProUGUI MakeText(Transform parent, string name, string text,
                                         float size, Color color, TextAlignmentOptions alignment,
                                         Vector2 anchorMin, Vector2 anchorMax)
        {
            var gameObject = MakeRect(parent, name, anchorMin, anchorMax);
            var tmp = gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            if (_serifFont != null)
                tmp.font = _serifFont;
            return tmp;
        }

        private static Image MakeImage(Transform parent, string name, Color color,
                                       Vector2 anchorMin, Vector2 anchorMax)
        {
            var image = MakeRect(parent, name, anchorMin, anchorMax).AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static GameObject MakeRect(Transform parent, string name,
                                           Vector2 anchorMin, Vector2 anchorMax)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            var rect = (RectTransform)gameObject.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return gameObject;
        }
    }
}
