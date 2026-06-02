using System;
using System.Collections.Generic;
using DialogueSystem.Core;
using DialogueSystem.Data;
using DialogueSystem.Interfaces;
using Otowa.Audio;
using UnityEngine;
using UnityEngine.Events;

namespace Otowa.Inquiry
{
    /// <summary>
    /// Day 1 map-only NPC flow. It bridges map conversations to Journal item
    /// selection while leaving indoor dialogue controllers independent.
    /// </summary>
    public class Day1MapNpcInquiryController : MonoBehaviour, IInteractable
    {
        private const string InquiryActionPrefix = "day1-map-inquiry:";
        private const string YujiTheme = "Sake & Sparks: Yuji, Artisan of Two Worlds";
        private const string JunkoTheme = "Summer Festival: An Introduction to Otowa Folklore";

        [SerializeField] private Day1InquiryNpc npc;
        [SerializeField] private NPCMovement movement;
        [SerializeField] private string interactPrompt = "[Space] Talk";

        private Character _rin;
        private Character _speaker;
        private bool _waitingForConversationEnd;

        public bool CanInteract => npc != Day1InquiryNpc.None
                                   && !InspirationManager.IsJournalOpen
                                   && DialogueManager.Instance != null
                                   && !DialogueManager.Instance.IsActive;

        public string InteractPrompt => interactPrompt;

        private string InquiryActionKey => InquiryActionPrefix + npc.ToString().ToLowerInvariant();

        private void Awake()
        {
            movement ??= GetComponent<NPCMovement>();
            _rin = Resources.Load<Character>("Characters/Rin");
            _speaker = Resources.Load<Character>($"Characters/{npc}");
        }

        private void OnEnable()
        {
            DialogueManager.OnActionRequested += HandleActionRequested;
        }

        private void OnDisable()
        {
            DialogueManager.OnActionRequested -= HandleActionRequested;
            StopWaitingForConversationEnd();

            if (npc == Day1InquiryNpc.Yuji)
            {
                GameAudioManager.Instance.StopSfxLoop(AudioId.BluesBeat, 0.15f);
                GameAudioManager.Instance.StopSfxLoop(AudioId.Wind, 0.15f);
            }
        }

        public void Configure(Day1InquiryNpc inquiryNpc)
        {
            npc = inquiryNpc;
            movement ??= GetComponent<NPCMovement>();
        }

        public void Interact(GameObject initiator)
        {
            if (!CanInteract) return;

            var progress = Day1InquiryProgress.Instance;
            DialogueGraph graph;
            if (progress.IsNpcIntroduced(npc))
            {
                graph = progress.HasPendingInquiry(npc)
                    ? BuildInquiryMenuGraph()
                    : BuildExhaustedGraph();
            }
            else
            {
                progress.MarkNpcIntroduced(npc);
                graph = BuildIntroductionGraph();
            }

            StartOwnedConversation(graph);
        }

        private void HandleActionRequested(string actionKey)
        {
            if (actionKey != InquiryActionKey) return;

            movement?.Pause();
            if (!InspirationManager.Instance.OpenItemInquiry(
                    npc,
                    HandleInquiryItemSelected,
                    HandleInquiryCancelled))
            {
                movement?.Resume();
            }
        }

        private void HandleInquiryItemSelected(int sortOrder)
        {
            var graph = BuildInquiryStoryGraph(sortOrder);
            if (graph == null)
            {
                Debug.LogWarning($"[Day1MapNpcInquiryController] No {npc} story for item {sortOrder}.");
                movement?.Resume();
                return;
            }

            StartOwnedConversation(graph);
        }

        private void HandleInquiryCancelled()
        {
            movement?.Resume();
        }

        private void StartOwnedConversation(DialogueGraph graph)
        {
            if (graph == null || DialogueManager.Instance == null) return;

            movement?.Pause();
            movement?.TurnToPlayer();
            StopWaitingForConversationEnd();
            DialogueManager.OnConversationEnded += HandleConversationEnded;
            _waitingForConversationEnd = true;
            DialogueManager.Instance.TriggerDialogue(graph);
        }

        private void HandleConversationEnded()
        {
            StopWaitingForConversationEnd();
            movement?.Resume();
        }

        private void StopWaitingForConversationEnd()
        {
            if (!_waitingForConversationEnd) return;

            DialogueManager.OnConversationEnded -= HandleConversationEnded;
            _waitingForConversationEnd = false;
        }

