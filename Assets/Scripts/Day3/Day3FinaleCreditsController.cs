using System.Collections;
using System.Linq;
using ExhibitionSystem.Data;
using Otowa.Audio;
using Otowa.IndoorDialogue;
using Otowa.SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Otowa.Day3
{
    public class Day3FinaleCreditsController : MonoBehaviour
    {
        private enum BeatPhase
        {
            Sky,
            Black,
            Silhouettes,
            Credits
        }

        private readonly struct Beat
        {
            public Beat(BeatPhase phase, string text)
            {
                Phase = phase;
                Text = text;
            }

            public BeatPhase Phase { get; }
            public string Text { get; }
        }

        [SerializeField] private TMP_FontAsset _serifFont;
        [SerializeField] private float _charactersPerSecond = 38f;
        [SerializeField] private float _fadeDuration = 0.65f;

        private const string AdvancePrompt = "Click to continue  >";
        private const string QuitButtonLabel = "thanks for playing";

        private static readonly Color SkyBg = new Color(0.025f, 0.08f, 0.22f, 1f);
        private static readonly Color SkyText = new Color(0.92f, 0.95f, 1f, 1f);
        private static readonly Color PromptText = new Color(0.62f, 0.76f, 0.95f, 0.90f);

        private static readonly Beat[] Beats =
        {
            Sky("Brilliant fireworks bloom across the night sky, and you know this is the moment Mr. Yuji has been waiting for."),
            Sky("The villagers crowd around you, excited and grateful, calling you the hero of Otowa."),
            Sky("You hear laughter, and the beat of festival drums."),
            Sky("Junko waves a fan painted with the bird deity, praying for good fortune in the year to come."),
            Sky("\"Look, look! The blue bird came back!\" Professor Rintaro raises his binoculars toward the moon."),
            Sky("You see Hachi perched on a big rock, eating dango, and Mizuki poring over the art book her friend brought her."),
            Sky("Hikaru comes weaving toward you, rubbing his eyes. It's just like a dream, he says."),
            Sky("Yes. Just like a dream. And you wish this summer night would never, never end."),
            Black("The noise of the festival slowly fades behind you, and you push open the door to the stationmaster's office."),
            Silhouettes("\"You are the hero of Otowa.\" The villagers' words fill you with quiet pride."),
            Silhouettes("Once, you could not save your own hometown. But this time, you saved Otowa."),
            Silhouettes("You proved that beyond being replaced, beyond being forgotten, a village can still hold another possibility."),
            Silhouettes("And yet... a faint unease lingers in your heart."),
            Silhouettes("What is the price of the railway company's goodwill?"),
            Silhouettes("Now that the world has seen Otowa, what unforeseeable changes will come to the villagers' lives?"),
            Silhouettes("Faced with the company, with the relentless tide of the times, what can you and Otowa really change?"),
            Silhouettes("And so, you run your fingers gently over each exhibit."),
            Silhouettes("The stone, the book, the octopus pot..."),
            Silhouettes("The clear touch under your fingertips brings back your three days in Otowa."),
            Silhouettes("The villagers' stories."),
            Silhouettes("And the revival of a village that had nearly been forgotten."),
            Silhouettes("All of it lives here."),
            Silhouettes("Here, in this one song. Otowa Blues."),
            Credits("CS247G\nTeam Binturong\n\nOtowa Blues"),
        };

        private CanvasGroup _fade;
        private Image _background;
        private TMP_Text _body;
        private TMP_Text _prompt;
        private Button _quitButton;
        private RectTransform _fireworkLayer;
        private GameObject _silhouetteGrid;
        private CanvasGroup _blackOverlay;
        private IndoorDialogueTextPlayer _textPlayer;
        private Sprite _glowSprite;
        private Coroutine _fireworkSpawner;
        private int _beatIndex;
        private bool _inputLock;
        private bool _finished;

        private void Awake()
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            BuildUi();
            _textPlayer = gameObject.AddComponent<IndoorDialogueTextPlayer>();
            _textPlayer.Initialize(_prompt, _charactersPerSecond);
        }

        private void Start()
        {
            GameAudioManager.Instance.PlayBgm(AudioId.Ending, fadeIn: 0.5f);
            GameAudioManager.Instance.PlaySfxLoop(AudioId.Fireworks, fadeIn: 0.35f);
            _fade.alpha = 0f;
            ShowBeat(0);
            StartFireworks();
            StartCoroutine(FadeTo(1f));
        }

        private void Update()
        {
            if (_inputLock || _finished || !WasAdvancePressed())
                return;

            if (_textPlayer.IsTyping)
            {
                _textPlayer.Skip();
                return;
            }

            if (Beats[_beatIndex].Phase == BeatPhase.Credits)
            {
                if (WasKeyboardConfirmPressed() && _quitButton != null && _quitButton.gameObject.activeSelf)
                    QuitGame();
                return;
            }

            var next = _beatIndex + 1;
            if (next >= Beats.Length)
            {
                QuitGame();
                return;
            }

            if (Beats[next].Phase != Beats[_beatIndex].Phase)
                StartCoroutine(CrossFadeTo(next));
            else
                ShowBeat(next);
        }

        private void QuitGame()
        {
            _finished = true;
            _prompt.gameObject.SetActive(false);
            if (_quitButton != null)
                _quitButton.gameObject.SetActive(false);
            GameAudioManager.Instance.StopSfxLoop(AudioId.Fireworks);
            GameAudioManager.Instance.StopBgm();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ShowBeat(int index)
        {
            _beatIndex = index;
            var phase = Beats[index].Phase;
            var showSilhouettes = phase == BeatPhase.Silhouettes;
            _background.color = phase == BeatPhase.Sky ? SkyBg : Color.black;
            _silhouetteGrid.SetActive(showSilhouettes);
            _prompt.text = phase == BeatPhase.Credits ? string.Empty : AdvancePrompt;
            _prompt.gameObject.SetActive(false);
            if (_quitButton != null)
                _quitButton.gameObject.SetActive(false);
            _body.fontSize = phase == BeatPhase.Credits ? 62f : 37f;
            _body.lineSpacing = phase == BeatPhase.Credits ? 24f : 14f;
            _body.fontStyle = phase == BeatPhase.Credits ? FontStyles.Bold : FontStyles.Normal;
            _textPlayer.Play(_body, Beats[index].Text,
                phase == BeatPhase.Credits ? ShowQuitButton : null);
        }

        private void ShowQuitButton()
        {
            _prompt.gameObject.SetActive(false);
            _quitButton.gameObject.SetActive(true);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(_quitButton.gameObject);
        }

        private IEnumerator CrossFadeTo(int next)
        {
            _inputLock = true;

            var currentPhase = Beats[_beatIndex].Phase;
            var nextPhase = Beats[next].Phase;
            if (currentPhase == BeatPhase.Sky && nextPhase == BeatPhase.Black)
            {
                GameAudioManager.Instance.StopSfxLoop(AudioId.Fireworks, 2f);
                GameAudioManager.Instance.StopBgm(2f);
                yield return FadeBlackOverlayTo(1f, 2f);
                StopFireworks();
                ShowBeat(next);
                yield return FadeBlackOverlayTo(0f, 0.65f);
                _inputLock = false;
                yield break;
            }

            if (currentPhase == BeatPhase.Black && nextPhase == BeatPhase.Silhouettes)
            {
                GameAudioManager.Instance.PlayBgm(AudioId.OtowaBlues, fadeIn: 1.1f);
                ShowBeat(next);
                _inputLock = false;
                yield break;
            }

            yield return FadeTo(0f);
            if (nextPhase != BeatPhase.Sky)
            {
                StopFireworks();
                GameAudioManager.Instance.StopSfxLoop(AudioId.Fireworks, 0.5f);
            }
            ShowBeat(next);
            yield return FadeTo(1f);
            _inputLock = false;
        }

        private IEnumerator FadeTo(float target)
        {
            var start = _fade.alpha;
            var elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _fade.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / _fadeDuration));
                yield return null;
            }

            _fade.alpha = target;
        }

        private IEnumerator FadeBlackOverlayTo(float target, float duration)
        {
            if (_blackOverlay == null)
                yield break;

            var start = _blackOverlay.alpha;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _blackOverlay.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _blackOverlay.alpha = target;
        }

        private void BuildUi()
        {
            EnsureEventSystem();

            var canvasObject = new GameObject("Day3FinaleCreditsCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            _fade = canvasObject.GetComponent<CanvasGroup>();

            _background = MakeImage(canvasObject.transform, "Background", SkyBg, Vector2.zero, Vector2.one);
            _fireworkLayer = (RectTransform)MakeRect(
                canvasObject.transform, "Fireworks", Vector2.zero, Vector2.one).transform;
            _glowSprite = CreateGlowSprite(18);

            BuildSilhouettes(canvasObject.transform);

            _body = MakeText(canvasObject.transform, "Body", string.Empty,
                37f, SkyText, TextAlignmentOptions.Center,
                new Vector2(0.15f, 0.19f), new Vector2(0.85f, 0.81f));
            _body.lineSpacing = 14f;

            _prompt = MakeText(canvasObject.transform, "Prompt", AdvancePrompt,
                22f, PromptText, TextAlignmentOptions.Center,
                new Vector2(0.30f, 0.035f), new Vector2(0.70f, 0.095f));
            _prompt.characterSpacing = 4f;
            _prompt.gameObject.SetActive(false);

            _quitButton = MakeButton(canvasObject.transform, "QuitButton", QuitButtonLabel,
                new Vector2(0.39f, 0.035f), new Vector2(0.61f, 0.105f));
            _quitButton.onClick.AddListener(QuitGame);
            _quitButton.gameObject.SetActive(false);

            var blackOverlayObject = MakeRect(canvasObject.transform, "BlackFadeOverlay", Vector2.zero, Vector2.one);
            var blackOverlayImage = blackOverlayObject.AddComponent<Image>();
            blackOverlayImage.color = Color.black;
            blackOverlayImage.raycastTarget = false;
            _blackOverlay = blackOverlayObject.AddComponent<CanvasGroup>();
            _blackOverlay.alpha = 0f;
        }

        private void BuildSilhouettes(Transform canvasRoot)
        {
            _silhouetteGrid = MakeRect(canvasRoot, "ExhibitSilhouettes",
                new Vector2(0.16f, 0.57f), new Vector2(0.84f, 0.91f));
            var grid = _silhouetteGrid.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 8;
            grid.cellSize = new Vector2(132f, 132f);
            grid.spacing = new Vector2(12f, 12f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            var items = Resources.LoadAll<ExhibitItemData>("Exhibitions/Items")
                .Where(item => item != null)
                .OrderBy(item => item.sortOrder)
                .Take(16)
                .ToArray();

            foreach (var item in items)
            {
                var iconObject = new GameObject($"Silhouette_{item.sortOrder:00}", typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(_silhouetteGrid.transform, false);
                var image = iconObject.GetComponent<Image>();
                image.sprite = item.icon;
                image.color = new Color(0.28f, 0.39f, 0.62f, 0.72f);
                image.preserveAspect = true;
                image.raycastTarget = false;
            }

            _silhouetteGrid.SetActive(false);
        }

        private void StartFireworks()
        {
            if (_fireworkSpawner == null)
                _fireworkSpawner = StartCoroutine(SpawnFireworks());
        }

        private void StopFireworks()
        {
            if (_fireworkSpawner != null)
            {
                StopCoroutine(_fireworkSpawner);
                _fireworkSpawner = null;
            }

            foreach (Transform child in _fireworkLayer)
                Destroy(child.gameObject);
        }

        private IEnumerator SpawnFireworks()
        {
            while (true)
            {
                SpawnBurst();
                yield return new WaitForSeconds(Random.Range(0.65f, 1.10f));
            }
        }

        private void SpawnBurst()
        {
            var width = _fireworkLayer.rect.width > 0f ? _fireworkLayer.rect.width : 1920f;
            var height = _fireworkLayer.rect.height > 0f ? _fireworkLayer.rect.height : 1080f;
            var center = new Vector2(Random.Range(width * 0.18f, width * 0.82f),
                Random.Range(height * 0.48f, height * 0.88f));
            var color = Color.HSVToRGB(Random.value, 0.55f, 1f);
            var count = Random.Range(12, 20);

            for (var i = 0; i < count; i++)
            {
                var light = new GameObject("FireworkLight", typeof(RectTransform), typeof(Image));
                light.transform.SetParent(_fireworkLayer, false);
                var image = light.GetComponent<Image>();
                image.sprite = _glowSprite;
                image.raycastTarget = false;
                image.color = new Color(color.r, color.g, color.b, 0.82f);
                var rect = (RectTransform)light.transform;
                rect.anchorMin = rect.anchorMax = Vector2.zero;
                rect.sizeDelta = new Vector2(24f, 24f);
                rect.anchoredPosition = center;
                var angle = Mathf.PI * 2f * i / count;
                StartCoroutine(AnimateBurstLight(rect, image,
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(90f, 170f)));
            }
        }

        private IEnumerator AnimateBurstLight(RectTransform rect, Image image, Vector2 velocity)
        {
            const float duration = 1.05f;
            var elapsed = 0f;
            var start = rect.anchoredPosition;
            while (elapsed < duration && rect != null && image != null)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                rect.anchoredPosition = start + velocity * t;
                var color = image.color;
                color.a = 0.82f * (1f - t);
                image.color = color;
                yield return null;
            }

            if (rect != null)
                Destroy(rect.gameObject);
        }

        private TMP_Text MakeText(Transform parent, string name, string value, float size,
                                  Color color, TextAlignmentOptions alignment,
                                  Vector2 anchorMin, Vector2 anchorMax)
        {
            var gameObject = MakeRect(parent, name, anchorMin, anchorMax);
            var text = gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            if (_serifFont != null)
                text.font = _serifFont;
            return text;
        }

        private Button MakeButton(Transform parent, string name, string label,
                                  Vector2 anchorMin, Vector2 anchorMax)
        {
            var gameObject = MakeRect(parent, name, anchorMin, anchorMax);
            var image = gameObject.AddComponent<Image>();
            image.color = new Color(0.78f, 0.86f, 1f, 0.16f);
            image.raycastTarget = true;

            var button = gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = new Color(0.78f, 0.86f, 1f, 0.16f),
                highlightedColor = new Color(0.78f, 0.86f, 1f, 0.28f),
                pressedColor = new Color(0.78f, 0.86f, 1f, 0.42f),
                selectedColor = new Color(0.78f, 0.86f, 1f, 0.30f),
                disabledColor = new Color(0.78f, 0.86f, 1f, 0.07f),
                colorMultiplier = 1f,
                fadeDuration = 0.12f
            };

            var labelText = MakeText(gameObject.transform, "Label", label, 25f,
                SkyText, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            labelText.fontStyle = FontStyles.Bold;
            labelText.characterSpacing = 2f;
            labelText.textWrappingMode = TextWrappingModes.NoWrap;

            return button;
        }

        private static Image MakeImage(Transform parent, string name, Color color,
                                       Vector2 anchorMin, Vector2 anchorMax)
        {
            var gameObject = MakeRect(parent, name, anchorMin, anchorMax);
            var image = gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
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

        private static void EnsureEventSystem()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                return;
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        private static Sprite CreateGlowSprite(int radius)
        {
            var size = radius * 2;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            var center = radius - 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    var alpha = Mathf.Clamp01(1f - distance / radius);
                    alpha *= alpha;
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static bool WasAdvancePressed()
        {
            if (PauseMenuController.ShouldSuppressWorldAdvance)
                return false;

            var mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            var keyboard = Keyboard.current;
            var keyboardPressed = keyboard != null
                                  && (keyboard.spaceKey.wasPressedThisFrame
                                      || keyboard.enterKey.wasPressedThisFrame);
            return mouseClicked || keyboardPressed;
        }

        private static bool WasKeyboardConfirmPressed()
        {
            var keyboard = Keyboard.current;
            return keyboard != null
                   && (keyboard.spaceKey.wasPressedThisFrame
                       || keyboard.enterKey.wasPressedThisFrame);
        }

        private static Beat Sky(string text) => new Beat(BeatPhase.Sky, text);
        private static Beat Black(string text) => new Beat(BeatPhase.Black, text);
        private static Beat Silhouettes(string text) => new Beat(BeatPhase.Silhouettes, text);
        private static Beat Credits(string text) => new Beat(BeatPhase.Credits, text);
    }
}
