using System.Collections;
using System.Linq;
using ExhibitionSystem.Data;
using Otowa.IndoorDialogue;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Otowa.Day3
{
    public class Day3FinaleCreditsController : MonoBehaviour
    {
        private enum BeatPhase
        {
            Sky,
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

        private static readonly Color SkyBg = new Color(0.025f, 0.08f, 0.22f, 1f);
        private static readonly Color SkyText = new Color(0.92f, 0.95f, 1f, 1f);
        private static readonly Color PromptText = new Color(0.62f, 0.76f, 0.95f, 0.90f);

        private static readonly Beat[] Beats =
        {
            Sky("Brilliant fireworks bloom across the night sky, and you know this is the moment Mr. Yuji has been waiting for."),
            Sky("You hear laughter, and the beat of festival drums. Junko waves a fan painted with the bird deity, praying for good fortune in the year to come."),
            Sky("\"Look, look! The blue bird came back!\" Professor Rintaro's shout carries across the square."),
            Sky("In the middle of all the merriment, you push open the door to the stationmaster's office."),
            Silhouettes("You run your fingers gently over each exhibit. The stone, the book, the octopus pot..."),
            Silhouettes("The clear touch under your fingertips brings back your three days in Otowa."),
            Silhouettes("The villagers' stories."),
            Silhouettes("And the revival of a village that had nearly been forgotten."),
            Silhouettes("All of it lives here."),
            Silhouettes("Here, in this one song. Otowa Blues."),
            Credits("Thanks for playing!\n\nCS247G\nTeam Binturong\n\nOtowa Blues"),
        };

        private CanvasGroup _fade;
        private Image _background;
        private TMP_Text _body;
        private TMP_Text _prompt;
        private RectTransform _fireworkLayer;
        private GameObject _silhouetteGrid;
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

            var next = _beatIndex + 1;
            if (next >= Beats.Length)
            {
                _finished = true;
                _prompt.gameObject.SetActive(false);
                return;
            }

            if (Beats[next].Phase != Beats[_beatIndex].Phase)
                StartCoroutine(CrossFadeTo(next));
            else
                ShowBeat(next);
        }

        private void ShowBeat(int index)
        {
            _beatIndex = index;
            var phase = Beats[index].Phase;
            var showSilhouettes = phase == BeatPhase.Silhouettes;
            _background.color = phase == BeatPhase.Sky ? SkyBg : Color.black;
            _silhouetteGrid.SetActive(showSilhouettes);
            _body.fontSize = phase == BeatPhase.Credits ? 62f : 37f;
            _body.lineSpacing = phase == BeatPhase.Credits ? 24f : 14f;
            _body.fontStyle = phase == BeatPhase.Credits ? FontStyles.Bold : FontStyles.Normal;
            _textPlayer.Play(_body, Beats[index].Text);
        }

        private IEnumerator CrossFadeTo(int next)
        {
            _inputLock = true;
            yield return FadeTo(0f);
            if (Beats[next].Phase != BeatPhase.Sky)
                StopFireworks();
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

        private void BuildUi()
        {
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

            _prompt = MakeText(canvasObject.transform, "Prompt", "Click to continue  >",
                22f, PromptText, TextAlignmentOptions.Center,
                new Vector2(0.30f, 0.035f), new Vector2(0.70f, 0.095f));
            _prompt.characterSpacing = 4f;
            _prompt.gameObject.SetActive(false);
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
            var mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            var keyboard = Keyboard.current;
            var keyboardPressed = keyboard != null
                                  && (keyboard.spaceKey.wasPressedThisFrame
                                      || keyboard.enterKey.wasPressedThisFrame);
            return mouseClicked || keyboardPressed;
        }

        private static Beat Sky(string text) => new Beat(BeatPhase.Sky, text);
        private static Beat Silhouettes(string text) => new Beat(BeatPhase.Silhouettes, text);
        private static Beat Credits(string text) => new Beat(BeatPhase.Credits, text);
    }
}
