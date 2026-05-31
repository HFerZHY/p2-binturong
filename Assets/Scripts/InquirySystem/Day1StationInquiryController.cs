using DialogueSystem.Core;
using DialogueSystem.Interfaces;
using UnityEngine;

namespace Otowa.Inquiry
{
    /// <summary>Day 1 station return gate for the inquiry loop.</summary>
    public class Day1StationInquiryController : MonoBehaviour, IInteractable
    {
        private const string PendingThought =
            "(It looks like there are still a few items in the journal I can look into. Let me chat with the villagers a bit more.)";

        private const string SleepTestLine = "TEST - SLEEP ZZZZ";

        [SerializeField] private string interactPrompt = "[Space] Return to station";

        public bool CanInteract => !InspirationManager.IsJournalOpen
                                   && DialogueManager.Instance != null
                                   && !DialogueManager.Instance.IsActive;

        public string InteractPrompt => interactPrompt;

        public void Interact(GameObject initiator)
        {
            if (!CanInteract) return;

            bool complete = Day1InquiryProgress.Instance.AreAllInquiryItemsAsked;
            DialogueManager.Instance.TriggerDialogue(
                Day1MapDialogueFactory.CreateRinThought(
                    complete ? "Day1StationSleepTest" : "Day1StationPending",
                    complete ? SleepTestLine : PendingThought));
        }
    }
}