        private DialogueGraph BuildIntroductionGraph()
        {
            return npc switch
            {
                Day1InquiryNpc.Yuji => BuildDialogueWithMenu(
                    "Day1YujiIntroduction",
                    new[]
                    {
                        Rin(
                            "(Is that... Mr. Yuji up ahead? He's wearing headphones and seems to be listening to music, swaying to the rhythm.)",
                            StartYujiIntroAudio),
                        Rin("Good evening, Mr. Yuji."),
                        Speaker("Oh! It's Rin! Still wandering around the village this late?"),
                        Rin("Listening to some music?"),
                        Speaker("Haha, it's called the Blues! It's the most popular thing out there right now - you young folks all love this, right?"),
                        Rin(
                            "(...Since when was this the most popular thing in the city?)",
                            SwitchYujiIntroToWind),
                        Rin("The blues... do you really like this kind of music, Mr. Yuji?"),
                        Speaker("Of course! You know, literally speaking, \"the blues\" means sorrow."),
                        Speaker("Sorrow! You know what I mean? Like getting drunk and having the mountain wind make your head pound..."),
                        Speaker("Or standing alone on the beach in the middle of the night, staring at the black ocean, feeling like it could swallow you whole..."),
                        Speaker("Whenever I listen to this music, I can feel the blues! The kind of blues that hits right in the soul!"),
                        Speaker("...Hahaha! Sorry, sorry - I had a couple too many today and said some weird things. Don't mind me!"),
                    },
                    RestoreYujiIntroAudio),
                Day1InquiryNpc.Junko => BuildDialogueWithMenu(
                    "Day1JunkoIntroduction",
                    new[]
                    {
                        Rin("Good evening, Chief. Haven't you turned in yet?"),
                        Speaker("Ah, Rin. Good evening. I was a bit concerned about the Summer Festival preparations, so I came out for a walk."),
                        Speaker("The nights here are quiet, aren't they. I hope you haven't found it too dull."),
                        Rin("Not at all. I like this kind of quiet."),
                        Rin("Come to think of it, at the welcome banquet earlier, everyone was so focused on the Summer Festival. What kind of festival is it, exactly?"),
                        Speaker("The Summer Festival is the most important day of the year in Otowa village."),
                        Speaker("By village tradition, on this day all the villagers who've gone away to work or study return home."),
                        Speaker("For all these years, they've come back to Otowa along these very rails. You could say the railway is just about Otowa's only link to the outside world."),
                        Speaker("So the trains absolutely cannot stop running at a time like this."),
                    }),
                _ => null,
            };
        }

        private DialogueGraph BuildInquiryMenuGraph()
        {
            return BuildDialogueWithMenu(
                $"Day1{npc}InquiryMenu",
                new[] { Speaker(GetReturnGreeting()) });
        }

        private DialogueGraph BuildExhaustedGraph()
        {
            return BuildSimpleStory(
                $"Day1{npc}Exhausted",
                new[] { Speaker(GetExhaustedLine()) });
        }

        private string GetReturnGreeting()
        {
            return npc switch
            {
                Day1InquiryNpc.Yuji => "Yo, Rin again! What, haven't had your fill of wandering yet?",
                Day1InquiryNpc.Junko => "Ah, Rin. Was there something else on your mind?",
                _ => string.Empty,
            };
        }

        private string GetExhaustedLine()
        {
            return npc switch
            {
                Day1InquiryNpc.Yuji => "Haha, it's getting late, Rin. Go on, get some rest.",
                Day1InquiryNpc.Junko => "The night's grown deep, Rin.",
                _ => string.Empty,
            };
        }

        private DialogueGraph BuildDialogueWithMenu(
            string graphName,
            IReadOnlyList<Line> lines,
            Action onMenuEntered = null)
        {
            var graph = CreateGraph(graphName);
            AppendLines(graph, lines, "line", "menu");

            var choices = new List<DialogueChoice>();
            if (Day1InquiryProgress.Instance.HasPendingInquiry(npc))
            {
                choices.Add(new DialogueChoice
                {
                    literalLabel = "Inquire about an item's story",
                    actionKey = InquiryActionKey,
                });
            }

            choices.Add(new DialogueChoice
            {
                literalLabel = "Leave",
                targetNodeId = "leave",
            });

            graph.nodes.Add(CreateBranch("menu", choices, onMenuEntered));
            graph.nodes.Add(CreateTerminal("leave"));
            return FinishGraph(graph, lines.Count > 0 ? "line_01" : "menu");
        }

