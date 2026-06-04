using System.Collections;
using Otowa.Audio;
using Otowa.IndoorDialogue;
using Otowa.SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Otowa.Day3
{
    public class Day3OtowaBluesMontageController : MonoBehaviour
    {
        private enum BeatPhase
        {
            Indigo,
            DeepBlue,
            DarkBlue,
            Black
        }

        private enum AudioCue
        {
            None,
            StartTrain,
            FadeOutBgm,
            StartWind,
            FadeOutWind,
            StartSnoring,
            CloseDoorAndLeave
        }

        private readonly struct Beat
        {
            public Beat(BeatPhase phase, string text, bool pauseBefore = false, AudioCue cue = AudioCue.None)
            {
                Phase = phase;
                Text = text;
                PauseBefore = pauseBefore;
                Cue = cue;
            }

            public BeatPhase Phase { get; }
            public string Text { get; }
            public bool PauseBefore { get; }
            public AudioCue Cue { get; }
        }

        [SerializeField] private TMP_FontAsset _serifFont;
        [SerializeField] private string _nextSceneName = "Day3SummerFestivalSquare";
        [SerializeField] private float _charactersPerSecond = 38f;
        [SerializeField] private float _initialDelay = 0.8f;
        [SerializeField] private float _backgroundTransitionDuration = 0.75f;

        private static readonly Color IndigoBg = new Color(0.06f, 0.11f, 0.27f, 1f);
        private static readonly Color DeepBlueBg = new Color(0.025f, 0.07f, 0.18f, 1f);
        private static readonly Color DarkBlueBg = new Color(0.012f, 0.035f, 0.09f, 1f);
        private static readonly Color BodyColor = new Color(0.91f, 0.95f, 1f, 1f);
        private static readonly Color PromptColor = new Color(0.57f, 0.73f, 0.92f, 0.90f);

        private static readonly Beat[] Beats =
        {
            Indigo("You hear the melody of the music."),
            Indigo("Otowa Blues, but it doesn't sound like the traditional blues."),
            Indigo("It sounds like a blue dream."),
            Indigo("You hear a guitar boy who once pictured the shape of the city from his mountain village, then dreamed of that village once he reached the city."),
            Indigo("Then you start to hear more."),
            Indigo("A blue bird. Fireworks over the sea."),
            Indigo("The taste of shichimi. The taste of dango."),
            Indigo("The steam off the hot spring. The summer night's moon."),
            Indigo("And then you think of your own hometown, back in the days before the factories."),
            Indigo("Swinging a bug net through the hills, coming home soaked in sweat to find cold watermelon waiting on the table."),
            Indigo("You remember the last day of summer break, saying goodbye to your grandparents in tears."),
            Indigo("On the train back to the city, the Milky Way overhead had never burned so bright."),
            Indigo("The blue mountains slipped away behind you, and never had you longed so badly for that blue world."),
            DeepBlue("Beyond the window, a train rolls slowly past. The passengers hear the music, too.", AudioCue.StartTrain),
            DeepBlue("You see them. Some holding up their phones. Some with their eyes closed. Some waving hard, saying goodbye to you."),
            DarkBlue("With the blues still playing, you tell Hikaru about the Inspector, and how the station might be shut down.", AudioCue.FadeOutBgm),
            DarkBlue("Hikaru curls up in the corner of the room, silent, like a child who knows he's done something wrong.", AudioCue.StartWind),
            DarkBlue("You try to comfort him. No news is good news, you tell him. Things can still turn around."),
            DarkBlue("Then you wait. A long time.", AudioCue.FadeOutWind),
            DarkBlue("Then the last train pulls away. Only a handful of people step off and head into the village."),
            Black("Night falls, and still the Inspector never comes to deliver his final verdict."),
            Black("No miracle came."),
            Black("So this really is a lonely Summer Festival. This really is Otowa Station's last night."),
            Black("Hikaru has fallen asleep at the desk, and you watch two trails of tears slide down his round face.", cue: AudioCue.StartSnoring),
            Black("Well then. Time to go check on the villagers in the square.", pauseBefore: true),
            Black("You drape a coat over Hikaru, then quietly pull the office door shut behind you.", cue: AudioCue.CloseDoorAndLeave),
        };

        private CanvasGroup _fade;
        private Image _background;
        private TMP_Text _body;
        private TMP_Text _prompt;
        private RectTransform _particleLayer;
        private IndoorDialogueTextPlayer _textPlayer;
        private Sprite _particleSprite;
        private Coroutine _particleSpawner;
        private int _beatIndex = -1;
        private bool _inputLock;
        private bool _loadingScene;

        private void Awake()
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            BuildUi();
            _textPlayer = gameObject.AddComponent<IndoorDialogueTextPlayer>();
            _textPlayer.Initialize(_prompt, _charactersPerSecond);
        }

        private void Start()
        {
            GameAudioManager.Instance.PlayBgm(AudioId.OtowaBlues, fadeIn: 0.5f);
            _fade.alpha = 0f;
            StartParticles();
            StartCoroutine(BeginSequence());
        }

        private void Update()
        {
            if (_inputLock || _loadingScene || !WasAdvancePressed())
                return;

            if (_textPlayer.IsTyping)
            {
                _textPlayer.Skip();
                return;
            }

            int next = _beatIndex + 1;
            if (next >= Beats.Length)
                StartCoroutine(FadeAndLoad());
            else if (Beats[next].Phase != Beats[_beatIndex].Phase)
                StartCoroutine(TransitionTo(next));
            else if (Beats[next].PauseBefore)
                StartCoroutine(ShowAfterPause(next));
            else
                ShowBeat(next);
        }

        private IEnumerator BeginSequence()
        {
            _inputLock = true;
            yield return new WaitForSeconds(_initialDelay);
            ShowBeat(0);
            yield return FadeCanvasTo(1f, 0.65f);
            _inputLock = false;
        }

        private void ShowBeat(int index)
        {
            _beatIndex = index;
            ApplyAudioCue(Beats[index].Cue);
            _background.color = BackgroundFor(Beats[index].Phase);
            if (Beats[index].Phase == BeatPhase.Black)
                StopParticles();

            if (Beats[index].Cue == AudioCue.CloseDoorAndLeave)
                _textPlayer.Play(_body, Beats[index].Text, () => StartCoroutine(CloseDoorThenLeave()));
            else
                _textPlayer.Play(_body, Beats[index].Text);
        }

        private IEnumerator TransitionTo(int next)
        {
            _inputLock = true;
            _prompt.gameObject.SetActive(false);

            var target = BackgroundFor(Beats[next].Phase);
            var start = _background.color;
            var elapsed = 0f;
            while (elapsed < _backgroundTransitionDuration)
            {
                elapsed += Time.deltaTime;
                _background.color = Color.Lerp(start, target, Mathf.Clamp01(elapsed / _backgroundTransitionDuration));
                yield return null;
            }

            ShowBeat(next);
            _inputLock = false;
        }

        private IEnumerator ShowAfterPause(int next)
        {
            _inputLock = true;
            _prompt.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.95f);
            ShowBeat(next);
            _inputLock = false;
        }

        private IEnumerator FadeAndLoad()
        {
            if (_loadingScene)
                yield break;

            _loadingScene = true;
            _inputLock = true;
            GameAudioManager.Instance.StopSfxLoop(AudioId.OnTheTrain, 0.2f);
            GameAudioManager.Instance.StopSfxLoop(AudioId.Snoring, 0.2f);
            GameAudioManager.Instance.StopSfxLoop(AudioId.Wind, 0.2f);
            yield return FadeCanvasTo(0f, 0.65f);
            SceneManager.LoadScene(_nextSceneName);
        }

        private static void ApplyAudioCue(AudioCue cue)
        {
            switch (cue)
            {
                case AudioCue.StartTrain:
                    GameAudioManager.Instance.PlaySfxLoop(AudioId.OnTheTrain, fadeIn: 0.25f);
                    break;
                case AudioCue.FadeOutBgm:
                    GameAudioManager.Instance.StopSfxLoop(AudioId.OnTheTrain, 0.25f);
                    GameAudioManager.Instance.StopBgm(5f);
                    break;
                case AudioCue.StartWind:
                    GameAudioManager.Instance.PlaySfxLoop(AudioId.Wind, volume: 0.7f, fadeIn: 5f);
                    break;
                case AudioCue.FadeOutWind:
                    GameAudioManager.Instance.FadeSfxLoopTo(AudioId.Wind, 0f, 10f);
                    break;
                case AudioCue.StartSnoring:
                    GameAudioManager.Instance.PlaySfxLoop(AudioId.Snoring, fadeIn: 0.3f);
                    break;
            }
        }

        private IEnumerator CloseDoorThenLeave()
        {
            if (_loadingScene)
                yield break;

            _inputLock = true;
            _prompt.gameObject.SetActive(false);
            GameAudioManager.Instance.StopSfxLoop(AudioId.Snoring, 0.25f);
            GameAudioManager.Instance.PlaySfxOnce(AudioId.DoorOpen);
            yield return new WaitForSeconds(2f);
            yield return FadeAndLoad();
        }

        private IEnumerator FadeCanvasTo(float target, float duration)
        {
            var start = _fade.alpha;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _fade.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _fade.alpha = target;
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("Day3OtowaBluesMontageCanvas",
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
            _background = MakeImage(canvasObject.transform, "Background", IndigoBg, Vector2.zero, Vector2.one);

            _particleLayer = (RectTransform)MakeRect(
                canvasObject.transform, "BlueParticles", Vector2.zero, Vector2.one).transform;
            _particleSprite = CreateGlowSprite(32);

            _body = MakeText(canvasObject.transform, "Narration", string.Empty,
                36f, BodyColor, TextAlignmentOptions.Center,
                new Vector2(0.16f, 0.20f), new Vector2(0.84f, 0.80f));
            _body.lineSpacing = 14f;

            _prompt = MakeText(canvasObject.transform, "Prompt", "Click to continue  >",
                22f, PromptColor, TextAlignmentOptions.Center,
                new Vector2(0.30f, 0.035f), new Vector2(0.70f, 0.095f));
            _prompt.characterSpacing = 4f;
            _prompt.gameObject.SetActive(false);
        }

        private void StartParticles()
        {
            if (_particleSpawner == null)
                _particleSpawner = StartCoroutine(SpawnParticles());
        }

        private void StopParticles()
        {
            if (_particleSpawner != null)
            {
                StopCoroutine(_particleSpawner);
                _particleSpawner = null;
            }

            foreach (Transform child in _particleLayer)
                Destroy(child.gameObject);
        }

        private IEnumerator SpawnParticles()
        {
            for (int i = 0; i < 20; i++)
                SpawnParticle(startMidway: true);

            while (true)
            {
                yield return new WaitForSeconds(0.22f);
                SpawnParticle(startMidway: false);
            }
        }

        private void SpawnParticle(bool startMidway)
        {
            var particle = new GameObject("BlueLight", typeof(RectTransform), typeof(Image));
            particle.transform.SetParent(_particleLayer, false);

            var image = particle.GetComponent<Image>();
            image.sprite = _particleSprite;
            image.raycastTarget = false;
            image.color = new Color(
                Random.Range(0.24f, 0.42f),
                Random.Range(0.48f, 0.78f),
                1f,
                Random.Range(0.18f, 0.48f));

            var rect = (RectTransform)particle.transform;
            var size = Random.Range(18f, 74f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);

            var width = _particleLayer.rect.width > 0f ? _particleLayer.rect.width : 1920f;
            var height = _particleLayer.rect.height > 0f ? _particleLayer.rect.height : 1080f;
            var startY = startMidway ? Random.Range(0f, height) : -size;
            rect.anchoredPosition = new Vector2(Random.Range(0f, width), startY);
            StartCoroutine(AnimateParticle(rect, image, height, startY));
        }

        private IEnumerator AnimateParticle(RectTransform rect, Image image, float height, float startY)
        {
            var speed = Random.Range(24f, 88f);
            var originX = rect.anchoredPosition.x;
            var drift = Random.Range(18f, 82f);
            var frequency = Random.Range(0.45f, 1.15f);
            var phase = Random.Range(0f, Mathf.PI * 2f);
            var baseAlpha = image.color.a;
            var y = startY;
            var elapsed = 0f;

            while (rect != null && image != null && y < height + rect.sizeDelta.y)
            {
                elapsed += Time.deltaTime;
                y += speed * Time.deltaTime;
                var color = image.color;
                color.a = baseAlpha * (0.55f + 0.45f * Mathf.Sin(elapsed * 2.4f + phase));
                image.color = color;
                rect.anchoredPosition = new Vector2(
                    originX + Mathf.Sin(elapsed * frequency + phase) * drift,
                    y);
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

        private static Color BackgroundFor(BeatPhase phase)
        {
            return phase switch
            {
                BeatPhase.Indigo => IndigoBg,
                BeatPhase.DeepBlue => DeepBlueBg,
                BeatPhase.DarkBlue => DarkBlueBg,
                _ => Color.black,
            };
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

        private static Beat Indigo(string text) => new Beat(BeatPhase.Indigo, text);
        private static Beat DeepBlue(string text, AudioCue cue = AudioCue.None) => new Beat(BeatPhase.DeepBlue, text, cue: cue);
        private static Beat DarkBlue(string text, AudioCue cue = AudioCue.None) => new Beat(BeatPhase.DarkBlue, text, cue: cue);
        private static Beat Black(string text, bool pauseBefore = false, AudioCue cue = AudioCue.None) => new Beat(BeatPhase.Black, text, pauseBefore, cue);
    }
}
