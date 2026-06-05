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

namespace Otowa.Day3
{
    public class Day3NightTrainArrivalController : MonoBehaviour
    {
        private enum BeatKind
        {
            WhiteNarration,
            StripLine
        }

        private enum AudioCue
        {
            None,
            StopEnding
        }

        private readonly struct Beat
        {
            public Beat(
                BeatKind kind,
                string speaker,
                string text,
                CinematicStripPortraitFocus focus = CinematicStripPortraitFocus.None,
                int passengerIndex = -1,
                bool showNamedPair = false,
                bool clearPassengers = false,
                bool showInspector = false,
                AudioCue cue = AudioCue.None)
            {
                Kind = kind;
                Speaker = speaker;
                Text = text;
                Focus = focus;
                PassengerIndex = passengerIndex;
                ShowNamedPair = showNamedPair;
                ClearPassengers = clearPassengers;
                ShowInspector = showInspector;
                Cue = cue;
            }

            public BeatKind Kind { get; }
            public string Speaker { get; }
            public string Text { get; }
            public CinematicStripPortraitFocus Focus { get; }
            public int PassengerIndex { get; }
            public bool ShowNamedPair { get; }
            public bool ClearPassengers { get; }
            public bool ShowInspector { get; }
            public AudioCue Cue { get; }
        }

        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private TMP_FontAsset _centeredFont;
        [SerializeField] private float _charactersPerSecond = 38f;
        [SerializeField] private string _nextSceneName = "Day3InspectorDecision";
        [SerializeField] private float _nightStationTransitionDuration = 1.1f;
        [SerializeField] private float _runLeadInDuration = 3f;
        [SerializeField] private float _arrivalLeadInDuration = 3f;
        [SerializeField] private float _whiteLightFadeDuration = 2f;

        private static readonly Color SoftWhiteBackground = new(0.93f, 0.92f, 0.86f, 1f);
        private static readonly Color WhiteNarrationText = new(0.10f, 0.12f, 0.16f, 1f);

        private static readonly Beat[] Beats =
        {
            White("Through a burst of brilliant white light, you see a train drawing near."),
            White("And then,"),
            White("It slows to a gentle stop right in front of Otowa Station."),
            White("You see young people stepping off the train one after another..."),
            Passenger("Passenger", "It's been forever since I've been back to Otowa! I had no idea there'd be a night train today.", 0),
            Passenger("Passenger", "Look, look! This exhibition is so cool!", 1),
            Passenger("Passenger", "It's Mr. Yuji's sake! Man, I've missed this taste.", 2),
            Passenger("Passenger", "Tomorrow I am soaking in that hot spring till I melt!", 3),
            Passenger("Passenger", "Hey there, Stationmaster! Sorry we're rolling in so late. Happy Summer Festival!", 4),
            Keep("Rin", "...Happy Summer Festival!", CinematicStripPortraitFocus.Left),
            Keep("Rin", "(Unbelievable. Otowa's young people are stepping off this train one after another.)", CinematicStripPortraitFocus.Left),
            Pair("Misaki", "Hahaha, isn't that Hachi's guitar? How did it end up on display in here?", CinematicStripPortraitFocus.Right),
            Pair("Hachi", "Ah, my guitar...!", CinematicStripPortraitFocus.SecondaryRight),
            Pair("Misaki", "Hachi, you're a real celebrity now.", CinematicStripPortraitFocus.Right),
            Pair("Hachi", "Heh, yeah, you bet I am! I can't wait to see the look on my old man's face.", CinematicStripPortraitFocus.SecondaryRight),
            Pair("Misaki", "He's gonna go, \"What good is music? You should've stayed and learned to cook with me.\"", CinematicStripPortraitFocus.Right),
            Pair("Hachi", "I don't care anymore! I'll do whatever I want. What's the old man gonna do about it?", CinematicStripPortraitFocus.SecondaryRight),
            Pair("Misaki", "I really want to see Mizuki. I wonder if she ever got those postcards.", CinematicStripPortraitFocus.Right),
            Pair("Hachi", "Oh, little Mizuki? I heard you even put together an art book for her.", CinematicStripPortraitFocus.SecondaryRight),
            Pair("Misaki", "Hey, don't go blurting it out! It's a surprise! A surprise!", CinematicStripPortraitFocus.Right),
            Pair("Misaki", "Oh, and Stationmaster, happy Summer Festival! We've never met, but somehow you already feel like an old friend.", CinematicStripPortraitFocus.Right),
            Pair("Hachi", "Huh, so that Hikaru guy isn't the stationmaster anymore?", CinematicStripPortraitFocus.SecondaryRight),
            Pair("Rin", "He still is! I'm just the acting one!", CinematicStripPortraitFocus.Left),
            Pair("Hachi", "Got it. Anyway, happy Summer Festival to you both!", CinematicStripPortraitFocus.SecondaryRight),
            Pair("Rin", "Same to you. Happy Summer Festival!", CinematicStripPortraitFocus.Left),
            Pair("Rin", "(They all came back... Mizuki's friend, Mr. Jiro's son. Every person the villagers longed to see, home again as if by some miracle.)", CinematicStripPortraitFocus.Left),
            InspectorReveal("Inspector", "...", AudioCue.StopEnding),
            Keep("Rin", "Huh?!", CinematicStripPortraitFocus.Left),
        };

