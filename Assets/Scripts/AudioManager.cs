using System;
using System.Collections.Generic;
using UnityEngine;

public enum SoundClip{
    popSound,
    clickSound,
    WarningSound
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    AudioSource bgmSource;
    AudioSource sfxSource;

    Dictionary<SoundClip, AudioClip> clips = new();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        bgmSource = GetComponent<AudioSource>();
        sfxSource = GetComponent<AudioSource>();

        LoadAudioClip();
    }

    private void LoadAudioClip()
    {
        foreach(SoundClip s in Enum.GetValues(typeof(SoundClip)))
        {
            AudioClip clip = Resources.Load<AudioClip>($"Sounds/{s.ToString()}");

            if (clip != null) {
                clips.Add(s, clip);
            }
            else
            {
                Debug.Log($"{s} 라는 이름의 clip이 존재하지 않습니다.");
            }
        }
    }

    public void BgmPlay(AudioClip clip, bool loopCheck)
    {
        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.loop = loopCheck;
        bgmSource.Play();
    }

    public void PlayOneShot(SoundClip clip, float pitch)
    {
        foreach(var s in clips)
        {
            if (s.Key == clip)
            {
                sfxSource.clip = s.Value;
                sfxSource.pitch = pitch;
                sfxSource.PlayOneShot(s.Value);
            }
            else
            {
                Debug.Log($"{s}clip의 재생에 실패하였습니다.");
            }
        }
    }
}
