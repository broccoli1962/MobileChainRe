using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance; 
    AudioSource audioSource;

    private void Awake()
    {
        if (instance==null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        audioSource = GetComponent<AudioSource>();
    }

    public void PlayOneShot(AudioClip clip)
    {
        if (clip == null) return;
        audioSource.PlayOneShot(clip);
    }
}
