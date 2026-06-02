using DialogueSystem.Interfaces;
using Otowa.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Otowa.Intro
{
    /// <summary>
    /// Attach to the Ryotei placeholder GameObject in TutorialToRyotei.
    /// Player presses E to enter, which loads Intro-5 (the Ryotei banquet scene).
    /// Requires a Collider2D on the same or child GameObject (on the Interactable layer).
    /// </summary>
    public class RyoteiEntrance : MonoBehaviour, IInteractable
    {
        private const string DEFAULT_INTERACT_PROMPT = "Space to interact";

        [SerializeField] private string nextSceneName = "Intro-5";
        [SerializeField] private string interactPrompt = DEFAULT_INTERACT_PROMPT;

        private bool _loading;

        public bool CanInteract => !_loading && !InspirationManager.IsJournalOpen;
        public string InteractPrompt =>
            string.IsNullOrWhiteSpace(interactPrompt) || interactPrompt == "[Space] Enter Ryotei"
                ? DEFAULT_INTERACT_PROMPT
                : interactPrompt;

        public void Interact(GameObject initiator)
        {
            if (!CanInteract) return;
            _loading = true;
            GameAudioManager.Instance.StopSfxLoop(AudioId.Wind, 0.25f);
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
