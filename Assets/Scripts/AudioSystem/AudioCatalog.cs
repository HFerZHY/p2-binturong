using System;
using System.Collections.Generic;
using UnityEngine;

namespace Otowa.Audio
{
    [CreateAssetMenu(fileName = "GameAudioCatalog", menuName = "Otowa/Audio/Game Audio Catalog")]
    public sealed class AudioCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public AudioId Id;
            public AudioClip Clip;

            [Range(0f, 1f)]
            public float DefaultVolume = 1f;
        }

        [SerializeField] private List<Entry> _entries = new();

        private readonly Dictionary<AudioId, Entry> _entriesById = new();

        private void OnEnable()
        {
            RebuildLookup();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RebuildLookup();
        }

        public void SetEntries(IEnumerable<Entry> entries)
        {
            _entries = new List<Entry>(entries);
            RebuildLookup();
        }
#endif

        public bool TryGet(AudioId id, out AudioClip clip, out float defaultVolume)
        {
            if (_entriesById.TryGetValue(id, out var entry) && entry.Clip != null)
            {
                clip = entry.Clip;
                defaultVolume = entry.DefaultVolume;
                return true;
            }

            clip = null;
            defaultVolume = 0f;
            return false;
        }

        private void RebuildLookup()
        {
            _entriesById.Clear();

            foreach (var entry in _entries)
            {
                if (entry == null || entry.Id == AudioId.None)
                {
                    continue;
                }

                _entriesById[entry.Id] = entry;
            }
        }
    }
}