        private DialogueGraph BuildInquiryStoryGraph(int sortOrder)
        {
            return sortOrder switch
            {
                7 => BuildSimpleStory("Day1YujiSake", new[]
                {
                    Speaker("You're asking about my sake? Oh, I could go on for hours. The one that won me the award was called \"Thousand Cranes.\""),
                    Speaker("Eh, I just got lucky back then - happened to draw a judge who loved his drink. If it'd come down to pure craft, honestly, old Jiro's still got the edge on me."),
                }),
                8 => BuildSimpleStory("Day1YujiHerbs", new[]
                {
                    Rin("In the stationmaster's office, I saw some air-dried herbs. These must be one of Otowa's local specialties."),
                    Speaker("That's right. So, how would you say the herb tastes?"),
                    Rin("Hard to say... In your sake, there's a hint of mint, but with a bitter edge. In Jiro's cooking, it turns into something like yuzu."),
                    Speaker("Hahaha, bitter, really? Don't tell me my taste buds are on the fritz?"),
                    Speaker("Tell you what - next time I brew a batch, maybe I'll toss in some honey."),
                    Rin("(I'd really rather he didn't...)"),
                }),
                10 => BuildSimpleStory("Day1YujiFireworks", new[]
                {
                    Rin("Mr. Yuji, is that a firework tube next to you? I saw one in the stationmaster's office, too."),
                    Speaker("Haha, you caught me. That's right - it's what the young folks love most at the Summer Festival, and I made it with my own hands!"),
                    Rin("Huh? Aren't you a brewer?"),
                    Speaker("Hah, mixing drinks and mixing fireworks are pretty much the same. Both are about precisely blending different ingredients together, and then - \"Boom!\" - they bloom in your mouth or up in the sky!"),
                    Speaker("Truth is, my real trade is fireworks artisan. Running the pub and mixing drinks is purely a personal hobby."),
                    Rin("So you had this hidden talent all along, Mr. Yuji. You've made me see you in a whole new light."),
                }, new[] { 8 }, () => InspirationManager.Instance.CompleteTheme(YujiTheme)),
                11 => BuildSimpleStory("Day1JunkoTrainTicket", new[]
                {
                    Rin("Chief, since this railway is so important, I'd like to add a train ticket to the exhibition."),
                    Speaker("Mm, that would indeed be a fitting exhibit."),
                    Speaker("Many villagers are waiting for the Summer Festival too, hoping the people they long to see will come back. Take Jiro, for instance - he hasn't seen his son in a very long time."),
                    Rin("So Jiro's son has left Otowa..."),
                }, new[] { 16 }, AddJunkoClosingIfComplete, appendJunkoClosing: true),
                9 => BuildSimpleStory("Day1JunkoFan", new[]
                {
                    Rin("Chief, I saw a fan in the stationmaster's office with a bird painted on it. I'm guessing... it might have something to do with the Summer Festival?"),
                    Speaker("You guessed right, Rin. The deity Otowa has worshipped for generations is a sacred bird. The Summer Festival is the day we honor it."),
                    Speaker("By the way, Rin - coming into the village today, did you happen to see many birds?"),
                    Rin("It was already getting late when I got off the train, so I don't think I really noticed."),
                    Speaker("Tomorrow, during the day, do go and take a look at the forest on the edge of the village."),
                    Speaker("Every summer, many migratory birds travel from afar to return to this forest - some of them quite rare."),
                    Speaker("Just like the children who've left home: no matter how far they fly, when summer comes, they always ride the wind back."),
                }, new[] { 7 }, AddJunkoClosingIfComplete, appendJunkoClosing: true),
                _ => null,
            };
        }

        private DialogueGraph BuildSimpleStory(
            string graphName,
            IReadOnlyList<Line> lines,
            IReadOnlyList<int> inspirationUnlockIds = null,
            Action onComplete = null,
            bool appendJunkoClosing = false)
        {
            var graph = CreateGraph(graphName);
            string terminalId = "end";
            AppendLines(graph, lines, "line", terminalId, inspirationUnlockIds);

            if (appendJunkoClosing
                && !Day1InquiryProgress.Instance.HasPendingInquiry(Day1InquiryNpc.Junko))
            {
                string closingStart = "closing_01";
                graph.nodes[^1].nextNodeId = closingStart;
                AppendLines(graph, new[]
                {
                    Speaker("It's late. Get some rest soon, Rin."),
                    Rin("I will. Please rest soon as well, Chief. Good night."),
                    Rin("(The once-a-year Summer Festival, the migratory birds flying home, and the wanderers who must take the train back...)"),
                    Rin("(Otowa Station carries far more weight than I ever imagined.)"),
                }, "closing", terminalId);
            }

            var terminal = CreateTerminal(terminalId);
            if (onComplete != null)
                terminal.onEnter.AddListener(() => onComplete());
            graph.nodes.Add(terminal);
            return FinishGraph(graph, "line_01");
        }

