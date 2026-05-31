using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Otowa.Inquiry
{
    public enum Day1InquiryNpc
    {
        None,
        Mizuki,
        Yuji,
        Junko,
    }

    /// <summary>
    /// Persistent progress for the Day 1 night inquiry loop.
    /// This state is intentionally separate from ExhibitionScene data.
    /// </summary>
    public class Day1InquiryProgress : MonoBehaviour
    {
        private static readonly Dictionary<int, Day1InquiryNpc> InquiryNpcByItem = new()
        {
            { 3, Day1InquiryNpc.Mizuki }, // Mineral Ore
            { 7, Day1InquiryNpc.Yuji },   // Sake
            { 8, Day1InquiryNpc.Yuji },   // Herbs
            { 9, Day1InquiryNpc.Junko },  // Fan
            { 10, Day1InquiryNpc.Yuji },  // Fireworks
            { 11, Day1InquiryNpc.Junko }, // Train Ticket
        };

        private static Day1InquiryProgress _instance;

        private readonly HashSet<int> _askedItemIds = new();
        private readonly HashSet<Day1InquiryNpc> _introducedNpcs = new();
        private bool _day1NightInitialized;
        private bool _amuletReceived;
        private bool _mizukiCityTopicComplete;
        private bool _mizukiFestivalTopicComplete;
        private bool _objectivePromptShown;
        private bool _allInquiryThoughtShown;
        private string _requestedMapSpawnObjectName;
        private Vector3 _requestedMapSpawnOffset;

        public static event Action OnProgressChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInitialized()
        {
            _ = Instance;
        }

        public static Day1InquiryProgress Instance
        {
            get
            {
                if (_instance != null) return _instance;

                var gameObject = new GameObject("Day1InquiryProgress");
                _instance = gameObject.AddComponent<Day1InquiryProgress>();
                return _instance;
            }
        }

        public bool IsDay1NightInitialized => _day1NightInitialized;
        public bool AreAllInquiryItemsAsked => _day1NightInitialized
                                               && _askedItemIds.Count >= InquiryNpcByItem.Count;
        public bool HasReceivedAmulet => _amuletReceived;
        public bool IsMizukiCityTopicComplete => _mizukiCityTopicComplete;
        public bool IsMizukiFestivalTopicComplete => _mizukiFestivalTopicComplete;
        public bool AreMizukiTopicsComplete => _mizukiCityTopicComplete
                                               && _mizukiFestivalTopicComplete;

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
            InitializeIfDay1ExplorationScene(SceneManager.GetActiveScene().name);
        }

        public void InitializeDay1Night()
        {
            if (_day1NightInitialized) return;

            _day1NightInitialized = true;
            OnProgressChanged?.Invoke();
        }

        public Day1InquiryNpc GetInquiryNpc(int sortOrder)
        {
            return InquiryNpcByItem.TryGetValue(sortOrder, out var npc)
                ? npc
                : Day1InquiryNpc.None;
        }

        public bool IsItemRevealed(int sortOrder)
        {
            if (!_day1NightInitialized) return false;
            return InquiryNpcByItem.ContainsKey(sortOrder) || (sortOrder == 4 && _amuletReceived);
        }

        public bool IsInquiryPending(int sortOrder)
        {
            return _day1NightInitialized
                   && InquiryNpcByItem.ContainsKey(sortOrder)
                   && !_askedItemIds.Contains(sortOrder);
        }

        public bool CanAsk(Day1InquiryNpc npc, int sortOrder)
        {
            return npc != Day1InquiryNpc.None
                   && GetInquiryNpc(sortOrder) == npc
                   && IsInquiryPending(sortOrder);
        }

        public bool HasPendingInquiry(Day1InquiryNpc npc)
        {
            if (!_day1NightInitialized || npc == Day1InquiryNpc.None)
                return false;

            foreach (var pair in InquiryNpcByItem)
            {
                if (pair.Value == npc && !_askedItemIds.Contains(pair.Key))
                    return true;
            }

            return false;
        }

        public bool IsNpcIntroduced(Day1InquiryNpc npc)
        {
            return _introducedNpcs.Contains(npc);
        }

        public void MarkNpcIntroduced(Day1InquiryNpc npc)
        {
            if (npc != Day1InquiryNpc.None)
                _introducedNpcs.Add(npc);
        }

        public bool TryMarkAsked(Day1InquiryNpc npc, int sortOrder)
        {
            if (!CanAsk(npc, sortOrder)) return false;

            _askedItemIds.Add(sortOrder);
            OnProgressChanged?.Invoke();
            return true;
        }

        public void ReceiveAmulet()
        {
            if (_amuletReceived) return;

            _amuletReceived = true;
            OnProgressChanged?.Invoke();
        }

        public void CompleteMizukiCityTopic()
        {
            if (_mizukiCityTopicComplete) return;

            _mizukiCityTopicComplete = true;
            OnProgressChanged?.Invoke();
        }

        public void CompleteMizukiFestivalTopic()
        {
            if (_mizukiFestivalTopicComplete) return;

            _mizukiFestivalTopicComplete = true;
            OnProgressChanged?.Invoke();
        }

        public void RequestDay1MapSpawn(string objectName, Vector3 offset)
        {
            _requestedMapSpawnObjectName = objectName;
            _requestedMapSpawnOffset = offset;
        }

        public bool TryConsumeObjectivePrompt()
        {
            if (_objectivePromptShown) return false;

            _objectivePromptShown = true;
            return true;
        }

        public bool TryConsumeAllInquiryThought()
        {
            if (!AreAllInquiryItemsAsked || _allInquiryThoughtShown) return false;

            _allInquiryThoughtShown = true;
            return true;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            InitializeIfDay1ExplorationScene(scene.name);
            ApplyRequestedDay1MapSpawn(scene);
        }

        private void InitializeIfDay1ExplorationScene(string sceneName)
        {
            if (sceneName == "Day1World" || sceneName == "HotSpring")
                InitializeDay1Night();
        }

        private void ApplyRequestedDay1MapSpawn(Scene scene)
        {
            if (scene.name != "Day1World" || string.IsNullOrEmpty(_requestedMapSpawnObjectName))
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
                    $"[Day1InquiryProgress] Could not apply map spawn '{spawnObjectName}'.");
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
