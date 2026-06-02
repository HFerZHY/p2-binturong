using System.Collections;
using System.Collections.Generic;
using DialogueSystem.Core;
using DialogueSystem.Data;
using Otowa.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Otowa.Day3
{
    public class Day3InspectorDecisionController : MonoBehaviour
    {
        [SerializeField] private float _inspectorWalkSpeed = 2.6f;
        [SerializeField] private float _departurePause = 0.55f;
        [SerializeField] private float _cameraPanDuration = 2f;
        [SerializeField] private float _cameraPanDistance = 28f;
        [SerializeField] private string _nextSceneName = "Day3FinaleCredits";
        [SerializeField] private TMP_FontAsset _serifFont;

        private static readonly string[] LetterPages =
        {
            "To Rin, Acting Stationmaster, Otowa Station:\n\n" +
            "Following internal review, the Company hereby issues the following notice regarding the continued operation of Otowa Station.",

            "A track titled \"Otowa Blues\" has recently drawn considerable attention on social media.\n\n" +
            "Our inquiry finds that travelers passing through filmed the station's exhibition of their own accord and posted the footage online, where its novelty won it wide circulation; other users have since cut that footage together with the track, fueling further high-volume discussion.",

            "The Company assesses that this exposure carries significant potential to drive traffic, and is expected to attract visitors and generate substantial economic value.\n\n" +
            "At the same time, platform data indicates that a considerable number of users, upon learning of the exhibition, have clearly expressed an intent to travel to Otowa, yet were unable to do so for lack of available tickets.",

            "In light of the above, the Company has resolved as follows:\n\n" +
            "1. Effective immediately, one additional night train shall be provisionally added to meet short-term passenger demand;\n\n" +
            "2. The previously planned closure of Otowa Station shall be suspended until further notice.",

            "The Company expects Otowa Station to continue generating economic value and to serve as a model case for rural tourism development, to be promoted across the Company's other stations and villages so as to improve overall profitability.",

            "This notice is hereby issued.\n\nRespectfully,\nThe Railway Company",
        };

        private GameObject _inspectorObject;
        private Animator _inspectorAnimator;
        private Rigidbody2D _inspectorBody;
        private GameObject _playerObject;
        private PlayerMovement _playerMovement;
        private Character _rin;
        private Character _inspector;
        private GameObject _letterCanvas;
        private TMP_Text _letterBody;
        private TMP_Text _letterPage;
        private int _letterPageIndex;
        private bool _letterActive;
        private float _letterInputUnlockTime;
        private bool _loadingScene;

        private void Awake()
        {
            _inspectorObject = FindSceneObject("Inspector");
            if (_inspectorObject != null)
            {
                _inspectorAnimator = _inspectorObject.GetComponentInChildren<Animator>(true);
                _inspectorBody = _inspectorObject.GetComponent<Rigidbody2D>();
                _inspectorObject.SetActive(true);
            }

            _playerObject = GameObject.FindGameObjectWithTag("Player");
            if (_playerObject != null)
                _playerMovement = _playerObject.GetComponent<PlayerMovement>();

            _rin = Resources.Load<Character>("Characters/Rin");
            _inspector = Resources.Load<Character>("Characters/Inspector");
            BuildLetterUi();
            GameAudioManager.Instance.PlaySfxLoop(AudioId.Wind, fadeIn: 0.4f);
        }

        private void Start()
        {
            StartCoroutine(StartWhenReady());
        }

        private void Update()
        {
            if (!_letterActive || Time.unscaledTime < _letterInputUnlockTime || !WasAdvancePressed())
                return;

            _letterPageIndex++;
            if (_letterPageIndex < LetterPages.Length)
                RefreshLetterPage();
            else
                CloseLetterAndContinue();
        }

        private void OnDisable()
        {
            _playerMovement?.SetExternalMovementLocked(false);
            GameAudioManager.Instance.StopSfxLoop(AudioId.Wind, 0.2f);
        }

        private IEnumerator StartWhenReady()
        {
            yield return null;
            while (FindFirstObjectByType<DialogueManager>() == null)
                yield return null;

            _playerMovement?.SetExternalMovementLocked(true);
            FaceInspectorTowardPlayer();
            DialogueManager.Instance.TriggerDialogue(BuildOpeningGraph());
        }

        private DialogueGraph BuildOpeningGraph()
        {
            return BuildGraph("Day3InspectorOpening", new[]
            {
                Inspector("Surprised, Rin?"),
                Rin("Completely. What is going on? There's never supposed to be a train at this hour."),
                Inspector("This letter is the railway company's decision. Take a look."),
            }, OpenLetter);
        }

        private DialogueGraph BuildEndingGraph()
        {
            return BuildGraph("Day3InspectorDecision", new[]
            {
                Rin("So... we proved this place is worth something, and Otowa Station isn't shutting down?"),
                Inspector("To be precise, the closure has been suspended. For now."),
                Rin("...Would it kill you to say one nice thing?"),
                Inspector("Heh..."),
                Inspector("Congratulations, Rin. You proved Otowa's worth."),
                Inspector("And for what it's worth... I don't think this was ever only about business."),
            }, BeginInspectorDeparture);
        }

        private DialogueGraph BuildFarewellGraph()
        {
            return BuildGraph("Day3InspectorFarewell", new[]
            {
                InspectorFacingAway("Happy Summer Festival, Rin."),
            }, BeginInspectorFinalExit);
        }

        private DialogueGraph BuildGraph(string graphName, IReadOnlyList<Line> lines,
                                         System.Action onComplete)
        {
            var graph = ScriptableObject.CreateInstance<DialogueGraph>();
            graph.name = graphName;
            graph.hideFlags = HideFlags.HideAndDontSave;
            graph.entryNodeId = "line_01";

            for (var i = 0; i < lines.Count; i++)
            {
                graph.nodes.Add(new DialogueNode
                {
                    id = $"line_{i + 1:00}",
                    nodeType = NodeType.Line,
                    speaker = lines[i].Speaker,
                    literalText = lines[i].Text,
                    nextNodeId = i == lines.Count - 1 ? "end" : $"line_{i + 2:00}",
                    onEnter = Event(lines[i].OnEntered),
                    onExit = new UnityEvent(),
                });
            }

            graph.nodes.Add(new DialogueNode
            {
                id = "end",
                nodeType = NodeType.Terminal,
                onEnter = Event(onComplete),
                onExit = new UnityEvent(),
            });
            graph.BuildLookup();
            return graph;
        }

        private void OpenLetter()
        {
            _letterPageIndex = 0;
            _letterActive = true;
            _letterInputUnlockTime = Time.unscaledTime + 0.20f;
            _letterCanvas.SetActive(true);
            RefreshLetterPage();
        }

        private void RefreshLetterPage()
        {
            GameAudioManager.Instance.PlaySfxOnce(AudioId.PageTurn);
            _letterBody.text = LetterPages[_letterPageIndex];
            _letterPage.text = $"{_letterPageIndex + 1}  /  {LetterPages.Length}";
        }

        private void CloseLetterAndContinue()
        {
            _letterActive = false;
            _letterCanvas.SetActive(false);
            GameAudioManager.Instance.StopSfxLoop(AudioId.Wind, 0.35f);
            GameAudioManager.Instance.PlayBgm(AudioId.Ending, fadeIn: 0.75f);
            DialogueManager.Instance.TriggerDialogue(BuildEndingGraph());
        }

        private void BeginInspectorDeparture()
        {
            StartCoroutine(RunInspectorDeparture());
        }

        private IEnumerator RunInspectorDeparture()
        {
            yield return null;
            FaceInspector(true);
            yield return MoveInspectorTo(GetViewportWorldX(0.82f));
            FaceInspector(true);
            yield return new WaitForSeconds(_departurePause);
            DialogueManager.Instance.TriggerDialogue(BuildFarewellGraph());
        }

        private void BeginInspectorFinalExit()
        {
            StartCoroutine(RunInspectorFinalExit());
        }

        private IEnumerator RunInspectorFinalExit()
        {
            yield return null;
            FaceInspector(true);
            yield return MoveInspectorTo(GetViewportWorldX(1.12f));
            if (_inspectorObject != null)
                _inspectorObject.SetActive(false);
            if (_playerMovement != null)
                _playerMovement.enabled = false;
            yield return PanCameraUp();
            if (!_loadingScene)
            {
                _loadingScene = true;
                SceneManager.LoadScene(_nextSceneName);
            }
        }

        private IEnumerator MoveInspectorTo(float targetX)
        {
            if (_inspectorObject == null)
                yield break;

            SetInspectorMoving(true);
            FaceInspector(true);
            while (Mathf.Abs(_inspectorObject.transform.position.x - targetX) > 0.04f)
            {
                var position = _inspectorObject.transform.position;
                position.x = Mathf.MoveTowards(position.x, targetX, _inspectorWalkSpeed * Time.deltaTime);
                SetInspectorPosition(position);
                yield return null;
            }

            var finalPosition = _inspectorObject.transform.position;
            finalPosition.x = targetX;
            SetInspectorPosition(finalPosition);
            SetInspectorMoving(false);
            FaceInspector(true);
        }

        private IEnumerator PanCameraUp()
        {
            var camera = Camera.main;
            if (camera == null)
                yield break;

            var start = camera.transform.position;
            var target = start + new Vector3(0f, _cameraPanDistance, 0f);
            var elapsed = 0f;
            while (elapsed < _cameraPanDuration)
            {
                elapsed += Time.deltaTime;
                camera.transform.position = Vector3.Lerp(start, target, Mathf.Clamp01(elapsed / _cameraPanDuration));
                yield return null;
            }

            camera.transform.position = target;
        }

        private void BuildLetterUi()
        {
            _letterCanvas = new GameObject("Day3RailwayLetterCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _letterCanvas.transform.SetParent(transform, false);

            var canvas = _letterCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 350;

            var scaler = _letterCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var blocker = MakeRect(_letterCanvas.transform, "Blocker", Vector2.zero, Vector2.one);
            blocker.AddComponent<Image>().color = new Color(0.10f, 0.08f, 0.055f, 0.97f);

            var paper = MakeRect(blocker.transform, "Paper",
                new Vector2(0.18f, 0.08f), new Vector2(0.82f, 0.92f));
            paper.AddComponent<Image>().color = new Color32(0xc4, 0xb8, 0xa0, 0xFF);

            var title = MakeText(paper.transform, "Title", "[ Railway Company Notice ]",
                23f, new Color32(0x5a, 0x52, 0x48, 0xFF), TextAlignmentOptions.Center,
                new Vector2(0.04f, 0.90f), new Vector2(0.96f, 0.98f));
            title.fontStyle = FontStyles.Bold;

            var separator = MakeRect(paper.transform, "Separator",
                new Vector2(0.04f, 0.87f), new Vector2(0.96f, 0.88f));
            separator.AddComponent<Image>().color = new Color32(0x9a, 0x90, 0x80, 0xFF);

            _letterBody = MakeText(paper.transform, "Body", string.Empty,
                31f, new Color32(0x3a, 0x35, 0x2e, 0xFF), TextAlignmentOptions.Left,
                new Vector2(0.06f, 0.14f), new Vector2(0.94f, 0.85f));
            _letterBody.lineSpacing = 7f;

            _letterPage = MakeText(paper.transform, "Page", string.Empty,
                18f, new Color32(0x5a, 0x52, 0x48, 0xFF), TextAlignmentOptions.Center,
                new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.11f));

            var prompt = MakeText(paper.transform, "Prompt", "Click to continue  >",
                18f, new Color32(0x5a, 0x52, 0x48, 0xCC), TextAlignmentOptions.Right,
                new Vector2(0.60f, 0.04f), new Vector2(0.94f, 0.11f));
            prompt.fontStyle = FontStyles.Italic;

            _letterCanvas.SetActive(false);
        }

        private TMP_Text MakeText(Transform parent, string name, string text, float size,
                                  Color color, TextAlignmentOptions alignment,
                                  Vector2 anchorMin, Vector2 anchorMax)
        {
            var gameObject = MakeRect(parent, name, anchorMin, anchorMax);
            var tmp = gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            if (_serifFont != null)
                tmp.font = _serifFont;
            return tmp;
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

        private void FaceInspectorTowardPlayer()
        {
            FaceInspector(_playerObject == null
                          || _playerObject.transform.position.x >= _inspectorObject.transform.position.x);
        }

        private void FaceInspector(bool facesRight)
        {
            if (_inspectorObject == null)
                return;

            var scale = _inspectorObject.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (facesRight ? 1f : -1f);
            _inspectorObject.transform.localScale = scale;
        }

        private void SetInspectorMoving(bool moving)
        {
            _inspectorAnimator?.SetBool("isMoving", moving);
        }

        private void SetInspectorPosition(Vector3 position)
        {
            if (_inspectorObject == null)
                return;

            _inspectorObject.transform.position = position;
            if (_inspectorBody != null)
                _inspectorBody.position = position;
        }

        private static float GetViewportWorldX(float viewportX)
        {
            var camera = Camera.main;
            if (camera == null)
                return viewportX > 1f ? 10f : 4f;

            var depth = Mathf.Abs(camera.transform.position.z);
            return camera.ViewportToWorldPoint(new Vector3(viewportX, 0.5f, depth)).x;
        }

        private static UnityEvent Event(System.Action action)
        {
            var unityEvent = new UnityEvent();
            if (action != null)
                unityEvent.AddListener(() => action());
            return unityEvent;
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

        private static GameObject FindSceneObject(string objectName)
        {
            foreach (var transform in FindObjectsByType<Transform>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (transform.name == objectName)
                    return transform.gameObject;
            }

            return null;
        }

        private Line Rin(string text) => new Line(_rin, text, null);
        private Line Inspector(string text) => new Line(_inspector, text, FaceInspectorTowardPlayer);
        private Line InspectorFacingAway(string text) => new Line(_inspector, text, () => FaceInspector(true));

        private readonly struct Line
        {
            public Line(Character speaker, string text, System.Action onEntered)
            {
                Speaker = speaker;
                Text = text;
                OnEntered = onEntered;
            }

            public Character Speaker { get; }
            public string Text { get; }
            public System.Action OnEntered { get; }
        }
    }
}
