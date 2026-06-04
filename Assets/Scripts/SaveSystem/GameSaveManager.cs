using System;
using System.Collections;
using System.IO;
using ExhibitionSystem.Core;
using ExhibitionSystem.Data;
using ExhibitionSystem.UI;
using Otowa.Audio;
using Otowa.Inquiry;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Otowa.SaveSystem
{
    [DefaultExecutionOrder(-10000)]
    public class GameSaveManager : MonoBehaviour
    {
        private const string SaveFileName = "otowa-autosave.json";
        private const string TitleSceneName = "StartMenu";
        private const string Day1WorldSceneName = "Day1World";
        private const string Day1HotSpringSceneName = "Day1HotSpring";
        private const string Day2WorldSceneName = "Day2World";
        private const string Day2RyoteiSceneName = "Day2Ryotei";
        private const string Day2HotSpringSceneName = "Day2HotSpring";
        private const string Day2ExhibitionSceneName = "ExhibitionDay2Scene";
        private const string Day3ExhibitionSceneName = "ExhibitionDay3Scene";
        private const string Day1HotSpringEntranceName = "HotSpring Entrance";
        private const string Day2RyoteiEntranceName = "Day2 Ryotei Entrance";
        private const string Day2HotSpringEntranceName = "Day2 HotSpring Entrance";

        private static GameSaveManager _instance;

        private GameSaveData _pendingLoadData;
        private bool _suppressAutosave;

        public static GameSaveManager Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                var gameObject = new GameObject("GameSaveManager");
                _instance = gameObject.AddComponent<GameSaveManager>();
                return _instance;
            }
        }

        public bool HasSave => File.Exists(SavePath) && TryLoad(out _);

        private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInitialized()
        {
            _ = Instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            Day1InquiryProgress.OnProgressChanged += SaveCurrent;
            Day2InquiryProgress.OnProgressChanged += SaveCurrent;
            ExhibitionManager.OnThemeSelected += HandleExhibitionChanged;
            ExhibitionManager.OnSlotInspirationChanged += HandleExhibitionSlotInspirationChanged;
            ExhibitionManager.OnItemPlaced += HandleExhibitionItemChanged;
            ExhibitionManager.OnItemRemoved += HandleExhibitionItemRemoved;
            ExhibitionManager.OnItemsSwapped += HandleExhibitionItemsSwapped;
            ExhibitionManager.OnExhibitionStarted += SaveCurrent;
            ExhibitionManager.OnVisitorReacted += HandleVisitorReacted;
            ExhibitionManager.OnExhibitionEnded += HandleExhibitionEnded;
            ExhibitionManager.OnCurationCleared += SaveCurrent;
            ExhibitionManager.OnStateChanged += HandleExhibitionStateChanged;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Day1InquiryProgress.OnProgressChanged -= SaveCurrent;
            Day2InquiryProgress.OnProgressChanged -= SaveCurrent;
            ExhibitionManager.OnThemeSelected -= HandleExhibitionChanged;
            ExhibitionManager.OnSlotInspirationChanged -= HandleExhibitionSlotInspirationChanged;
            ExhibitionManager.OnItemPlaced -= HandleExhibitionItemChanged;
            ExhibitionManager.OnItemRemoved -= HandleExhibitionItemRemoved;
            ExhibitionManager.OnItemsSwapped -= HandleExhibitionItemsSwapped;
            ExhibitionManager.OnExhibitionStarted -= SaveCurrent;
            ExhibitionManager.OnVisitorReacted -= HandleVisitorReacted;
            ExhibitionManager.OnExhibitionEnded -= HandleExhibitionEnded;
            ExhibitionManager.OnCurationCleared -= SaveCurrent;
            ExhibitionManager.OnStateChanged -= HandleExhibitionStateChanged;
        }

        public void StartNewGame(string firstSceneName)
        {
            DeleteSave();
            ResetRuntimeState();
            SceneManager.LoadScene(firstSceneName);
        }

        public bool ContinueGame()
        {
            if (!TryLoad(out var data) || string.IsNullOrWhiteSpace(data.sceneName))
                return false;

            _pendingLoadData = data;
            _suppressAutosave = true;
            ApplyPersistentSaveData(data);
            SceneManager.LoadScene(data.sceneName);
            return true;
        }

        public void SaveAndReturnToTitle()
        {
            SaveCurrent();
            GameAudioManager.Instance.StopBgm();
            GameAudioManager.Instance.StopAllSfx();
            Time.timeScale = 1f;
            SceneManager.LoadScene(TitleSceneName);
        }

        public void SaveAndQuit()
        {
            SaveCurrent();
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void SaveCurrent()
        {
            if (_suppressAutosave)
                return;

            string activeScene = SceneManager.GetActiveScene().name;
            if (!CanSaveScene(activeScene))
                return;

            GameSaveData data = TryLoad(out var existing) ? existing : new GameSaveData();
            data.version = 1;
            data.savedAtUtc = DateTime.UtcNow.ToString("O");
            data.sceneName = NormalizeSceneName(activeScene);
            data.day1 = Day1InquiryProgress.Instance.CaptureSaveData();
            data.day2 = Day2InquiryProgress.Instance.CaptureSaveData();
            data.journal = InspirationManager.Instance.CaptureSaveData();
            data.audio = GameAudioManager.Instance.CaptureSaveData();
            CaptureMapPosition(activeScene, data);
            CaptureIndoorMapSpawn(activeScene, data);

            var exhibitionManager = FindFirstObjectByType<ExhibitionManager>(FindObjectsInactive.Include);
            if (exhibitionManager != null)
            {
                if (activeScene == Day2ExhibitionSceneName)
                {
                    data.exhibitionDay2 = exhibitionManager.CaptureSaveData(activeScene);
                    CaptureTutorialPopup(data.exhibitionDay2);
                }
                else if (activeScene == Day3ExhibitionSceneName)
                {
                    data.exhibitionDay3 = exhibitionManager.CaptureSaveData(activeScene);
                    CaptureTutorialPopup(data.exhibitionDay3);
                }
            }

            Write(data);
        }

        public void DeleteSave()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_pendingLoadData != null)
            {
                var loadedData = _pendingLoadData;
                ApplySceneSaveData(scene.name, loadedData);
                _pendingLoadData = null;
                StartCoroutine(FinishContinueLoadAfterSceneStart(loadedData));
                return;
            }

            SaveCurrent();
        }

        private IEnumerator FinishContinueLoadAfterSceneStart(GameSaveData data)
        {
            yield return null;
            GameAudioManager.Instance.ApplySaveData(data.audio);
            _suppressAutosave = false;
            SaveCurrent();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                SaveCurrent();
        }

        private void OnApplicationQuit()
        {
            SaveCurrent();
        }

        private void ApplyPersistentSaveData(GameSaveData data)
        {
            Day1InquiryProgress.Instance.ApplySaveData(data.day1);
            Day2InquiryProgress.Instance.ApplySaveData(data.day2);
            InspirationManager.Instance.ApplySaveData(data.journal);
        }

        private static void ApplySceneSaveData(string sceneName, GameSaveData data)
        {
            ApplyMapPosition(sceneName, data);
            if (sceneName == Day2ExhibitionSceneName || sceneName == Day3ExhibitionSceneName)
                TutorialPopup.EnsureTutorialPopupExists();

            var manager = FindFirstObjectByType<ExhibitionManager>(FindObjectsInactive.Include);
            if (manager == null)
                return;

            if (sceneName == Day2ExhibitionSceneName)
            {
                manager.ApplySaveData(data.exhibitionDay2);
                ApplyTutorialPopup(data.exhibitionDay2);
            }
            else if (sceneName == Day3ExhibitionSceneName)
            {
                manager.ApplySaveData(data.exhibitionDay3);
                ApplyTutorialPopup(data.exhibitionDay3);
            }
        }

        private void ResetRuntimeState()
        {
            _suppressAutosave = true;
            Day1InquiryProgress.Instance.ResetProgress();
            Day2InquiryProgress.Instance.ResetProgress();
            InspirationManager.Instance.ResetProgress();
            ExhibitionManager.ResetKnownInspirationMatches();

            foreach (var item in Resources.LoadAll<ExhibitItemData>("Exhibitions/Items"))
            {
                if (item == null)
                    continue;

                item.isUnlocked = true;
                item.ClearUsageHistory();
            }

            foreach (var inspiration in Resources.LoadAll<InspirationData>("Exhibitions/Inspirations"))
            {
                if (inspiration != null)
                    inspiration.isUnlocked = true;
            }

            foreach (var theme in Resources.LoadAll<ExhibitionTheme>("Exhibitions/Themes"))
            {
                if (theme != null)
                    theme.ResetCompletion();
            }

            _suppressAutosave = false;
        }

        private static bool TryLoad(out GameSaveData data)
        {
            data = null;
            if (!File.Exists(SavePath))
                return false;

            try
            {
                string json = File.ReadAllText(SavePath);
                data = JsonUtility.FromJson<GameSaveData>(json);
                return data != null && data.version > 0;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[GameSaveManager] Could not read save data: {exception.Message}");
                data = null;
                return false;
            }
        }

        private static void Write(GameSaveData data)
        {
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[GameSaveManager] Could not write save data: {exception.Message}");
            }
        }

        private static string NormalizeSceneName(string sceneName)
        {
            return sceneName switch
            {
                Day1HotSpringSceneName => Day1WorldSceneName,
                Day2RyoteiSceneName => Day2WorldSceneName,
                Day2HotSpringSceneName => Day2WorldSceneName,
                _ => sceneName,
            };
        }

        private static bool CanSaveScene(string sceneName)
        {
            return !string.IsNullOrWhiteSpace(sceneName) && sceneName != TitleSceneName;
        }

        private static void CaptureTutorialPopup(ExhibitionSaveData data)
        {
            if (data == null)
                return;

            var popup = FindFirstObjectByType<TutorialPopup>(FindObjectsInactive.Include);
            if (popup != null)
                data.tutorialPopup = popup.CaptureSaveData();
        }

        private static void ApplyTutorialPopup(ExhibitionSaveData data)
        {
            if (data == null)
                return;

            var popup = FindFirstObjectByType<TutorialPopup>(FindObjectsInactive.Include);
            if (popup != null)
                popup.ApplySaveData(data.tutorialPopup);
        }

        private static void CaptureMapPosition(string sceneName, GameSaveData data)
        {
            if (sceneName != Day1WorldSceneName && sceneName != Day2WorldSceneName)
                return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return;

            var target = sceneName == Day1WorldSceneName
                ? data.day1WorldPosition
                : data.day2WorldPosition;
            if (target == null)
            {
                target = new MapPositionSaveData();
                if (sceneName == Day1WorldSceneName)
                    data.day1WorldPosition = target;
                else
                    data.day2WorldPosition = target;
            }

            Vector3 position = player.transform.position;
            target.hasPosition = true;
            target.x = position.x;
            target.y = position.y;
            target.z = position.z;

            ClearMapSpawn(sceneName == Day1WorldSceneName
                ? data.day1WorldSpawn
                : data.day2WorldSpawn);
        }

        private static void CaptureIndoorMapSpawn(string sceneName, GameSaveData data)
        {
            switch (sceneName)
            {
                case Day1HotSpringSceneName:
                    data.day1WorldSpawn ??= new MapSpawnSaveData();
                    SetMapSpawn(data.day1WorldSpawn, Day1HotSpringEntranceName, new Vector3(0f, -2f, 0f));
                    break;
                case Day2RyoteiSceneName:
                    data.day2WorldSpawn ??= new MapSpawnSaveData();
                    SetMapSpawn(data.day2WorldSpawn, Day2RyoteiEntranceName, new Vector3(0f, -2f, 0f));
                    break;
                case Day2HotSpringSceneName:
                    data.day2WorldSpawn ??= new MapSpawnSaveData();
                    SetMapSpawn(data.day2WorldSpawn, Day2HotSpringEntranceName, new Vector3(0f, -2f, 0f));
                    break;
            }
        }

        private static void SetMapSpawn(MapSpawnSaveData target, string spawnObjectName, Vector3 offset)
        {
            target.hasSpawn = true;
            target.spawnObjectName = spawnObjectName;
            target.offsetX = offset.x;
            target.offsetY = offset.y;
            target.offsetZ = offset.z;
        }

        private static void ClearMapSpawn(MapSpawnSaveData target)
        {
            if (target == null)
                return;

            target.hasSpawn = false;
            target.spawnObjectName = null;
            target.offsetX = 0f;
            target.offsetY = 0f;
            target.offsetZ = 0f;
        }

        private static void ApplyMapPosition(string sceneName, GameSaveData data)
        {
            MapSpawnSaveData savedSpawn = sceneName switch
            {
                Day1WorldSceneName => data.day1WorldSpawn,
                Day2WorldSceneName => data.day2WorldSpawn,
                _ => null,
            };

            if (TryApplyMapSpawn(savedSpawn))
                return;

            MapPositionSaveData savedPosition = sceneName switch
            {
                Day1WorldSceneName => data.day1WorldPosition,
                Day2WorldSceneName => data.day2WorldPosition,
                _ => null,
            };

            if (savedPosition == null || !savedPosition.hasPosition)
                return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return;

            var position = new Vector3(savedPosition.x, savedPosition.y, savedPosition.z);
            SetPlayerPosition(player, position);
        }

        private static bool TryApplyMapSpawn(MapSpawnSaveData savedSpawn)
        {
            if (savedSpawn == null || !savedSpawn.hasSpawn || string.IsNullOrWhiteSpace(savedSpawn.spawnObjectName))
                return false;

            var spawnObject = GameObject.Find(savedSpawn.spawnObjectName);
            if (spawnObject == null)
            {
                Debug.LogWarning($"[GameSaveManager] Could not find map spawn '{savedSpawn.spawnObjectName}'.");
                return false;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return false;

            var offset = new Vector3(savedSpawn.offsetX, savedSpawn.offsetY, savedSpawn.offsetZ);
            SetPlayerPosition(player, spawnObject.transform.position + offset);
            return true;
        }

        private static void SetPlayerPosition(GameObject player, Vector3 position)
        {
            player.transform.position = position;

            var rigidbody = player.GetComponent<Rigidbody2D>();
            if (rigidbody == null)
                return;

            rigidbody.position = position;
            rigidbody.linearVelocity = Vector2.zero;
            rigidbody.angularVelocity = 0f;
        }

        private void HandleExhibitionChanged(ExhibitionTheme theme) => SaveCurrent();
        private void HandleExhibitionSlotInspirationChanged(int slotIndex, InspirationData inspiration) => SaveCurrent();
        private void HandleExhibitionItemChanged(int slotIndex, ExhibitItemData item) => SaveCurrent();
        private void HandleExhibitionItemRemoved(int slotIndex) => SaveCurrent();
        private void HandleExhibitionItemsSwapped(int slotA, int slotB) => SaveCurrent();
        private void HandleVisitorReacted(
            int slotIndex,
            InspirationData inspiration,
            ExhibitItemData item,
            ExhibitionSlotValidation validation,
            int satisfaction) => SaveCurrent();
        private void HandleExhibitionEnded(bool success, int satisfaction, int threshold) => SaveCurrent();
        private void HandleExhibitionStateChanged(ExhibitionState state) => SaveCurrent();
    }
}
