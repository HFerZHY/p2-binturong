using System.Collections;
using System.Collections.Generic;
using DialogueSystem.Core;
using DialogueSystem.Data;
using UnityEngine;
using UnityEngine.Events;

namespace Otowa.Inquiry
{
    /// <summary>Runs the station-side Inspector scene before Day 2 free exploration.</summary>
    public class Day2MapFlowController : MonoBehaviour
    {
        [SerializeField] private float inspectorWalkSpeed = 2.6f;
        [SerializeField] private float entrancePause = 0.45f;
        [SerializeField] private float departurePause = 0.55f;

        private GameObject _inspectorObject;
        private SpriteRenderer[] _inspectorRenderers;
        private Animator _inspectorAnimator;
        private Rigidbody2D _inspectorBody;
        private GameObject _playerObject;
        private PlayerMovement _playerMovement;
        private Character _rin;
        private Character _inspector;

        private void Awake()
        {
            _inspectorObject = FindSceneObject("Inspector");
            if (_inspectorObject != null)
            {
                _inspectorRenderers = _inspectorObject.GetComponentsInChildren<SpriteRenderer>(true);
                _inspectorAnimator = _inspectorObject.GetComponentInChildren<Animator>(true);
                _inspectorBody = _inspectorObject.GetComponent<Rigidbody2D>();
                SetInspectorVisible(false);
            }

            _playerObject = GameObject.FindGameObjectWithTag("Player");
            if (_playerObject != null)
                _playerMovement = _playerObject.GetComponent<PlayerMovement>();

            _rin = Resources.Load<Character>("Characters/Rin");
            _inspector = Resources.Load<Character>("Characters/Inspector");
        }

        private void OnDisable()
        {
            _playerMovement?.SetExternalMovementLocked(false);
        }

        private void Start()
        {
            StartCoroutine(StartIntroWhenReady());
        }

        private IEnumerator StartIntroWhenReady()
        {
            yield return null;

            while (FindFirstObjectByType<DialogueManager>() == null)
                yield return null;

            var progress = Day2InquiryProgress.Instance;
            if (progress.IsFreeExplorationUnlocked)
                yield break;

            _playerMovement?.SetExternalMovementLocked(true);
            DialogueManager.Instance.TriggerDialogue(BuildOpeningThoughts());
        }

        private DialogueGraph BuildOpeningThoughts()
        {
            return BuildGraph("Day2InspectorOpening", new List<Line>
            {
                Rin("(Phew... today's exhibition came together smoothly enough, I'd say.)"),
                Rin("(Seems to have gone over well, too. A few passing travelers actually stopped and looked for a good while.)"),
                Rin("(Some snapped photos and took videos on their phones. One even came over to ask me where they could buy Mr. Yuji's sake.)"),
                Rin("(All this clutter is slowly falling into order... Not a bad feeling at all.)"),
            }, BeginInspectorEntrance);
        }

        private DialogueGraph BuildInspectorExchange()
        {
            return BuildGraph("Day2InspectorExchange", new List<Line>
            {
                Inspector("So. It seems you didn't waste all your time spacing out."),
                Rin("Mr. Inspector. Did you see? Quite a few travelers took notice of the exhibition today."),
                Inspector("From where I stand, there's some difference from before. But not much."),
                Rin("..."),
                Inspector("Whether your station gets to keep running is still very much up in the air. The company doesn't care about feel-good sentiment, only impact metrics."),
                Rin("But impact isn't something that changes overnight..."),
                Inspector("That's for you to figure out. Tomorrow is the deadline. Goodbye."),
            }, BeginInspectorDeparture);
        }

        private DialogueGraph BuildInspectorMurmur()
        {
            return BuildGraph("Day2InspectorMurmur", new List<Line>
            {
                InspectorFacingAway("This guy..."),
                InspectorFacingAway("If there'd been someone like this to help back in my hometown, maybe things wouldn't have turned out the way they did..."),
            }, BeginInspectorFinalExit);
        }

        private DialogueGraph BuildClosingThoughts()
        {
            return BuildGraph("Day2InspectorClosing", new List<Line>
            {
                Rin("(The Inspector walked off without looking back.)"),
                Rin("(Still... he seemed to murmur something as he left... His hometown?)"),
                Rin("(Anyway, I'll clock out for the day. I should take another walk around the village.)"),
                Rin("(Tomorrow is the Summer Festival, and the deadline for the station's shutdown. Time's running out.)"),
                Rin("(There are still some items at the station whose stories I haven't figured out. Let me ask the villagers some more.)"),
            }, UnlockFreeExploration);
        }

