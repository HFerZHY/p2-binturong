using DialogueSystem.Interfaces;
using Otowa.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Otowa.Inquiry
{
    /// <summary>Map entrance shared by Day 2 indoor exploration scenes.</summary>
    public class Day2InteriorEntrance : MonoBehaviour, IInteractable
    {
        private const string DefaultInteractPrompt = "Space to react";

        [SerializeField] private string sceneName;
        [SerializeField] private string interactPrompt = DefaultInteractPrompt;

        private bool _loading;

        public bool CanInteract => !_loading
                                   && Day2InquiryProgress.Instance.IsFreeExplorationUnlocked
                                   && !InspirationManager.IsJournalOpen;

        public string InteractPrompt => string.IsNullOrWhiteSpace(interactPrompt)
            ? DefaultInteractPrompt
            : interactPrompt;

        public void Configure(string targetSceneName)
        {
            sceneName = targetSceneName;
        }

        public void Interact(GameObject initiator)
        {
            if (!CanInteract || string.IsNullOrWhiteSpace(sceneName))
                return;

            _loading = true;
            GameAudioManager.Instance.StopBgm(0.25f, savePosition: true);
            SceneManager.LoadScene(sceneName);
        }
    }
}
