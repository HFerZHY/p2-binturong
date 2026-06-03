using System.Collections;
using System.Collections.Generic;
using Otowa.Audio;
using Otowa.IndoorDialogue;
using Otowa.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Otowa.Day3
{
    public class Day3HikaruArrivalController : MonoBehaviour
    {
        private enum BeatKind
        {
            BlackLine,
            StripLine,
            RecordPopup
        }

        private enum AudioCue
        {
            None,
            Footsteps,
            SwitchToNightWalk,
            EnterOffice
        }

        private struct Beat
        {
            public BeatKind Kind;
            public string Speaker;
            public string Text;
            public CinematicStripPortraitFocus Focus;
            public AudioCue Cue;

            public Beat(
                BeatKind kind,
                string speaker,
                string text,
                CinematicStripPortraitFocus focus = CinematicStripPortraitFocus.None,
                AudioCue cue = AudioCue.None)
            {
                Kind = kind;
                Speaker = speaker;
                Text = text;
                Focus = focus;
                Cue = cue;
            }
        }

        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private TMP_FontAsset _centeredFont;
        [SerializeField] private float _charactersPerSecond = 38f;
        [SerializeField] private string _nextSceneName = "Day3OtowaBluesMontage";

        private readonly List<Beat> _beats = new List<Beat>();
        private CanvasGroup _canvasGroup;
        private GameObject _blackNarrationRoot;
        private TextMeshProUGUI _narrationSpeaker;
        private TextMeshProUGUI _narrationBody;
        private IndoorDialogueTextPlayer _narrationPlayer;
        private CinematicStripDialoguePlayer _stripPlayer;
        private GameObject _recordPopup;
        private Sprite _rinPortrait;
        private Sprite _hikaruPortrait;
        private int _beatIndex = -1;
        private bool _recordGranted;
        private bool _transitioning;

        private void Awake()
        {
            _font = RuntimeFontLibrary.BreeSerifRegularOr(_font);
            _centeredFont = RuntimeFontLibrary.BreeSerifRegularOr(_centeredFont);
            BuildBeats();
            LoadResources();
            BuildInterface();
            EnsureEventSystem();
            StartCoroutine(BeginSequence());
        }

        private void Update()
        {
            if (_transitioning || _recordPopup.activeSelf)
                return;

            if (!WasAdvancePressed())
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
            GameAudioManager.Instance.StopBgm();
            GameAudioManager.Instance.PlaySfxLoop(AudioId.ForestAtmosphere, fadeIn: 0.35f);
            _canvasGroup.alpha = 0f;
            yield return FadeCanvas(0f, 1f, 0.8f);
            AdvanceBeat();
        }

        private void BuildBeats()
        {
            _beats.Add(new Beat(BeatKind.BlackLine, "Rin", "(...Done.)"));
            _beats.Add(new Beat(BeatKind.BlackLine, "Rin", "(That's enough for today. About time to clock out...)"));
            _beats.Add(new Beat(BeatKind.BlackLine, "Rin", "(Huh? A passenger, at this hour?)",
                cue: AudioCue.Footsteps));

            _beats.Add(new Beat(BeatKind.StripLine, "???", "Hello there.", CinematicStripPortraitFocus.Right));
            _beats.Add(new Beat(BeatKind.StripLine, "???", "Hmm... let me guess. You must be Rin?", CinematicStripPortraitFocus.Right));
            _beats.Add(new Beat(BeatKind.StripLine, "Rin", "Huh? And you are...", CinematicStripPortraitFocus.Left));
            _beats.Add(new Beat(BeatKind.StripLine, "???", "Ah, sorry. I forgot to introduce myself.", CinematicStripPortraitFocus.Right));
            _beats.Add(new Beat(BeatKind.StripLine, "???", "I'm Hikaru. Nice to finally meet you.", CinematicStripPortraitFocus.Right));
            _beats.Add(new Beat(BeatKind.StripLine, "Rin", "!", CinematicStripPortraitFocus.Left));

            _beats.Add(new Beat(BeatKind.BlackLine, "", "It's Hikaru. The former stationmaster who left this whole place in my hands.",
                cue: AudioCue.SwitchToNightWalk));
            _beats.Add(new Beat(BeatKind.BlackLine, "", "He stands on the platform, looking around the station I've put back in order, and says nothing for a long while."));

            _beats.Add(new Beat(BeatKind.StripLine, "Hikaru", "...It's real.", CinematicStripPortraitFocus.Right));
            _beats.Add(new Beat(BeatKind.StripLine, "Hikaru", "This is exactly how I always dreamed it would look.", CinematicStripPortraitFocus.Right));
            _beats.Add(new Beat(BeatKind.StripLine, "Hikaru", "Thank you, Rin.", CinematicStripPortraitFocus.Right));
            _beats.Add(new Beat(BeatKind.StripLine, "Rin", "Honestly, when I first took this job, the whole thing made no sense to me.", CinematicStripPortraitFocus.Left));
            _beats.Add(new Beat(BeatKind.StripLine, "Rin", "But somewhere along the way... learning the stories behind these things, it started to mean something to me, too.", CinematicStripPortraitFocus.Left));
            _beats.Add(new Beat(BeatKind.StripLine, "Rin", "Oh, right. Mr. Hikaru. Where did you run off to all these days, anyway?", CinematicStripPortraitFocus.Left));
            _beats.Add(new Beat(BeatKind.StripLine, "Hikaru", "Ah, that...", CinematicStripPortraitFocus.Right));
            _beats.Add(new Beat(BeatKind.StripLine, "Hikaru", "Right around the time you were due to arrive in Otowa, it hit me. There was one exhibit this collection absolutely couldn't do without.", CinematicStripPortraitFocus.Right));
            _beats.Add(new Beat(BeatKind.StripLine, "Rin", "What could be that important?", CinematicStripPortraitFocus.Left));
            _beats.Add(new Beat(BeatKind.StripLine, "Hikaru", "Mm. Music a friend of mine made.", CinematicStripPortraitFocus.Right));
            _beats.Add(new Beat(BeatKind.StripLine, "Hikaru", "I went into the city, and everyone kept telling me nobody buys records anymore, that it's all phones now. But I searched for days, and in the end I tracked one down...", CinematicStripPortraitFocus.Right));
            _beats.Add(new Beat(BeatKind.StripLine, "Rin", "Why does this record matter so much to you, Hikaru?", CinematicStripPortraitFocus.Left));
            _beats.Add(new Beat(BeatKind.StripLine, "Hikaru", "I think our exhibition can't only be about the village's past.", CinematicStripPortraitFocus.Right));
            _beats.Add(new Beat(BeatKind.StripLine, "Hikaru", "It should hold the village's future, too. This record, for instance... after Hachi left Otowa, he went and wrote a song about home.", CinematicStripPortraitFocus.Right));
            _beats.Add(new Beat(BeatKind.StripLine, "Rin", "(Hachi? Mr. Jiro's son, the guitar boy?)", CinematicStripPortraitFocus.Left));
            _beats.Add(new Beat(BeatKind.RecordPopup, "", ""));
            _beats.Add(new Beat(BeatKind.StripLine, "Hikaru", "Otowa Blues... I'd been thinking, if this ever became a real exhibition, that could be its name.", CinematicStripPortraitFocus.Right));
            _beats.Add(new Beat(BeatKind.StripLine, "Rin", "...Mr. Hikaru.", CinematicStripPortraitFocus.Left));
            _beats.Add(new Beat(BeatKind.StripLine, "Hikaru", "Hm?", CinematicStripPortraitFocus.Right));
            _beats.Add(new Beat(BeatKind.StripLine, "Rin", "You know, you've got a real eye for this curating thing after all.", CinematicStripPortraitFocus.Left));
            _beats.Add(new Beat(BeatKind.StripLine, "Hikaru", "R-really?", CinematicStripPortraitFocus.Right));
            _beats.Add(new Beat(BeatKind.StripLine, "Rin", "Come on, let's give the song a listen.", CinematicStripPortraitFocus.Left));

            _beats.Add(new Beat(BeatKind.BlackLine, "", "You and Hikaru head back into the stationmaster's office.",
                cue: AudioCue.EnterOffice));
            _beats.Add(new Beat(BeatKind.BlackLine, "", "Hikaru digs an old Walkman out of some corner and loads the Otowa Blues record into it."));
        }

        private void LoadResources()
        {
            _rinPortrait = LoadSpriteSlice("Characters/WorldSprite/rin", "spritesheet_template_0");
            _hikaruPortrait = LoadSprite("Characters/PassengerPortraits/Hikaru");
        }

        private void BuildInterface()
        {
            var canvasObject = new GameObject("Day3HikaruArrivalCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            _canvasGroup = canvasObject.GetComponent<CanvasGroup>();

            _blackNarrationRoot = CreateRect("BlackNarration", canvasObject.transform, Vector2.zero, Vector2.one);
            var black = _blackNarrationRoot.AddComponent<Image>();
            black.color = Color.black;

            _narrationSpeaker = CreateText("Speaker", _blackNarrationRoot.transform, new Vector2(0.18f, 0.55f), new Vector2(0.82f, 0.64f));
            _narrationSpeaker.fontSize = 38f;
            _narrationSpeaker.fontStyle = FontStyles.Bold;
            _narrationSpeaker.alignment = TextAlignmentOptions.Center;
            _narrationSpeaker.color = new Color(0.74f, 0.86f, 1f);
            UseFont(_narrationSpeaker, _centeredFont);

            _narrationBody = CreateText("Narration", _blackNarrationRoot.transform, new Vector2(0.18f, 0.36f), new Vector2(0.82f, 0.57f));
            _narrationBody.fontSize = 34f;
            _narrationBody.alignment = TextAlignmentOptions.Center;
            _narrationBody.color = Color.white;
            UseFont(_narrationBody, _centeredFont);

            var prompt = CreateText("Prompt", _blackNarrationRoot.transform, new Vector2(0.72f, 0.08f), new Vector2(0.94f, 0.15f));
            prompt.text = "click to continue";
            prompt.fontSize = 19f;
            prompt.fontStyle = FontStyles.Italic;
            prompt.alignment = TextAlignmentOptions.BottomRight;
            prompt.color = new Color(1f, 1f, 1f, 0.66f);

            _narrationPlayer = gameObject.AddComponent<IndoorDialogueTextPlayer>();
            _narrationPlayer.Initialize(prompt, _charactersPerSecond);

            _stripPlayer = gameObject.AddComponent<CinematicStripDialoguePlayer>();
            _stripPlayer.Initialize(canvasObject.transform, _font, _charactersPerSecond);
            _stripPlayer.SetStripBackground(LoadSprite("Exhibitions/Icons/passenger-background"));
            _stripPlayer.SetPortraits(_rinPortrait, _hikaruPortrait);

            BuildRecordPopup(canvasObject.transform);
        }

        private void BuildRecordPopup(Transform canvasRoot)
        {
            _recordPopup = CreateRect("RecordObtainedPopup", canvasRoot, Vector2.zero, Vector2.one);
            var blocker = _recordPopup.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.72f);

            var panel = CreateRect("Panel", _recordPopup.transform, new Vector2(0.30f, 0.23f), new Vector2(0.70f, 0.77f));
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.13f, 0.19f, 0.32f, 0.99f);

            var title = CreateText("Title", panel.transform, new Vector2(0.08f, 0.76f), new Vector2(0.92f, 0.92f));
            title.text = "Item obtained";
            title.fontSize = 42f;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(0.94f, 0.85f, 0.58f);

            var iconObject = CreateRect("RecordIcon", panel.transform, new Vector2(0.35f, 0.30f), new Vector2(0.65f, 0.72f));
            var icon = iconObject.AddComponent<Image>();
            icon.sprite = LoadSprite("Exhibitions/Icons/blues-16");
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var label = CreateText("RecordName", panel.transform, new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.30f));
            label.text = "Otowa Blues";
            label.fontSize = 31f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;

            var buttonObject = CreateRect("ConfirmButton", panel.transform, new Vector2(0.35f, 0.06f), new Vector2(0.65f, 0.16f));
            var buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.61f, 0.43f, 0.24f, 1f);
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(HandleRecordPopupConfirmed);

            var buttonText = CreateText("Label", buttonObject.transform, Vector2.zero, Vector2.one);
            buttonText.text = "Continue";
            buttonText.fontSize = 25f;
            buttonText.fontStyle = FontStyles.Bold;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;

            _recordPopup.SetActive(false);
        }

        private void AdvanceBeat()
        {
            _beatIndex++;
            if (_beatIndex >= _beats.Count)
            {
                StartCoroutine(LeaveScene());
                return;
            }

            var beat = _beats[_beatIndex];
            ApplyAudioCue(beat.Cue);
            _blackNarrationRoot.SetActive(false);
            _stripPlayer.SetVisible(false);

            switch (beat.Kind)
            {
                case BeatKind.BlackLine:
                    _blackNarrationRoot.SetActive(true);
                    _narrationSpeaker.text = beat.Speaker;
                    _narrationSpeaker.gameObject.SetActive(!string.IsNullOrWhiteSpace(beat.Speaker));
                    _narrationPlayer.Play(_narrationBody, beat.Text);
                    break;

                case BeatKind.StripLine:
                    _stripPlayer.SetVisible(true);
                    _stripPlayer.PlayLine(beat.Speaker, beat.Text, beat.Focus);
                    break;

                case BeatKind.RecordPopup:
                    ShowRecordPopup();
                    break;
            }
        }

        private bool CurrentBeatIsTyping()
        {
            if (_beatIndex < 0 || _beatIndex >= _beats.Count)
                return false;

            return _beats[_beatIndex].Kind == BeatKind.StripLine
                ? _stripPlayer.IsTyping
                : _narrationPlayer.IsTyping;
        }

        private void SkipCurrentTyping()
        {
            if (_beats[_beatIndex].Kind == BeatKind.StripLine)
                _stripPlayer.SkipTyping();
            else
                _narrationPlayer.Skip();
        }

        private void ShowRecordPopup()
        {
            GameAudioManager.Instance.PlaySfxOnce(AudioId.Jingle);
            GameAudioManager.Instance.StopBgm(0.35f);
            GameAudioManager.Instance.PlaySfxLoop(AudioId.ForestAtmosphere, fadeIn: 0.35f);

            if (!_recordGranted)
            {
                _recordGranted = true;
                if (InspirationManager.Instance != null)
                {
                    InspirationManager.Instance.CollectItem(16);
                    InspirationManager.Instance.Unlock(9, showJournalHint: false, playSfx: false);
                }
            }

            _recordPopup.SetActive(true);
        }

        private void HandleRecordPopupConfirmed()
        {
            _recordPopup.SetActive(false);
            AdvanceBeat();
        }

        private IEnumerator LeaveScene()
        {
            _transitioning = true;
            GameAudioManager.Instance.PlaySfxOnce(AudioId.SwitchClick);
            GameAudioManager.Instance.PlayBgm(AudioId.OtowaBlues, fadeIn: 0.8f);
            yield return FadeCanvas(1f, 0f, 0.8f);
            SceneManager.LoadScene(_nextSceneName);
        }

        private static void ApplyAudioCue(AudioCue cue)
        {
            switch (cue)
            {
                case AudioCue.Footsteps:
                    GameAudioManager.Instance.PlaySfxOnce(AudioId.LeatherFootsteps);
                    break;
                case AudioCue.SwitchToNightWalk:
                    GameAudioManager.Instance.StopSfxLoop(AudioId.ForestAtmosphere, 0.35f);
                    GameAudioManager.Instance.PlayBgm(AudioId.NightWalk, fadeIn: 0.65f);
                    break;
                case AudioCue.EnterOffice:
                    GameAudioManager.Instance.StopSfxLoop(AudioId.ForestAtmosphere, 0.25f);
                    GameAudioManager.Instance.PlaySfxOnce(AudioId.DoorOpen);
                    break;
            }
        }

        private IEnumerator FadeCanvas(float from, float to, float duration)
        {
            _canvasGroup.alpha = from;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _canvasGroup.alpha = to;
        }

        private static bool WasAdvancePressed()
        {
            var mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            var keyboard = Keyboard.current;
            var keyboardPressed = keyboard != null &&
                (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame);
            return mouseClicked || keyboardPressed;
        }

        private Sprite LoadSprite(string resourcePath)
        {
            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
                return sprite;

            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                Debug.LogWarning($"[Day3HikaruArrival] Missing sprite resource: {resourcePath}");
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

            Debug.LogWarning($"[Day3HikaruArrival] Missing sprite slice: {resourcePath}/{spriteName}");
            return fallback;
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
                return;

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        private TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var textObject = CreateRect(name, parent, anchorMin, anchorMax);
            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = _font;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        private static void UseFont(TMP_Text text, TMP_FontAsset font)
        {
            if (font != null)
                text.font = font;
        }

        private static GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
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
    }
}
