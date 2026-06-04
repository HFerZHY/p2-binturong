using System;
using System.Collections;
using System.Collections.Generic;
using Otowa.Audio;
using Otowa.IndoorDialogue;
using Otowa.Inquiry;
using Otowa.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Otowa.HotSpring
{
    /// <summary>
    /// Indoor visual-novel scene for Mizuki's Day 1 hot spring conversation.
    /// This scene owns its presentation and branches independently from the map
    /// dialogue player. Journal inquiry is the only shared interaction surface.
    /// </summary>
    public class HotSpringController : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string nextSceneName = "Day1World";
        [SerializeField] private float typewriterSpeed = 35f;
        [SerializeField] private float fadeDuration = 0.35f;

        [Header("Fonts")]
        [SerializeField] private TMP_FontAsset serifFont;

        private const float ACTIVE_ALPHA = 1f;
        private const float INACTIVE_ALPHA = 0.32f;

        private static readonly Color PanelBg = new(0.02f, 0.05f, 0.02f, 0.93f);
        private static readonly Color BodyColor = new(0.78f, 0.83f, 0.78f, 1f);
        private static readonly Color RinColor = new(0.88f, 0.76f, 0.62f, 1f);
        private static readonly Color MizukiColor = new(0.50f, 0.72f, 0.91f, 1f);
        private static readonly Color PromptColor = new(1f, 1f, 1f, 0.73f);

        private CanvasGroup _fade;
        private Image _rinImage;
        private Image _mizukiImage;
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

        private Day1InquiryProgress Progress => Day1InquiryProgress.Instance;

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
            _fade.alpha = 0f;
            var audio = GameAudioManager.Instance;
            audio.StopSfxLoop(AudioId.BluesBeat);
            audio.StopSfxLoop(AudioId.Wind);
            audio.PlayBgm(AudioId.HotSpring, fadeIn: 0.35f);
            StartCoroutine(StartAfterFade());
        }

        private IEnumerator StartAfterFade()
        {
            yield return FadeTo(1f);

            if (!Progress.IsNpcIntroduced(Day1InquiryNpc.Mizuki))
            {
                PlaySequence(BuildIntroduction(), CompleteIntroduction);
            }
            else
            {
                PlaySequence(
                    HasPendingMizukiConversation()
                        ? BuildReturnGreeting()
                        : BuildExhaustedReturn(),
                    HasPendingMizukiConversation() ? ShowChoices : Leave);
            }
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
            _speakerText.color = isRin ? RinColor : MizukiColor;
            _speakerText.alignment = TextAlignmentOptions.Left;
            SetAlpha(_rinImage, isRin ? ACTIVE_ALPHA : INACTIVE_ALPHA);
            SetAlpha(_mizukiImage, isRin ? INACTIVE_ALPHA : ACTIVE_ALPHA);
            _textPlayer.Play(_bodyText, beat.Text);
        }

        private void CompleteIntroduction()
        {
            Progress.MarkNpcIntroduced(Day1InquiryNpc.Mizuki);

            if (!Progress.HasReceivedAmulet)
            {
                Progress.ReceiveAmulet();
                InspirationManager.Instance.Unlock(13);
            }

            ShowChoices();
        }

        private void ShowChoices()
        {
            _sequenceActive = false;
            _choicesVisible = true;
            SetPrompt(false);
            _speakerText.text = string.Empty;
            _bodyText.text = string.Empty;
            SetAlpha(_rinImage, INACTIVE_ALPHA);
            SetAlpha(_mizukiImage, INACTIVE_ALPHA);

            ClearChoices();
            var choices = new List<(string label, Action action)>();
            if (!Progress.IsMizukiCityTopicComplete)
                choices.Add(("Talk about life in the big city", SelectCityTopic));
            if (!Progress.IsMizukiFestivalTopicComplete)
                choices.Add(("Ask about the Summer Festival", SelectFestivalTopic));
            if (Progress.AreMizukiTopicsComplete
                && Progress.HasPendingInquiry(Day1InquiryNpc.Mizuki))
            {
                choices.Add(("Inquire about an item's story", SelectInquiry));
            }
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

        private void SelectCityTopic()
        {
            PlaySequence(BuildCityTopic(), () =>
            {
                Progress.CompleteMizukiCityTopic();
                ShowChoices();
            });
        }

        private void SelectFestivalTopic()
        {
            PlaySequence(BuildFestivalTopic(), () =>
            {
                Progress.CompleteMizukiFestivalTopic();
                ShowChoices();
            });
        }

        private void SelectInquiry()
        {
            HideChoices();
            if (!InspirationManager.Instance.OpenItemInquiry(
                    Day1InquiryNpc.Mizuki,
                    HandleInquiryItemSelected,
                    ShowChoices))
            {
                ShowChoices();
            }
        }

        private void HandleInquiryItemSelected(int sortOrder)
        {
            if (sortOrder != 3)
            {
                Debug.LogWarning($"[HotSpringController] Unexpected Mizuki inquiry item: {sortOrder}.");
                ShowChoices();
                return;
            }

            PlaySequence(BuildStoneInquiry(), () =>
            {
                Progress.TryMarkAsked(Day1InquiryNpc.Mizuki, sortOrder);
                Leave();
            });
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
            yield return FadeTo(0f);
            GameAudioManager.Instance.StopBgm(0.25f);
            Progress.RequestDay1MapSpawn("HotSpring Entrance", new Vector3(0f, -2f, 0f));
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
                Rin("(There's a damp smell in the air... have I reached the hot spring?)"),
                Rin("(There's a girl up ahead... she's cleaning the stone Jizo statue.)"),
                Mizuki("...Good evening. Sorry, the hot spring's already closed for the night."),
                Rin("Ah, good evening. I'm Rin, the acting stationmaster who just arrived today."),
                Mizuki("Mm, Chief Junko told me. I'm Mizuki - I look after the hot spring."),
                Mizuki("The evening mountain wind is a bit chilly... Oh, here - this is for you."),
                Rin("Is this... an amulet?"),
                Mizuki("It's a blessing from Otowa. May the flowing water wash away your fatigue, and keep you safe and in good health."),
                Rin("Thank you, Mizuki."),
            };
        }

        private IReadOnlyList<Beat> BuildCityTopic()
        {
            return new[]
            {
                Mizuki("You're from the big city, aren't you, Rin. The outside world... what's it like?"),
                Mizuki("The city... the schools... is it bustling and lively every day?"),
                Rin("It's lively, sure. But after a while, it actually doesn't feel very good."),
                Mizuki("Not very good?"),
                Rin("Everyone's always in a rush, only caring about how much money they make, never about the people around them."),
                Rin("After staying there long enough, I felt like even breathing had become difficult."),
                Mizuki("Mm... that sounds... gray."),
                Rin("Gray?"),
                Mizuki("Yes. Like an ocean without starlight, or a forest hidden by mist. Dim and gray."),
                Rin("I like your metaphor. When I was in the city, everything around me really did feel gray."),
            };
        }

        private bool HasPendingMizukiConversation()
        {
            return !Progress.IsMizukiCityTopicComplete
                   || !Progress.IsMizukiFestivalTopicComplete
                   || Progress.HasPendingInquiry(Day1InquiryNpc.Mizuki);
        }

        private IReadOnlyList<Beat> BuildReturnGreeting()
        {
            return new[]
            {
                Mizuki("...Mm, Rin. Is there something else you'd like to ask?"),
            };
        }

        private IReadOnlyList<Beat> BuildExhaustedReturn()
        {
            return new[]
            {
                Mizuki("The night wind's turned cold, Rin. Don't catch a cold."),
            };
        }

        private IReadOnlyList<Beat> BuildFestivalTopic()
        {
            return new[]
            {
                Rin("I just heard everyone saying the Summer Festival is two days away. Is there anything you're looking forward to, Mizuki?"),
                Mizuki("Something I'm looking forward to..."),
                Mizuki("I have a very dear friend. She left here a few years ago to study in the big city."),
                Mizuki("During the Summer Festival, they return to Otowa, like birds on the night of a full moon."),
                Mizuki("I hope this year... I can see her again."),
                Rin("Since it's Otowa's most important festival, I'm sure she'll come back."),
                Mizuki("Mm..."),
            };
        }

        private IReadOnlyList<Beat> BuildStoneInquiry()
        {
            return new[]
            {
                Rin("Oh, Mizuki, I noticed some glittering stones beside the pool. They're black, but they catch the light in all sorts of colors."),
                Mizuki("Mm... these are wondrous stones. The hot spring carries their energy, too."),
                Rin("Energy?"),
                Mizuki("Yes. Grandpa Rintaro says the ores buried beneath the water can nourish the body. And if you grind them up, they can even be made into a blue pigment."),
                Rin("Grandpa Rintaro? Is he someone from the village?"),
                Mizuki("Yes, he knows a great deal about stones... and he's a very gentle man, just like my own grandfather."),
                Mizuki("The Jizo statue is all clean now.", () => InspirationManager.Instance.Unlock(14)),
                Mizuki("The birds are starting to head home. Good night to you, Rin. Be careful not to catch a cold."),
                Rin("I will. Good night, Mizuki."),
            };
        }

        private static Beat Rin(string text, Action onEntered = null)
        {
            return new Beat("Rin", text, onEntered);
        }

        private static Beat Mizuki(string text, Action onEntered = null)
        {
            return new Beat("Mizuki", text, onEntered);
        }

        private void BuildUI()
        {
            var canvasObject = new GameObject("HotSpringCanvas",
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
            background.sprite = LoadSprite("Map/spring-night", new Color32(0x12, 0x1d, 0x22, 0xFF));
            background.color = Color.white;
            background.preserveAspect = false;
            background.raycastTarget = false;

            _rinImage = BuildPortrait(canvasObject.transform, "RinSprite", "Characters/WorldSprite/rin_portrait",
                new Vector2(0.06f, 0.25f), new Vector2(0.45f, 0.98f), false);
            _mizukiImage = BuildPortrait(canvasObject.transform, "MizukiSprite", "Characters/WorldSprite/Mizuki_portrait",
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
                38f, MizukiColor, TextAlignmentOptions.Left,
                new Vector2(0.04f, 0.68f), new Vector2(0.81f, 0.96f));
            _speakerText.fontStyle = FontStyles.Bold;

            _bodyText = MakeText(panelObject.transform, "Body", string.Empty,
                34f, BodyColor, TextAlignmentOptions.Left,
                new Vector2(0.04f, 0.04f), new Vector2(0.81f, 0.65f));
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
            image.color = new Color(1f, 1f, 1f, INACTIVE_ALPHA);
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
