using UnityEngine;
using UnityEngine.SceneManagement;

namespace ExhibitionSystem.UI
{
    internal static class ExhibitionPopupRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadedHandler()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TutorialPopup.EnsureTutorialPopupExists();
            ExhibitionErrorHintBar.EnsureErrorHintBarExists();
            RewardPopup.EnsureRewardPopupExists();
            Day3CompletionPopup.EnsurePopupExists();
        }
    }
}
