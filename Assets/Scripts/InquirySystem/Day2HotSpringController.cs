using System;
using System.Collections;
using System.Collections.Generic;
using Otowa.Audio;
using Otowa.IndoorDialogue;
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
    /// Day 2 hot spring visual-novel scene for Mizuki. The scene owns its
    /// presentation while sharing only persistent inquiry progress and Journal
    /// item selection with the map.
    /// </summary>
    public class Day2HotSpringController : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string nextSceneName = "Day2World";
        [SerializeField] private float typewriterSpeed = 35f;
        [SerializeField] private float fadeDuration = 0.35f;

        [Header("Fonts")]
        [SerializeField] private TMP_FontAsset serifFont;

        private const string HotSpringTheme = "The Mountain Springs: A Soak Beneath the Milky Way";
        private const float ActiveAlpha = 1f;
        private const float InactiveAlpha = 0.32f;

        private static readonly Color PanelBg = new(0.02f, 0.05f, 0.02f, 0.93f);
        private static readonly Color ChoiceBg = new(0.02f, 0.05f, 0.02f, 0.86f);
        private static readonly Color BodyColor = new(0.78f, 0.83f, 0.78f, 1f);
        private static readonly Color RinColor = new(0.88f, 0.76f, 0.62f, 1f);
        private static readonly Color MizukiColor = new(0.50f, 0.72f, 0.91f, 1f);
        private static readonly Color PromptColor = new(1f, 1f, 1f, 0.73f);

        private static readonly string[] PaintingReflectionLines =
        {
            "Beneath a deep blue night sky, a bird in flight over the sea.",
            "Deep blue, indigo, a pale blue almost translucent...",
            "In all this blue, you find yourself remembering last night's dream.",
        };

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

        private GameObject _paintingRevealObject;
        private GameObject _paintingRevealWindow;
        private CanvasGroup _paintingRevealCanvasGroup;
        private Button _paintingRevealButton;
        private GameObject _paintingReflectionObject;
        private TMP_Text _paintingReflectionBody;
        private TMP_Text _paintingReflectionPrompt;
        private IndoorDialogueTextPlayer _paintingReflectionTextPlayer;
        private int _paintingReflectionIndex;
        private bool _paintingRevealVisible;
        private bool _paintingReflectionActive;

        private Day2InquiryProgress Progress => Day2InquiryProgress.Instance;

        private void Awake()
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            BuildUI();
            _textPlayer = gameObject.AddComponent<IndoorDialogueTextPlayer>();
            _textPlayer.Initialize(_promptText, typewriterSpeed);
            _paintingReflectionTextPlayer = gameObject.AddComponent<IndoorDialogueTextPlayer>();
            _paintingReflectionTextPlayer.Initialize(_paintingReflectionPrompt, typewriterSpeed);
            EnsureEventSystem();
            InspirationManager.Instance.SetFont(serifFont);
        }

        private void Start()
        {
            GameAudioManager.Instance.PlayBgm(AudioId.HotSpring, fadeIn: 0.35f);
            _fade.alpha = 0f;
            StartCoroutine(StartAfterFade());
        }

        private IEnumerator StartAfterFade()
        {
            yield return FadeTo(1f);

            if (!Progress.IsNpcIntroduced(Day2InquiryNpc.Mizuki))
            {
                Progress.MarkNpcIntroduced(Day2InquiryNpc.Mizuki);
                PlaySequence(BuildIntroduction(), ShowChoices);
                yield break;
            }

            bool hasPendingConversation = HasPendingMizukiConversation();
            PlaySequence(
                hasPendingConversation ? BuildReturnGreeting() : BuildExhaustedReturn(),
                hasPendingConversation ? ShowChoices : Leave);
        }

        private void Update()
        {
            if (_inputLock || _choicesVisible || InspirationManager.IsJournalOpen
                || (!_sequenceActive && !_paintingReflectionActive))
                return;

            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            bool mouseClicked = mouse != null && mouse.leftButton.wasPressedThisFrame;
            bool keyboardPressed = keyboard != null
                                   && (keyboard.spaceKey.wasPressedThisFrame
                                       || keyboard.enterKey.wasPressedThisFrame);

            if (!mouseClicked && !keyboardPressed) return;
            if (mouseClicked && !_paintingReflectionActive
                && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var activeTextPlayer = _paintingReflectionActive
                ? _paintingReflectionTextPlayer
                : _textPlayer;
            if (activeTextPlayer.IsTyping)
            {
                activeTextPlayer.Skip();
                return;
            }

            if (_paintingReflectionActive)
                AdvancePaintingReflection();
            else
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
            _speakerText.alignment = isRin ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
            SetAlpha(_rinImage, isRin ? ActiveAlpha : InactiveAlpha);
            SetAlpha(_mizukiImage, isRin ? InactiveAlpha : ActiveAlpha);
            _textPlayer.Play(_bodyText, beat.Text);
        }

        private void ShowChoices()
        {
            _sequenceActive = false;
            _choicesVisible = true;
            SetPrompt(false);
            _speakerText.text = string.Empty;
            _bodyText.text = string.Empty;
            SetAlpha(_rinImage, InactiveAlpha);
            SetAlpha(_mizukiImage, InactiveAlpha);

            ClearChoices();
            var choices = new List<(string label, Action action)>();
            if (!Progress.IsMizukiFestivalTopicComplete)
                choices.Add(("Ask about the Summer Festival", SelectFestivalTopic));
            if (Progress.HasPendingInquiry(Day2InquiryNpc.Mizuki))
                choices.Add(("Inquire about an item's story", SelectInquiry));
            choices.Add(("Leave", Leave));

            for (int i = 0; i < choices.Count; i++)
                BuildChoiceButton(choices[i].label, choices[i].action, i, choices.Count);

            _choicesContainer.SetActive(true);
        }

        private void HideChoices()
        {
            _choicesVisible = false;
            _choicesContainer.SetActive(false);
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
                    Day2InquiryNpc.Mizuki,
                    HandleInquiryItemSelected,
                    ShowChoices))
            {
                ShowChoices();
            }
        }

        private void HandleInquiryItemSelected(int sortOrder)
        {
            switch (sortOrder)
            {
                case 5:
                    PlaySequence(BuildDangoInquiry(), ShowChoices);
                    break;
                case 13:
                    PlaySequence(BuildOctopusPotInquiry(), ShowChoices);
                    break;
                case 15:
                    PlaySequence(BuildPaintingRequest(), BeginPaintingReveal);
                    break;
                default:
                    Debug.LogWarning(
                        $"[Day2HotSpringController] Unexpected Mizuki inquiry item: {sortOrder}.");
                    ShowChoices();
                    break;
            }
        }

        private bool HasPendingMizukiConversation()
        {
            return !Progress.IsMizukiFestivalTopicComplete
                   || Progress.HasPendingInquiry(Day2InquiryNpc.Mizuki);
        }

        private void BeginPaintingReveal()
        {
            StartCoroutine(ShowPaintingReveal());
        }

        private IEnumerator ShowPaintingReveal()
        {
            _inputLock = true;
            _paintingRevealVisible = true;
            _paintingRevealObject.SetActive(true);
            _paintingRevealWindow.SetActive(false);
            _paintingRevealCanvasGroup.alpha = 0f;
            _paintingRevealCanvasGroup.blocksRaycasts = true;
            _paintingRevealCanvasGroup.interactable = true;
            yield return FadeCanvasGroup(_paintingRevealCanvasGroup, 1f);

            Progress.ReceivePainting();
            InspirationManager.Instance.Unlock(4);
            _paintingRevealWindow.SetActive(true);
            _paintingRevealButton.interactable = true;
        }

        private void DismissPaintingReveal()
        {
            if (!_paintingRevealVisible || !_paintingRevealButton.interactable)
                return;

            _paintingRevealButton.interactable = false;
            _paintingRevealWindow.SetActive(false);
            _paintingReflectionIndex = 0;
            _paintingReflectionActive = true;
            _paintingReflectionObject.SetActive(true);
            _inputLock = false;
            ShowPaintingReflectionLine();
        }

        private void AdvancePaintingReflection()
        {
            _paintingReflectionIndex++;
            if (_paintingReflectionIndex < PaintingReflectionLines.Length)
            {
                ShowPaintingReflectionLine();
                return;
            }

            StartCoroutine(HidePaintingReveal());
        }

        private void ShowPaintingReflectionLine()
        {
            _paintingReflectionTextPlayer.Play(
                _paintingReflectionBody,
                PaintingReflectionLines[_paintingReflectionIndex]);
        }

        private IEnumerator HidePaintingReveal()
        {
            _inputLock = true;
            _paintingReflectionActive = false;
            _paintingReflectionTextPlayer.Cancel();
            _paintingReflectionPrompt.gameObject.SetActive(false);
            _paintingReflectionObject.SetActive(false);
            _paintingRevealWindow.SetActive(false);
            yield return FadeCanvasGroup(_paintingRevealCanvasGroup, 0f);

            _paintingRevealCanvasGroup.blocksRaycasts = false;
            _paintingRevealCanvasGroup.interactable = false;
            _paintingRevealObject.SetActive(false);
            _paintingRevealVisible = false;
            _inputLock = false;
            PlaySequence(BuildPaintingClosing(), CompletePaintingStory);
        }

        private void CompletePaintingStory()
        {
            InspirationManager.Instance.CompleteTheme(HotSpringTheme);
            ShowChoices();
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
            GameAudioManager.Instance.StopBgm(0.25f);
            yield return FadeTo(0f);
            Progress.RequestDay2MapSpawn("Day2 HotSpring Entrance", new Vector3(0f, -2f, 0f));
            SceneManager.LoadScene(nextSceneName);
        }

        private IEnumerator FadeTo(float target)
        {
            yield return FadeCanvasGroup(_fade, target);
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float target)
        {
            float start = canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            canvasGroup.alpha = target;
        }

        private IReadOnlyList<Beat> BuildIntroduction()
        {
            return new[]
            {
                Rin("(I've come to the hot spring again... Mizuki's over there.)"),
                Rin("(Her head is bowed, and it looks like she's holding a brush... Is she painting?)"),
                Mizuki("...Ah, it's Rin."),
                Mizuki("Good afternoon. The station was lively today, wasn't it."),
                Rin("It was alright. Mizuki, are you painting?"),
                Mizuki("...Just doodling. Nothing worth looking at."),
            };
        }

        private IReadOnlyList<Beat> BuildReturnGreeting()
        {
            return new[] { Mizuki("...Mm, Rin. Is there something else you'd like to ask?") };
        }

        private IReadOnlyList<Beat> BuildExhaustedReturn()
        {
            return new[] { Mizuki("...The birds are heading home. You should rest soon too, Rin.") };
        }

        private IReadOnlyList<Beat> BuildFestivalTopic()
        {
            return new[]
            {
                Rin("Tomorrow's the Summer Festival. Are you still waiting for that friend of yours, Mizuki?"),
                Mizuki("Mm... I'm waiting for her."),
                Mizuki("A few years back, she still sent me postcards from the city. Thin little things, the backs filled with writing."),
                Mizuki("But these last two years, I can't reach her at all. No postcards, and her phone number won't connect either. I suppose... she's probably just too busy."),
                Rin("Studying in the city really is hard. Everyone gets pushed forward, and little by little they lose track of what they left behind."),
                Mizuki("Is that so..."),
                Mizuki("In her postcards she said that school in the city was tough, but fun too. That the nights there sparkle."),
                Mizuki("...No matter what, as long as she's healthy and safe over there, my heart's at ease."),
            };
        }

        private IReadOnlyList<Beat> BuildDangoInquiry()
        {
            return new[]
            {
                Rin("These dango... I saw some in the stationmaster's office, too."),
                Mizuki("Mm. Pink, white, green. When you see those three colors, you know the festival is near."),
                Rin("Does everyone only eat dango at the festival?"),
                Mizuki("Usually... Hachi would bring me some dango, too."),
                Rin("Hachi?"),
                Mizuki("Mm. Mr. Jiro's son. Tri-colored dango were his favorite."),
                Mizuki("But these past few years, there've been none to eat. He's gone now."),
                Rin("Gone? Don't tell me he..."),
                Mizuki("He left Otowa. Hachi told me over the phone that he's making music now."),
                Mizuki("The blues, or something. I don't really get it, but it sounds... like the evening sky, I think."),
                Mizuki("Look, right now, even. The sky is full of that blue."),
            };
        }

        private IReadOnlyList<Beat> BuildOctopusPotInquiry()
        {
            return new[]
            {
                Rin("This pot, I saw one exactly like it in the stationmaster's office."),
                Mizuki("\"Octopus traps, fleeting dreams under the summer moon.\""),
                Rin("...?"),
                Mizuki("This pot is for catching octopus."),
                Mizuki("Octopuses love narrow, dark places, so fishermen sink the pots to the seabed. The octopus mistakes it for home and crawls inside."),
                Mizuki("...But there's no octopus left in this pot now."),
                Rin("Otowa is by the sea, too."),
                Mizuki("Mm... though more than the sea, I prefer the hot spring.",
                    () => InspirationManager.Instance.Unlock(6)),
            };
        }

        private IReadOnlyList<Beat> BuildPaintingRequest()
        {
            return new[]
            {
                Rin("Mizuki, the truth is, I'm still stuck on the station's exhibition."),
                Rin("I want to find some new exhibits, something that lets people passing through feel the vibe of this village."),
                Mizuki("New exhibits..."),
                Mizuki("Then... how about the tri-colored dango? Or the octopus pot. They're both things of Otowa."),
                Rin("Mm... but those are already in the exhibition."),
                Mizuki("...I see."),
                Mizuki("Rin...", () => GameAudioManager.Instance.PlaySfxOnce(AudioId.RunningWater)),
                Mizuki("Tell me... what kind of Otowa would people in the city want to see?"),
                Rin("Mm... I think maybe everyone holds their own Otowa in their heart."),
                Rin("There's no single answer. As long as it shows a true voice, it's bound to strike a chord."),
                Mizuki("..."),
                Mizuki("Then... you can use this."),
            };
        }

        private IReadOnlyList<Beat> BuildPaintingClosing()
        {
            return new[]
            {
                Mizuki("In my heart, this is the color of Otowa."),
                Rin("Thank you, Mizuki..."),
                Mizuki("I'm counting on you, Rin."),
                Mizuki("Tomorrow... is sure to be a beautiful day."),
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
            var canvasObject = new GameObject("Day2HotSpringCanvas",
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
            background.sprite = LoadSprite("Map/spring-afternoon", new Color32(0x7c, 0x9b, 0x91, 0xFF));
            background.color = Color.white;
            background.preserveAspect = false;
            background.raycastTarget = false;

            _rinImage = BuildPortrait(canvasObject.transform, "RinSprite", "Characters/WorldSprite/rin_portrait",
                new Vector2(0.06f, 0.25f), new Vector2(0.45f, 0.98f), false);
            _mizukiImage = BuildPortrait(canvasObject.transform, "MizukiSprite", "Characters/WorldSprite/Mizuki_portrait",
                new Vector2(0.55f, 0.25f), new Vector2(0.94f, 0.98f), true);

            BuildDialoguePanel(canvasObject.transform);
            _choicesContainer = MakeRect(canvasObject.transform, "Choices",
                new Vector2(0.25f, 0.32f), new Vector2(0.75f, 0.68f));
            _choicesContainer.SetActive(false);

            _promptText = MakeText(canvasObject.transform, "Prompt", "Click to continue  v",
                22f, PromptColor, TextAlignmentOptions.Right,
                new Vector2(0.60f, 0.02f), new Vector2(0.97f, 0.08f));
            _promptText.gameObject.SetActive(false);

            BuildPaintingReveal(canvasObject.transform);
        }

        private void BuildDialoguePanel(Transform canvas)
        {
            var panelObject = MakeRect(canvas, "DialoguePanel", Vector2.zero, new Vector2(1f, 0.28f));
            var panel = panelObject.AddComponent<Image>();
            panel.color = PanelBg;
            panel.raycastTarget = false;

            _speakerText = MakeText(panelObject.transform, "Speaker", string.Empty,
                28f, MizukiColor, TextAlignmentOptions.Right,
                new Vector2(0.04f, 0.68f), new Vector2(0.96f, 0.96f));
            _speakerText.fontStyle = FontStyles.Bold;

            _bodyText = MakeText(panelObject.transform, "Body", string.Empty,
                27f, BodyColor, TextAlignmentOptions.Left,
                new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.65f));
            _bodyText.lineSpacing = 6f;
        }

        private void BuildPaintingReveal(Transform canvas)
        {
            _paintingRevealObject = MakeRect(canvas, "PaintingReveal", Vector2.zero, Vector2.one);
            var overlay = _paintingRevealObject.AddComponent<Image>();
            overlay.color = new Color(0.025f, 0.075f, 0.16f, 1f);

            _paintingRevealCanvasGroup = _paintingRevealObject.AddComponent<CanvasGroup>();
            _paintingRevealCanvasGroup.alpha = 0f;
            _paintingRevealCanvasGroup.blocksRaycasts = false;
            _paintingRevealCanvasGroup.interactable = false;

            _paintingRevealWindow = MakeRect(_paintingRevealObject.transform, "Window",
                new Vector2(0.18f, 0.23f), new Vector2(0.82f, 0.77f));
            var window = _paintingRevealWindow.AddComponent<Image>();
            window.color = new Color(0.26f, 0.16f, 0.11f, 0.96f);

            var title = MakeText(_paintingRevealWindow.transform, "Title",
                "Item Obtained - Mizuki's Painting", 42f,
                new Color(0.98f, 0.86f, 0.62f, 1f), TextAlignmentOptions.Center,
                new Vector2(0.07f, 0.77f), new Vector2(0.93f, 0.94f));
            title.fontStyle = FontStyles.Bold;

            var iconObject = MakeRect(_paintingRevealWindow.transform, "PaintingIcon",
                new Vector2(0.16f, 0.20f), new Vector2(0.84f, 0.74f));
            var icon = iconObject.AddComponent<Image>();
            icon.sprite = LoadSprite(
                "Exhibitions/Icons/painting-15",
                new Color(0.30f, 0.38f, 0.58f, 1f));
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var buttonObject = MakeRect(_paintingRevealWindow.transform, "Continue",
                new Vector2(0.36f, 0.04f), new Vector2(0.64f, 0.16f));
            var buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.48f, 0.30f, 0.18f, 1f);
            _paintingRevealButton = buttonObject.AddComponent<Button>();
            _paintingRevealButton.targetGraphic = buttonImage;
            _paintingRevealButton.onClick.AddListener(DismissPaintingReveal);
            MakeText(buttonObject.transform, "Label", "Continue", 28f,
                new Color(0.98f, 0.90f, 0.72f, 1f), TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one);

            _paintingReflectionObject = MakeRect(
                _paintingRevealObject.transform, "PaintingReflection", Vector2.zero, Vector2.one);
            _paintingReflectionBody = MakeText(
                _paintingReflectionObject.transform, "Body", string.Empty,
                40f, new Color(0.92f, 0.96f, 1f, 1f), TextAlignmentOptions.Center,
                new Vector2(0.15f, 0.22f), new Vector2(0.85f, 0.78f));
            _paintingReflectionBody.lineSpacing = 14f;
            _paintingReflectionPrompt = MakeText(
                _paintingReflectionObject.transform, "Prompt", "Click to continue  >",
                22f, new Color(0.76f, 0.84f, 0.92f, 0.92f), TextAlignmentOptions.Center,
                new Vector2(0.30f, 0.035f), new Vector2(0.70f, 0.095f));
            _paintingReflectionPrompt.characterSpacing = 4f;
            _paintingReflectionPrompt.gameObject.SetActive(false);
            _paintingReflectionObject.SetActive(false);

            _paintingRevealObject.SetActive(false);
        }

        private void ClearChoices()
        {
            foreach (Transform child in _choicesContainer.transform)
                Destroy(child.gameObject);
        }

        private void BuildChoiceButton(string label, Action action, int index, int count)
        {
            const float gap = 0.045f;
            float height = (1f - gap * (count - 1)) / count;
            float yMax = 1f - index * (height + gap);
            float yMin = yMax - height;

            var buttonObject = MakeRect(_choicesContainer.transform, $"Choice_{index + 1}",
                new Vector2(0f, yMin), new Vector2(1f, yMax));
            var image = buttonObject.AddComponent<Image>();
            image.color = ChoiceBg;

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => action());

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.82f, 0.89f, 0.82f, 1f);
            colors.pressedColor = new Color(0.68f, 0.78f, 0.68f, 1f);
            button.colors = colors;

            MakeText(buttonObject.transform, "Label", label,
                28f, BodyColor, TextAlignmentOptions.Center,
                new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f));
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
            if (EventSystem.current != null) return;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
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
