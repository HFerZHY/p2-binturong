using System.Collections;
using TMPro;
using Otowa.Audio;
using Otowa.SaveSystem;
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
        private Button _continueButton;
        private GameObject _confirmRoot;
        private System.Action _loadAfterFade;
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

        private void StartNewGame()
        {
            if (GameSaveManager.Instance.HasSave)
            {
                ShowNewGameConfirm();
                return;
            }

            StartNewGameConfirmed();
        }

        private void StartNewGameConfirmed()
        {
            HideNewGameConfirm();
            GameSaveManager.Instance.DeleteSave();
            HideContinueButton();
            BeginLoad(() => GameSaveManager.Instance.StartNewGame(_nextSceneName));
        }

        private void ContinueGame()
        {
            if (!GameSaveManager.Instance.HasSave)
                return;

            BeginLoad(() => GameSaveManager.Instance.ContinueGame());
        }

        private void BeginLoad(System.Action loadAction)
        {
            if (_loading)
                return;

            _loading = true;
            _loadAfterFade = loadAction;
            _startButton.interactable = false;
            if (_continueButton != null)
                _continueButton.interactable = false;
            StartCoroutine(FadeAndLoad());
        }

        private IEnumerator FadeAndLoad()
        {
            GameAudioManager.Instance.StopBgm(0.35f);
            yield return FadeTo(0f);
            _loadAfterFade?.Invoke();
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

            bool hasSave = GameSaveManager.Instance.HasSave;
            _startButton = MakeMenuButton(canvasObject.transform, "StartButton", "START NEW GAME",
                new Vector2(0.37f, 0.13f), new Vector2(0.63f, 0.225f), StartNewGame);
            _continueButton = MakeMenuButton(canvasObject.transform, "ContinueButton", "CONTINUE",
                new Vector2(0.37f, 0.035f), new Vector2(0.63f, 0.115f), ContinueGame);
            _continueButton.gameObject.SetActive(hasSave);
            _continueButton.interactable = hasSave;
            BuildNewGameConfirmPopup(canvasObject.transform);
        }

        private void BuildNewGameConfirmPopup(Transform parent)
        {
            _confirmRoot = MakeRect(parent, "NewGameConfirmPopup", Vector2.zero, Vector2.one);
            MakeImage(_confirmRoot.transform, "Backdrop", new Color(0f, 0f, 0f, 0.68f), Vector2.zero, Vector2.one);

            var panel = MakeImage(_confirmRoot.transform, "Panel", new Color32(0x11, 0x26, 0x30, 0xF8),
                new Vector2(0.32f, 0.35f), new Vector2(0.68f, 0.64f));

            var message = MakeText(panel.transform, "Message",
                "Starting a new game will overwrite your save. Continue?",
                28f, ButtonText, TextAlignmentOptions.Center,
                new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.82f));
            message.textWrappingMode = TextWrappingModes.Normal;

            MakeMenuButton(panel.transform, "CancelButton", "CANCEL",
                new Vector2(0.10f, 0.16f), new Vector2(0.46f, 0.34f), HideNewGameConfirm);
            MakeMenuButton(panel.transform, "ConfirmButton", "CONTINUE",
                new Vector2(0.54f, 0.16f), new Vector2(0.90f, 0.34f), StartNewGameConfirmed);

            _confirmRoot.SetActive(false);
        }

        private void ShowNewGameConfirm()
        {
            if (_confirmRoot != null)
                _confirmRoot.SetActive(true);
        }

        private void HideNewGameConfirm()
        {
            if (_confirmRoot != null)
                _confirmRoot.SetActive(false);
        }

        private void HideContinueButton()
        {
            if (_continueButton == null)
                return;

            _continueButton.interactable = false;
            _continueButton.gameObject.SetActive(false);
        }

        private Button MakeMenuButton(
            Transform parent,
            string name,
            string labelText,
            Vector2 anchorMin,
            Vector2 anchorMax,
            UnityEngine.Events.UnityAction action)
        {
            var buttonObject = MakeRect(parent, name, anchorMin, anchorMax);
            var buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = Panel;

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = PanelHover,
                pressedColor = PanelPressed,
                selectedColor = Color.white,
                disabledColor = new Color(0.32f, 0.38f, 0.42f, 0.62f),
                colorMultiplier = 1f,
                fadeDuration = 0.12f,
            };
            button.onClick.AddListener(action);

            var label = MakeText(buttonObject.transform, "Label", labelText,
                30f, ButtonText, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 4f;
            return button;
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
                eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
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
