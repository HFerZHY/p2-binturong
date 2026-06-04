using System;
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
        private const string RyoteiInteractPrompt = "[Space] Enter ryotei";
        private const string HotSpringInteractPrompt = "[Space] Enter hot spring";

        [SerializeField] private string sceneName;
        [SerializeField] private string interactPrompt = DefaultInteractPrompt;

        private bool _loading;

        public bool CanInteract => !_loading
                                   && Day2InquiryProgress.Instance.IsFreeExplorationUnlocked
                                   && !InspirationManager.IsJournalOpen;

        public string InteractPrompt
        {
            get
            {
                var scenePrompt = GetScenePrompt();
                if (!string.IsNullOrEmpty(scenePrompt)
                    && (string.IsNullOrWhiteSpace(interactPrompt)
                        || interactPrompt == DefaultInteractPrompt
                        || interactPrompt == "[Space] Enter Ryotei"
                        || interactPrompt == "[Space] Enter Hot Spring"))
                {
                    return scenePrompt;
                }

                return string.IsNullOrWhiteSpace(interactPrompt)
                    ? DefaultInteractPrompt
                    : interactPrompt;
            }
        }

        public void Configure(string targetSceneName)
        {
            sceneName = targetSceneName;
        }

        private string GetScenePrompt()
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return null;

            if (sceneName.IndexOf("Ryotei", StringComparison.OrdinalIgnoreCase) >= 0)
                return RyoteiInteractPrompt;

            if (sceneName.IndexOf("HotSpring", StringComparison.OrdinalIgnoreCase) >= 0)
                return HotSpringInteractPrompt;

            return null;
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
