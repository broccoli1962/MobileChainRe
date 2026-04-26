using Backend.Object.Management;
using Backend.Util.Management;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum SoundClip
{
    popSound,
    clickSound,
    WarningSound
}

public class AudioManager : SingletonGameObject<AudioManager>
{
    private AudioSource bgmSource;
    private AudioSource sfxSource;

    private Dictionary<SoundClip, AudioClip> _clips = new();

    protected override void OnAwake()
    {
        base.OnAwake();

        bgmSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();

        LoadAudioClip().Forget();
    }

    private async UniTaskVoid LoadAudioClip()
    {
        foreach (SoundClip s in Enum.GetValues(typeof(SoundClip)))
        {
            AudioClip clip = await ResourceManager.LoadResourceAsync<AudioClip>(s.ToString());

            if (clip != null)
            {
                _clips.Add(s, clip);
            }
            else
            {
                Debug.Log($"Not Found Clip : {s}");
            }
        }
    }

    #region #Internal Method
    private void PlayBgm_Internal(AudioClip clip, bool loopCheck)
    {
        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.loop = loopCheck;
        bgmSource.Play();
    }
    private void PlaySfx_Internal(SoundClip clip, float pitch)
    {
        if (_clips.TryGetValue(clip, out AudioClip audioClip))
        {
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(audioClip);
        }
        else
        {
            Debug.Log($"Fail PlaySfx soundClip : {clip}");
        }
    }
    #endregion

    #region #Static Method
    public static void PlayBgm(AudioClip clip, bool loopCheck) => Instance.PlayBgm_Internal(clip, loopCheck);
    public static void PlaySfx(SoundClip clip, float pitch) => Instance.PlaySfx_Internal(clip, pitch);
    #endregion
}
