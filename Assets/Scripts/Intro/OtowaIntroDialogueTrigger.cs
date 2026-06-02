using System.Collections.Generic;
using DialogueSystem.Core;
using DialogueSystem.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

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

        private Character _junko;
        private Character _rin;

        private void Start()
        {
            if (autoTriggerOnStart)
                TriggerIntroDialogue();
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
            if (!string.IsNullOrEmpty(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
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
                Line("line_01", "Junko", "You must be tired from your journey. You are Rin, right? I am Junko, the chief of Otowa village.", "line_02"),
                Line("line_02", "Rin",   "Hello, it's nice to meet you. The air here is so nice, completely different from the city.", "line_03"),
                Line("line_03", "Junko", "I am relieved to hear you say that. Welcome to Otowa.", "line_04"),
                Line("line_04", "Rin",   "Thank you. Excuse me, is Mr. Hikaru here? We agreed on the phone to hand over the work today.", "line_05"),
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
    }
}
