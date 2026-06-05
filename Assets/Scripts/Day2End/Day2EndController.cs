using System.Collections;
using Otowa.Audio;
using Otowa.IndoorDialogue;
using Otowa.SaveSystem;
using Otowa.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Otowa.Day2End
{
    /// <summary>Text-only bridge from the Day 2 afternoon map to the Day 3 exhibition.</summary>
    public class Day2EndController : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string nextSceneName = "ExhibitionDay3Scene";
        [SerializeField] private float typewriterSpeed = 38f;
        [SerializeField] private float fadeDuration = 0.45f;
        [SerializeField] private float morningTransitionPause = 0.75f;
        [SerializeField] private float morningFadeDuration = 2f;

        [Header("Font")]
        [SerializeField] private TMP_FontAsset serifFont;

        private static readonly Color DuskBg = new(0.10f, 0.12f, 0.19f, 1f);
        private static readonly Color DarkBg = new(0.045f, 0.06f, 0.11f, 1f);
        private static readonly Color SleepBg = Color.black;
        private static readonly Color MorningBg = new(0.88f, 0.92f, 0.90f, 1f);
        private static readonly Color NightText = new(0.92f, 0.95f, 1f, 1f);
        private static readonly Color MorningText = new(0.20f, 0.18f, 0.15f, 1f);
        private static readonly Color RinText = new(0.30f, 0.43f, 0.48f, 1f);
        private static readonly Color PromptText = new(0.62f, 0.72f, 0.82f, 0.92f);
        private static readonly Color CinematicSpeakerText = new Color32(0x8f, 0xbc, 0x8f, 0xff);
        private static readonly Color CinematicPromptText = new(1f, 1f, 1f, 0.66f);

        private const string CinematicBackgroundResource = "Exhibitions/Icons/passenger-background";

        private static readonly Beat[] Beats =
        {
            Dusk("Monogatari, which means story."),
            Dusk("Literally speaking, mono means items. A story, then, is a narrative about items."),
            Dusk("You have indeed collected many stories, such as the father's love carried in the dango, or the dreams imbued within the painting."),
            Dusk("However, can these stories truly touch those passengers? Can they really change the railway company's mind?"),
            Dark("Tomorrow is the final day. The day of the Summer Festival, and the day the station closes."),
            Sleep("Lost in these wandering thoughts, you fall into a dreamless sleep..."),
            Title("DAY 3\n~ The Final Day ~"),
            Morning("All right, the next train should be here in about half an hour."),
            Morning("Let me think... I came up with three new exhibition themes yesterday, and gathered quite a bit of Inspiration."),
            Morning("(Come to think of it, why isn't Mr. Hikaru back yet? Today is already the Summer Festival...)"),
            Morning("(If he knew the station was about to be permanently closed, I wonder how he would feel...)"),
            Morning("(Anyway, let's give it our best, Rin!)"),
        };

        private CanvasGroup _fade;
        private Image _background;
        private GameObject _narrationPanel;
        private TMP_Text _narrationBody;
        private GameObject _titlePanel;
        private TMP_Text _titleBody;
        private GameObject _morningPanel;
        private TMP_Text _morningBody;
        private TMP_Text _prompt;
        private IndoorDialogueTextPlayer _textPlayer;
        private int _beatIndex;
        private bool _inputLock;
        private bool _loadingScene;

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
            audio.StopBgm();
            audio.StopAllSfx();
            audio.PlaySfxLoop(AudioId.FaintInsectChirp, fadeIn: 0.25f);
            ShowBeat(0);
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
            {
                StartCoroutine(FadeAndLoad());
                return;
            }

            if (Beats[_beatIndex].Phase == BeatPhase.Title
                && Beats[next].Phase == BeatPhase.Morning)
            {
                StartCoroutine(FadeTitleIntoMorning(next));
            }
            else if (!ShouldCrossFade(Beats[_beatIndex].Phase, Beats[next].Phase))
                ShowBeat(next);
            else
                StartCoroutine(CrossFadeTo(next));
        }

        private void ShowBeat(int index)
        {
            _beatIndex = index;
            ApplyAudioCue(index);
            var beat = Beats[index];
            bool isMorning = beat.Phase == BeatPhase.Morning;
            bool isTitle = beat.Phase == BeatPhase.Title;

            _background.color = BackgroundFor(beat.Phase);
            _narrationPanel.SetActive(!isMorning && !isTitle);
            _titlePanel.SetActive(isTitle);
            _morningPanel.SetActive(isMorning);
            ConfigurePromptForCinematic(isMorning);

            if (isTitle)
            {
                _textPlayer.Play(_titleBody, beat.Text);
                return;
            }

            _textPlayer.Play(isMorning ? _morningBody : _narrationBody, beat.Text);
        }

        private static void ApplyAudioCue(int beatIndex)
        {
            if (beatIndex != 7)
                return;

            var audio = GameAudioManager.Instance;
            audio.StopSfxLoop(AudioId.FaintInsectChirp, 0.2f);
            audio.PlaySfxOnce(AudioId.WhistleFar);
            audio.PlaySfxLoop(AudioId.ForestAtmosphere, fadeIn: 0.25f);
        }

        private IEnumerator CrossFadeTo(int next)
        {
            _inputLock = true;
            yield return FadeTo(0f);
            ShowBeat(next);
            yield return FadeTo(1f);
            _inputLock = false;
        }

        private IEnumerator FadeTitleIntoMorning(int next)
        {
            _inputLock = true;
            yield return FadeTo(0f);
            _titlePanel.SetActive(false);
            _prompt.gameObject.SetActive(false);
            yield return new WaitForSeconds(morningTransitionPause);
            ShowBeat(next);
            yield return FadeTo(1f, morningFadeDuration);
            _inputLock = false;
        }

        private IEnumerator FadeAndLoad()
        {
            if (_loadingScene)
                yield break;

            _loadingScene = true;
            _inputLock = true;
            GameAudioManager.Instance.StopSfxLoop(AudioId.FaintInsectChirp, 0.2f);
            GameAudioManager.Instance.StopSfxLoop(AudioId.ForestAtmosphere, 0.25f);
            GameAudioManager.Instance.PlaySfxOnce(AudioId.TrainRunning);
            yield return new WaitForSeconds(2f);
            yield return FadeTo(0f);
            SceneManager.LoadScene(nextSceneName);
        }

        private IEnumerator FadeTo(float target)
        {
            yield return FadeTo(target, fadeDuration);
        }

        private IEnumerator FadeTo(float target, float duration)
        {
            float start = _fade.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                _fade.alpha = Mathf.Lerp(start, target, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _fade.alpha = target;
        }

        private void BuildUI()
        {
            var canvasObject = new GameObject("Day2EndCanvas",
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
            _background = MakeImage(
                canvasObject.transform, "Background", DuskBg, Vector2.zero, Vector2.one);

            _narrationPanel = MakeRect(
                canvasObject.transform, "NarrationPanel", Vector2.zero, Vector2.one);
            _narrationBody = MakeText(_narrationPanel.transform, "NarrationBody", string.Empty,
                36f, NightText, TextAlignmentOptions.Center,
                new Vector2(0.16f, 0.22f), new Vector2(0.84f, 0.78f));
            _narrationBody.lineSpacing = 14f;

            _titlePanel = MakeRect(
                canvasObject.transform, "DayTitlePanel", Vector2.zero, Vector2.one);
            _titleBody = MakeText(_titlePanel.transform, "DayTitle", string.Empty,
                84f, NightText, TextAlignmentOptions.Center,
                new Vector2(0.15f, 0.30f), new Vector2(0.85f, 0.70f));
            _titleBody.lineSpacing = 18f;
            _titleBody.characterSpacing = 4f;
            _titlePanel.SetActive(false);

            _morningPanel = MakeRect(
                canvasObject.transform, "MorningPanel", Vector2.zero, Vector2.one);
            var morningStrip = MakeImage(_morningPanel.transform, "MorningBackground",
                Color.white, new Vector2(0f, 0.23f), new Vector2(1f, 0.81f));
            morningStrip.sprite = LoadSprite(CinematicBackgroundResource);

            var morningSubtitleBar = MakeRect(_morningPanel.transform, "MorningSubtitleBar",
                Vector2.zero, new Vector2(1f, 0.236f));
            MakeImage(morningSubtitleBar.transform, "Background", Color.black, Vector2.zero, Vector2.one);
            var morningSpeaker = MakeText(morningSubtitleBar.transform, "Speaker", "Rin",
                34f, CinematicSpeakerText, TextAlignmentOptions.Left,
                new Vector2(0.08f, 0.57f), new Vector2(0.80f, 0.91f));
            _morningBody = MakeText(morningSubtitleBar.transform, "MorningBody", string.Empty,
                31f, Color.white, TextAlignmentOptions.Left,
                new Vector2(0.08f, 0.15f), new Vector2(0.80f, 0.63f));
            _morningBody.lineSpacing = 14f;
            RuntimeFontLibrary.ApplyBreeSerif(morningSpeaker, serifFont);
            RuntimeFontLibrary.ApplyBreeSerif(_morningBody, serifFont);
            _morningPanel.SetActive(false);

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

        private static Color BackgroundFor(BeatPhase phase)
        {
            return phase switch
            {
                BeatPhase.Dusk => DuskBg,
                BeatPhase.Dark => DarkBg,
                BeatPhase.Sleep => SleepBg,
                BeatPhase.Title => SleepBg,
                BeatPhase.Morning => Color.black,
                _ => SleepBg,
            };
        }

        private static bool ShouldCrossFade(BeatPhase current, BeatPhase next)
        {
            return current != next
                   && !(current == BeatPhase.Sleep && next == BeatPhase.Title);
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

        private static Sprite LoadSprite(string resourcePath)
        {
            var sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites.Length > 0)
                return sprites[0];

            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                Debug.LogWarning($"[Day2EndController] Missing sprite resource: {resourcePath}");
                return null;
            }

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        private static Beat Dusk(string text) => new(BeatPhase.Dusk, text);
        private static Beat Dark(string text) => new(BeatPhase.Dark, text);
        private static Beat Sleep(string text) => new(BeatPhase.Sleep, text);
        private static Beat Title(string text) => new(BeatPhase.Title, text);
        private static Beat Morning(string text) => new(BeatPhase.Morning, text);

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

        private enum BeatPhase
        {
            Dusk,
            Dark,
            Sleep,
            Title,
            Morning,
        }
    }
}
