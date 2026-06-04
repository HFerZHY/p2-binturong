using System;
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

namespace Otowa.Inquiry
{
    /// <summary>
    /// Day 2 ryotei visual-novel scene. This owns Jiro's indoor presentation
    /// while sharing only persistent inquiry progress and Journal selection.
    /// </summary>
    public class Day2RyoteiController : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string nextSceneName = "Day2World";
        [SerializeField] private float typewriterSpeed = 35f;
        [SerializeField] private float fadeDuration = 0.35f;

        [Header("Fonts")]
        [SerializeField] private TMP_FontAsset serifFont;

        private const float ActiveAlpha = 1f;
        private const float InactiveAlpha = 0.32f;
        private static readonly Color PanelBg = new(0.02f, 0.05f, 0.02f, 0.93f);
        private static readonly Color BodyColor = new(0.78f, 0.83f, 0.78f, 1f);
        private static readonly Color RinColor = new(0.88f, 0.76f, 0.62f, 1f);
        private static readonly Color JiroColor = new(0.70f, 0.70f, 0.77f, 1f);
        private static readonly Color PromptColor = new(1f, 1f, 1f, 0.73f);

        private CanvasGroup _fade;
        private Image _rinImage;
        private Image _jiroImage;
        private TMP_Text _speakerText;
        private TMP_Text _bodyText;
        private TMP_Text _promptText;
        private GameObject _choicesContainer;
        private IndoorDialogueTextPlayer _textPlayer;
        private IReadOnlyList<Beat> _activeBeats;
        private Action _onSequenceComplete;
        private int _beatIndex;
        private bool _choicesVisible;
        private bool _sequenceActive;
        private bool _inputLock;

        private Day2InquiryProgress Progress => Day2InquiryProgress.Instance;