        private static readonly Color[] PassengerSpeakerColors =
        {
            new Color32(0xc8, 0x6a, 0x66, 0xff),
            new Color32(0x8d, 0x96, 0xd0, 0xff),
            new Color32(0xd0, 0x98, 0x58, 0xff),
            new Color32(0xba, 0x6a, 0x62, 0xff),
            new Color32(0x70, 0xa8, 0xb4, 0xff),
        };

        private CanvasGroup _fade;
        private Image _background;
        private GameObject _whiteNarrationRoot;
        private TMP_Text _whiteNarrationBody;
        private IndoorDialogueTextPlayer _whiteNarrationPlayer;
        private CinematicStripDialoguePlayer _stripPlayer;
        private Sprite _rinPortrait;
        private Sprite _misakiPortrait;
        private Sprite _hachiPortrait;
        private Sprite _inspectorPortrait;
        private Sprite[] _passengerPortraits;
        private int _beatIndex = -1;
        private bool _inputLock = true;
        private bool _loadingScene;

        private void Awake()
        {
            _font = RuntimeFontLibrary.BreeSerifRegularOr(_font);
            LoadResources();
            BuildInterface();
            StartCoroutine(BeginSequence());
        }

        private void Update()
        {
            if (_inputLock || _loadingScene || !WasAdvancePressed())
                return;

            if (CurrentBeatIsTyping())
            {
                SkipCurrentTyping();
                return;
            }

            AdvanceBeat();
        }

        private IEnumerator BeginSequence()
        {
            _inputLock = true;
            _fade.alpha = 0f;
            _background.color = Color.black;
            GameAudioManager.Instance.StopSfxLoop(AudioId.Wind, 0.35f);
            GameAudioManager.Instance.PlaySfxOnce(AudioId.Run);
            yield return new WaitForSeconds(_runLeadInDuration);
            GameAudioManager.Instance.PlaySfxOnce(AudioId.WhistleIn);
            yield return new WaitForSeconds(_arrivalLeadInDuration);
            yield return FadeCanvas(0f, 1f, 0.40f);
            yield return FadeBackground(Color.black, SoftWhiteBackground, _whiteLightFadeDuration);
            AdvanceBeat();
            _inputLock = false;
        }

        private void AdvanceBeat()
        {
            var nextBeatIndex = _beatIndex + 1;
            if (nextBeatIndex >= Beats.Length)
            {
                StartCoroutine(LeaveScene());
                return;
            }

            if (_beatIndex >= 0 &&
                Beats[_beatIndex].Kind == BeatKind.WhiteNarration &&
                Beats[nextBeatIndex].Kind == BeatKind.StripLine)
            {
                StartCoroutine(TransitionToNightStation(nextBeatIndex));
                return;
            }

            ShowBeat(nextBeatIndex);
        }

