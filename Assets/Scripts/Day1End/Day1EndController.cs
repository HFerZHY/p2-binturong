using System.Collections;
using System.Collections.Generic;
using Otowa.Audio;
using Otowa.IndoorDialogue;
using Otowa.SaveSystem;
using Otowa.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Otowa.Day1End
{
    /// <summary>Text-only bridge from the Day 1 night map to the Day 2 exhibition.</summary>
    public class Day1EndController : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string nextSceneName = "ExhibitionDay2Scene";
        [SerializeField] private float typewriterSpeed = 38f;
        [SerializeField] private float fadeDuration = 0.45f;
        [SerializeField] private float titleClickHoldDuration = 2f;

        [Header("Font")]
        [SerializeField] private TMP_FontAsset serifFont;

        private static readonly Color DreamBg = Color.black;
        private static readonly Color BrightDreamBg = new(0.93f, 0.92f, 0.86f, 1f);
        private static readonly Color DreamText = new(0.92f, 0.96f, 1f, 1f);
        private static readonly Color TitleBg = new(0.97f, 0.96f, 0.92f, 1f);
        private static readonly Color WakeBg = new(0.88f, 0.92f, 0.90f, 1f);
        private static readonly Color WakeText = new(0.20f, 0.18f, 0.15f, 1f);
        private static readonly Color RinText = new(0.30f, 0.43f, 0.48f, 1f);
        private static readonly Color PromptText = new(0.40f, 0.54f, 0.58f, 0.92f);
        private static readonly Color CinematicSpeakerText = new Color32(0x8f, 0xbc, 0x8f, 0xff);
        private static readonly Color CinematicPromptText = new(1f, 1f, 1f, 0.66f);

        private const string CinematicBackgroundResource = "Exhibitions/Icons/passenger-background";

        private static readonly Beat[] Beats =
        {
            Dream("On the narrow folding bed in the stationmaster's office, you had a dream."),
            Dream("In the dream, there were no towering gray buildings, nor was there the suffocating air conditioning of an office building."),
            Dream("You dreamed you had turned into a glowing bird, flying over the ocean in the night sky. Colorful fireworks bloomed all around you."),
            Dream("Immediately after, you heard a long, drawn-out call."),
            Title("DAY 2"),
            Wake("...Mmh. Is that the whistle of the morning train?"),
            Wake("I slept so heavily... The insomnia that's been torturing me for months has miraculously disappeared."),
            Wake("Alright, now is not the time to space out."),
            Wake("Until the Summer Festival, which is also the deadline set by the inspector... there's less than two days left."),
            Wake("Since I've taken on this task, I have to deliver perfect results."),
            Wake("Let's see how I can turn this \"junk\" into treasures."),
        };

        private CanvasGroup _fade;
        private Image _background;
        private GameObject _dreamPanel;
        private TMP_Text _dreamBody;
        private GameObject _titlePanel;
        private GameObject _wakePanel;
        private TMP_Text _wakeBody;
        private TMP_Text _prompt;
        private RectTransform _particleLayer;
        private IndoorDialogueTextPlayer _textPlayer;
        private Sprite _particleSprite;
        private Coroutine _particleSpawner;
        private int _beatIndex;
        private bool _inputLock;
        private bool _loadingScene;
        private Coroutine _dreamBrightenCoroutine;

        private void Awake()
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            BuildUI();
            _textPlayer = gameObject.AddComponent<IndoorDialogueTextPlayer>();
            _textPlayer.Initialize(_prompt, typewriterSpeed);
        }

        private void Start()
        {
            _fade.alpha = 0f;
            var audio = GameAudioManager.Instance;
            audio.StopSfxLoop(AudioId.BluesBeat);
            audio.StopSfxLoop(AudioId.Wind);
            audio.PlayBgm(AudioId.HotSpring, fadeIn: 0.4f);
            ShowBeat(0);
            StartDreamParticles();
            StartCoroutine(FadeTo(1f));
        }

        private void Update()
        {
            if (_inputLock)
                return;

            if (PauseMenuController.ShouldSuppressWorldAdvance)
                return;

            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            bool advance = mouse != null && mouse.leftButton.wasPressedThisFrame
                           || keyboard != null && keyboard.spaceKey.wasPressedThisFrame
                           || keyboard != null && keyboard.enterKey.wasPressedThisFrame;
            if (!advance)
                return;

            if (_textPlayer.IsTyping)
            {
                _textPlayer.Skip();
                return;
            }

            int next = _beatIndex + 1;
            if (next >= Beats.Length)
                StartCoroutine(FadeAndLoad());
            else if (_beatIndex == 2 && next == 3)
            {
                if (_dreamBrightenCoroutine == null)
                    _dreamBrightenCoroutine = StartCoroutine(BrightenDreamThenShowWhistle());
            }
            else if (Beats[_beatIndex].Phase == BeatPhase.Title && Beats[next].Phase == BeatPhase.Wake)
            {
                StartCoroutine(TransitionFromTitleToWake(next));
            }
            else
                ShowBeat(next);
        }

        private void ShowBeat(int index)
        {
            _beatIndex = index;
            ApplyAudioCue(index);
            var beat = Beats[index];
            bool isDream = beat.Phase == BeatPhase.Dream;
            bool isTitle = beat.Phase == BeatPhase.Title;
            _background.color = isDream ? DreamBg : isTitle ? TitleBg : Color.black;
            _dreamPanel.SetActive(isDream);
            _titlePanel.SetActive(isTitle);
            _wakePanel.SetActive(!isDream && !isTitle);
            ConfigurePromptForCinematic(!isDream && !isTitle);

            if (isDream)
            {
                bool isBrightDream = index == 3;
                _background.color = isBrightDream ? BrightDreamBg : DreamBg;
                _dreamBody.color = isBrightDream ? WakeText : DreamText;
                _dreamBody.fontStyle = isBrightDream ? FontStyles.Bold : FontStyles.Normal;
                _textPlayer.Play(_dreamBody, beat.Text);
                return;
            }

            StopDreamParticles();
            if (isTitle)
            {
                _textPlayer.Play(_titlePanel.GetComponentInChildren<TMP_Text>(), beat.Text);
                return;
            }

            _textPlayer.Play(_wakeBody, beat.Text);
        }

        private static void ApplyAudioCue(int beatIndex)
        {
            var audio = GameAudioManager.Instance;
            switch (beatIndex)
            {
                case 3:
                    audio.StopBgm(0.35f);
                    audio.PlaySfxOnce(AudioId.WhistleClose);
                    break;
                case 5:
                    audio.PlaySfxLoop(AudioId.ForestAtmosphere, fadeIn: 0.25f);
                    break;
            }
        }

        private IEnumerator BrightenDreamThenShowWhistle()
        {
            _inputLock = true;
            _textPlayer.SetPromptVisible(false);
            StopDreamParticles();

            const float duration = 3f;
            Color start = _background.color;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _background.color = Color.Lerp(start, BrightDreamBg, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _background.color = BrightDreamBg;
            ShowBeat(3);
            _dreamBrightenCoroutine = null;
            _inputLock = false;
        }

        private IEnumerator TransitionFromTitleToWake(int nextBeatIndex)
        {
            _inputLock = true;
            _textPlayer.SetPromptVisible(false);

            yield return new WaitForSeconds(titleClickHoldDuration);
            yield return FadeTo(0f);
            ShowBeat(nextBeatIndex);
            yield return FadeTo(1f);

            _inputLock = false;
        }

        private IEnumerator FadeAndLoad()
        {
            if (_loadingScene)
                yield break;

            _loadingScene = true;
            _inputLock = true;
            GameAudioManager.Instance.StopSfxLoop(AudioId.ForestAtmosphere, 0.25f);
            GameAudioManager.Instance.PlaySfxOnce(AudioId.TrainRunning);
            yield return new WaitForSeconds(2f);
            yield return FadeTo(0f);
            SceneManager.LoadScene(nextSceneName);
        }

        private IEnumerator FadeTo(float target)
        {
            float start = _fade.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                _fade.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _fade.alpha = target;
        }

        private void BuildUI()
        {
            var canvasObject = new GameObject("Day1EndCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _fade = canvasObject.AddComponent<CanvasGroup>();
            _background = MakeImage(canvasObject.transform, "Background", DreamBg, Vector2.zero, Vector2.one);

            _particleLayer = (RectTransform)MakeRect(
                canvasObject.transform, "DreamParticles", Vector2.zero, Vector2.one).transform;
            _particleSprite = CreateGlowSprite(32);

            _dreamPanel = MakeRect(canvasObject.transform, "DreamPanel", Vector2.zero, Vector2.one);
            _dreamBody = MakeText(_dreamPanel.transform, "DreamBody", string.Empty,
                36f, DreamText, TextAlignmentOptions.Center,
                new Vector2(0.17f, 0.22f), new Vector2(0.83f, 0.78f));
            _dreamBody.lineSpacing = 14f;

            _titlePanel = MakeRect(canvasObject.transform, "DayTitlePanel", Vector2.zero, Vector2.one);
            MakeText(_titlePanel.transform, "DayTitle", string.Empty,
                96f, WakeText, TextAlignmentOptions.Center,
                new Vector2(0.15f, 0.35f), new Vector2(0.85f, 0.65f));
            _titlePanel.SetActive(false);

            _wakePanel = MakeRect(canvasObject.transform, "WakePanel", Vector2.zero, Vector2.one);
            var wakeStrip = MakeImage(_wakePanel.transform, "WakeBackground",
                Color.white, new Vector2(0f, 0.23f), new Vector2(1f, 0.81f));
            wakeStrip.sprite = LoadSprite(CinematicBackgroundResource);

            var wakeSubtitleBar = MakeRect(_wakePanel.transform, "WakeSubtitleBar",
                Vector2.zero, new Vector2(1f, 0.236f));
            MakeImage(wakeSubtitleBar.transform, "Background", Color.black, Vector2.zero, Vector2.one);
            var wakeSpeaker = MakeText(wakeSubtitleBar.transform, "Speaker", "Rin",
                34f, CinematicSpeakerText, TextAlignmentOptions.Left,
                new Vector2(0.08f, 0.57f), new Vector2(0.80f, 0.91f));
            _wakeBody = MakeText(wakeSubtitleBar.transform, "WakeBody", string.Empty,
                31f, Color.white, TextAlignmentOptions.Left,
                new Vector2(0.08f, 0.15f), new Vector2(0.80f, 0.63f));
            _wakeBody.lineSpacing = 14f;
            RuntimeFontLibrary.ApplyBreeSerif(wakeSpeaker, serifFont);
            RuntimeFontLibrary.ApplyBreeSerif(_wakeBody, serifFont);
            _wakePanel.SetActive(false);

            _prompt = MakeText(canvasObject.transform, "Prompt", string.Empty,
                22f, PromptText, TextAlignmentOptions.Center,
                new Vector2(0.30f, 0.035f), new Vector2(0.70f, 0.095f));
            _prompt.characterSpacing = 4f;
            _prompt.gameObject.SetActive(false);
        }

        private void ConfigurePromptForCinematic(bool cinematic)
        {
            var rect = (RectTransform)_prompt.transform;
            if (cinematic)
            {
                rect.anchorMin = new Vector2(0.78f, 0.009f);
                rect.anchorMax = new Vector2(0.97f, 0.052f);
                _prompt.fontSize = 18f;
                _prompt.fontStyle = FontStyles.Italic;
                _prompt.alignment = TextAlignmentOptions.BottomRight;
                _prompt.color = CinematicPromptText;
                _prompt.characterSpacing = 0f;
                RuntimeFontLibrary.ApplyBreeSerif(_prompt, serifFont);
            }
            else
            {
                rect.anchorMin = new Vector2(0.30f, 0.035f);
                rect.anchorMax = new Vector2(0.70f, 0.095f);
                _prompt.fontSize = 22f;
                _prompt.fontStyle = FontStyles.Normal;
                _prompt.alignment = TextAlignmentOptions.Center;
                _prompt.color = PromptText;
                _prompt.characterSpacing = 4f;
                if (serifFont != null)
                    _prompt.font = serifFont;
            }

            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void StartDreamParticles()
        {
            if (_particleSpawner == null)
                _particleSpawner = StartCoroutine(SpawnDreamParticles());
        }

        private void StopDreamParticles()
        {
            if (_particleSpawner != null)
            {
                StopCoroutine(_particleSpawner);
                _particleSpawner = null;
            }

            foreach (Transform child in _particleLayer)
                Destroy(child.gameObject);
        }

        private IEnumerator SpawnDreamParticles()
        {
            for (int i = 0; i < 18; i++)
                SpawnParticle(startMidway: true);

            while (true)
            {
                yield return new WaitForSeconds(0.22f);
                SpawnParticle(startMidway: false);
            }
        }

        private void SpawnParticle(bool startMidway)
        {
            var particle = new GameObject("DreamLight", typeof(RectTransform), typeof(Image));
            particle.transform.SetParent(_particleLayer, false);

            var image = particle.GetComponent<Image>();
            image.sprite = _particleSprite;
            image.raycastTarget = false;
            image.color = new Color(
                UnityEngine.Random.Range(0.25f, 0.44f),
                UnityEngine.Random.Range(0.55f, 0.82f),
                1f,
                UnityEngine.Random.Range(0.20f, 0.54f));

            var rect = (RectTransform)particle.transform;
            float size = UnityEngine.Random.Range(18f, 76f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);

            float width = _particleLayer.rect.width > 0f ? _particleLayer.rect.width : 1920f;
            float height = _particleLayer.rect.height > 0f ? _particleLayer.rect.height : 1080f;
            float startY = startMidway ? UnityEngine.Random.Range(0f, height) : -size;
            rect.anchoredPosition = new Vector2(UnityEngine.Random.Range(0f, width), startY);
            StartCoroutine(AnimateParticle(rect, image, height, startY));
        }

        private IEnumerator AnimateParticle(RectTransform rect, Image image, float height, float startY)
        {
            float speed = UnityEngine.Random.Range(24f, 92f);
            float originX = rect.anchoredPosition.x;
            float drift = UnityEngine.Random.Range(18f, 86f);
            float frequency = UnityEngine.Random.Range(0.45f, 1.15f);
            float phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float baseAlpha = image.color.a;
            float y = startY;
            float elapsed = 0f;

            while (true)
            {
                if (rect == null || image == null)
                    yield break;
                if (y >= height + rect.sizeDelta.y)
                    break;

                elapsed += Time.deltaTime;
                y += speed * Time.deltaTime;
                float pulse = 0.55f + 0.45f * Mathf.Sin(elapsed * 2.4f + phase);
                var color = image.color;
                color.a = baseAlpha * pulse;
                image.color = color;
                rect.anchoredPosition = new Vector2(
                    originX + Mathf.Sin(elapsed * frequency + phase) * drift,
                    y);
                yield return null;
            }

            Destroy(rect.gameObject);
        }

        private Image MakeImage(Transform parent, string name, Color color,
                                Vector2 anchorMin, Vector2 anchorMax)
        {
            var gameObject = MakeRect(parent, name, anchorMin, anchorMax);
            var image = gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
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
            if (serifFont != null)
                text.font = serifFont;
            return text;
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
            int size = radius * 2;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float center = radius - 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(1f - distance / radius);
                    alpha *= alpha;
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite LoadSprite(string resourcePath)
        {
            var sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites.Length > 0)
                return sprites[0];

            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                Debug.LogWarning($"[Day1EndController] Missing sprite resource: {resourcePath}");
                return null;
            }

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        private static Beat Dream(string text) => new(true, text);
        private static Beat Title(string text) => new(BeatPhase.Title, text);
        private static Beat Wake(string text) => new(BeatPhase.Wake, text);

        private readonly struct Beat
        {
            public Beat(bool isDream, string text)
                : this(isDream ? BeatPhase.Dream : BeatPhase.Wake, text)
            {
            }

            public Beat(BeatPhase phase, string text)
            {
                Phase = phase;
                Text = text;
            }

            public BeatPhase Phase { get; }
            public string Text { get; }
        }

        private enum BeatPhase
        {
            Dream,
            Title,
            Wake,
        }
    }
}
