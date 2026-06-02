using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Otowa.Audio
{
    public readonly struct SfxLoopHandle
    {
        internal SfxLoopHandle(int value)
        {
            Value = value;
        }

        internal int Value { get; }
        public bool IsValid => Value != 0;
    }

    public sealed class GameAudioManager : MonoBehaviour
    {
        private const string CATALOG_RESOURCE_PATH = "Audio/GameAudioCatalog";

        private enum Bus
        {
            Master,
            Bgm,
            Sfx
        }

        private sealed class BgmChannel
        {
            public AudioSource Source;
            public AudioId Id;
            public float Volume;
            public int FadeVersion;
        }

        private sealed class SfxVoice
        {
            public AudioSource Source;
            public AudioId Id;
            public float Volume;
            public int FadeVersion;
            public int HandleId;
            public bool IsLoop;
            public bool IsDefaultLoop;
        }

        private static GameAudioManager _instance;

        private AudioCatalog _catalog;
        private BgmChannel _bgmA;
        private BgmChannel _bgmB;
        private BgmChannel _currentBgm;
        private readonly Stack<AudioSource> _availableSfxSources = new();
        private readonly List<SfxVoice> _oneShotVoices = new();
        private readonly Dictionary<int, SfxVoice> _loopVoicesByHandle = new();
        private readonly Dictionary<AudioId, SfxVoice> _defaultLoopVoicesById = new();
        private readonly Dictionary<AudioId, float> _savedBgmPlaybackTimes = new();
        private readonly HashSet<AudioId> _warnedMissingIds = new();
        private int _nextLoopHandle = 1;
        private int _masterFadeVersion;
        private int _bgmBusFadeVersion;
        private int _sfxBusFadeVersion;
        private float _masterVolume = 1f;
        private float _bgmBusVolume = 1f;
        private float _sfxBusVolume = 1f;

        public static GameAudioManager Instance
        {
            get
            {
                return EnsureInstance();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
        }

        private static GameAudioManager EnsureInstance()
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = FindFirstObjectByType<GameAudioManager>(FindObjectsInactive.Include);
            if (_instance != null)
            {
                return _instance;
            }

            var gameObject = new GameObject(nameof(GameAudioManager));
            _instance = gameObject.AddComponent<GameAudioManager>();
            return _instance;
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
            _catalog = Resources.Load<AudioCatalog>(CATALOG_RESOURCE_PATH);
            _bgmA = CreateBgmChannel("BGM A");
            _bgmB = CreateBgmChannel("BGM B");

            if (_catalog == null)
            {
                Debug.LogError(
                    $"Missing Resources/{CATALOG_RESOURCE_PATH}.asset. " +
                    "Run Tools/Otowa/Audio/Rebuild Game Audio Catalog.");
            }
        }

        private void Update()
        {
            ApplyVolumes();

            for (var index = _oneShotVoices.Count - 1; index >= 0; index--)
            {
                var voice = _oneShotVoices[index];
                if (!voice.Source.isPlaying)
                {
                    _oneShotVoices.RemoveAt(index);
                    ReleaseSource(voice.Source);
                }
            }
        }

        public void PlayBgm(
            AudioId id,
            float volume = -1f,
            float fadeIn = 0f,
            bool restart = false,
            bool resumePlayback = false)
        {
            if (!TryResolve(id, volume, out var clip, out var targetVolume))
            {
                return;
            }

            if (_currentBgm != null &&
                _currentBgm.Id == id &&
                _currentBgm.Source.isPlaying &&
                !restart)
            {
                FadeBgmChannelTo(_currentBgm, targetVolume, fadeIn, stopAfterFade: false);
                return;
            }

            var previousChannel = _currentBgm;
            var nextChannel = previousChannel == _bgmA ? _bgmB : _bgmA;
            StopBgmChannel(nextChannel);
            nextChannel.Source.clip = clip;
            nextChannel.Source.loop = true;
            nextChannel.Source.time = resumePlayback
                ? GetSavedBgmPlaybackTime(id, clip.length)
                : 0f;
            nextChannel.Id = id;
            nextChannel.Volume = fadeIn > 0f ? 0f : targetVolume;
            nextChannel.Source.Play();
            _currentBgm = nextChannel;

            if (previousChannel != null && previousChannel.Source.isPlaying)
            {
                FadeBgmChannelTo(previousChannel, 0f, fadeIn, stopAfterFade: true);
            }

            FadeBgmChannelTo(nextChannel, targetVolume, fadeIn, stopAfterFade: false);
        }

        public void CrossFadeBgm(AudioId id, float duration, float volume = -1f)
        {
            PlayBgm(id, volume, duration);
        }

        public void FadeBgmTo(float volume, float duration)
        {
            if (_currentBgm != null)
            {
                FadeBgmChannelTo(_currentBgm, Mathf.Clamp01(volume), duration, stopAfterFade: false);
            }
        }

        public void StopBgm(float fadeOut = 0f, bool savePosition = false)
        {
            FadeBgmChannelTo(_bgmA, 0f, fadeOut, stopAfterFade: true, savePosition);
            FadeBgmChannelTo(_bgmB, 0f, fadeOut, stopAfterFade: true, savePosition);
            _currentBgm = null;
        }

        public void PlaySfxOnce(AudioId id, float volume = -1f)
        {
            if (!TryResolve(id, volume, out var clip, out var targetVolume))
            {
                return;
            }

            var source = AcquireSource();
            source.clip = clip;
            source.loop = false;

            var voice = new SfxVoice
            {
                Source = source,
                Id = id,
                Volume = targetVolume
            };

            _oneShotVoices.Add(voice);
            source.Play();
        }

        public SfxLoopHandle PlaySfxLoop(
            AudioId id,
            float volume = -1f,
            float fadeIn = 0f,
            bool restart = false,
            bool allowDuplicate = false)
        {
            if (!TryResolve(id, volume, out var clip, out var targetVolume))
            {
                return default;
            }

            if (!allowDuplicate && _defaultLoopVoicesById.TryGetValue(id, out var existingVoice))
            {
                if (restart)
                {
                    ReleaseLoopVoice(existingVoice);
                }
                else
                {
                    FadeSfxVoiceTo(existingVoice, targetVolume, fadeIn, stopAfterFade: false);
                    return new SfxLoopHandle(existingVoice.HandleId);
                }
            }

            var source = AcquireSource();
            source.clip = clip;
            source.loop = true;

            var voice = new SfxVoice
            {
                Source = source,
                Id = id,
                Volume = fadeIn > 0f ? 0f : targetVolume,
                HandleId = _nextLoopHandle++,
                IsLoop = true,
                IsDefaultLoop = !allowDuplicate
            };

            _loopVoicesByHandle.Add(voice.HandleId, voice);
            if (voice.IsDefaultLoop)
            {
                _defaultLoopVoicesById[id] = voice;
            }

            source.Play();
            FadeSfxVoiceTo(voice, targetVolume, fadeIn, stopAfterFade: false);
            return new SfxLoopHandle(voice.HandleId);
        }

        public void FadeSfxLoopTo(AudioId id, float volume, float duration)
        {
            foreach (var voice in new List<SfxVoice>(_loopVoicesByHandle.Values))
            {
                if (voice.Id == id)
                {
                    FadeSfxVoiceTo(voice, Mathf.Clamp01(volume), duration, stopAfterFade: false);
                }
            }
        }

        public void StopSfxLoop(AudioId id, float fadeOut = 0f)
        {
            foreach (var voice in new List<SfxVoice>(_loopVoicesByHandle.Values))
            {
                if (voice.Id == id)
                {
                    FadeSfxVoiceTo(voice, 0f, fadeOut, stopAfterFade: true);
                }
            }
        }

        public void StopSfxLoop(SfxLoopHandle handle, float fadeOut = 0f)
        {
            if (_loopVoicesByHandle.TryGetValue(handle.Value, out var voice))
            {
                FadeSfxVoiceTo(voice, 0f, fadeOut, stopAfterFade: true);
            }
        }

        public void StopAllSfx(float loopFadeOut = 0f)
        {
            foreach (var voice in new List<SfxVoice>(_oneShotVoices))
            {
                _oneShotVoices.Remove(voice);
                ReleaseSource(voice.Source);
            }

            foreach (var voice in new List<SfxVoice>(_loopVoicesByHandle.Values))
            {
                FadeSfxVoiceTo(voice, 0f, loopFadeOut, stopAfterFade: true);
            }
        }

        public void SetMasterVolume(float volume, float duration = 0f)
        {
            FadeBusTo(Bus.Master, volume, duration);
        }

        public void SetBgmBusVolume(float volume, float duration = 0f)
        {
            FadeBusTo(Bus.Bgm, volume, duration);
        }

        public void SetSfxBusVolume(float volume, float duration = 0f)
        {
            FadeBusTo(Bus.Sfx, volume, duration);
        }

        private BgmChannel CreateBgmChannel(string channelName)
        {
            return new BgmChannel
            {
                Source = CreateSource(channelName)
            };
        }

        private AudioSource CreateSource(string sourceName)
        {
            var sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform);

            var source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        private AudioSource AcquireSource()
        {
            if (_availableSfxSources.Count > 0)
            {
                return _availableSfxSources.Pop();
            }

            return CreateSource($"SFX {_oneShotVoices.Count + _loopVoicesByHandle.Count + 1}");
        }

        private void ReleaseSource(AudioSource source)
        {
            source.Stop();
            source.clip = null;
            source.loop = false;
            _availableSfxSources.Push(source);
        }

        private bool TryResolve(AudioId id, float requestedVolume, out AudioClip clip, out float volume)
        {
            if (id != AudioId.None &&
                _catalog != null &&
                _catalog.TryGet(id, out clip, out var defaultVolume))
            {
                volume = requestedVolume >= 0f ? Mathf.Clamp01(requestedVolume) : defaultVolume;
                return true;
            }

            clip = null;
            volume = 0f;

            if (id != AudioId.None && _warnedMissingIds.Add(id))
            {
                Debug.LogWarning($"Audio clip is not configured for {id}.");
            }

            return false;
        }

        private void ApplyVolumes()
        {
            ApplyBgmVolume(_bgmA);
            ApplyBgmVolume(_bgmB);

            foreach (var voice in _oneShotVoices)
            {
                voice.Source.volume = voice.Volume * _sfxBusVolume * _masterVolume;
            }

            foreach (var voice in _loopVoicesByHandle.Values)
            {
                voice.Source.volume = voice.Volume * _sfxBusVolume * _masterVolume;
            }
        }

        private void ApplyBgmVolume(BgmChannel channel)
        {
            if (channel != null)
            {
                channel.Source.volume = channel.Volume * _bgmBusVolume * _masterVolume;
            }
        }

        private void FadeBgmChannelTo(
            BgmChannel channel,
            float volume,
            float duration,
            bool stopAfterFade,
            bool savePosition = false)
        {
            if (channel == null)
            {
                return;
            }

            if (stopAfterFade && savePosition)
            {
                SaveBgmPlaybackTime(channel);
            }

            channel.FadeVersion++;
            if (duration <= 0f)
            {
                channel.Volume = volume;
                if (stopAfterFade)
                {
                    StopBgmChannel(channel);
                }

                return;
            }

            StartCoroutine(FadeBgmChannel(channel, volume, duration, stopAfterFade, channel.FadeVersion));
        }

        private IEnumerator FadeBgmChannel(
            BgmChannel channel,
            float targetVolume,
            float duration,
            bool stopAfterFade,
            int version)
        {
            var elapsed = 0f;
            var startVolume = channel.Volume;

            while (elapsed < duration && channel.FadeVersion == version)
            {
                elapsed += Time.unscaledDeltaTime;
                channel.Volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }

            if (channel.FadeVersion != version)
            {
                yield break;
            }

            channel.Volume = targetVolume;
            if (stopAfterFade)
            {
                StopBgmChannel(channel);
            }
        }

        private void StopBgmChannel(BgmChannel channel)
        {
            channel.FadeVersion++;
            channel.Source.Stop();
            channel.Source.clip = null;
            channel.Id = AudioId.None;
            channel.Volume = 0f;
        }

        private void SaveBgmPlaybackTime(BgmChannel channel)
        {
            if (channel.Id == AudioId.None || channel.Source.clip == null)
            {
                return;
            }

            _savedBgmPlaybackTimes[channel.Id] = channel.Source.time;
        }

        private float GetSavedBgmPlaybackTime(AudioId id, float clipLength)
        {
            if (!_savedBgmPlaybackTimes.TryGetValue(id, out var playbackTime) || clipLength <= 0f)
            {
                return 0f;
            }

            return Mathf.Repeat(playbackTime, clipLength);
        }

        private void FadeSfxVoiceTo(SfxVoice voice, float volume, float duration, bool stopAfterFade)
        {
            voice.FadeVersion++;
            if (duration <= 0f)
            {
                voice.Volume = volume;
                if (stopAfterFade)
                {
                    ReleaseLoopVoice(voice);
                }

                return;
            }

            StartCoroutine(FadeSfxVoice(voice, volume, duration, stopAfterFade, voice.FadeVersion));
        }

        private IEnumerator FadeSfxVoice(
            SfxVoice voice,
            float targetVolume,
            float duration,
            bool stopAfterFade,
            int version)
        {
            var elapsed = 0f;
            var startVolume = voice.Volume;

            while (elapsed < duration && voice.FadeVersion == version)
            {
                elapsed += Time.unscaledDeltaTime;
                voice.Volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }

            if (voice.FadeVersion != version)
            {
                yield break;
            }

            voice.Volume = targetVolume;
            if (stopAfterFade)
            {
                ReleaseLoopVoice(voice);
            }
        }

        private void ReleaseLoopVoice(SfxVoice voice)
        {
            voice.FadeVersion++;
            _loopVoicesByHandle.Remove(voice.HandleId);

            if (voice.IsDefaultLoop &&
                _defaultLoopVoicesById.TryGetValue(voice.Id, out var existingVoice) &&
                existingVoice == voice)
            {
                _defaultLoopVoicesById.Remove(voice.Id);
            }

            ReleaseSource(voice.Source);
        }

        private void FadeBusTo(Bus bus, float volume, float duration)
        {
            volume = Mathf.Clamp01(volume);
            var version = IncrementBusFadeVersion(bus);

            if (duration <= 0f)
            {
                SetBusVolume(bus, volume);
                return;
            }

            StartCoroutine(FadeBus(bus, volume, duration, version));
        }

        private IEnumerator FadeBus(Bus bus, float targetVolume, float duration, int version)
        {
            var elapsed = 0f;
            var startVolume = GetBusVolume(bus);

            while (elapsed < duration && GetBusFadeVersion(bus) == version)
            {
                elapsed += Time.unscaledDeltaTime;
                SetBusVolume(bus, Mathf.Lerp(startVolume, targetVolume, elapsed / duration));
                yield return null;
            }

            if (GetBusFadeVersion(bus) == version)
            {
                SetBusVolume(bus, targetVolume);
            }
        }

        private int IncrementBusFadeVersion(Bus bus)
        {
            switch (bus)
            {
                case Bus.Master:
                    return ++_masterFadeVersion;
                case Bus.Bgm:
                    return ++_bgmBusFadeVersion;
                default:
                    return ++_sfxBusFadeVersion;
            }
        }

        private int GetBusFadeVersion(Bus bus)
        {
            switch (bus)
            {
                case Bus.Master:
                    return _masterFadeVersion;
                case Bus.Bgm:
                    return _bgmBusFadeVersion;
                default:
                    return _sfxBusFadeVersion;
            }
        }

        private float GetBusVolume(Bus bus)
        {
            switch (bus)
            {
                case Bus.Master:
                    return _masterVolume;
                case Bus.Bgm:
                    return _bgmBusVolume;
                default:
                    return _sfxBusVolume;
            }
        }

        private void SetBusVolume(Bus bus, float volume)
        {
            switch (bus)
            {
                case Bus.Master:
                    _masterVolume = volume;
                    break;
                case Bus.Bgm:
                    _bgmBusVolume = volume;
                    break;
                default:
                    _sfxBusVolume = volume;
                    break;
            }
        }
    }
}
