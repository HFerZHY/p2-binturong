using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using Otowa.IndoorDialogue;

namespace Otowa.Intro
{
    /// <summary>
    /// Scene 5 (Ryotei): welcome banquet, character introductions, inspector confrontation.
    ///
    /// Setup: empty scene → empty GameObject → attach this script.
    /// Inspector fields: nextSceneName = "Day1World", assign serifFont.
    /// Background: assign a Sprite; falls back to dark-green solid colour.
    /// Sprites: auto-loaded from Resources/Characters/WorldSprite/*_portrait if not assigned.
    ///
    /// Inspiration unlocks 10, 11, 12 fire when the player advances past
    /// "Well, I guess that sparked some inspiration..." — InspirationManager
    /// shows the toast pop-ups and manages the E-key journal automatically.
    /// </summary>
    public class RyoteiController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Scene")]
        [SerializeField] private string nextSceneName   = "Day1World";
        [SerializeField] private float  typewriterSpeed = 35f;
        [SerializeField] private float  fadeDuration    = 0.35f;

        [Header("Fonts")]
        [SerializeField] private TMP_FontAsset serifFont;

        [Header("Background (leave blank for dark-green placeholder)")]
        [SerializeField] private Sprite backgroundSprite;

        [Header("Character Sprites (leave blank to auto-load portraits)")]
        [SerializeField] private Sprite rinSprite;
        [SerializeField] private Sprite junkoSprite;
        [SerializeField] private Sprite yujiSprite;
        [SerializeField] private Sprite jiroSprite;
        [SerializeField] private Sprite inspectorSprite;

        [Header("Audio")]
        [SerializeField] private AudioClip bgmClip;
        [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.4f;

        // ── Beat data ─────────────────────────────────────────────────────────

        private struct Beat
        {
            public string   Speaker;
            public string   Text;
            public bool     IsThought;
            public bool     ShowsInspector;      // fade inspector in at start of this beat
            public bool     HidesInspector;      // fade inspector out after this beat is advanced
            public int[]    UnlocksInspirations; // IDs to unlock when advancing past this beat
        }

        private List<Beat> _beats;
        private int _current = -1;

        // ── State ─────────────────────────────────────────────────────────────

        private bool      _inputLock;
        private IndoorDialogueTextPlayer _textPlayer;
        private bool      _inspectorVisible;

        // ── UI refs ───────────────────────────────────────────────────────────

        private CanvasGroup _fade;

        private GameObject _dlgPanel;
        private Image      _rinImg;
        private Image      _jiroImg;
        private Image      _junkoImg;
        private Image      _yujiImg;
        private Image      _inspImg;

        private TMP_Text   _speakerTmp;
        private TMP_Text   _bodyTmp;
        private TMP_Text   _promptTmp;

        private AudioSource _audioSource;

        // ── Colours ───────────────────────────────────────────────────────────

        private static readonly Color PanelBg  = new Color32(0x06, 0x0e, 0x06, 0xEE);
        private static readonly Color RinGreen = new Color32(0x8f, 0xbc, 0x8f, 0xFF);
        private static readonly Color JunkoC   = new Color32(0xd4, 0xa0, 0x60, 0xFF);
        private static readonly Color YujiC    = new Color32(0x80, 0xb8, 0xe8, 0xFF);
        private static readonly Color JiroC    = new Color32(0x98, 0x98, 0xa8, 0xFF);
        private static readonly Color InspC    = new Color32(0xa0, 0xa8, 0xc0, 0xFF);
        private static readonly Color BodyC    = new Color32(0xc8, 0xd4, 0xc8, 0xFF);
        private static readonly Color ThoughtC = new Color32(0x90, 0x9a, 0x90, 0xFF);
        private static readonly Color PromptC  = new Color32(0xFF, 0xFF, 0xFF, 0xBB);

        private const float ActiveAlpha   = 1.00f;
        private const float InactiveAlpha = 0.30f;

        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            BuildBeats();
            LoadSprites();
            BuildUI();
            _textPlayer = gameObject.AddComponent<IndoorDialogueTextPlayer>();
            _textPlayer.Initialize(_promptTmp, typewriterSpeed);
            SetupAudio();
            // Share font with the persistent InspirationManager
            InspirationManager.Instance.SetFont(serifFont);
        }

