using System;
using System.Collections.Generic;
using DialogueSystem.Core;
using UnityEngine;

namespace Otowa.Inquiry
{
    /// <summary>Queues Day 1 map thoughts until the map dialogue view is available.</summary>
    public class Day1MapFlowController : MonoBehaviour
    {
        private static readonly string[] ObjectivePrompt =
        {
            "(I remember Hikaru collected a bunch of stuff, but I still don't know the stories behind these items, let alone how to curate them.)",
            "(Maybe I should ask the villagers for advice.)",
        };

        private static readonly string[] AllInquiryThought =
        {
            "(I think there's nothing left to ask. Let me head back to the station and rest. There's quite a challenge waiting tomorrow.)",
        };

        private readonly Queue<(string graphName, IReadOnlyList<string> lines, Action onComplete)> _pendingThoughts = new();
        private bool _waitingForThoughtEnd;
        private Action _activeThoughtCompletion;

        private void OnEnable()
        {
            Day1InquiryProgress.OnProgressChanged += HandleProgressChanged;
        }

        private void Start()
        {
            var progress = Day1InquiryProgress.Instance;
            if (progress.TryConsumeObjectivePrompt())
                _pendingThoughts.Enqueue((
                    "Day1MapObjective",
                    ObjectivePrompt,
                    () => InspirationManager.Instance.BeginDay1JournalGuide()));

            QueueAllInquiryThoughtIfReady();
        }

        private void OnDisable()
        {
            Day1InquiryProgress.OnProgressChanged -= HandleProgressChanged;
            StopWaitingForThoughtEnd();
        }

        private void Update()
        {
            if (_waitingForThoughtEnd || _pendingThoughts.Count == 0 || InspirationManager.IsJournalOpen)
                return;

            var dialogueManager = DialogueManager.Instance;
            if (dialogueManager == null || dialogueManager.IsActive)
                return;

            var thought = _pendingThoughts.Dequeue();
            _activeThoughtCompletion = thought.onComplete;
            DialogueManager.OnConversationEnded += HandleThoughtEnded;
            _waitingForThoughtEnd = true;
            dialogueManager.TriggerDialogue(
                Day1MapDialogueFactory.CreateRinThought(thought.graphName, thought.lines));
        }

        private void HandleThoughtEnded()
        {
            var onComplete = _activeThoughtCompletion;
            StopWaitingForThoughtEnd();
            onComplete?.Invoke();
        }

        private void StopWaitingForThoughtEnd()
        {
            if (!_waitingForThoughtEnd)
                return;

            DialogueManager.OnConversationEnded -= HandleThoughtEnded;
            _waitingForThoughtEnd = false;
            _activeThoughtCompletion = null;
        }

        private void HandleProgressChanged()
        {
            QueueAllInquiryThoughtIfReady();
        }

        private void QueueAllInquiryThoughtIfReady()
        {
            if (Day1InquiryProgress.Instance.TryConsumeAllInquiryThought())
                _pendingThoughts.Enqueue(("Day1MapInquiryComplete", AllInquiryThought, null));
        }
    }
}
