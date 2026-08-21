using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SHIN
{
    public enum SoundType
    {
        BGM,
        SE
    }

    public class SoundManager : ManagerBase
    {
        /// <summary>
        /// Unity 기본 Maximum Real Voices(보통 32)보다 약간 작게.
        /// BGM 1채널 여유를 남긴다.
        /// </summary>
        private const int SeVoiceMargin = 2;

        [SerializeField] private int _maxSePlayCount = 30;

        private readonly Dictionary<string, AudioClip> _clipCache = new();
        private readonly Dictionary<string, Task<AudioClip>> _loadingTasks = new();
        private readonly List<AudioSource> _seSources = new();

        private AudioSource _bgmSource;
        private Transform _seRoot;
        private int _seCursor;

        private void Awake()
        {
            EnsureAudioSetup();
        }

        public void Play(string address, SoundType type)
        {
            if (string.IsNullOrWhiteSpace(address))
                return;

            address = address.Trim();
            EnsureAudioSetup();

            // 캐시 히트는 await 없이 즉시 재생 (버튼 입력 지연 방지)
            if (_clipCache.TryGetValue(address, out var cached) && cached != null)
            {
                PlayClip(cached, type);
                return;
            }

            _ = PlayInternalAsync(address, type);
        }

        /// <summary>보이스/효과음 등 SE 재생. address는 Addressables 주소.</summary>
        public void PlaySe(string address)
        {
            Play(address, SoundType.SE);
        }

        public void PlayBgm(string address)
        {
            Play(address, SoundType.BGM);
        }

        /// <summary>Addressables에서 미리 로드해 SoundManager 캐시에 넣는다.</summary>
        public async Task PreloadAsync(IEnumerable<string> addresses)
        {
            if (addresses == null)
                return;

            EnsureAudioSetup();
            var tasks = new List<Task>();
            foreach (var raw in addresses)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                var address = raw.Trim();
                if (_clipCache.ContainsKey(address))
                    continue;

                tasks.Add(GetOrLoadClipAsync(address));
            }

            if (tasks.Count == 0)
                return;

            await Task.WhenAll(tasks);
        }

        public void StopBgm()
        {
            if (_bgmSource == null)
                return;

            _bgmSource.Stop();
            _bgmSource.clip = null;
        }

        public void StopAllSe()
        {
            for (var i = 0; i < _seSources.Count; i++)
            {
                var source = _seSources[i];
                if (source == null)
                    continue;

                source.Stop();
                source.clip = null;
            }
        }

        private async Task PlayInternalAsync(string address, SoundType type)
        {
            EnsureAudioSetup();

            var clip = await GetOrLoadClipAsync(address);
            if (this == null || clip == null)
                return;

            PlayClip(clip, type);
        }

        private void PlayClip(AudioClip clip, SoundType type)
        {
            if (type == SoundType.BGM)
            {
                PlayBgm(clip);
                return;
            }

            PlaySe(clip);
        }

        private async Task<AudioClip> GetOrLoadClipAsync(string address)
        {
            if (_clipCache.TryGetValue(address, out var cached) && cached != null)
                return cached;

            if (_loadingTasks.TryGetValue(address, out var inflight))
                return await inflight;

            var loadTask = LoadClipAsync(address);
            _loadingTasks[address] = loadTask;
            try
            {
                return await loadTask;
            }
            finally
            {
                _loadingTasks.Remove(address);
            }
        }

        private async Task<AudioClip> LoadClipAsync(string address)
        {
            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null)
            {
                Debug.LogError("[SoundManager] ResourceManager가 없습니다.");
                return null;
            }

            var clip = await resourceManager.LoadAsync<AudioClip>(address);
            if (clip == null)
                return null;

            _clipCache[address] = clip;
            return clip;
        }

        private void PlayBgm(AudioClip clip)
        {
            if (_bgmSource.clip == clip && _bgmSource.isPlaying)
                return;

            _bgmSource.clip = clip;
            _bgmSource.loop = true;
            _bgmSource.Play();
        }

        private void PlaySe(AudioClip clip)
        {
            var source = GetAvailableSeSource();
            if (source == null)
                return;

            // PlayOneShot: 같은 소스 재사용 시 끊김/할당 오버헤드를 줄임
            source.PlayOneShot(clip);
        }

        private AudioSource GetAvailableSeSource()
        {
            if (_seSources.Count == 0)
                return null;

            for (var i = 0; i < _seSources.Count; i++)
            {
                var index = (_seCursor + i) % _seSources.Count;
                var source = _seSources[index];
                if (source == null)
                    continue;

                if (!source.isPlaying)
                {
                    _seCursor = (index + 1) % _seSources.Count;
                    return source;
                }
            }

            // 전부 재생 중이면 가장 오래된(커서) 소스를 재사용
            var fallback = _seSources[_seCursor];
            _seCursor = (_seCursor + 1) % _seSources.Count;
            return fallback;
        }

        private void EnsureAudioSetup()
        {
            if (_bgmSource == null)
            {
                var bgmGo = new GameObject("BGM");
                bgmGo.transform.SetParent(transform, false);
                _bgmSource = bgmGo.AddComponent<AudioSource>();
                _bgmSource.playOnAwake = false;
                _bgmSource.loop = true;
                _bgmSource.spatialBlend = 0f;
            }

            var realVoices = AudioSettings.GetConfiguration().numRealVoices;
            var maxSe = Mathf.Clamp(_maxSePlayCount, 1, Mathf.Max(1, realVoices - SeVoiceMargin));
            if (_maxSePlayCount != maxSe)
                _maxSePlayCount = maxSe;

            if (_seRoot == null)
            {
                var seRootGo = new GameObject("SE");
                seRootGo.transform.SetParent(transform, false);
                _seRoot = seRootGo.transform;
            }

            while (_seSources.Count < _maxSePlayCount)
            {
                var seGo = new GameObject($"SE_{_seSources.Count:00}");
                seGo.transform.SetParent(_seRoot, false);
                var source = seGo.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                _seSources.Add(source);
            }

            while (_seSources.Count > _maxSePlayCount)
            {
                var last = _seSources[^1];
                _seSources.RemoveAt(_seSources.Count - 1);
                if (last != null)
                    Destroy(last.gameObject);
            }
        }
    }
}
