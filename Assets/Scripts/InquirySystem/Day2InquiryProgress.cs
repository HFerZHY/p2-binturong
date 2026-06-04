using System;
using System.Collections.Generic;
using Otowa.SaveSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Otowa.Inquiry
{
    public enum Day2InquiryNpc
    {
        None,
        Mizuki,
        Yuji,
        Junko,
        Rintaro,
        Jiro,
    }

    /// <summary>
    /// Persistent progress for the Day 2 afternoon inquiry loop.
    /// Kept separate from Day 1 so the stable Day 1 flow can remain unchanged.
    /// </summary>
    public class Day2InquiryProgress : MonoBehaviour
    {
        private static readonly HashSet<int> PreviouslyRevealedItemIds = new()
        {
            3, 4, 6, 7, 8, 9, 10, 11,
        };

        private static Day2InquiryProgress _instance;

        private readonly HashSet<int> _askedItemIds = new();
        private readonly HashSet<Day2InquiryNpc> _introducedNpcs = new();
        private bool _day2AfternoonInitialized;
        private bool _freeExplorationUnlocked;
        private bool _yujiFestivalTopicComplete;
        private bool _junkoLastTrainTopicComplete;
        private bool _jiroStationTopicComplete;
        private bool _jiroFestivalTopicComplete;
        private bool _mizukiFestivalTopicComplete;
        private bool _dangoAskedByJiro;
        private bool _dangoAskedByMizuki;
        private bool _paintingInquiryStarted;
        private bool _paintingReceived;
        private bool _allInquiryThoughtShown;
        private string _requestedMapSpawnObjectName;
        private Vector3 _requestedMapSpawnOffset;

        public static event Action OnProgressChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInitialized()
        {
            _ = Instance;
        }

        public static Day2InquiryProgress Instance
        {
            get
            {
                if (_instance != null) return _instance;

                var gameObject = new GameObject("Day2InquiryProgress");
                _instance = gameObject.AddComponent<Day2InquiryProgress>();
                return _instance;
            }
        }

        public bool IsDay2AfternoonInitialized => _day2AfternoonInitialized;
        public bool IsFreeExplorationUnlocked => _freeExplorationUnlocked;
        public bool HasReceivedPainting => _paintingReceived;
        public bool IsYujiFestivalTopicComplete => _yujiFestivalTopicComplete;
        public bool IsJunkoLastTrainTopicComplete => _junkoLastTrainTopicComplete;
        public bool IsJiroStationTopicComplete => _jiroStationTopicComplete;
        public bool IsJiroFestivalTopicComplete => _jiroFestivalTopicComplete;
        public bool IsMizukiFestivalTopicComplete => _mizukiFestivalTopicComplete;
        public bool AreJiroTopicsComplete => _jiroStationTopicComplete
                                             && _jiroFestivalTopicComplete;

        public Day2InquirySaveData CaptureSaveData()
        {
            var data = new Day2InquirySaveData
            {
                day2AfternoonInitialized = _day2AfternoonInitialized,
                freeExplorationUnlocked = _freeExplorationUnlocked,
                yujiFestivalTopicComplete = _yujiFestivalTopicComplete,
                junkoLastTrainTopicComplete = _junkoLastTrainTopicComplete,
                jiroStationTopicComplete = _jiroStationTopicComplete,
                jiroFestivalTopicComplete = _jiroFestivalTopicComplete,
                mizukiFestivalTopicComplete = _mizukiFestivalTopicComplete,
                dangoAskedByJiro = _dangoAskedByJiro,
                dangoAskedByMizuki = _dangoAskedByMizuki,
                paintingInquiryStarted = _paintingInquiryStarted,
                paintingReceived = _paintingReceived,
                allInquiryThoughtShown = _allInquiryThoughtShown,
            };

            foreach (int sortOrder in _askedItemIds)
                data.askedItemIds.Add(sortOrder);

            foreach (var npc in _introducedNpcs)
                data.introducedNpcs.Add(npc.ToString());

            return data;
        }

        public void ApplySaveData(Day2InquirySaveData data)
        {
            ResetProgress(invokeChanged: false);
            if (data == null)
            {
                OnProgressChanged?.Invoke();
                return;
            }

            _day2AfternoonInitialized = data.day2AfternoonInitialized;
            _freeExplorationUnlocked = data.freeExplorationUnlocked;
            _yujiFestivalTopicComplete = data.yujiFestivalTopicComplete;
            _junkoLastTrainTopicComplete = data.junkoLastTrainTopicComplete;
            _jiroStationTopicComplete = data.jiroStationTopicComplete;
            _jiroFestivalTopicComplete = data.jiroFestivalTopicComplete;
            _mizukiFestivalTopicComplete = data.mizukiFestivalTopicComplete;
            _dangoAskedByJiro = data.dangoAskedByJiro;
            _dangoAskedByMizuki = data.dangoAskedByMizuki;
            _paintingInquiryStarted = data.paintingInquiryStarted;
            _paintingReceived = data.paintingReceived;
            _allInquiryThoughtShown = data.allInquiryThoughtShown;

            if (data.askedItemIds != null)
            {
                foreach (int sortOrder in data.askedItemIds)
                    _askedItemIds.Add(sortOrder);
            }

            if (data.introducedNpcs != null)
            {
                foreach (string npcName in data.introducedNpcs)
                {
                    if (Enum.TryParse(npcName, out Day2InquiryNpc npc) && npc != Day2InquiryNpc.None)
                        _introducedNpcs.Add(npc);
                }
            }

            OnProgressChanged?.Invoke();
        }

        public void ResetProgress()
        {
            ResetProgress(invokeChanged: true);
        }

        private void ResetProgress(bool invokeChanged)
        {
            _askedItemIds.Clear();
            _introducedNpcs.Clear();
            _day2AfternoonInitialized = false;
            _freeExplorationUnlocked = false;
            _yujiFestivalTopicComplete = false;
            _junkoLastTrainTopicComplete = false;
            _jiroStationTopicComplete = false;
            _jiroFestivalTopicComplete = false;
            _mizukiFestivalTopicComplete = false;
            _dangoAskedByJiro = false;
            _dangoAskedByMizuki = false;
            _paintingInquiryStarted = false;
            _paintingReceived = false;
            _allInquiryThoughtShown = false;
            _requestedMapSpawnObjectName = null;
            _requestedMapSpawnOffset = Vector3.zero;

            if (invokeChanged)
                OnProgressChanged?.Invoke();
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
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Start()
        {
            InitializeIfDay2ExplorationScene(SceneManager.GetActiveScene().name);
        }

        public static bool IsDay2ExplorationScene(string sceneName)
        {
            return sceneName == "Day2World"
                   || sceneName == "Day2Ryotei"
                   || sceneName == "Day2HotSpring";
        }

        public void InitializeDay2Afternoon()
        {
            if (_day2AfternoonInitialized) return;

            _day2AfternoonInitialized = true;
            InspirationManager.Instance.SeedDay2JournalBaseline();
            OnProgressChanged?.Invoke();
        }

        public void UnlockFreeExploration()
        {
            InitializeDay2Afternoon();
            if (_freeExplorationUnlocked) return;

            _freeExplorationUnlocked = true;
            OnProgressChanged?.Invoke();
        }

        public Day2InquiryNpc GetInquiryNpc(int sortOrder)
        {
            if (!_freeExplorationUnlocked)
                return Day2InquiryNpc.None;

            return sortOrder switch
            {
                1 when !_askedItemIds.Contains(1) => Day2InquiryNpc.Rintaro,
                2 when !_askedItemIds.Contains(2) => Day2InquiryNpc.Rintaro,
                4 when !_askedItemIds.Contains(4) => Day2InquiryNpc.Junko,
                5 when !_dangoAskedByMizuki => Day2InquiryNpc.Mizuki,
                5 when !_dangoAskedByJiro => Day2InquiryNpc.Jiro,
                12 when !_askedItemIds.Contains(12) => Day2InquiryNpc.Rintaro,
                13 when !_askedItemIds.Contains(13) => Day2InquiryNpc.Mizuki,
                14 when !_askedItemIds.Contains(14) => Day2InquiryNpc.Yuji,
                15 when _askedItemIds.Contains(4) && !_paintingReceived => Day2InquiryNpc.Mizuki,
                _ => Day2InquiryNpc.None,
            };
        }

        public bool IsItemRevealed(int sortOrder)
        {
            if (!_freeExplorationUnlocked)
                return false;

            if (sortOrder == 15)
                return _paintingReceived;

            return PreviouslyRevealedItemIds.Contains(sortOrder)
                   || sortOrder is 1 or 2 or 5 or 12 or 13 or 14;
        }

        public bool IsInquiryPending(int sortOrder)
        {
            return GetInquiryNpc(sortOrder) != Day2InquiryNpc.None;
        }

        public bool CanAsk(Day2InquiryNpc npc, int sortOrder)
        {
            if (npc == Day2InquiryNpc.None || GetInquiryNpc(sortOrder) != npc)
                return false;

            if (sortOrder == 15)
                return CanReceivePainting;

            if (sortOrder == 5 && npc == Day2InquiryNpc.Jiro)
                return AreJiroTopicsComplete;

            return true;
        }

        public bool HasPendingInquiry(Day2InquiryNpc npc)
        {
            if (!_freeExplorationUnlocked || npc == Day2InquiryNpc.None)
                return false;

            for (int sortOrder = 1; sortOrder <= 16; sortOrder++)
            {
                if (GetInquiryNpc(sortOrder) == npc)
                    return true;
            }

            return false;
        }

        public bool HasPendingInquiryAfterAsking(Day2InquiryNpc npc, int completedSortOrder)
        {
            if (!_freeExplorationUnlocked || npc == Day2InquiryNpc.None)
                return false;

            for (int sortOrder = 1; sortOrder <= 16; sortOrder++)
            {
                if (sortOrder == completedSortOrder && GetInquiryNpc(sortOrder) == npc)
                    continue;

                if (GetInquiryNpc(sortOrder) == npc)
                    return true;
            }

            return false;
        }

        public bool IsNpcIntroduced(Day2InquiryNpc npc)
        {
            return _introducedNpcs.Contains(npc);
        }

        public void MarkNpcIntroduced(Day2InquiryNpc npc)
        {
            if (npc != Day2InquiryNpc.None)
                _introducedNpcs.Add(npc);
        }

        public bool TryMarkAsked(Day2InquiryNpc npc, int sortOrder)
        {
            if (!CanAsk(npc, sortOrder))
                return false;

            if (sortOrder == 5 && npc == Day2InquiryNpc.Jiro)
                _dangoAskedByJiro = true;
            else if (sortOrder == 5 && npc == Day2InquiryNpc.Mizuki)
                _dangoAskedByMizuki = true;
            else if (sortOrder == 15)
                _paintingInquiryStarted = true;
            else
                _askedItemIds.Add(sortOrder);

            OnProgressChanged?.Invoke();
            return true;
        }

        public void ReceivePainting()
        {
            if (_paintingReceived) return;

            _paintingInquiryStarted = true;
            _paintingReceived = true;
            InspirationManager.Instance.CollectItem(15);
            OnProgressChanged?.Invoke();
        }

        public void CompleteYujiFestivalTopic()
        {
            if (_yujiFestivalTopicComplete) return;

            _yujiFestivalTopicComplete = true;
            OnProgressChanged?.Invoke();
        }

        public void CompleteJunkoLastTrainTopic()
        {
            if (_junkoLastTrainTopicComplete) return;

            _junkoLastTrainTopicComplete = true;
            OnProgressChanged?.Invoke();
        }

        public void CompleteJiroStationTopic()
        {
            if (_jiroStationTopicComplete) return;

            _jiroStationTopicComplete = true;
            OnProgressChanged?.Invoke();
        }

        public void CompleteJiroFestivalTopic()
        {
            if (_jiroFestivalTopicComplete) return;

            _jiroFestivalTopicComplete = true;
            OnProgressChanged?.Invoke();
        }

        public void CompleteMizukiFestivalTopic()
        {
            if (_mizukiFestivalTopicComplete) return;

            _mizukiFestivalTopicComplete = true;
            OnProgressChanged?.Invoke();
        }

        public void RequestDay2MapSpawn(string objectName, Vector3 offset)
        {
            _requestedMapSpawnObjectName = objectName;
            _requestedMapSpawnOffset = offset;
        }

        public bool AreRintaroInquiryItemsAsked =>
            _askedItemIds.Contains(1)
            && _askedItemIds.Contains(2)
            && _askedItemIds.Contains(12);

        public bool CanReceivePainting =>
            _mizukiFestivalTopicComplete
            && _dangoAskedByMizuki
            && _askedItemIds.Contains(4)
            && _askedItemIds.Contains(13);

        public bool AreAllInquiryItemsAsked =>
            _freeExplorationUnlocked
            && _askedItemIds.Contains(1)
            && _askedItemIds.Contains(2)
            && _askedItemIds.Contains(4)
            && _askedItemIds.Contains(12)
            && _askedItemIds.Contains(13)
            && _askedItemIds.Contains(14)
            && _dangoAskedByJiro
            && _dangoAskedByMizuki
            && _paintingInquiryStarted
            && _paintingReceived;

        public bool TryConsumeAllInquiryThought()
        {
            if (!AreAllInquiryItemsAsked || _allInquiryThoughtShown)
                return false;

            _allInquiryThoughtShown = true;
            return true;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            InitializeIfDay2ExplorationScene(scene.name);
            ApplyRequestedDay2MapSpawn(scene);
        }

        private void InitializeIfDay2ExplorationScene(string sceneName)
        {
            if (IsDay2ExplorationScene(sceneName))
                InitializeDay2Afternoon();
        }

        private void ApplyRequestedDay2MapSpawn(Scene scene)
        {
            if (scene.name != "Day2World" || string.IsNullOrEmpty(_requestedMapSpawnObjectName))
                return;

            string spawnObjectName = _requestedMapSpawnObjectName;
            Vector3 spawnOffset = _requestedMapSpawnOffset;
            _requestedMapSpawnObjectName = null;
            _requestedMapSpawnOffset = Vector3.zero;

            var spawnObject = GameObject.Find(spawnObjectName);
            var player = GameObject.FindGameObjectWithTag("Player");
            if (spawnObject == null || player == null)
            {
                Debug.LogWarning(
                    $"[Day2InquiryProgress] Could not apply map spawn '{spawnObjectName}'.");
                return;
            }

            Vector3 spawnPosition = spawnObject.transform.position + spawnOffset;
            player.transform.position = spawnPosition;

            var rigidbody = player.GetComponent<Rigidbody2D>();
            if (rigidbody == null) return;

            rigidbody.position = spawnPosition;
            rigidbody.linearVelocity = Vector2.zero;
            rigidbody.angularVelocity = 0f;
        }
    }
}
