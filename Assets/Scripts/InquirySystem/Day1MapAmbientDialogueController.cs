using System.Collections.Generic;
using DialogueSystem.Core;
using DialogueSystem.Data;
using DialogueSystem.Interfaces;
using UnityEngine;
using UnityEngine.Events;

namespace Otowa.Inquiry
{
    /// <summary>Runs one repeatable ambient line for a Day 1 map character.</summary>
    public class Day1MapAmbientDialogueController : MonoBehaviour, IInteractable
    {
        [SerializeField] private string speakerResourcePath;
        [SerializeField] private string speakerDisplayName;
        [SerializeField] private string dialogueLine;
        [SerializeField] private NPCMovement movement;
        [SerializeField] private string interactPrompt = "[Space] Talk";

        private Character _speaker;
        private Character _runtimeSpeaker;
        private bool _waitingForConversationEnd;

        public bool CanInteract => !InspirationManager.IsJournalOpen
                                   && DialogueManager.Instance != null
                                   && !DialogueManager.Instance.IsActive;

        public string InteractPrompt => interactPrompt;

        private void Awake()
        {
            movement ??= GetComponent<NPCMovement>();
            LoadSpeaker();
        }

        private void OnDisable()
        {
            StopWaitingForConversationEnd();
            movement?.Resume();
        }

        private void OnDestroy()
        {
            if (_runtimeSpeaker != null)
                Destroy(_runtimeSpeaker);
        }

        public void Configure(string resourcePath, string displayName, string line)
        {
            speakerResourcePath = resourcePath;
            speakerDisplayName = displayName;
            dialogueLine = line;
            movement ??= GetComponent<NPCMovement>();
        }

        public void Interact(GameObject initiator)
        {
            if (!CanInteract)
                return;

            LoadSpeaker();
            movement?.Pause();
            movement?.TurnToPlayer();
            StopWaitingForConversationEnd();
            DialogueManager.OnConversationEnded += HandleConversationEnded;
            _waitingForConversationEnd = true;
            DialogueManager.Instance.TriggerDialogue(BuildGraph());
        }

        private void LoadSpeaker()
        {
            if (_speaker != null)
                return;

            if (!string.IsNullOrEmpty(speakerResourcePath))
                _speaker = Resources.Load<Character>(speakerResourcePath);

            if (_speaker != null)
                return;

            _runtimeSpeaker = ScriptableObject.CreateInstance<Character>();
            _runtimeSpeaker.name = speakerDisplayName;
            _runtimeSpeaker.characterName = speakerDisplayName;
            _runtimeSpeaker.hideFlags = HideFlags.HideAndDontSave;
            _speaker = _runtimeSpeaker;
        }

        private DialogueGraph BuildGraph()
        {
            var graph = ScriptableObject.CreateInstance<DialogueGraph>();
            graph.name = $"Day1Ambient{speakerDisplayName}";
            graph.hideFlags = HideFlags.HideAndDontSave;
            graph.entryNodeId = "line";
            graph.nodes = new List<DialogueNode>
            {
                new()
                {
                    id = "line",
                    nodeType = NodeType.Line,
                    speaker = _speaker,
                    literalText = dialogueLine,
                    nextNodeId = "end",
                    onEnter = new UnityEvent(),
                    onExit = new UnityEvent(),
                },
                new()
                {
                    id = "end",
                    nodeType = NodeType.Terminal,
                    onEnter = new UnityEvent(),
                    onExit = new UnityEvent(),
                },
            };
            graph.BuildLookup();
            return graph;
        }

        private void HandleConversationEnded()
        {
            StopWaitingForConversationEnd();
            movement?.Resume();
        }

        private void StopWaitingForConversationEnd()
        {
            if (!_waitingForConversationEnd)
                return;

            DialogueManager.OnConversationEnded -= HandleConversationEnded;
            _waitingForConversationEnd = false;
        }
    }
}
