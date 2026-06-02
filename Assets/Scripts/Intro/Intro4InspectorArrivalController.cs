using System.Collections;
using Otowa.Audio;
using Otowa.Day3;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Otowa.Intro
{
    public class Intro4InspectorArrivalController : MonoBehaviour
    {
        private readonly struct Beat
        {
            public Beat(string speaker, string text, CinematicStripPortraitFocus focus)
            {
                Speaker = speaker;
                Text = text;
                Focus = focus;
            }

            public string Speaker { get; }
            public string Text { get; }
            public CinematicStripPortraitFocus Focus { get; }
        }

        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private float _charactersPerSecond = 38f;
        [SerializeField] private float _blackScreenDuration = 2f;
        [SerializeField] private float _fadeDuration = 0.8f;
        [SerializeField] private string _nextSceneName = "TutorialToRyotei";

        private static readonly Beat[] Beats =
        {
            Inspector("???", "...Excuse me."),
            Rin("Ah, hello!"),
            Inspector("???", "Are you an employee here? Where is Stationmaster Hikaru?"),
            Rin("I'm the newly arrived acting stationmaster, Rin. Mr. Hikaru has temporarily left, so I am in charge for the next few days."),
            Inspector("???", "Acting? The railway company hasn't received any notification of such a personnel change. Whatever, I am here for a follow-up review."),
            Inspector("Inspector", "Have there been any substantive changes in the village? A commercial plan, or new development projects."),
            Rin("(I haven't even been off the train for an hour. How would I know?)"),
            Rin("Mr. Hikaru is planning to transform this place into a museum to showcase Otowa's unique features. Look at these..."),
            Inspector("Inspector", "A museum?"),
            Inspector("Inspector", "I only see a floor full of garbage and extremely poor work efficiency."),
            Rin("These haven't been set up yet. With just a little bit of planning..."),
            Inspector("Inspector", "I do not need to hear unrealistic plans, Stationmaster Rin. The railway company only looks at data and results."),
            Inspector("Inspector", "It seems this place is just as worthless as it was during the last evaluation."),
            Rin("You've already reached a conclusion?"),
            Inspector("Inspector", "I reach conclusions based on evidence. I suggest you spend less time arguing and more time preparing something presentable."),
            Inspector("Inspector", "Let's hope that before I finish writing my report, you can present something a bit more convincing."),
            Inspector("Inspector", "Goodbye."),
            Rin("(He really just left...)"),
            Rin("(Wait - evaluation report. What happens if we fail?)"),
            Rin("(Hikaru, just how big of a mess have you dumped on me?)"),
        };

        private CanvasGroup _canvasGroup;
        private CinematicStripDialoguePlayer _stripPlayer;
        private int _beatIndex = -1;
        private bool _inputLocked = true;
        private bool _transitioning;

        private void Awake()
        {
            BuildInterface();
            StartCoroutine(BeginSequence());
        }

        private void Update()
        {
            if (_inputLocked || _transitioning || !WasAdvancePressed())
                return;

            if (_stripPlayer.IsTyping)
            {
                _stripPlayer.SkipTyping();
                return;
            }

            AdvanceBeat();
        }

        private IEnumerator BeginSequence()
        {
            _canvasGroup.alpha = 0f;
            GameAudioManager.Instance.StopBgm(0.25f);
            GameAudioManager.Instance.PlaySfxOnce(AudioId.KnockingDoor);
            yield return new WaitForSecondsRealtime(_blackScreenDuration);
            yield return FadeCanvas(0f, 1f, _fadeDuration);
            _inputLocked = false;
            AdvanceBeat();
        }

        private void BuildInterface()
        {
            var canvasObject = new GameObject(
                "Intro4InspectorArrivalCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup));

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _canvasGroup = canvasObject.GetComponent<CanvasGroup>();
            _stripPlayer = gameObject.AddComponent<CinematicStripDialoguePlayer>();
            _stripPlayer.Initialize(canvasObject.transform, _font, _charactersPerSecond);
            _stripPlayer.SetStripBackground(LoadSprite("Exhibitions/Icons/passenger-background"));
            _stripPlayer.SetPortraits(
                LoadSpriteSlice("Characters/WorldSprite/rin", "spritesheet_template_0"),
                null);
            _stripPlayer.SetCenteredFullBodyPortrait(
                LoadSprite("Characters/WorldSprite/Inspector_portrait"));
            _stripPlayer.SetVisible(true);
        }

        private void AdvanceBeat()
        {
            _beatIndex++;
            if (_beatIndex >= Beats.Length)
            {
                StartCoroutine(LeaveScene());
                return;
            }

            var beat = Beats[_beatIndex];
            _stripPlayer.PlayLine(beat.Speaker, beat.Text, beat.Focus);
        }

        private IEnumerator LeaveScene()
        {
            if (_transitioning)
                yield break;

            _transitioning = true;
            _inputLocked = true;
            yield return FadeCanvas(1f, 0f, _fadeDuration);
            SceneManager.LoadScene(_nextSceneName);
        }

        private IEnumerator FadeCanvas(float from, float to, float duration)
        {
            _canvasGroup.alpha = from;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _canvasGroup.alpha = to;
        }

        private static Sprite LoadSprite(string resourcePath)
        {
            var sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites.Length > 0)
                return sprites[0];

            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                Debug.LogWarning($"[Intro4InspectorArrival] Missing sprite resource: {resourcePath}");
                return null;
            }

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
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

            Debug.LogWarning($"[Intro4InspectorArrival] Missing sprite slice: {resourcePath}/{spriteName}");
            return fallback;
        }

        private static bool WasAdvancePressed()
        {
            var mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            var keyboard = Keyboard.current;
            var keyboardPressed = keyboard != null &&
                (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame);
            return mouseClicked || keyboardPressed;
        }

        private static Beat Rin(string text)
        {
            return new Beat("Rin", text, CinematicStripPortraitFocus.Left);
        }

        private static Beat Inspector(string speaker, string text)
        {
            return new Beat(speaker, text, CinematicStripPortraitFocus.Right);
        }
    }
}
