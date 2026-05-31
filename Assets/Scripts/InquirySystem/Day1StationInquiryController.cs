using DialogueSystem.Core;
using DialogueSystem.Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Otowa.Inquiry
{
    /// <summary>Day 1 station return gate for the inquiry loop.</summary>
    public class Day1StationInquiryController : MonoBehaviour, IInteractable
    {
        private const string PendingThought =
            "(It looks like there are still a few items in the journal I can look into. Let me chat with the villagers a bit more.)";

        [SerializeField] private string interactPrompt = "[Space] Return to station";
        [SerializeField] private string day1EndSceneName = "day1end";

        public bool CanInteract => !InspirationManager.IsJournalOpen
                                   && DialogueManager.Instance != null
                                   && !DialogueManager.Instance.IsActive;

        public string InteractPrompt => interactPrompt;

        public void Interact(GameObject initiator)
        {
            if (!CanInteract) return;

            if (Day1InquiryProgress.Instance.AreAllInquiryItemsAsked)
            {
                SceneManager.LoadScene(day1EndSceneName);
                return;
            }

            DialogueManager.Instance.TriggerDialogue(
                Day1MapDialogueFactory.CreateRinThought("Day1StationPending", PendingThought));
        }
    }
}
