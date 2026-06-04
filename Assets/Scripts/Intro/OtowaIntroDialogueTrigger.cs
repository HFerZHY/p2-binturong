using System.Collections;
using System.Collections.Generic;
using DialogueSystem.Core;
using DialogueSystem.Data;
using Otowa.Audio;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Otowa.Intro
{
    /// <summary>
    /// Drop on any GameObject in the JunkoIntro scene.
    /// Auto-triggers the Junko introduction dialogue using the world map's
    /// DialogueManager popup system. Includes two player-choice branch points.
    /// Loads nextSceneName when the conversation ends.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class OtowaIntroDialogueTrigger : MonoBehaviour
    {
        [SerializeField] private bool autoTriggerOnStart = true;
        [SerializeField] private string nextSceneName = "Intro-3";
        [SerializeField] private float junkoWalkSpeed = 2.6f;
        [SerializeField] private float departurePause = 0.15f;
        [SerializeField] private float fadeDuration = 0.65f;

        private Character _junko;
        private Character _rin;
        private GameObject _junkoObject;
        private NPCMovement _junkoMovement;
        private Animator _junkoAnimator;
        private Rigidbody2D _junkoBody;
        private PlayerMovement _playerMovement;
        private CanvasGroup _fadeOverlay;
        private bool _endingSequenceStarted;

        private void Awake()
        {
            _junkoObject = GameObject.Find("Junko");
            if (_junkoObject != null)
            {
                _junkoMovement = _junkoObject.GetComponent<NPCMovement>();
                _junkoAnimator = _junkoObject.GetComponentInChildren<Animator>(true);
                _junkoBody = _junkoObject.GetComponent<Rigidbody2D>();
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _playerMovement = player.GetComponent<PlayerMovement>();

            BuildFadeOverlay();
        }

        private void Start()
        {
            StartCoroutine(StartAfterFadeIn());
        }

        private IEnumerator StartAfterFadeIn()
        {
            var audio = GameAudioManager.Instance;
            audio.StopSfxLoop(AudioId.LivelierBirdsong, 0.2f);
            audio.PlayBgm(AudioId.DayWalk, fadeIn: 0.35f);
            audio.PlaySfxLoop(AudioId.ForestAtmosphere, fadeIn: 0.3f);
            _junkoMovement?.Pause();
            _playerMovement?.SetExternalMovementLocked(true);

            yield return FadeOverlay(1f, 0f);

            _playerMovement?.SetExternalMovementLocked(false);
            if (autoTriggerOnStart)
                TriggerIntroDialogue();
        }

        private void OnDisable()
        {
            DialogueManager.OnConversationEnded -= HandleConversationEnded;
            DialogueManager.OnConversationEnded -= HandleClosingThoughtEnded;
        }

        public void TriggerIntroDialogue()
        {
            if (DialogueManager.Instance == null)
            {
                Debug.LogWarning("[OtowaIntroDialogueTrigger] DialogueManager not found in scene.");
                return;
            }

            if (DialogueManager.Instance.IsActive) return;

            _junko = Resources.Load<Character>("Characters/Junko");
            _rin   = Resources.Load<Character>("Characters/Rin");

            if (_junko == null) Debug.LogWarning("[OtowaIntroDialogueTrigger] Characters/Junko asset not found.");
            if (_rin   == null) Debug.LogWarning("[OtowaIntroDialogueTrigger] Characters/Rin asset not found.");

            DialogueManager.OnConversationEnded += HandleConversationEnded;
            DialogueManager.Instance.TriggerDialogue(BuildGraph());
        }

        private void HandleConversationEnded()
        {
            DialogueManager.OnConversationEnded -= HandleConversationEnded;

            if (_endingSequenceStarted)
                return;

            _endingSequenceStarted = true;
            _playerMovement?.SetExternalMovementLocked(true);
            _junkoMovement?.Pause();
            FaceJunkoRight();
            SetJunkoMoving(true);
            StartCoroutine(RunJunkoDeparture());
        }

        private IEnumerator RunJunkoDeparture()
        {
            yield return MoveJunkoTo(GetViewportWorldX(1.12f));

            if (_junkoObject != null)
                _junkoObject.SetActive(false);

            yield return new WaitForSeconds(departurePause);

            if (DialogueManager.Instance == null)
                yield break;

            DialogueManager.OnConversationEnded += HandleClosingThoughtEnded;
            DialogueManager.Instance.TriggerDialogue(BuildClosingThoughtGraph());
        }

        private IEnumerator MoveJunkoTo(float targetX)
        {
            if (_junkoObject == null)
                yield break;

            SetJunkoMoving(true);
            FaceJunkoRight();

            while (Mathf.Abs(_junkoObject.transform.position.x - targetX) > 0.04f)
            {
                var position = _junkoObject.transform.position;
                position.x = Mathf.MoveTowards(position.x, targetX, junkoWalkSpeed * Time.deltaTime);
                SetJunkoPosition(position);
                FaceJunkoRight();
                yield return null;
            }

            var finalPosition = _junkoObject.transform.position;
            finalPosition.x = targetX;
            SetJunkoPosition(finalPosition);
            SetJunkoMoving(false);
        }

        private void HandleClosingThoughtEnded()
        {
            DialogueManager.OnConversationEnded -= HandleClosingThoughtEnded;
            StartCoroutine(FadeAndLoadNextScene());
        }

        private IEnumerator FadeAndLoadNextScene()
        {
            if (_fadeOverlay != null)
            {
                _fadeOverlay.blocksRaycasts = true;
                yield return FadeOverlay(_fadeOverlay.alpha, 1f);
            }

            if (!string.IsNullOrEmpty(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
        }

        private IEnumerator FadeOverlay(float from, float to)
        {
            if (_fadeOverlay == null)
                yield break;

            _fadeOverlay.blocksRaycasts = true;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                _fadeOverlay.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }

            _fadeOverlay.alpha = to;
            _fadeOverlay.blocksRaycasts = to > 0.001f;
        }

        private void SetJunkoPosition(Vector3 position)
        {
            if (_junkoObject == null)
                return;

            _junkoObject.transform.position = position;
            if (_junkoBody != null)
                _junkoBody.position = position;
        }

        private void SetJunkoMoving(bool moving)
        {
            if (_junkoAnimator != null)
                _junkoAnimator.SetBool("isMoving", moving);
        }

        private void FaceJunkoRight()
        {
            if (_junkoObject == null)
                return;

            var scale = _junkoObject.transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            _junkoObject.transform.localScale = scale;
        }

        private static float GetViewportWorldX(float viewportX)
        {
            var camera = Camera.main;
            if (camera == null)
                return viewportX > 1f ? 10f : 4f;

            float depth = Mathf.Abs(camera.transform.position.z);
            return camera.ViewportToWorldPoint(new Vector3(viewportX, 0.5f, depth)).x;
        }

        private void BuildFadeOverlay()
        {
            var canvasObject = new GameObject(
                "Intro2FadeCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            _fadeOverlay = canvasObject.GetComponent<CanvasGroup>();
            _fadeOverlay.alpha = 1f;
            _fadeOverlay.blocksRaycasts = true;

            var imageObject = new GameObject("Fade", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);

            var rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            imageObject.GetComponent<Image>().color = Color.black;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private DialogueNode Line(string id, string speaker, string text, string next) => new()
        {
            id          = id,
            nodeType    = NodeType.Line,
            speaker     = speaker == "Junko" ? _junko : _rin,
            literalText = text,
            nextNodeId  = next,
            onEnter     = new UnityEvent(),
            onExit      = new UnityEvent(),
        };

        private DialogueNode Branch(string id, List<DialogueChoice> choices) => new()
        {
            id       = id,
            nodeType = NodeType.Branch,
            choices  = choices,
            onEnter  = new UnityEvent(),
            onExit   = new UnityEvent(),
        };

        private DialogueNode Terminal() => new()
        {
            id       = "end",
            nodeType = NodeType.Terminal,
            onEnter  = new UnityEvent(),
            onExit   = new UnityEvent(),
        };

        private static DialogueChoice Choice(string label, string target) => new()
        {
            literalLabel = label,
            targetNodeId = target,
        };

        // ── Graph ─────────────────────────────────────────────────────────────

        private DialogueGraph BuildGraph()
        {
            var graph = ScriptableObject.CreateInstance<DialogueGraph>();
            graph.name        = "OtowaIntroDialogue";
            graph.hideFlags   = HideFlags.HideAndDontSave;
            graph.entryNodeId = "line_01";
            graph.nodes       = new List<DialogueNode>
            {
                // ── Opening exchange ──────────────────────────────────────────
                Line("line_01", "Junko", "You must be tired from your journey.", "intro_junko_name"),
                Line("intro_junko_name", "Junko", "You are Rin, right? I am Junko, the chief of Otowa village.", "line_02"),
                Line("line_02", "Rin",   "Hello, it's nice to meet you.", "intro_rin_air"),
                Line("intro_rin_air", "Rin", "The air here is so nice, completely different from the city.", "line_03"),
                Line("line_03", "Junko", "I am relieved to hear you say that. Welcome to Otowa. Please, make yourself at home here.", "intro_rin_hometown"),
                Line("intro_rin_hometown", "Rin", "(My hometown… I hope the air there is this fresh, too.)", "line_04"),
                Line("line_04", "Rin",   "Thank you, chief. And.. by the way, is Mr. Hikaru here? We agreed on the phone to hand over the work today.", "line_05"),
                Line("line_05", "Junko", "Ah... regarding that...", "line_06"),
                Line("line_06", "Junko", "Yesterday afternoon, he suddenly packed a bag and left. He said there was something extremely important he had to go take care of immediately.", "line_07"),
                Line("line_07", "Junko", "So, for the next few days until Hikaru returns, I'm afraid we will have to impose on you to temporarily act as the acting stationmaster here.", "branch_reaction"),

                // ── Branch 1: Rin's reaction to being made stationmaster ──────
                Branch("branch_reaction", new List<DialogueChoice>
                {
                    Choice("I can handle it.",                      "confident_01"),
                    Choice("Me? But I can't run a station!",        "concerned_01"),
                    Choice("I don't have the experience for this.", "anxious_01"),
                }),

                // Confident path
                Line("confident_01", "Rin",   "Acting stationmaster... that actually sounds like quite the adventure. I'm a quick learner — I'll figure it out.", "confident_02"),
                Line("confident_02", "Junko", "Haha, what a cheerful attitude. I'm glad. Still, it's not quite as simple as it might sound.", "merge_01"),

                // Concerned path
                Line("concerned_01", "Rin",   "What? Me, as the acting stationmaster? But I can't lead a train station!", "concerned_02"),
                Line("concerned_02", "Junko", "Please don't feel too much pressure, Rin. We rarely get any trains stopping here anyway. Just think of it as taking a short vacation.", "merge_01"),

                // Anxious path
                Line("anxious_01", "Rin",   "I was a programmer. I have no training in transportation or station management whatsoever. I'm really not qualified for this.", "anxious_02"),
                Line("anxious_02", "Junko", "Your resume matters far less than you think, Rin. Hikaru chose you specifically, and that means something.", "merge_01"),

                // ── All paths converge ────────────────────────────────────────
                Line("merge_01", "Rin",   "He mentioned on the phone that this job would be a bit challenging... Is this what he meant?", "merge_02"),
                Line("merge_02", "Junko", "Sigh, that boy is always like this, doing things purely on impulse. He really has caused you a lot of trouble.", "banquet_01"),

                // ── Banquet invitation ────────────────────────────────────────
                Line("banquet_01", "Junko", "To express our apologies, and to welcome your arrival, we've prepared a welcome banquet for you tonight at the village Ryotei.", "banquet_02"),
                Line("banquet_02", "Junko", "Please do me the honor of attending. It's been a long time since a young person came to the village; everyone is looking forward to meeting you.", "branch_attend"),

                // ── Branch 2: Will Rin attend? ────────────────────────────────
                Branch("branch_attend", new List<DialogueChoice>
                {
                    Choice("I'll think about it — when is it?", "attend_unsure"),
                    Choice("I'll be there.",                     "attend_yes"),
                }),

                // Unsure path
                Line("attend_unsure", "Junko", "It's this evening! Everyone is really looking forward to meeting you. We would be very sad if you missed it.", "attend_unsure_02"),
                Line("attend_unsure_02", "Rin", "Okay... I'll strongly consider it.", "final_01"),

                // Confirmed path
                Line("attend_yes", "Rin", "Thank you for the invitation, I will be there.", "final_01"),

                // ── Closing ───────────────────────────────────────────────────
                Line("final_01", "Junko", "Well then, I will leave the stationmaster's office key with you. You can go settle in first. See you tonight.", "final_02"),
                Line("final_02", "Rin",   "Alright, see you tonight.", "end"),

                Terminal(),
            };

            graph.BuildLookup();
            return graph;
        }

        private DialogueGraph BuildClosingThoughtGraph()
        {
            var graph = ScriptableObject.CreateInstance<DialogueGraph>();
            graph.name = "OtowaIntroClosingThought";
            graph.hideFlags = HideFlags.HideAndDontSave;
            graph.entryNodeId = "line_01";
            graph.nodes = new List<DialogueNode>
            {
                Line(
                    "line_01",
                    "Rin",
                    "(Anyway, I should head into the stationmaster's office and take a look.)",
                    "end"),
                Terminal(),
            };

            graph.BuildLookup();
            return graph;
        }
    }
}