        private void Awake()
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            serifFont = RuntimeFontLibrary.BreeSerifRegularOr(serifFont);
            BuildUI();
            _textPlayer = gameObject.AddComponent<IndoorDialogueTextPlayer>();
            _textPlayer.Initialize(_promptText, typewriterSpeed);
            EnsureEventSystem();
            InspirationManager.Instance.SetFont(serifFont);
        }

        private void Start()
        {
            ConfigureAudioForVisit();
            _fade.alpha = 0f;
            StartCoroutine(StartAfterFade());
        }

        private void OnDisable()
        {
            GameAudioManager.Instance.StopSfxLoop(AudioId.Chopping);
        }

        private void ConfigureAudioForVisit()
        {
            GameAudioManager.Instance.StopBgm();

            if (!Progress.IsNpcIntroduced(Day2InquiryNpc.Jiro))
            {
                GameAudioManager.Instance.PlaySfxLoop(AudioId.Chopping, fadeIn: 0.25f);
                GameAudioManager.Instance.PlayBgm(AudioId.OtowaBlues, fadeIn: 0.25f);
                return;
            }

            if (HasPendingJiroConversation())
                GameAudioManager.Instance.PlaySfxLoop(AudioId.Chopping, fadeIn: 0.25f);
            else
                GameAudioManager.Instance.StopSfxLoop(AudioId.Chopping);
        }

        private static void HandleJiroStopsMusic()
        {
            GameAudioManager.Instance.PlaySfxOnce(AudioId.SwitchClick);
            GameAudioManager.Instance.StopBgm(0.2f);
        }

        private static void StopChopping()
        {
            GameAudioManager.Instance.StopSfxLoop(AudioId.Chopping, 0.15f);
        }

        private static void ResumeChopping()
        {
            GameAudioManager.Instance.PlaySfxLoop(AudioId.Chopping, fadeIn: 0.15f);
        }

        private IEnumerator StartAfterFade()
        {
            yield return FadeTo(1f);

            if (!Progress.IsNpcIntroduced(Day2InquiryNpc.Jiro))
            {
                PlaySequence(BuildIntroduction(), CompleteIntroduction);
                yield break;
            }

            bool hasPendingConversation = HasPendingJiroConversation();
            PlaySequence(
                hasPendingConversation ? BuildReturnGreeting() : BuildExhaustedReturn(),
                hasPendingConversation ? ShowChoices : Leave);
        }

        private void Update()
        {
            if (_inputLock || _choicesVisible || InspirationManager.IsJournalOpen || !_sequenceActive)
                return;

            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            bool mouseClicked = mouse != null && mouse.leftButton.wasPressedThisFrame;
            bool keyboardPressed = keyboard != null
                                   && (keyboard.spaceKey.wasPressedThisFrame
                                       || keyboard.enterKey.wasPressedThisFrame);

            if (!mouseClicked && !keyboardPressed) return;
            if (mouseClicked && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (_textPlayer.IsTyping)
            {
                _textPlayer.Skip();
                return;
            }

            AdvanceSequence();
        }

        private void PlaySequence(IReadOnlyList<Beat> beats, Action onComplete)
        {
            HideChoices();
            _activeBeats = beats;
            _onSequenceComplete = onComplete;
            _beatIndex = 0;
            _sequenceActive = true;
            ShowCurrentBeat();
        }

        private void AdvanceSequence()
        {
            _beatIndex++;
            if (_activeBeats != null && _beatIndex < _activeBeats.Count)
            {
                ShowCurrentBeat();
                return;
            }

            _sequenceActive = false;
            SetPrompt(false);
            var onComplete = _onSequenceComplete;
            _onSequenceComplete = null;
            onComplete?.Invoke();
        }

        private void ShowCurrentBeat()
        {
            var beat = _activeBeats[_beatIndex];
            beat.OnEntered?.Invoke();

            bool isRin = beat.Speaker == "Rin";
            _speakerText.text = beat.Speaker;
            _speakerText.color = isRin ? RinColor : JiroColor;
            _speakerText.alignment = TextAlignmentOptions.Left;
            SetAlpha(_rinImage, isRin ? ActiveAlpha : InactiveAlpha);
            SetAlpha(_jiroImage, isRin ? InactiveAlpha : ActiveAlpha);
            _textPlayer.Play(_bodyText, beat.Text);
        }

        private void CompleteIntroduction()
        {
            Progress.MarkNpcIntroduced(Day2InquiryNpc.Jiro);
            ShowChoices();
        }

        private void ShowChoices()
        {
            _sequenceActive = false;
            _choicesVisible = true;
            SetPrompt(false);
            _speakerText.text = string.Empty;
            _bodyText.text = string.Empty;
            SetAlpha(_rinImage, InactiveAlpha);
            SetAlpha(_jiroImage, InactiveAlpha);

            ClearChoices();
            var choices = new List<(string label, Action action)>();
            if (!Progress.IsJiroStationTopicComplete)
                choices.Add(("About the station's closure", SelectStationTopic));
            if (!Progress.IsJiroFestivalTopicComplete)
                choices.Add(("About the Summer Festival", SelectFestivalTopic));
            if (Progress.AreJiroTopicsComplete && Progress.HasPendingInquiry(Day2InquiryNpc.Jiro))
                choices.Add(("Inquire about an item's story", SelectInquiry));
            choices.Add(("Leave", Leave));

            for (int i = 0; i < choices.Count; i++)
                BuildChoiceButton(choices[i].label, choices[i].action, i);

            _choicesContainer.SetActive(true);
        }

        private void HideChoices()
        {
            _choicesVisible = false;
            _choicesContainer.SetActive(false);
        }

        private void SelectStationTopic()
        {
            PlaySequence(BuildStationTopic(), () =>
            {
                Progress.CompleteJiroStationTopic();
                ShowChoices();
            });
        }

        private void SelectFestivalTopic()
        {
            PlaySequence(BuildFestivalTopic(), () =>
            {
                Progress.CompleteJiroFestivalTopic();
                ResumeChopping();
                ShowChoices();
            });
        }

        private void SelectInquiry()
        {
            HideChoices();
            if (!InspirationManager.Instance.OpenItemInquiry(
                    Day2InquiryNpc.Jiro,
                    HandleInquiryItemSelected,
                    ShowChoices))
            {
                ShowChoices();
            }
        }

        private void HandleInquiryItemSelected(int sortOrder)
        {
            if (sortOrder != 5)
            {
                Debug.LogWarning($"[Day2RyoteiController] Unexpected Jiro inquiry item: {sortOrder}.");
                ShowChoices();
                return;
            }

            PlaySequence(BuildDangoInquiry(), () =>
            {
                Progress.TryMarkAsked(Day2InquiryNpc.Jiro, sortOrder);
                Leave();
            });
        }

        private bool HasPendingJiroConversation()
        {
            return !Progress.IsJiroStationTopicComplete
                   || !Progress.IsJiroFestivalTopicComplete
                   || Progress.HasPendingInquiry(Day2InquiryNpc.Jiro);
        }

        private void Leave()
        {
            if (_inputLock) return;
            StartCoroutine(FadeAndLoad());
        }

        private IEnumerator FadeAndLoad()
        {
            _inputLock = true;
            HideChoices();
            GameAudioManager.Instance.StopSfxLoop(AudioId.Chopping, 0.25f);
            GameAudioManager.Instance.StopBgm(0.25f);
            yield return FadeTo(0f);
            Progress.RequestDay2MapSpawn("Day2 Ryotei Entrance", new Vector3(0f, -2f, 0f));
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

        private IReadOnlyList<Beat> BuildIntroduction()
        {
            return new[]
            {
                Rin("(What's Mr. Jiro so busy making...)"),
                Rin("(Ah, the moment he saw me come in, he shut the music right off.)",
                    HandleJiroStopsMusic),
                Rin("Good afternoon. Sorry to intrude, Mr. Jiro."),
                Jiro("...The acting stationmaster, is it? The ryotei's closed today."),
            };
        }

        private IReadOnlyList<Beat> BuildReturnGreeting()
        {
            return new[] { Jiro("...What is it now?") };
        }

        private IReadOnlyList<Beat> BuildExhaustedReturn()
        {
            return new[] { Jiro("...Hmph. The ryotei's closed.") };
        }

        private IReadOnlyList<Beat> BuildStationTopic()
        {
            return new[]
            {
                Rin("Tomorrow is the deadline for the evaluation. If we can't satisfy the Inspector, the trains might stop running for good."),
                Jiro("Let them stop, then. I couldn't care less."),
                Jiro("If anything, no more trains would mean some peace and quiet. Otowa was never meant to cater to those restless outsiders anyway."),
            };
        }

        private IReadOnlyList<Beat> BuildFestivalTopic()
        {
            return new[]
            {
                Rin("Tomorrow's the Summer Festival. Is there anyone you're hoping to see at the festival, Mr. Jiro?"),
                Jiro("...No."),
                Jiro("I never cared for festivals to begin with. Too noisy. They set my teeth on edge.",
                    StopChopping),
                Jiro("And even if those young people come back, they wouldn't know how to appreciate traditional cooking. All they chase after is flashy nonsense."),
            };
        }

        private IReadOnlyList<Beat> BuildDangoInquiry()
        {
            return new[]
            {
                Rin("(Pink, white, green... that shape is...)"),
                Rin("Mr. Jiro, were you just making tri-colored dango?"),
                Jiro("...!"),
                Jiro("It's nothing more than an idle pastime."),
                Rin("(Mr. Jiro quickly covered the dango on the board with a white cloth, then turned and walked away.)",
                    () =>
                    {
                        StopChopping();
                        InspirationManager.Instance.Unlock(15);
                    }),
            };
        }

        private static Beat Rin(string text, Action onEntered = null)
        {
            return new Beat("Rin", text, onEntered);
        }

        private static Beat Jiro(string text, Action onEntered = null)
        {
            return new Beat("Jiro", text, onEntered);
        }

        private void BuildUI()
        {
            var canvasObject = new GameObject("Day2RyoteiCanvas",
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

            var backgroundObject = MakeRect(canvasObject.transform, "Background", Vector2.zero, Vector2.one);
            var background = backgroundObject.AddComponent<Image>();
            background.sprite = LoadSprite("Map/ryotei-afternoon", new Color32(0x4a, 0x35, 0x25, 0xFF));
            background.color = Color.white;
            background.preserveAspect = false;
            background.raycastTarget = false;

            _rinImage = BuildPortrait(canvasObject.transform, "RinSprite", "Characters/WorldSprite/rin_portrait",
                new Vector2(0.06f, 0.25f), new Vector2(0.45f, 0.98f), false);
            _jiroImage = BuildPortrait(canvasObject.transform, "JiroSprite", "Characters/WorldSprite/Jiro_portrait",
                new Vector2(0.55f, 0.25f), new Vector2(0.94f, 0.98f), true);

            BuildDialoguePanel(canvasObject.transform);
            _choicesContainer = MakeRect(canvasObject.transform, "Choices",
                new Vector2(0.25f, 0.32f), new Vector2(0.75f, 0.72f));
            IndoorDialogueChoiceStyle.ConfigureContainer(_choicesContainer);
            _choicesContainer.SetActive(false);

            _promptText = MakeText(canvasObject.transform, "Prompt", "Click to continue  v",
                22f, PromptColor, TextAlignmentOptions.Right,
                new Vector2(0.60f, 0.02f), new Vector2(0.97f, 0.08f));
            _promptText.gameObject.SetActive(false);
        }

        private void BuildDialoguePanel(Transform canvas)
        {
            var panelObject = MakeRect(canvas, "DialoguePanel", Vector2.zero, new Vector2(1f, 0.28f));
            var panel = panelObject.AddComponent<Image>();
            panel.color = PanelBg;
            panel.raycastTarget = false;

            _speakerText = MakeText(panelObject.transform, "Speaker", string.Empty,
                38f, JiroColor, TextAlignmentOptions.Left,
                new Vector2(0.04f, 0.68f), new Vector2(0.96f, 0.96f));
            _speakerText.fontStyle = FontStyles.Bold;

            _bodyText = MakeText(panelObject.transform, "Body", string.Empty,
                34f, BodyColor, TextAlignmentOptions.Left,
                new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.65f));
            _bodyText.lineSpacing = 6f;
        }

        private void ClearChoices()
        {
            foreach (Transform child in _choicesContainer.transform)
                Destroy(child.gameObject);
        }

        private void BuildChoiceButton(string label, Action action, int index)
        {
            IndoorDialogueChoiceStyle.AddButton(
                _choicesContainer.transform, $"Choice_{index + 1}", label, serifFont, action);
        }

        private Image BuildPortrait(Transform parent, string name, string resourcePath,
                                    Vector2 anchorMin, Vector2 anchorMax, bool flip)
        {
            var portraitObject = MakeRect(parent, name, anchorMin, anchorMax);
            if (flip)
                portraitObject.transform.localScale = new Vector3(-1f, 1f, 1f);

            var image = portraitObject.AddComponent<Image>();
            image.sprite = LoadSprite(resourcePath, new Color(0.3f, 0.3f, 0.35f, 1f));
            image.color = new Color(1f, 1f, 1f, InactiveAlpha);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static Sprite LoadSprite(string resourcePath, Color fallbackColor)
        {
            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null) return sprite;

            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                texture = new Texture2D(1, 1);
                texture.SetPixel(0, 0, fallbackColor);
                texture.Apply();
            }

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f);
        }

        private TMP_Text MakeText(Transform parent, string name, string value, float size,
                                  Color color, TextAlignmentOptions alignment,
                                  Vector2 anchorMin, Vector2 anchorMax)
        {
            var textObject = MakeRect(parent, name, anchorMin, anchorMax);
            var text = textObject.AddComponent<TextMeshProUGUI>();
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

        private void SetPrompt(bool visible)
        {
            _textPlayer.SetPromptVisible(visible);
        }

        private static void SetAlpha(Image image, float alpha)
        {
            var color = image.color;
            color.a = alpha;
            image.color = color;
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        private readonly struct Beat
        {
            public Beat(string speaker, string text, Action onEntered = null)
            {
                Speaker = speaker;
                Text = text;
                OnEntered = onEntered;
            }

            public string Speaker { get; }
            public string Text { get; }
            public Action OnEntered { get; }
        }
    }
}
