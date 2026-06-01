using System.Collections;
using System.Collections.Generic;
using DialogueSystem.Core;
using DialogueSystem.Data;
using Otowa.IndoorDialogue;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Otowa.Day3
{
    public class Day3FestivalFlowController : MonoBehaviour
    {
        private const int REQUIRED_VISITS = 5;

        [SerializeField] private string _nextSceneName = "Day3NightTrainArrival";
        [SerializeField] private float _charactersPerSecond = 38f;

        private readonly HashSet<Day3FestivalNpc> _visited = new HashSet<Day3FestivalNpc>();
        private readonly string[] _blackScreenLines =
        {
            "(A whistle...)",
            "(...It's getting clearer...)",
            "Hold on. I'm going to go look!",
        };

        private Character _rin;
        private Character _junko;
        private PlayerMovement _playerMovement;
        private CanvasGroup _blackScreen;
        private TMP_Text _blackScreenBody;
        private TMP_Text _blackScreenPrompt;
        private IndoorDialogueTextPlayer _blackScreenTextPlayer;
        private int _blackScreenLineIndex = -1;
        private bool _finaleStarted;
        private bool _waitingForClosingConversation;
        private bool _blackScreenActive;
        private bool _loadingScene;

        private void Awake()
        {
            _rin = Resources.Load<Character>("Characters/Rin");
            _junko = Resources.Load<Character>("Characters/Junko");
            _playerMovement = FindFirstObjectByType<PlayerMovement>();
            BuildBlackScreen();
        }

        private void Update()
        {
            if (!_blackScreenActive || _loadingScene || !WasAdvancePressed())
                return;

            if (_blackScreenTextPlayer.IsTyping)
            {
                _blackScreenTextPlayer.Skip();
                return;
            }

            ShowNextBlackScreenLine();
        }

        public void RegisterVisited(Day3FestivalNpc npc)
        {
            if (npc != Day3FestivalNpc.None)
                _visited.Add(npc);
        }

        public void NotifyNpcConversationEnded()
        {
            if (_finaleStarted || _visited.Count < REQUIRED_VISITS)
                return;

            _finaleStarted = true;
            _playerMovement?.SetExternalMovementLocked(true);
            StartCoroutine(BeginClosingConversation());
        }

        private IEnumerator BeginClosingConversation()
        {
            yield return new WaitForSeconds(0.30f);
            if (DialogueManager.Instance == null)
            {
                StartCoroutine(BeginBlackScreen());
                yield break;
            }

            DialogueManager.OnConversationEnded += HandleClosingConversationEnded;
            _waitingForClosingConversation = true;
            DialogueManager.Instance.TriggerDialogue(BuildClosingGraph());
        }

        private DialogueGraph BuildClosingGraph()
        {
            var graph = ScriptableObject.CreateInstance<DialogueGraph>();
            graph.name = "Day3FestivalClosing";
            graph.hideFlags = HideFlags.HideAndDontSave;

            var lines = new[]
            {
                new Line(_junko, "...Everyone."),
                new Line(_junko, "Thank you all for today."),
                new Line(_junko, "But the weather's really turned. It may start raining before long."),
                new Line(_junko, "Why don't we head home and turn in early..."),
                new Line(_junko, "Thank you for coming..."),
                new Line(_rin, "(...?)"),
                new Line(_rin, "(Wait, that sound...)"),
            };

            for (var i = 0; i < lines.Length; i++)
            {
                graph.nodes.Add(new DialogueNode
                {
                    id = $"line_{i + 1:00}",
                    nodeType = NodeType.Line,
                    speaker = lines[i].Speaker,
                    literalText = lines[i].Text,
                    nextNodeId = i == lines.Length - 1 ? "end" : $"line_{i + 2:00}",
                    onEnter = new UnityEvent(),
                    onExit = new UnityEvent(),
                });
            }

            graph.nodes.Add(new DialogueNode
            {
                id = "end",
                nodeType = NodeType.Terminal,
                onEnter = new UnityEvent(),
                onExit = new UnityEvent(),
            });
            graph.entryNodeId = "line_01";
            graph.BuildLookup();
            return graph;
        }

        private void HandleClosingConversationEnded()
        {
            if (!_waitingForClosingConversation)
                return;

            DialogueManager.OnConversationEnded -= HandleClosingConversationEnded;
            _waitingForClosingConversation = false;
            StartCoroutine(BeginBlackScreen());
        }

        private IEnumerator BeginBlackScreen()
        {
            _blackScreenActive = true;
            _blackScreen.gameObject.SetActive(true);
            yield return FadeBlackScreen(0f, 1f, 0.60f);
            ShowNextBlackScreenLine();
        }

        private void ShowNextBlackScreenLine()
        {
            _blackScreenLineIndex++;
            if (_blackScreenLineIndex >= _blackScreenLines.Length)
            {
                StartCoroutine(LoadNextScene());
                return;
            }

            _blackScreenTextPlayer.Play(_blackScreenBody, _blackScreenLines[_blackScreenLineIndex]);
        }

        private IEnumerator LoadNextScene()
        {
            if (_loadingScene)
                yield break;

            _loadingScene = true;
            _blackScreenPrompt.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.40f);
            SceneManager.LoadScene(_nextSceneName);
        }

        private IEnumerator FadeBlackScreen(float from, float to, float duration)
        {
            _blackScreen.alpha = from;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _blackScreen.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _blackScreen.alpha = to;
        }

        private void BuildBlackScreen()
        {
            var canvasObject = new GameObject("Day3FestivalFinaleCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 250;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _blackScreen = canvasObject.GetComponent<CanvasGroup>();
            var background = canvasObject.AddComponent<Image>();
            background.color = Color.black;

            _blackScreenBody = MakeText(canvasObject.transform, "Body", string.Empty,
                38f, Color.white, TextAlignmentOptions.Center,
                new Vector2(0.18f, 0.30f), new Vector2(0.82f, 0.70f));
            _blackScreenPrompt = MakeText(canvasObject.transform, "Prompt", "Click to continue  >",
                22f, new Color(0.70f, 0.80f, 0.94f, 0.88f), TextAlignmentOptions.Center,
                new Vector2(0.30f, 0.035f), new Vector2(0.70f, 0.095f));
            _blackScreenPrompt.gameObject.SetActive(false);

            _blackScreenTextPlayer = gameObject.AddComponent<IndoorDialogueTextPlayer>();
            _blackScreenTextPlayer.Initialize(_blackScreenPrompt, _charactersPerSecond);
            canvasObject.SetActive(false);
        }

        private static TMP_Text MakeText(Transform parent, string name, string value, float size,
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

        private static bool WasAdvancePressed()
        {
            var mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            var keyboard = Keyboard.current;
            var keyboardPressed = keyboard != null
                                  && (keyboard.spaceKey.wasPressedThisFrame
                                      || keyboard.enterKey.wasPressedThisFrame);
            return mouseClicked || keyboardPressed;
        }

        private readonly struct Line
        {
            public Line(Character speaker, string text)
            {
                Speaker = speaker;
                Text = text;
            }

            public Character Speaker { get; }
            public string Text { get; }
        }
    }
}
