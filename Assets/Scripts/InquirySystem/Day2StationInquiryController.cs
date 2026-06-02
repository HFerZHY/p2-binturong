using System.Collections.Generic;
using DialogueSystem.Core;
using DialogueSystem.Data;
using DialogueSystem.Interfaces;
using Otowa.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Otowa.Inquiry
{
    /// <summary>Day 2 station return gate for the afternoon inquiry loop.</summary>
    public class Day2StationInquiryController : MonoBehaviour, IInteractable
    {
        private const string PendingThought =
            "(There are still quite a few things I need to ask about. Let me look around a little longer.)";
        private const string ReadyToRestThought =
            "(It seems there's nothing left I need to ask about. Should I call it a day?)";
        private const string RestActionKey = "day2-station-rest";

        [SerializeField] private string interactPrompt = "[Space] Return to station";
        [SerializeField] private string day2EndSceneName = "day2end";

        public bool CanInteract => !InspirationManager.IsJournalOpen
                                   && Day2InquiryProgress.Instance.IsFreeExplorationUnlocked
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

            if (Day2InquiryProgress.Instance.AreAllInquiryItemsAsked)
            {
                DialogueManager.Instance.TriggerDialogue(
                    Day1MapDialogueFactory.CreateRinThoughtWithChoices(
                        "Day2StationReadyToRest",
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
                                literalLabel = "Keep looking around",
                                targetNodeId = "end",
                            },
                        }));
                return;
            }

            DialogueManager.Instance.TriggerDialogue(
                Day1MapDialogueFactory.CreateRinThought("Day2StationPending", PendingThought));
        }

        private void HandleActionRequested(string actionKey)
        {
            if (actionKey == RestActionKey)
            {
                GameAudioManager.Instance.StopBgm(0.35f);
                SceneManager.LoadScene(day2EndSceneName);
            }
        }
    }
}
