using DialogueSystem.Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Otowa.HotSpring
{
    /// <summary>Simple Day 1 map entrance for the indoor hot spring prototype.</summary>
    public class HotSpringEntrance : MonoBehaviour, IInteractable
    {
        private const string DEFAULT_INTERACT_PROMPT = "Space to react";

        [SerializeField] private string sceneName = "HotSpring";
        [SerializeField] private string interactPrompt = DEFAULT_INTERACT_PROMPT;

        private bool _loading;

        public bool CanInteract => !_loading && !InspirationManager.IsJournalOpen;
        public string InteractPrompt =>
            string.IsNullOrWhiteSpace(interactPrompt) || interactPrompt == "[Space] Enter Hot Spring"
                ? DEFAULT_INTERACT_PROMPT
                : interactPrompt;

        public void Interact(GameObject initiator)
        {
            if (!CanInteract) return;

            _loading = true;
            SceneManager.LoadScene(sceneName);
        }
    }
}
