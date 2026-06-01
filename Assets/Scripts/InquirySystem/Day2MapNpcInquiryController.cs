using System;
using System.Collections.Generic;
using DialogueSystem.Core;
using DialogueSystem.Data;
using DialogueSystem.Interfaces;
using UnityEngine;
using UnityEngine.Events;

namespace Otowa.Inquiry
{
    /// <summary>
    /// Day 2 map-only NPC flow. Indoor conversations remain independent so
    /// their visual-novel presentation can evolve without affecting the map.
    /// </summary>
    public class Day2MapNpcInquiryController : MonoBehaviour, IInteractable
    {
        private const string InquiryActionPrefix = "day2-map-inquiry:";
        private const string TopicActionPrefix = "day2-map-topic:";
        private const string YujiTheme = "Master Jiro: Culinary Devotion and Hidden Sorrows";
        private const string RintaroTheme = "Wings Over Otowa: A Birdwatcher's Paradise";

        [SerializeField] private Day2InquiryNpc npc;
        [SerializeField] private NPCMovement movement;
        [SerializeField] private string interactPrompt = "[Space] Talk";

        private Character _rin;
        private Character _speaker;
        private Character _unknownSpeaker;
        private bool _ownsSpeaker;
        private bool _waitingForConversationEnd;

        public bool CanInteract => npc != Day2InquiryNpc.None
                                   && Day2InquiryProgress.Instance.IsFreeExplorationUnlocked
                                   && !InspirationManager.IsJournalOpen
                                   && DialogueManager.Instance != null
                                   && !DialogueManager.Instance.IsActive;

        public string InteractPrompt => interactPrompt;

        private string InquiryActionKey => InquiryActionPrefix + NpcKey;
        private string FestivalTopicActionKey => TopicActionPrefix + NpcKey + ":festival";
        private string LastTrainTopicActionKey => TopicActionPrefix + NpcKey + ":last-train";
        private string NpcKey => npc.ToString().ToLowerInvariant();

        private void Awake()
        {
            movement ??= GetComponent<NPCMovement>();
            _rin = LoadOrCreateCharacter("Rin", out _);
            _speaker = LoadOrCreateCharacter(npc.ToString(), out _ownsSpeaker);
            _unknownSpeaker = CreateRuntimeCharacter("???");
        }

        private void OnEnable()
        {
            DialogueManager.OnActionRequested += HandleActionRequested;
        }

        private void OnDisable()
        {
            DialogueManager.OnActionRequested -= HandleActionRequested;
            StopWaitingForConversationEnd();
        }

        private void OnDestroy()
        {
            if (_ownsSpeaker)
                Destroy(_speaker);
            Destroy(_unknownSpeaker);
        }

        public void Configure(Day2InquiryNpc inquiryNpc)
        {
            npc = inquiryNpc;
            movement ??= GetComponent<NPCMovement>();
        }