        private void Start()
        {
            _fade.alpha = 0f;
            StartCoroutine(StartAfterFade());
        }

        private IEnumerator StartAfterFade()
        {
            yield return StartCoroutine(FadeTo(1f));
            AdvanceBeat();
        }

        private void Update()
        {
            if (_inputLock) return;
            if (InspirationManager.IsJournalOpen) return; // journal blocks dialogue input

            var mouse = Mouse.current;
            var kb    = Keyboard.current;
            bool clicked = (mouse != null && mouse.leftButton.wasPressedThisFrame)
                        || (kb    != null && kb.spaceKey.wasPressedThisFrame)
                        || (kb    != null && kb.enterKey.wasPressedThisFrame);
            if (!clicked) return;
            if (_textPlayer.IsTyping) { _textPlayer.Skip(); return; }
            AdvanceBeat();
        }

        // ── Beat flow ─────────────────────────────────────────────────────────

        private void AdvanceBeat()
        {
            int next = _current + 1;
            if (next >= _beats.Count) { StartCoroutine(FadeAndLoad()); return; }

            if (_current >= 0)
            {
                var prev = _beats[_current];
                if (prev.HidesInspector)
                    StartCoroutine(FadeOutInspector());
                if (prev.UnlocksInspirations != null)
                    foreach (int id in prev.UnlocksInspirations)
                        InspirationManager.Instance.Unlock(id);
            }

            ShowBeat(next);
        }

        private void ShowBeat(int index)
        {
            _current = index;
            var b = _beats[index];

            _dlgPanel.SetActive(true);
            SetPrompt(false);

            // Inspector entrance: fade in and cut BGM
            if (b.ShowsInspector && !_inspectorVisible)
            {
                _inspectorVisible = true;
                _inspImg.gameObject.SetActive(true);
                SetAlpha(_inspImg, 0f);
                _audioSource.Stop();
                StartCoroutine(FadeInInspector(b.Speaker == "Inspector" ? ActiveAlpha : InactiveAlpha));
            }

            // Speaker label
            _speakerTmp.text = b.Speaker ?? "";
            _speakerTmp.color = b.Speaker switch
            {
                "Rin"       => RinGreen,
                "Junko"     => JunkoC,
                "Yuji"      => YujiC,
                "Jiro"      => JiroC,
                "Inspector" => InspC,
                _           => BodyC,
            };
            _speakerTmp.alignment = b.Speaker == "Rin"
                ? TextAlignmentOptions.Left
                : TextAlignmentOptions.Right;

            // Highlight active speaker, dim others
            SetAlpha(_rinImg,   b.Speaker == "Rin"   ? ActiveAlpha : InactiveAlpha);
            SetAlpha(_junkoImg, b.Speaker == "Junko" ? ActiveAlpha : InactiveAlpha);
            SetAlpha(_yujiImg,  b.Speaker == "Yuji"  ? ActiveAlpha : InactiveAlpha);
            SetAlpha(_jiroImg,  b.Speaker == "Jiro"  ? ActiveAlpha : InactiveAlpha);

            // Inspector alpha only managed here once visible (FadeIn handles the entrance)
            if (_inspectorVisible && !b.ShowsInspector)
                SetAlpha(_inspImg, b.Speaker == "Inspector" ? ActiveAlpha : InactiveAlpha);

            _bodyTmp.color     = b.IsThought ? ThoughtC : BodyC;
            _bodyTmp.fontStyle = b.IsThought ? FontStyles.Italic : FontStyles.Normal;

            _textPlayer.Play(_bodyTmp, b.Text);
        }

