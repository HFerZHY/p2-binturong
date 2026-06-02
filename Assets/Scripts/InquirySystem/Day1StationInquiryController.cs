using System.Collections.Generic;
using DialogueSystem.Core;
using DialogueSystem.Data;
using DialogueSystem.Interfaces;
using UnityEngine;

namespace Otowa.Inquiry
{
    /// <summary>Day 1 station return gate for the inquiry loop.</summary>
    public class Day1StationInquiryController : MonoBehaviour, IInteractable
    {
        private const string PendingThought =
            "(It looks like there are still a few items in the journal I can look into. Let me chat with the villagers a bit more.)";
        private const string ReadyToRestThought =
            "(I'm feeling a little sleepy. I should get ready to rest.)";
        private const string RestActionKey = "day1-station-rest";

        [SerializeField] private string interactPrompt = "[Space] Return to station";
        [SerializeField] private string day1EndSceneName = "day1end";

        private bool _transitioning;

        public bool CanInteract => !InspirationManager.IsJournalOpen
                                   && !_transitioning
                                   && DialogueManager.Instance != null
                                   && !DialogueManager.Instance.IsActive;

        public string InteractPrompt => interactPrompt;

        private void OnEnable()
        {
            DialogueManager.OnActionRequested += HandleActionRequested;
        }

        private void OnDisable()
        {
            DialogueManager.OnActionRequested -= HandleActionRequested;
        }

        public void Interact(GameObject initiator)
        {
            if (!CanInteract) return;

            if (Day1InquiryProgress.Instance.AreAllInquiryItemsAsked)
            {
                DialogueManager.Instance.TriggerDialogue(
                    Day1MapDialogueFactory.CreateRinThoughtWithChoices(
                        "Day1StationReadyToRest",
                        ReadyToRestThought,
                        new List<DialogueChoice>
                        {
                            new()
                            {
                                literalLabel = "Rest",
                                actionKey = RestActionKey,
                            },
                            new()
                            {
                                literalLabel = "Walk around a little longer",
                                targetNodeId = "end",
                            },
                        }));
                return;
            }

            DialogueManager.Instance.TriggerDialogue(
                Day1MapDialogueFactory.CreateRinThought("Day1StationPending", PendingThought));
        }

        private void HandleActionRequested(string actionKey)
        {
            if (actionKey == RestActionKey)
            {
                _transitioning = true;
                StartCoroutine(MapStationFadeTransition.FadeOutAndLoad(day1EndSceneName));
            }
        }
    }
}