        private void ShowBeat(int beatIndex)
        {
            _beatIndex = beatIndex;
            var beat = Beats[_beatIndex];
            ApplyAudioCue(beat.Cue);
            _whiteNarrationRoot.SetActive(beat.Kind == BeatKind.WhiteNarration);
            _stripPlayer.SetVisible(beat.Kind == BeatKind.StripLine);

            if (beat.Kind == BeatKind.WhiteNarration)
            {
                _whiteNarrationPlayer.Play(_whiteNarrationBody, beat.Text);
                return;
            }

            if (beat.ShowInspector)
                _stripPlayer.SetCenteredFullBodyPortrait(_inspectorPortrait);
            else if (beat.ClearPassengers)
                _stripPlayer.SetPassengerPortraits(null);
            else if (beat.ShowNamedPair)
                _stripPlayer.SetPassengerPortraits(_misakiPortrait, _hachiPortrait);
            else if (beat.PassengerIndex >= 0 && beat.PassengerIndex < _passengerPortraits.Length)
            {
                _stripPlayer.SetPassengerPortraits(_passengerPortraits[beat.PassengerIndex]);
                if (beat.PassengerIndex < PassengerSpeakerColors.Length)
                    _stripPlayer.SetSpeakerColorOverride(PassengerSpeakerColors[beat.PassengerIndex]);
            }

            _stripPlayer.PlayLine(beat.Speaker, beat.Text, beat.Focus);
        }

        private static void ApplyAudioCue(AudioCue cue)
        {
            switch (cue)
            {
                case AudioCue.StopEnding:
                    GameAudioManager.Instance.StopBgm(0.35f, savePosition: true);
                    break;
            }
        }

        private IEnumerator TransitionToNightStation(int nextBeatIndex)
        {
            _inputLock = true;
            GameAudioManager.Instance.PlayBgm(AudioId.Ending, fadeIn: 0.75f);
            yield return FadeCanvas(1f, 0f, _nightStationTransitionDuration);
            ShowBeat(nextBeatIndex);
            yield return FadeCanvas(0f, 1f, _nightStationTransitionDuration);
            _inputLock = false;
        }

        private bool CurrentBeatIsTyping()
        {
            if (_beatIndex < 0 || _beatIndex >= Beats.Length)
                return false;

            return Beats[_beatIndex].Kind == BeatKind.WhiteNarration
                ? _whiteNarrationPlayer.IsTyping
                : _stripPlayer.IsTyping;
        }

        private void SkipCurrentTyping()
        {
            if (Beats[_beatIndex].Kind == BeatKind.WhiteNarration)
                _whiteNarrationPlayer.Skip();
            else
                _stripPlayer.SkipTyping();
        }

        private IEnumerator LeaveScene()
        {
            if (_loadingScene)
                yield break;

            _loadingScene = true;
            _inputLock = true;
            yield return FadeCanvas(1f, 0f, 0.65f);
            SceneManager.LoadScene(_nextSceneName);
        }

        private IEnumerator FadeCanvas(float from, float to, float duration)
        {
            _fade.alpha = from;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _fade.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _fade.alpha = to;
        }

        private IEnumerator FadeBackground(Color from, Color to, float duration)
        {
            _background.color = from;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _background.color = Color.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _background.color = to;
        }

        private void LoadResources()
        {
            _rinPortrait = LoadSpriteSlice("Characters/WorldSprite/rin", "spritesheet_template_0");
            _misakiPortrait = LoadSprite("Characters/PassengerPortraits/Misaki");
            _hachiPortrait = LoadSprite("Characters/PassengerPortraits/Hachi");
            _inspectorPortrait = LoadSprite("Characters/WorldSprite/Inspector_portrait");
            _passengerPortraits = new[]
            {
                LoadSprite("Characters/PassengerPortraits/Passenger01"),
                LoadSprite("Characters/PassengerPortraits/Passenger02"),
                LoadSprite("Characters/PassengerPortraits/Passenger03"),
                LoadSprite("Characters/PassengerPortraits/Passenger04"),
                LoadSprite("Characters/PassengerPortraits/Passenger05"),
            };
        }

