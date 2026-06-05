using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Otowa.SaveSystem
{
    [DefaultExecutionOrder(-9000)]
    public class PauseMenuController : MonoBehaviour
    {
        private const string TitleSceneName = "StartMenu";

        private static readonly Color Backdrop = new(0f, 0f, 0f, 0.62f);
        private static readonly Color Panel = new Color32(0x11, 0x26, 0x30, 0xF4);
        private static readonly Color ButtonNormal = new Color32(0x20, 0x8C, 0xB8, 0xFF);
        private static readonly Color ButtonHover = new Color32(0x39, 0xB9, 0xE4, 0xFF);
        private static readonly Color ButtonPressed = new Color32(0x15, 0x62, 0x86, 0xFF);
        private static readonly Color TextColor = new Color32(0xF0, 0xFB, 0xFF, 0xFF);
        private static readonly Color SmallTextColor = new Color32(0xBA, 0xD3, 0xDA, 0xFF);

        private static PauseMenuController _instance;

        private GameObject _root;
        private float _previousTimeScale = 1f;
        private bool _visible;

        private static int _suppressAdvanceUntilFrame = -1;

        public static bool IsVisible => _instance != null && _instance._visible;
        public static bool ShouldSuppressWorldAdvance =>
            IsVisible || Time.frameCount <= _suppressAdvanceUntilFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInitialized()
        {
            if (_instance != null)
                return;

            var gameObject = new GameObject("PauseMenuController");
            _instance = gameObject.AddComponent<PauseMenuController>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureEventSystem();
            BuildUi();
            SetVisible(false, restoreTime: false);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
                return;

            if (!CanPauseCurrentScene())
                return;

            if (_visible)
                Continue();
            else
                Open();
        }

        private void Open()
        {
            GameSaveManager.Instance.SaveCurrent();
            _previousTimeScale = Time.timeScale;
            SetVisible(true, restoreTime: false);
            Time.timeScale = 0f;
        }

        private void Continue()
        {
            SetVisible(false, restoreTime: true);
        }

        private void ReturnToTitle()
        {
            SetVisible(false, restoreTime: false);
            GameSaveManager.Instance.SaveAndReturnToTitle();
        }

        private void Quit()
        {
            SetVisible(false, restoreTime: false);
            GameSaveManager.Instance.SaveAndQuit();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureEventSystem();
            SetVisible(false, restoreTime: true);
        }

        private void SetVisible(bool visible, bool restoreTime)
        {
            SuppressWorldAdvanceForInputFrame();
            _visible = visible;
            if (_root != null)
                _root.SetActive(visible);

            if (!visible && restoreTime)
                Time.timeScale = Mathf.Approximately(_previousTimeScale, 0f) ? 1f : _previousTimeScale;
        }

        private static bool CanPauseCurrentScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            return !string.IsNullOrWhiteSpace(sceneName) && sceneName != TitleSceneName;
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("PauseMenuCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            _root = canvasObject;

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32767;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            MakeImage(canvasObject.transform, "Backdrop", Backdrop, Vector2.zero, Vector2.one);
            var panel = MakeImage(canvasObject.transform, "Panel", Panel,
                new Vector2(0.34f, 0.29f), new Vector2(0.66f, 0.72f));

            var title = MakeText(panel.transform, "Title", "Paused",
                46f, TextColor, TextAlignmentOptions.Center,
                new Vector2(0.08f, 0.77f), new Vector2(0.92f, 0.92f));
            title.fontStyle = FontStyles.Bold;

            MakeButton(panel.transform, "ContinueButton", "Continue",
                new Vector2(0.18f, 0.56f), new Vector2(0.82f, 0.69f), Continue);
            MakeButton(panel.transform, "ReturnToTitleButton", "Return to Title",
                new Vector2(0.18f, 0.38f), new Vector2(0.82f, 0.51f), ReturnToTitle);
            MakeButton(panel.transform, "QuitButton", "Quit",
                new Vector2(0.18f, 0.20f), new Vector2(0.82f, 0.33f), Quit);

            MakeText(panel.transform, "AutosaveText", "Progress has been autosaved.",
                22f, SmallTextColor, TextAlignmentOptions.Center,
                new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.14f));
        }

        private void MakeButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            UnityEngine.Events.UnityAction action)
        {
            var gameObject = MakeRect(parent, name, anchorMin, anchorMax);
            var image = gameObject.AddComponent<Image>();
            image.color = ButtonNormal;

            var button = gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = new ColorBlock
            {
                normalColor = ButtonNormal,
                highlightedColor = ButtonHover,
                pressedColor = ButtonPressed,
                selectedColor = ButtonHover,
                disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.7f),
                colorMultiplier = 1f,
                fadeDuration = 0.12f,
            };
            button.onClick.AddListener(action);

            var text = MakeText(gameObject.transform, "Label", label,
                28f, TextColor, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            text.fontStyle = FontStyles.Bold;
        }

        private static void EnsureEventSystem()
        {
            var eventSystems = FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            if (eventSystems.Length == 0)
            {
                var eventSystemObject = new GameObject("EventSystem");
                EnsureInputSystemModule(eventSystemObject.AddComponent<EventSystem>());
                return;
            }

            foreach (var eventSystem in eventSystems)
                EnsureInputSystemModule(eventSystem);
        }

        private static void EnsureInputSystemModule(EventSystem eventSystem)
        {
            if (eventSystem == null)
                return;

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

            foreach (var module in eventSystem.GetComponents<BaseInputModule>())
            {
                if (module is InputSystemUIInputModule)
                    continue;

                module.enabled = false;
            }
        }

        public static void SuppressWorldAdvanceForInputFrame()
        {
            _suppressAdvanceUntilFrame = Mathf.Max(
                _suppressAdvanceUntilFrame,
                Time.frameCount + 1);
        }

        private static TextMeshProUGUI MakeText(
            Transform parent,
            string name,
            string text,
            float size,
            Color color,
            TextAlignmentOptions alignment,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var gameObject = MakeRect(parent, name, anchorMin, anchorMax);
            var tmp = gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
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
