using DialogueSystem.Interfaces;
using Otowa.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Otowa.HotSpring
{
    /// <summary>Simple Day 1 map entrance for the indoor hot spring prototype.</summary>
    public class HotSpringEntrance : MonoBehaviour, IInteractable
    {
        private const string DEFAULT_INTERACT_PROMPT = "[Space] Enter hot spring";
        private const string LEGACY_INTERACT_PROMPT = "Space to react";

        [SerializeField] private string sceneName = "Day1HotSpring";
        [SerializeField] private string interactPrompt = DEFAULT_INTERACT_PROMPT;

        private bool _loading;

        public bool CanInteract => !_loading && !InspirationManager.IsJournalOpen;
        public string InteractPrompt =>
            string.IsNullOrWhiteSpace(interactPrompt)
            || interactPrompt == LEGACY_INTERACT_PROMPT
            || interactPrompt == "[Space] Enter Hot Spring"
                ? DEFAULT_INTERACT_PROMPT
                : interactPrompt;

        public void Interact(GameObject initiator)
        {
            if (!CanInteract) return;

            _loading = true;
            GameAudioManager.Instance.StopBgm(0.25f, savePosition: true);
            SceneManager.LoadScene(sceneName);
        }
    }
}
