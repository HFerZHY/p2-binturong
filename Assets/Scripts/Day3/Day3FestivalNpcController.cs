using System.Collections.Generic;
using DialogueSystem.Core;
using DialogueSystem.Data;
using DialogueSystem.Interfaces;
using UnityEngine;
using UnityEngine.Events;

namespace Otowa.Day3
{
    public enum Day3FestivalNpc
    {
        None,
        Junko,
        Jiro,
        Yuji,
        Mizuki,
        Rintaro
    }

    public class Day3FestivalNpcController : MonoBehaviour, IInteractable
    {
        [SerializeField] private Day3FestivalNpc _npc;
        [SerializeField] private NPCMovement _movement;
        [SerializeField] private string _interactPrompt = "[Space] Talk";

        private Character _rin;
        private Character _speaker;
        private Day3FestivalFlowController _flow;
        private bool _visited;
        private bool _waitingForConversationEnd;

        public bool CanInteract => !_visited
                                   && !InspirationManager.IsJournalOpen
                                   && DialogueManager.Instance != null
                                   && !DialogueManager.Instance.IsActive;

        public string InteractPrompt => _interactPrompt;

        private void Awake()
        {
            _movement ??= GetComponent<NPCMovement>();
            _flow = FindFirstObjectByType<Day3FestivalFlowController>();
            _rin = Resources.Load<Character>("Characters/Rin");
            _speaker = Resources.Load<Character>($"Characters/{_npc}");
        }

        private void OnDisable()
        {
            StopWaitingForConversationEnd();
            _movement?.Resume();
        }

        public void Configure(Day3FestivalNpc npc)
        {
            _npc = npc;
            _movement ??= GetComponent<NPCMovement>();
        }

        public void Interact(GameObject initiator)
        {
            if (!CanInteract)
                return;

            _visited = true;
            _flow ??= FindFirstObjectByType<Day3FestivalFlowController>();
            _flow?.RegisterVisited(_npc);

            _movement?.Pause();
            _movement?.TurnToPlayer();
            StopWaitingForConversationEnd();
            DialogueManager.OnConversationEnded += HandleConversationEnded;
            _waitingForConversationEnd = true;
            DialogueManager.Instance.TriggerDialogue(BuildGraph());
        }

        private DialogueGraph BuildGraph()
        {
            var graph = ScriptableObject.CreateInstance<DialogueGraph>();
            graph.name = $"Day3Festival{_npc}";
            graph.hideFlags = HideFlags.HideAndDontSave;

            var lines = LinesForNpc();
            for (var i = 0; i < lines.Count; i++)
            {
                var id = $"line_{i + 1:00}";
                graph.nodes.Add(new DialogueNode
                {
                    id = id,
                    nodeType = NodeType.Line,
                    speaker = lines[i].Speaker,
                    literalText = lines[i].Text,
                    nextNodeId = i == lines.Count - 1 ? "end" : $"line_{i + 2:00}",
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

        private List<Line> LinesForNpc()
        {
            return _npc switch
            {
                Day3FestivalNpc.Junko => new List<Line>
                {
                    Speaker("One... two... three... Are these the only children who came home this year...?"),
                },
                Day3FestivalNpc.Jiro => new List<Line>
                {
                    Rin("(Mr. Jiro is carrying a bag, and I can see the dango inside it.)"),
                },
                Day3FestivalNpc.Yuji => new List<Line>
                {
                    Rin("(Mr. Yuji stands beside a pile of fireworks tubes, not saying a word.)"),
                },
                Day3FestivalNpc.Mizuki => new List<Line>
                {
                    Speaker("...Otowa... Blues..."),
                    Rin("(Mizuki is staring at her phone screen. Strange... how does she know the name Otowa Blues?)"),
                },
                Day3FestivalNpc.Rintaro => new List<Line>
                {
                    Speaker("...This weather's no good."),
                    Speaker("Clouds hanging this low. Looks like another year I won't get to see that blue bird."),
                },
                _ => new List<Line>
                {
                    Rin("(There's nothing more to say.)"),
                },
            };
        }

        private void HandleConversationEnded()
        {
            StopWaitingForConversationEnd();
            _movement?.Resume();
            _flow?.NotifyNpcConversationEnded();
        }

        private void StopWaitingForConversationEnd()
        {
            if (!_waitingForConversationEnd)
                return;

            DialogueManager.OnConversationEnded -= HandleConversationEnded;
            _waitingForConversationEnd = false;
        }

        private Line Rin(string text) => new Line(_rin, text);
        private Line Speaker(string text) => new Line(_speaker, text);

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
