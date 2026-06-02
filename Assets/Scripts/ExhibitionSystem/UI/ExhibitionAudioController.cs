using ExhibitionSystem.Core;
using ExhibitionSystem.Data;
using Otowa.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ExhibitionSystem.UI
{
    /// <summary>
    /// Adds exhibition audio without requiring scene-local AudioSources.
    /// </summary>
    public sealed class ExhibitionAudioController : MonoBehaviour
    {
        private const string DAY2_EXHIBITION_SCENE = "ExhibitionDay2Scene";
        private const string DAY3_EXHIBITION_SCENE = "ExhibitionDay3Scene";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadedHandler()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!IsSupportedScene(scene.name) ||
                FindFirstObjectByType<ExhibitionAudioController>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            var controllerObject = new GameObject(nameof(ExhibitionAudioController));
            controllerObject.AddComponent<ExhibitionAudioController>();
        }

        private static bool IsSupportedScene(string sceneName)
        {
            return sceneName == DAY2_EXHIBITION_SCENE || sceneName == DAY3_EXHIBITION_SCENE;
        }

        private void OnEnable()
        {
            ExhibitionManager.OnVisitorReacted += HandleVisitorReacted;
            ExhibitionManager.OnExhibitionEnded += HandleExhibitionEnded;
        }

        private void OnDisable()
        {
            ExhibitionManager.OnVisitorReacted -= HandleVisitorReacted;
            ExhibitionManager.OnExhibitionEnded -= HandleExhibitionEnded;
        }

        private void Start()
        {
            GameAudioManager.Instance.PlayBgm(AudioId.Gameplay, fadeIn: 0.35f);
        }

        private static void HandleVisitorReacted(
            int slotIndex,
            InspirationData inspiration,
            ExhibitItemData item,
            ExhibitionSlotValidation validation,
            int satisfaction)
        {
            GameAudioManager.Instance.PlaySfxOnce(
                validation.IsCorrect ? AudioId.InspirationUnlocked : AudioId.Failure);
        }

        private static void HandleExhibitionEnded(bool success, int satisfaction, int threshold)
        {
            if (success)
                GameAudioManager.Instance.PlaySfxOnce(AudioId.Jingle);
        }
    }
}