        private void BuildInterface()
        {
            var canvasObject = new GameObject("Day3NightTrainArrivalCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            _fade = canvasObject.GetComponent<CanvasGroup>();

            var backgroundObject = MakeRect("Background", canvasObject.transform, Vector2.zero, Vector2.one);
            _background = backgroundObject.AddComponent<Image>();
            _background.color = Color.black;

            _whiteNarrationRoot = MakeRect("WhiteNarration", canvasObject.transform, Vector2.zero, Vector2.one);
            _whiteNarrationBody = MakeText("Body", _whiteNarrationRoot.transform,
                new Vector2(0.17f, 0.28f), new Vector2(0.83f, 0.72f));
            _whiteNarrationBody.fontSize = 39f;
            _whiteNarrationBody.fontStyle = FontStyles.Bold;
            _whiteNarrationBody.alignment = TextAlignmentOptions.Center;
            _whiteNarrationBody.color = WhiteNarrationText;
            UseFont(_whiteNarrationBody, _centeredFont);

            var prompt = MakeText("Prompt", _whiteNarrationRoot.transform,
                new Vector2(0.30f, 0.035f), new Vector2(0.70f, 0.095f));
            prompt.text = string.Empty;
            prompt.fontSize = 22f;
            prompt.alignment = TextAlignmentOptions.Center;
            prompt.color = new Color(0.27f, 0.34f, 0.44f, 0.90f);
            UseFont(prompt, _centeredFont);
            prompt.gameObject.SetActive(false);

            _whiteNarrationPlayer = gameObject.AddComponent<IndoorDialogueTextPlayer>();
            _whiteNarrationPlayer.Initialize(prompt, _charactersPerSecond);

            _stripPlayer = gameObject.AddComponent<CinematicStripDialoguePlayer>();
            _stripPlayer.Initialize(canvasObject.transform, _font, _charactersPerSecond);
            _stripPlayer.SetStripBackground(LoadSprite("Exhibitions/Icons/passenger-night"));
            _stripPlayer.SetPortraits(_rinPortrait, null);
            _stripPlayer.SetVisible(false);
        }

        private Sprite LoadSprite(string resourcePath)
        {
            var sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites.Length > 0)
                return sprites[0];

            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                Debug.LogWarning($"[Day3NightTrainArrival] Missing sprite resource: {resourcePath}");
                return null;
            }

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite LoadSpriteSlice(string resourcePath, string spriteName)
        {
            Sprite fallback = null;
            foreach (var sprite in Resources.LoadAll<Sprite>(resourcePath))
            {
                if (sprite == null)
                    continue;

                if (sprite.name == spriteName)
                    return sprite;

                if (fallback == null)
                    fallback = sprite;
            }

            return fallback;
        }

        private TextMeshProUGUI MakeText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var gameObject = MakeRect(name, parent, anchorMin, anchorMax);
            var text = gameObject.AddComponent<TextMeshProUGUI>();
            if (_centeredFont != null)
                text.font = _centeredFont;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static void UseFont(TMP_Text text, TMP_FontAsset font)
        {
            if (font != null)
                text.font = font;
        }

        private static GameObject MakeRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return gameObject;
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

        private static Beat White(string text) => new Beat(BeatKind.WhiteNarration, "", text);
        private static Beat Passenger(string speaker, string text, int passengerIndex, AudioCue cue = AudioCue.None) =>
            new Beat(BeatKind.StripLine, speaker, text, CinematicStripPortraitFocus.Right, passengerIndex, cue: cue);
        private static Beat Keep(string speaker, string text, CinematicStripPortraitFocus focus) =>
            new Beat(BeatKind.StripLine, speaker, text, focus);
        private static Beat Pair(string speaker, string text, CinematicStripPortraitFocus focus) =>
            new Beat(BeatKind.StripLine, speaker, text, focus, showNamedPair: true);
        private static Beat Clear(string speaker, string text, CinematicStripPortraitFocus focus = CinematicStripPortraitFocus.None) =>
            new Beat(BeatKind.StripLine, speaker, text, focus, clearPassengers: true);
        private static Beat InspectorReveal(string speaker, string text, AudioCue cue = AudioCue.None) =>
            new Beat(BeatKind.StripLine, speaker, text, CinematicStripPortraitFocus.Right, showInspector: true, cue: cue);
    }
}