        // ── Inspector fade in / out ───────────────────────────────────────────

        private IEnumerator FadeInInspector(float targetAlpha)
        {
            const float duration = 0.5f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                SetAlpha(_inspImg, Mathf.Lerp(0f, targetAlpha, Mathf.Clamp01(t / duration)));
                yield return null;
            }
            SetAlpha(_inspImg, targetAlpha);
        }

        private IEnumerator FadeOutInspector()
        {
            const float duration = 0.4f;
            float startAlpha = _inspImg.color.a;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                SetAlpha(_inspImg, Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(t / duration)));
                yield return null;
            }
            _inspImg.gameObject.SetActive(false);
            _inspectorVisible = false;
        }

        // ── Transitions ───────────────────────────────────────────────────────

        private IEnumerator FadeAndLoad()
        {
            _inputLock = true;
            yield return StartCoroutine(FadeTo(0f));
            SceneManager.LoadScene(nextSceneName);
        }

        private IEnumerator FadeTo(float target)
        {
            float start = _fade.alpha, elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                _fade.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            _fade.alpha = target;
        }

        private void SetPrompt(bool show) => _textPlayer.SetPromptVisible(show);

        private static void SetAlpha(Image img, float a)
        {
            if (img == null) return;
            var c = img.color; c.a = a; img.color = c;
        }

        // ── Sprite loading ────────────────────────────────────────────────────

        private void LoadSprites()
        {
            rinSprite       = LoadPortrait(rinSprite,       "rin");
            junkoSprite     = LoadPortrait(junkoSprite,     "Junko");
            yujiSprite      = LoadPortrait(yujiSprite,      "Yuji");
            jiroSprite      = LoadPortrait(jiroSprite,      "Jiro");
            inspectorSprite = LoadPortrait(inspectorSprite, "Inspector");
        }

        private static Sprite LoadPortrait(Sprite existing, string charName)
        {
            if (existing != null) return existing;
            var tex = Resources.Load<Texture2D>($"Characters/WorldSprite/{charName}_portrait");
            if (tex != null)
                return Sprite.Create(tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0f), 100f);
            var fb = new Texture2D(1, 1);
            fb.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.35f, 1f));
            fb.Apply();
            return Sprite.Create(fb, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0f), 100f);
        }

        // ── UI construction ───────────────────────────────────────────────────

        private void BuildUI()
        {
            var cvGo = new GameObject("RyoteiCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cvGo.transform.SetParent(transform, false);

            var canvas = cvGo.GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = cvGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;

            _fade = cvGo.AddComponent<CanvasGroup>();

            // Background
            var bgGo = MakeRect(cvGo.transform, "BG", Vector2.zero, Vector2.one);
            var bgImg = bgGo.AddComponent<Image>();
            if (backgroundSprite != null)
            {
                bgImg.sprite = backgroundSprite;
                bgImg.color  = Color.white;
            }
            else
            {
                bgImg.color = new Color32(0x0c, 0x1c, 0x0c, 0xFF);
            }

            BuildDialoguePanel(cvGo.transform);

            _promptTmp = MakeTMP(cvGo.transform, "Prompt", "Click to continue  ▼",
                22f, PromptC, TextAlignmentOptions.Right,
                new Vector2(0.60f, 0.02f), new Vector2(0.97f, 0.08f));
            UseFont(_promptTmp);
            _promptTmp.gameObject.SetActive(false);
        }

        private void BuildDialoguePanel(Transform cv)
        {
            _dlgPanel = MakeRect(cv, "DlgPanel", Vector2.zero, Vector2.one);
            var t = _dlgPanel.transform;

            // 5 equal columns (20% each), upper 70% of screen.
            // Rin faces right (no flip); all NPCs flip to face Rin.
            _rinImg   = CharSlot(t, "RinSprite",   0.00f, 0.20f, rinSprite,   false);
            _jiroImg  = CharSlot(t, "JiroSprite",  0.20f, 0.40f, jiroSprite,  true);
            _junkoImg = CharSlot(t, "JunkoSprite", 0.40f, 0.60f, junkoSprite, true);
            _yujiImg  = CharSlot(t, "YujiSprite",  0.60f, 0.80f, yujiSprite,  true);

            // Inspector — far right, hidden until his entrance
            var inspGo = MakeRect(t, "InspSprite",
                new Vector2(0.80f, 0.28f), new Vector2(1.00f, 0.98f));
            inspGo.transform.localScale = new Vector3(-1f, 1f, 1f);
            _inspImg = inspGo.AddComponent<Image>();
            _inspImg.sprite         = inspectorSprite;
            _inspImg.preserveAspect = true;
            _inspImg.color          = new Color(1, 1, 1, 0f);
            _inspImg.raycastTarget  = false;
            _inspImg.gameObject.SetActive(false);

            // Bottom text box
            var panel = MakeRect(t, "Panel", Vector2.zero, new Vector2(1f, 0.28f));
            panel.AddComponent<Image>().color = PanelBg;
            var pt = panel.transform;

            _speakerTmp = MakeTMP(pt, "Speaker", "",
                28f, RinGreen, TextAlignmentOptions.Left,
                new Vector2(0.04f, 0.68f), new Vector2(0.96f, 0.96f));
            _speakerTmp.fontStyle = FontStyles.Bold;
            UseFont(_speakerTmp);

            _bodyTmp = MakeTMP(pt, "Body", "",
                27f, BodyC, TextAlignmentOptions.Left,
                new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.65f));
            _bodyTmp.lineSpacing = 6f;
            UseFont(_bodyTmp);

            _dlgPanel.SetActive(false);
        }

        private Image CharSlot(Transform parent, string name,
                               float xMin, float xMax,
                               Sprite sprite, bool flip)
        {
            var go = MakeRect(parent, name,
                new Vector2(xMin, 0.28f), new Vector2(xMax, 0.98f));
            if (flip) go.transform.localScale = new Vector3(-1f, 1f, 1f);
            var img = go.AddComponent<Image>();
            img.sprite         = sprite;
            img.preserveAspect = true;
            img.color          = new Color(1, 1, 1, InactiveAlpha);
            img.raycastTarget  = false;
            return img;
        }

        // ── UI factory helpers ────────────────────────────────────────────────

        private GameObject MakeRect(Transform parent, string name,
                                    Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }

        private TMP_Text MakeTMP(Transform parent, string name, string text,
                                  float size, Color color, TextAlignmentOptions align,
                                  Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text             = text;
            tmp.fontSize         = size;
            tmp.color            = color;
            tmp.alignment        = align;
            tmp.richText         = true;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return tmp;
        }

        private void UseFont(TMP_Text tmp)
        {
            if (serifFont != null) tmp.font = serifFont;
        }

        // ── Audio ─────────────────────────────────────────────────────────────

        private void SetupAudio()
        {
            _audioSource             = gameObject.AddComponent<AudioSource>();
            _audioSource.clip        = bgmClip;
            _audioSource.volume      = musicVolume;
            _audioSource.loop        = true;
            _audioSource.playOnAwake = false;
            if (bgmClip != null) _audioSource.Play();
        }

        // ── Beat helpers ──────────────────────────────────────────────────────

        private static Beat D(string speaker, bool thought, string text,
                               int[] unlocks = null) => new()
        {
            Speaker              = speaker,
            IsThought            = thought,
            Text                 = text,
            UnlocksInspirations  = unlocks,
        };

        // ── Beat list ─────────────────────────────────────────────────────────

        private void BuildBeats()
        {
            _beats = new List<Beat>
            {
                // ── Arrival + introductions ───────────────────────────────────
                D("Junko", false, "Rin, you've arrived. Please, have a seat."),
                D("Junko", false, "You've had a long day today. Welcome to Otowa."),
                D("Rin",   false, "It's no problem, Chief. Although it was a bit unexpected, I'll just treat it as a change of pace."),
                D("Rin",   false, "It's just... this afternoon on the platform, I ran into a man in a suit who said he was here for a follow-up evaluation."),
                D("Junko", false, "Sigh... Let's save the heavy topics for later. Tonight is a welcome banquet prepared just for you."),

                D("Yuji",  false, "Exactly, exactly! Toss the work stuff right out of your head! Welcome to Otowa, I'm Yuji!"),
                D("Rin",   false, "Hello, Mr. Yuji."),
                D("Yuji",  false, "The Chief's been talking non-stop these past few days about a young person coming. If you ever get bored hanging around the station, come hang out at my pub anytime! It's the hippest spot in the whole village!"),

                D("Jiro",  false, "Hmph. Don't go corrupting the youth the second they arrive."),
                D("Rin",   true,  "(The older man next to him is wearing a pristine chef's uniform, arms crossed, looking incredibly stern.)"),
                D("Jiro",  false, "I am the head chef here, Jiro. Try the food on the table. It'd be a waste if it gets cold."),
                D("Rin",   false, "Thank you, Mr. Jiro."),

                // ── Food & sake ───────────────────────────────────────────────
                D("Rin",   true,  "(Hmm... the taste is fascinating. It has a very unique, refreshing feel to it.)"),
                D("Rin",   false, "This flavor... is really special. It tastes like the mountains."),
                D("Jiro",  false, "At least you have some taste, unlike those city folks that are used to eating cheap, mass-produced garbage."),
                D("Jiro",  false, "The soul of this dish is the shichimi powder I hand-grind and blend myself. It's a recreation of a recipe from hundreds of years ago."),
                D("Rin",   false, "A centuries-old recipe? No wonder the flavor has so much depth."),

                D("Yuji",  false, "How can you have good food without good booze! Come on, Rin, try my pride and joy."),
                D("Rin",   false, "Is this... sake?"),
                D("Yuji",  false, "This isn't just any ordinary sake. Look at that old newspaper on the wall. My sake won a prize at the local specialty competition over a decade ago!"),
                D("Yuji",  false, "I tweaked the recipe during the brewing process to make the mouthfeel softer. A lot of young people really love this flavor."),
                D("Rin",   true,  "(So bitter! It's way too bitter... Do young people actually like this?)"),
                D("Jiro",  false, "Newfangled nonsense. Brewing should follow the rules. Adding all sorts of random garbage just to pander to the youth is nothing but grandstanding."),
                D("Yuji",  false, "Times are changing, old man Jiro! Back in that competition, my booze and your cooking were fighting for the gold medal, and the judges ultimately gave their votes to me, didn't they?"),
                D("Jiro",  false, "That just proves the judges had terrible taste."),
                D("Rin",   false, "Haha... you two really are complete opposites."),

                // ── Shared flavour revelation ─────────────────────────────────
                D("Rin",   false, "However, it's a bit strange. Whether it was the food or the sake just now, I tasted a very similar flavor in both."),
                D("Yuji",  false, "Oh? You've got a sharp palate."),
                D("Yuji",  false, "Whether it's my sake or his shichimi powder, we both added a kind of herb unique to these mountains. Yep... this is the flavor of Otowa!"),
                D("Rin",   true,  "(Mr. Jiro's shichimi powder, Mr. Yuji's gold medal sake, and the specialty herb...)"),
                D("Rin",   true,  "(I think I saw these things in the stationmaster's office, too. I thought they were just junk before, but I never expected them to have stories like this.)"),

                // Advancing past THIS beat fires unlocks 10, 11, 12 → InspirationManager shows toasts
                D("Rin",   true,  "(Well, I guess that sparked some inspiration...)",
                    unlocks: new[] { 10, 11, 12 }),

                // ── Inspector arrives ─────────────────────────────────────────
                new Beat
                {
                    Speaker        = "Inspector",
                    Text           = "Good evening, everyone. It seems you're all in a rather leisurely mood.",
                    ShowsInspector = true,
                },

                D("Junko",     false, "You must be... the inspector sent by the railway company, right? Please, sit down and eat with us."),
                D("Inspector", false, "That won't be necessary. My time is limited, and I have already completed my on-site follow-up evaluation of Otowa Station."),
                D("Inspector", false, "Regrettably, the platform is dilapidated, passenger traffic is practically non-existent, and it holds absolutely no economic value."),
                D("Inspector", false, "The results of these past few inspections have shown zero signs of improvement. The railway company is not running a charity."),
                D("Junko",     false, "Mr. Inspector, we are already working hard to rectify the situation. Please just give us a little more time…"),
                D("Inspector", false, "The company's patience has been exhausted. Just now, my superiors replied with their final decision."),
                D("Inspector", false, "In two days, Otowa Station will be permanently closed. All trains will cease stopping here."),
                D("Yuji",      false, "Hey! Are you kidding me?! Two days? That's way too sudden!"),
                D("Junko",     false, "Two days from now... Absolutely not!"),
                D("Junko",     false, "Two days from now is the Summer Festival! That is the most important day for us in Otowa! The trains absolutely cannot stop running at this time!"),
                D("Inspector", false, "Madam Chief, my duty is solely to convey the company's decision."),
                D("Inspector", false, "Unless you can prove to me within these two days that this station possesses irreplaceable value, our decision will not change."),

                // Advancing past THIS beat fades out the inspector
                new Beat
                {
                    Speaker        = "Inspector",
                    Text           = "Excuse me.",
                    HidesInspector = true,
                },

                // ── Aftermath ─────────────────────────────────────────────────
                D("Rin",   true,  "(Silence... Yuji is tightly clenching his fists, and Jiro has his head bowed without saying a word.)"),
                D("Junko", false, "Rin... I am truly very sorry."),
                D("Junko", false, "You came here full of expectations, and right after you got off the train, we dragged you into such a massive mess."),
                D("Junko", false, "Take the early train tomorrow and head back to the city. This mess shouldn't be yours to bear."),
                D("Rin",   false, "Chief, since the inspector said there might be a turning point if we can prove the station's value, that means it's not time to give up entirely just yet."),
                D("Rin",   false, "In his letter, Mr. Hikaru said he wanted to transform the station into a museum that showcases Otowa's charm."),
                D("Rin",   false, "Even though he only collected a pile of \"junk,\" he asked me to be the curator."),
                D("Rin",   false, "If I can properly exhibit these items that carry the soul of Otowa, wouldn't that make the inspector change his mind?"),
                D("Yuji",  false, "Turn the station into a museum? That guy Hikaru was actually doing something like that behind our backs?!"),
                D("Jiro",  false, "Hmph, with that kid's brain, he wouldn't be able to display anything decent anyway. But, if it's you..."),
                D("Yuji",  false, "Yeah! Rin! If even a picky guy like Jiro approves of your taste, I think you can definitely pull it off!"),
                D("Yuji",  false, "If you need anything at all, come find me at the pub next door anytime!"),
                D("Junko", false, "Rin... are you really willing to help us with this?"),
                D("Rin",   false, "Yeah. Just leave it to me."),
                D("Junko", false, "No matter what happens... on behalf of all the villagers in Otowa, I thank you."),
                D("Junko", false, "We will prepare properly for this year's Summer Festival... but for it to be held successfully, we are counting on you."),
                D("Rin",   true,  "(The Summer Festival... what kind of day is that? Why do they value it so much?)"),
                D("Rin",   true,  "(Regardless, I need to solve the crisis right in front of me first. Looks like I've got my work cut out for me for the next two days.)"),
            };
        }
    }
}