        private void AddJunkoClosingIfComplete()
        {
            if (!Day1InquiryProgress.Instance.HasPendingInquiry(Day1InquiryNpc.Junko))
                InspirationManager.Instance.CompleteTheme(JunkoTheme);
        }

        private static DialogueGraph CreateGraph(string graphName)
        {
            var graph = ScriptableObject.CreateInstance<DialogueGraph>();
            graph.name = graphName;
            graph.hideFlags = HideFlags.HideAndDontSave;
            return graph;
        }

        private static DialogueGraph FinishGraph(DialogueGraph graph, string entryNodeId)
        {
            graph.entryNodeId = entryNodeId;
            graph.BuildLookup();
            return graph;
        }

        private static void AppendLines(
            DialogueGraph graph,
            IReadOnlyList<Line> lines,
            string prefix,
            string nextAfterLines,
            IReadOnlyList<int> finalUnlockIds = null)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                bool isLast = i == lines.Count - 1;
                string id = $"{prefix}_{i + 1:00}";
                string nextId = isLast ? nextAfterLines : $"{prefix}_{i + 2:00}";
                graph.nodes.Add(CreateLine(id, lines[i], nextId, isLast ? finalUnlockIds : null));
            }
        }

        private static DialogueNode CreateLine(
            string id,
            Line line,
            string nextNodeId,
            IReadOnlyList<int> inspirationUnlockIds = null)
        {
            var node = new DialogueNode
            {
                id = id,
                nodeType = NodeType.Line,
                speaker = line.Speaker,
                literalText = line.Text,
                nextNodeId = nextNodeId,
                inspirationUnlockIds = inspirationUnlockIds != null
                    ? new List<int>(inspirationUnlockIds)
                    : new List<int>(),
                onEnter = new UnityEvent(),
                onExit = new UnityEvent(),
            };

            if (line.OnEntered != null)
                node.onEnter.AddListener(() => line.OnEntered());

            return node;
        }

        private static DialogueNode CreateBranch(
            string id,
            List<DialogueChoice> choices,
            Action onEntered = null)
        {
            var node = new DialogueNode
            {
                id = id,
                nodeType = NodeType.Branch,
                choices = choices,
                onEnter = new UnityEvent(),
                onExit = new UnityEvent(),
            };

            if (onEntered != null)
                node.onEnter.AddListener(() => onEntered());

            return node;
        }

        private static DialogueNode CreateTerminal(string id)
        {
            return new DialogueNode
            {
                id = id,
                nodeType = NodeType.Terminal,
                onEnter = new UnityEvent(),
                onExit = new UnityEvent(),
            };
        }

        private static void StartYujiIntroAudio()
        {
            var audio = GameAudioManager.Instance;
            audio.StopSfxLoop(AudioId.Wind, 0.15f);
            audio.StopBgm(0.2f, savePosition: true);
            audio.PlaySfxLoop(AudioId.BluesBeat, fadeIn: 0.15f);
        }

        private static void SwitchYujiIntroToWind()
        {
            var audio = GameAudioManager.Instance;
            audio.StopSfxLoop(AudioId.BluesBeat, 0.15f);
            audio.PlaySfxLoop(AudioId.Wind, fadeIn: 0.2f);
        }

        private static void RestoreYujiIntroAudio()
        {
            var audio = GameAudioManager.Instance;
            audio.StopSfxLoop(AudioId.BluesBeat, 0.15f);
            audio.StopSfxLoop(AudioId.Wind, 0.15f);
            audio.PlayBgm(AudioId.NightWalk, fadeIn: 0.25f, resumePlayback: true);
        }

        private Line Rin(string text, Action onEntered = null) => new(_rin, text, onEntered);
        private Line Speaker(string text, Action onEntered = null) => new(_speaker, text, onEntered);

        private readonly struct Line
        {
            public Line(Character speaker, string text, Action onEntered = null)
            {
                Speaker = speaker;
                Text = text;
                OnEntered = onEntered;
            }

            public Character Speaker { get; }
            public string Text { get; }
            public Action OnEntered { get; }
        }
    }
}
