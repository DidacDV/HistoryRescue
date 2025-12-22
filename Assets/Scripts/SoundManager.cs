using System.Diagnostics;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Level Sounds")]
    public AudioClip levelStart;
    public AudioClip levelFail;
    public AudioClip levelPass;

    private AudioSource audioSource;
    private AudioSource voiceSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            AudioSource[] sources = GetComponents<AudioSource>();

            if (sources.Length >= 2)
            {
                audioSource = sources[0];
                voiceSource = sources[1];
            }
            else
            {
                audioSource = GetComponent<AudioSource>();
                voiceSource = gameObject.AddComponent<AudioSource>();
                voiceSource.volume = 0.6f;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Play(AudioClip clip)
    {
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }

    public void PlayVoice(AudioClip clip)
    {
        if (clip == null) return;

        if (voiceSource != null && voiceSource.isPlaying)
            voiceSource.Stop();

        voiceSource.clip = clip;
        voiceSource.Play();
    }

    public void Stop()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        if (voiceSource != null)
        {
            voiceSource.Stop();
        }
    }
}
