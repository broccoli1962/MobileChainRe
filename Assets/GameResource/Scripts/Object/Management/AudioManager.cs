using System.Collections.Generic;
using System.Threading;
using Backend.Util.Management;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Backend.Object.Management
{
    public class AudioManager : SingletonGameObject<AudioManager>
    {
        private static readonly string _audioSourcePoolKey = "AudioSource";

        // PlayerPrefs 키
        private const string PREF_BGM_ENABLED = "AudioManager_BgmEnabled";
        private const string PREF_SFX_ENABLED = "AudioManager_SfxEnabled";

        // 로드된 클립을 캐싱할 딕셔너리
        private readonly Dictionary<string, AudioClip> _clips = new();

        // BGM 관련 변수
        private AudioSource _currentBgm;
        private string _currentBgmKey;
        private string _pendingBgmKey;
        private CancellationTokenSource _bgmCancellationTokenSource;
        private const float _fadeDuration = 0.5f;

        // 설정 프로퍼티
        public bool IsBgmEnabled
        {
            get => PlayerPrefs.GetInt(PREF_BGM_ENABLED, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(PREF_BGM_ENABLED, value ? 1 : 0);
                if (!value) StopBgmImmediate();
            }
        }

        public bool IsSfxEnabled
        {
            get => PlayerPrefs.GetInt(PREF_SFX_ENABLED, 1) == 1;
            set => PlayerPrefs.SetInt(PREF_SFX_ENABLED, value ? 1 : 0);
        }

        private void OnDisable()
        {
            _bgmCancellationTokenSource?.Cancel();
            _bgmCancellationTokenSource?.Dispose();
            _bgmCancellationTokenSource = null;
        }

        #region Resource Loading (Lazy Load)

        /// <summary>
        /// 캐시를 확인하고 없다면 어드레서블에서 비동기로 로드하여 반환합니다.
        /// </summary>
        private async UniTask<AudioClip> GetOrLoadClipAsync(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            // 1. 캐시에서 확인
            if (_clips.TryGetValue(key, out var cachedClip))
            {
                return cachedClip;
            }

            // 2. 캐시에 없으면 로드
            AudioClip clip = await ResourceManager.LoadResourceAsync<AudioClip>(key);

            if (clip != null)
            {
                _clips.TryAdd(key, clip);
            }
            else
            {
                Debug.LogWarning($"[AudioManager] Failed to load AudioClip from Addressables: {key}");
            }

            return clip;
        }

        #endregion

        #region SFX Internal Methods

        private async UniTaskVoid PlaySfx_InternalAsync(string key, float pitch = 1f)
        {
            if (!IsSfxEnabled) return;

            var audioClip = await GetOrLoadClipAsync(key);
            if (audioClip == null) return;

            var audioSource = GetOrCreateAudioSource();
            if (audioSource == null) return;

            audioSource.clip = audioClip;
            audioSource.playOnAwake = true;
            audioSource.loop = false;
            audioSource.volume = 1f;
            audioSource.pitch = pitch;
            audioSource.Play();

            ReturnToPoolAfterPlay(audioSource, audioClip.length).Forget();
        }

        private async UniTaskVoid PlaySfx_DelayAsync(string key, float delay, float pitch = 1f)
        {
            if (!IsSfxEnabled) return;

            // 클립을 먼저 로드해둠 (지연 시간 동안 로딩이 겹치지 않도록)
            var audioClip = await GetOrLoadClipAsync(key);
            if (audioClip == null) return;

            await UniTask.WaitForSeconds(delay);

            // 딜레이 중에 설정이 꺼졌을 수 있으므로 다시 체크
            if (!IsSfxEnabled) return;

            var audioSource = GetOrCreateAudioSource();
            if (audioSource == null) return;

            audioSource.clip = audioClip;
            audioSource.playOnAwake = true;
            audioSource.loop = false;
            audioSource.volume = 1f;
            audioSource.pitch = pitch;
            audioSource.Play();

            ReturnToPoolAfterPlay(audioSource, audioClip.length).Forget();
        }

        #endregion

        #region BGM Internal Methods

        private async UniTaskVoid PlayBgm_Internal(string key)
        {
            _pendingBgmKey = key;

            _bgmCancellationTokenSource?.Cancel();
            _bgmCancellationTokenSource?.Dispose();
            _bgmCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _bgmCancellationTokenSource.Token;

            if (!IsBgmEnabled)
            {
                StopBgmImmediate();
                _pendingBgmKey = null;
                return;
            }

            if (_currentBgmKey == key && _currentBgm != null && _currentBgm.isPlaying)
            {
                _pendingBgmKey = null;
                return;
            }

            // 이전 BGM 정지 및 풀 반환
            if (_currentBgm != null)
            {
                var oldBgm = _currentBgm;
                oldBgm.Stop();
                ReturnToPool(oldBgm);
                _currentBgm = null;
                _currentBgmKey = null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var audioClip = await GetOrLoadClipAsync(key);
            if (audioClip == null)
            {
                _pendingBgmKey = null;
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var newAudioSource = GetOrCreateAudioSource();
            if (newAudioSource == null) return;

            cancellationToken.ThrowIfCancellationRequested();

            // 비동기 로딩 중 다른 BGM 재생 요청이 들어왔는지 확인
            if (_pendingBgmKey != key)
            {
                ReturnToPool(newAudioSource);
                return;
            }

            newAudioSource.clip = audioClip;
            newAudioSource.playOnAwake = false;
            newAudioSource.loop = true;
            newAudioSource.volume = 0f;
            newAudioSource.pitch = 1f;
            newAudioSource.Play();

            _currentBgm = newAudioSource;
            _currentBgmKey = key;
            _pendingBgmKey = null;

            await FadeVolume(newAudioSource, 1f, _fadeDuration);
        }

        private void StopBgmImmediate()
        {
            _bgmCancellationTokenSource?.Cancel();
            _bgmCancellationTokenSource?.Dispose();
            _bgmCancellationTokenSource = null;

            if (_currentBgm != null)
            {
                _currentBgm.Stop();
                ReturnToPool(_currentBgm);
                _currentBgm = null;
                _currentBgmKey = null;
            }
        }

        private void StopBgm_Internal()
        {
            _bgmCancellationTokenSource?.Cancel();
            _bgmCancellationTokenSource?.Dispose();
            _bgmCancellationTokenSource = null;

            if (_currentBgm != null)
            {
                var oldBgm = _currentBgm;
                _currentBgm = null;
                _currentBgmKey = null;
                FadeOutAndReturn(oldBgm).Forget();
            }
        }

        private async UniTaskVoid FadeOutAndReturn(AudioSource source)
        {
            await FadeVolume(source, 0f, _fadeDuration);
            if (source != null)
            {
                source.Stop();
                ReturnToPool(source);
            }
        }

        private static async UniTask FadeVolume(AudioSource source, float targetVolume, float duration)
        {
            if (source == null || duration <= 0f)
            {
                if (source != null) source.volume = targetVolume;
                return;
            }

            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (source == null) return;
                source.volume = Mathf.Lerp(startVolume, targetVolume, t);
                await UniTask.Yield();
            }

            if (source != null) source.volume = targetVolume;
        }

        #endregion

        #region Pooling

        private AudioSource GetOrCreateAudioSource()
        {
            var audioSource = ObjectPoolManager.Get<AudioSource>(_audioSourcePoolKey);
            if (audioSource == null)
            {
                Debug.LogError($"[AudioManager] Failed to get AudioSource from PoolManager: {_audioSourcePoolKey}");
            }
            return audioSource;
        }

        private void ReturnToPool(AudioSource audioSource)
        {
            if (audioSource == null) return;

            audioSource.Stop();
            audioSource.clip = null;
            audioSource.volume = 1f;
            audioSource.pitch = 1f;

            ObjectPoolManager.Release(audioSource);
        }

        private async UniTaskVoid ReturnToPoolAfterPlay(AudioSource source, float duration)
        {
            await UniTask.Delay((int)(duration * 1000));
            if (source != null) ReturnToPool(source);
        }

        #endregion

        #region Static Public Methods

        /// <summary>
        /// BGM 재생 (페이드 인 적용)
        /// </summary>
        public static void PlayBgm(string key) => Instance.PlayBgm_Internal(key).Forget();

        /// <summary>
        /// BGM 정지 (페이드 아웃 적용)
        /// </summary>
        public static void StopBgm() => Instance.StopBgm_Internal();

        /// <summary>
        /// 사운드 효과음 재생
        /// </summary>
        public static void PlaySfx(string key, float pitch = 1f) => Instance.PlaySfx_InternalAsync(key, pitch).Forget();

        /// <summary>
        /// 딜레이 후 사운드 효과음 재생
        /// </summary>
        public static void PlaySfxDelay(string key, float delay, float pitch = 1f) => Instance.PlaySfx_DelayAsync(key, delay, pitch).Forget();

        #endregion
    }
}