        public void Interact(GameObject initiator)
        {
            if (!CanInteract) return;

            var progress = Day2InquiryProgress.Instance;
            DialogueGraph graph;
            if (progress.IsNpcIntroduced(npc))
            {
                graph = HasPendingConversation()
                    ? BuildReturnMenuGraph()
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
            if (actionKey == InquiryActionKey)
            {
                movement?.Pause();
                if (!InspirationManager.Instance.OpenItemInquiry(
                        npc,
                        HandleInquiryItemSelected,
                        HandleInquiryCancelled))
                {
                    movement?.Resume();
                }
                return;
            }

            DialogueGraph graph = actionKey switch
            {
                var key when key == FestivalTopicActionKey => BuildFestivalTopicGraph(),
                var key when key == LastTrainTopicActionKey => BuildLastTrainTopicGraph(),
                _ => null,
            };

            if (graph != null)
                StartOwnedConversation(graph);
        }

        private void HandleInquiryItemSelected(int sortOrder)
        {
            var graph = BuildInquiryStoryGraph(sortOrder);
            if (graph == null)
            {
                Debug.LogWarning($"[Day2MapNpcInquiryController] No {npc} story for item {sortOrder}.");
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

        private bool HasPendingConversation()
        {
            var progress = Day2InquiryProgress.Instance;
            return progress.HasPendingInquiry(npc)
                   || (npc == Day2InquiryNpc.Yuji && !progress.IsYujiFestivalTopicComplete)
                   || (npc == Day2InquiryNpc.Junko && !progress.IsJunkoLastTrainTopicComplete);
        }

        private DialogueGraph BuildIntroductionGraph()
        {
            return npc switch
            {
                Day2InquiryNpc.Yuji => BuildDialogueWithMenu(
                    "Day2YujiIntroduction",
                    new[]
                    {
                        Rin("Good afternoon, Mr. Yuji."),
                        Speaker("Oh, it's Rin. Good afternoon."),
                    }),
                Day2InquiryNpc.Junko => BuildDialogueWithMenu(
                    "Day2JunkoIntroduction",
                    new[]
                    {
                        Rin("Good afternoon, Chief. Is everyone busy preparing for the Summer Festival?"),
                        Speaker("Ah, it's Rin. Yes, everyone's making the final preparations."),
                        Speaker("A few young people came back today. They saw the station museum you set up, and they all found it quite novel."),
                        Speaker("They didn't just stop to take photos, they got to talking about all sorts of old times, too."),
                        Speaker("You've done wonderfully, Rin. You've breathed new life into this station that's been silent for far too long."),
                        Rin("Thank you. It's thanks to your stories that these objects took on such a wonderful meaning."),
                    }),
                Day2InquiryNpc.Rintaro => BuildDialogueWithMenu(
                    "Day2RintaroIntroduction",
                    new[]
                    {
                        Rin("Good afternoon. Excuse me, might you be...?"),
                        Unknown("Shh! Keep your voice down!"),
                        Unknown("If you scare off the birds, I won't forgive you."),
                        Rin("Sorry to bother you. I'm Rin, the new acting stationmaster, I was hoping to ask you about some of Otowa's stories..."),
                        Speaker("Hmph... I'm Rintaro. Out with it, then."),
                        Rin("(So this is Grandpa Rintaro... His temperament's a bit different from how Mizuki described him.)"),
                    }),
                _ => null,
            };
        }

        private DialogueGraph BuildReturnMenuGraph()
        {
            return BuildDialogueWithMenu(
                $"Day2{npc}Return",
                new[] { Speaker(GetReturnGreeting()) });
        }

        private DialogueGraph BuildExhaustedGraph()
        {
            return BuildSimpleStory(
                $"Day2{npc}Exhausted",
                new[] { Speaker(GetExhaustedLine()) });
        }

        private string GetReturnGreeting()
        {
            return npc switch
            {
                Day2InquiryNpc.Yuji => "Yo, Rin again! What, haven't had your fill of wandering yet?",
                Day2InquiryNpc.Junko => "Ah, Rin. Was there something else on your mind?",
                Day2InquiryNpc.Rintaro => "Hmph, you're still here? Make it quick, the birds won't wait.",
                _ => string.Empty,
            };
        }

        private string GetExhaustedLine()
        {
            return npc switch
            {
                Day2InquiryNpc.Yuji => "Ha! That's all from me, kid. Go stretch those legs.",
                Day2InquiryNpc.Junko => "Take care now, Rin. There's still much to be done.",
                Day2InquiryNpc.Rintaro => "Shh! Off with you, you'll scare them away.",
                _ => string.Empty,
            };
        }

        private DialogueGraph BuildDialogueWithMenu(string graphName, IReadOnlyList<Line> lines)
        {
            var graph = CreateGraph(graphName);
            AppendLines(graph, lines, "line", "menu");

            var progress = Day2InquiryProgress.Instance;
            var choices = new List<DialogueChoice>();
            if (npc == Day2InquiryNpc.Yuji && !progress.IsYujiFestivalTopicComplete)
                choices.Add(ActionChoice("About tomorrow's Summer Festival", FestivalTopicActionKey));
            if (npc == Day2InquiryNpc.Junko && !progress.IsJunkoLastTrainTopicComplete)
                choices.Add(ActionChoice("About the last train", LastTrainTopicActionKey));
            if (progress.HasPendingInquiry(npc))
                choices.Add(ActionChoice("Inquire about an item's story", InquiryActionKey));

            choices.Add(TargetChoice("Leave", "leave"));
            graph.nodes.Add(CreateBranch("menu", choices));
            graph.nodes.Add(CreateTerminal("leave"));
            return FinishGraph(graph, lines.Count > 0 ? "line_01" : "menu");
        }

        private DialogueGraph BuildFestivalTopicGraph()
        {
            if (npc != Day2InquiryNpc.Yuji)
                return null;

            return BuildSimpleStory("Day2YujiFestival", new[]
            {
                Rin("Tomorrow's the Summer Festival. How are your preparations coming along, Mr. Yuji?"),
                Speaker("Just about there. The sake and fireworks are all set, now we just need an audience."),
                Speaker("Everyone's waiting for the people they long to see to come back by train. But..."),
                Speaker("Truth is, fewer and fewer have been coming back these past few years."),
                Rin("Fewer and fewer?"),
                Speaker("That's Otowa for you, fewer people every year, and older every year."),
                Speaker("Take this plaza. Come summer, it used to be packed with little brats running around, loud enough to give you a headache."),
                Speaker("And now? All that's left is this rusty old swing set."),
            }, onComplete: Day2InquiryProgress.Instance.CompleteYujiFestivalTopic);
        }

        private DialogueGraph BuildLastTrainTopicGraph()
        {
            if (npc != Day2InquiryNpc.Junko)
                return null;

            return BuildSimpleStory("Day2JunkoLastTrain", new[]
            {
                Rin("But if the Inspector still isn't satisfied tomorrow..."),
                Speaker("Then it may well be Otowa Station's final day."),
                Speaker("Tomorrow morning, the last train will pull in. It's the final chance for the young ones to come home."),
                Speaker("I do hope a few more people step down off of it."),
            }, onComplete: Day2InquiryProgress.Instance.CompleteJunkoLastTrainTopic);
        }

        private DialogueGraph BuildInquiryStoryGraph(int sortOrder)
        {
            return sortOrder switch
            {
                14 => BuildSimpleStory("Day2YujiGuitar", new[]
                {
                    Rin("Oh, right, Mr. Yuji, I saw a beat-up guitar in the stationmaster's office. I know you love music; is it yours?"),
                    Speaker("Ah, the guitar... that'd be that kid Hachi's, I think."),
                    Rin("Hachi?"),
                    Speaker("Yeah, the son of that old stick-in-the-mud, Jiro."),
                    Speaker("The kid was a real rebel. Jiro was dead set on him carrying on the family trade, learning to cook. But he flat-out refused."),
                    Speaker("Figured being a cook in this dead-end village was too dull. He loved music, even got himself an acoustic guitar and was always strumming away."),
                    Rin("Loves music... he must've gotten along great with you, then, Mr. Yuji."),
                    Speaker("Sure did. I actually rooted for him, even let him bring his guitar and play regular gigs at my pub."),
                    Speaker("And you know what? The little punk had the nerve to call my pub too lame!"),
                    Rin("(...Pfft...)"),
                    Speaker("Then one day, Jiro and Hachi had a huge blowout, and the kid stormed off without looking back. Never came home again."),
                    Rin("Mr. Jiro must regret it."),
                    Speaker("Bet he does. But he'd never say so. Jiro's the type who would rather bite his tongue clean off than say a kind word."),
                }, new[] { 5 }, () => InspirationManager.Instance.CompleteTheme(YujiTheme)),
                2 => BuildSimpleStory("Day2RintaroBinoculars", new[]
                {
                    Rin("Excuse me, what's that hanging around your neck...?"),
                    Speaker("Huh? Are you an idiot?"),
                    Speaker("These are obviously binoculars for birdwatching! 8x42, the gold standard, with ED glass, no less!"),
                    Speaker("Move, move, you're blocking my view."),
                }, new[] { 2 }, CompleteRintaroThemeIfReady),
                1 => BuildSimpleStory("Day2RintaroFeather", new[]
                {
                    Rin("Chief Junko said this forest is home to many kinds of birds. Are you here looking for some rare species?"),
                    Speaker("Ho, you know a thing or two. Have you ever heard of a migratory bird with pure blue feathers?"),
                    Speaker("That little fellow is exceedingly rare, it only returns to Otowa's forest during these few days around the Summer Festival."),
                    Speaker("Last year my luck was rotten and I never spotted one. This year, I won't miss the chance for anything."),
                }, new[] { 1 }, CompleteRintaroThemeIfReady),
                12 => BuildSimpleStory("Day2RintaroGeologyBook", new[]
                {
                    Rin("Actually, I found a thick book in the stationmaster's office with a mountain drawn on it, looks like a geology textbook."),
                    Speaker("Hm? How did you end up with that book?"),
                    Speaker("Oh... right, I remember. That Hikaru fellow kept pestering me until I handed it over. Going on about turning the station into a museum, insisting on using it as an exhibit."),
                    Rin("Mr. Yuji mentioned you know a great deal about ores. Did you learn it from that book?"),
                    Speaker("Oh, I wrote that book."),
                    Rin("What?!"),
                    Speaker("I wasted the better part of my life on research. A few years ago, I even came to Otowa to survey its geology."),
                    Speaker("Then, halfway through the survey, it hit me, compared to those stones that just sit there, the birds in this forest are far more interesting!"),
                    Speaker("So the moment I retired, I packed my bags and moved here. Watching these birds in the forest beats scheming against people in the city, that's what real life is."),
                    Rin("Thank you, Professor Rintaro. Enjoy your birdwatching."),
                }, new[] { 3 }, CompleteRintaroThemeIfReady),
                4 => BuildSimpleStory("Day2JunkoCharm", new[]
                {
                    Rin("Oh, Chief, I saw this charm in the stationmaster's office. Mizuki gave me one too. Does it carry some kind of meaning?"),
                    Speaker("Oh, this charm stands for health and safety. See? I carry one with me as well, Mizuki even painted the pattern on it."),
                    Rin("Come to think of it, Chief, may I ask you about Mizuki? When all the young people leave, why has she stayed in the village all this time?"),
                    Speaker("...Mizuki is a child who tugs at your heart."),
                    Speaker("Her parents died in a shipwreck when she was very young. Ever since, the whole village has looked after her as if she were their own, watching over her with the greatest care."),
                    Speaker("You see, the hot spring her family has run for generations is vital to Otowa. She inherited that work, so she's stayed here all along."),
                    Rin("So she's been carrying such an important responsibility."),
                    Speaker("But I can tell, really, this child's true dream... is probably to become a painter. If it weren't for the hot spring, she'd likely have gone off to study at an art school."),
                    Speaker("Yet every time I bring it up, she always says it's fine, that she's happy here in the village."),
                    Speaker("...I think she believes that since the villagers helped her, she ought to stay and repay them."),
                    Speaker("As chief, I know her devotion is crucial to the village. But I truly don't want her bound to a place that's slowly growing old."),
                    Rin("(A dream she's holding back, huh... Maybe I should go and ask Mizuki herself.)"),
                }),
                _ => null,
            };
        }

        private void CompleteRintaroThemeIfReady()
        {
            if (Day2InquiryProgress.Instance.AreRintaroInquiryItemsAsked)
                InspirationManager.Instance.CompleteTheme(RintaroTheme);
        }

        private DialogueGraph BuildSimpleStory(
            string graphName,
            IReadOnlyList<Line> lines,
            IReadOnlyList<int> inspirationUnlockIds = null,
            Action onComplete = null)
        {
            var graph = CreateGraph(graphName);
            AppendLines(graph, lines, "line", "end", inspirationUnlockIds);
            var terminal = CreateTerminal("end");
            if (onComplete != null)
                terminal.onEnter.AddListener(() => onComplete());
            graph.nodes.Add(terminal);
            return FinishGraph(graph, "line_01");
        }

        private static DialogueChoice ActionChoice(string label, string actionKey)
        {
            return new DialogueChoice { literalLabel = label, actionKey = actionKey };
        }

        private static DialogueChoice TargetChoice(string label, string targetNodeId)
        {
            return new DialogueChoice { literalLabel = label, targetNodeId = targetNodeId };
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
            return new DialogueNode
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
        }

        private static DialogueNode CreateBranch(string id, List<DialogueChoice> choices)
        {
            return new DialogueNode
            {
                id = id,
                nodeType = NodeType.Branch,
                choices = choices,
                onEnter = new UnityEvent(),
                onExit = new UnityEvent(),
            };
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

        private static Character LoadOrCreateCharacter(string characterName, out bool created)
        {
            var character = Resources.Load<Character>($"Characters/{characterName}");
            if (character != null)
            {
                created = false;
                return character;
            }

            created = true;
            return CreateRuntimeCharacter(characterName);
        }

        private static Character CreateRuntimeCharacter(string characterName)
        {
            var character = ScriptableObject.CreateInstance<Character>();
            character.name = characterName;
            character.characterName = characterName;
            character.hideFlags = HideFlags.HideAndDontSave;
            return character;
        }

        private Line Rin(string text) => new(_rin, text);
        private Line Speaker(string text) => new(_speaker, text);
        private Line Unknown(string text) => new(_unknownSpeaker, text);

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