        private DialogueGraph BuildGraph(string graphName, IReadOnlyList<Line> lines,
                                         System.Action onComplete)
        {
            var graph = ScriptableObject.CreateInstance<DialogueGraph>();
            graph.name = graphName;
            graph.hideFlags = HideFlags.HideAndDontSave;
            graph.entryNodeId = "line_01";
            graph.nodes = new List<DialogueNode>();

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                graph.nodes.Add(new DialogueNode
                {
                    id = $"line_{i + 1:00}",
                    nodeType = NodeType.Line,
                    speaker = line.Speaker,
                    literalText = line.Text,
                    nextNodeId = i + 1 < lines.Count ? $"line_{i + 2:00}" : "end",
                    onEnter = Event(line.OnEntered),
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

        private void BeginInspectorEntrance()
        {
            StartCoroutine(RunInspectorEntrance());
        }

        private IEnumerator RunInspectorEntrance()
        {
            yield return null;

            PositionInspectorAtViewportX(1.08f);
            SetInspectorVisible(true);
            SetInspectorAlpha(1f);
            yield return MoveInspectorTo(GetPlayerX() + 2f);
            FaceInspectorTowardPlayer();
            yield return new WaitForSeconds(entrancePause);
            DialogueManager.Instance.TriggerDialogue(BuildInspectorExchange());
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
            yield return new WaitForSeconds(departurePause);
            DialogueManager.Instance.TriggerDialogue(BuildInspectorMurmur());
        }

        private void BeginInspectorFinalExit()
        {
            StartCoroutine(RunInspectorFinalExit());
        }

        private IEnumerator RunInspectorFinalExit()
        {
            yield return null;

            yield return MoveInspectorTo(GetViewportWorldX(1.12f));
            SetInspectorVisible(false);
            yield return new WaitForSeconds(0.15f);
            DialogueManager.Instance.TriggerDialogue(BuildClosingThoughts());
        }

        private void UnlockFreeExploration()
        {
            SetInspectorVisible(false);
            _playerMovement?.SetExternalMovementLocked(false);
            Day2InquiryProgress.Instance.UnlockFreeExploration();
            InspirationManager.Instance.BeginJournalGuide(restart: true);
        }

        private IEnumerator MoveInspectorTo(float targetX)
        {
            if (_inspectorObject == null)
                yield break;

            SetInspectorMoving(true);
            FaceInspector(targetX >= _inspectorObject.transform.position.x);

            while (Mathf.Abs(_inspectorObject.transform.position.x - targetX) > 0.04f)
            {
                var position = _inspectorObject.transform.position;
                position.x = Mathf.MoveTowards(position.x, targetX, inspectorWalkSpeed * Time.deltaTime);
                SetInspectorPosition(position);
                yield return null;
            }

            var finalPosition = _inspectorObject.transform.position;
            finalPosition.x = targetX;
            SetInspectorPosition(finalPosition);
            SetInspectorMoving(false);
        }

        private void PositionInspectorAtViewportX(float viewportX)
        {
            if (_inspectorObject == null)
                return;

            var position = _inspectorObject.transform.position;
            position.x = GetViewportWorldX(viewportX);
            position.y = _playerObject != null ? _playerObject.transform.position.y : position.y;
            SetInspectorPosition(position);
        }

        private void SetInspectorPosition(Vector3 position)
        {
            if (_inspectorObject == null)
                return;

            _inspectorObject.transform.position = position;
            if (_inspectorBody != null)
                _inspectorBody.position = position;
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

        private float GetPlayerX()
        {
            return _playerObject != null ? _playerObject.transform.position.x : 0f;
        }

        private static float GetViewportWorldX(float viewportX)
        {
            var camera = Camera.main;
            if (camera == null)
                return viewportX > 1f ? 10f : 4f;

            float depth = Mathf.Abs(camera.transform.position.z);
            return camera.ViewportToWorldPoint(new Vector3(viewportX, 0.5f, depth)).x;
        }

        private void SetInspectorVisible(bool visible)
        {
            if (_inspectorObject != null)
                _inspectorObject.SetActive(visible);
        }

        private void SetInspectorAlpha(float alpha)
        {
            if (_inspectorRenderers == null) return;

            foreach (var renderer in _inspectorRenderers)
            {
                var color = renderer.color;
                color.a = alpha;
                renderer.color = color;
            }
        }

        private static UnityEvent Event(System.Action action)
        {
            var unityEvent = new UnityEvent();
            if (action != null)
                unityEvent.AddListener(() => action());
            return unityEvent;
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

        private Line Rin(string text)
        {
            return new Line(_rin, text, null);
        }

        private Line Inspector(string text)
        {
            return new Line(_inspector, text, FaceInspectorTowardPlayer);
        }

        private Line InspectorFacingAway(string text)
        {
            return new Line(_inspector, text, () => FaceInspector(true));
        }

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
