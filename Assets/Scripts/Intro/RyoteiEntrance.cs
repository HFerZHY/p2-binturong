using DialogueSystem.Interfaces;
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
        [SerializeField] private string nextSceneName = "Intro-5";
        [SerializeField] private string interactPrompt = "[Space] Enter Ryotei";

        private bool _loading;

        public bool CanInteract => !_loading && !InspirationManager.IsJournalOpen;
        public string InteractPrompt => interactPrompt;

        public void Interact(GameObject initiator)
        {
            if (!CanInteract) return;
            _loading = true;
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
