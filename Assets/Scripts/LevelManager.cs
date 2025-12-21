using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public string nextLevelScene; //name of next level scene
    public LevelTheme currentTheme; //NULL if the current level keeps the same font, otherwise the new font to be set in the UI

    public AudioClip backgroundMusic;
    public PlayerController playerCube;

    private BackgroundMusicController musicController;

    [Header("Animation Components")]
    public InitLevelAnimations initAnimation;
    public FailLevelAnimations failAnimation;
    public PassLevelAnimations passAnimation;

    [Header("Voice & Subtitles")]
    public AudioClip introVoiceClip;
    [TextArea(3, 10)]
    public string introFullText;

    void Awake()
    {
        musicController = UnityEngine.Object.FindAnyObjectByType<BackgroundMusicController>();

        if (musicController == null)
            UnityEngine.Debug.LogError("Still can't find BackgroundMusicController in the scene!");
    }

    void SetUpPlayerEvents()
    {
        if (playerCube != null)
        {
            playerCube.OnFellOff.AddListener(OnPlayerFailed);
            playerCube.OnReachedVictoryHole.AddListener(OnPlayerSucceeded);
        }
        else
            UnityEngine.Debug.LogError("player not set in level manager");
    }

    void PlayInitialAnimation()
    {
        UnityEngine.Debug.Log("playing level start animation");
        if (initAnimation != null)
            initAnimation.PlayLevelStartAnimation(playerCube.gameObject);
        else
            UnityEngine.Debug.LogError("levelInitAnimations not set for level manager");
    }

    void SetUpTheme()
    {
        if (currentTheme != null)
        {
            UIManager.Instance.ApplyTheme(currentTheme);
        }
        else
            return;
    }

    void Start()
    {
        SetUpPlayerEvents();

        if (musicController != null && backgroundMusic != null)
        {
            AudioSource source = musicController.GetComponent<AudioSource>();
            if (source != null)
            {
                source.clip = backgroundMusic;
                source.loop = true;
                // DEBUG: Check if clip assigned
                UnityEngine.Debug.Log($"[Music] Clip assigned: {source.clip.name}");
            }
            musicController.PlayMusic();
            // DEBUG: Check if playing
            UnityEngine.Debug.Log($"[Music] IsPlaying: {source.isPlaying}");
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.Play(AudioManager.Instance.levelStart);

        if (introVoiceClip != null)
        {
            AudioManager.Instance.Play(introVoiceClip);
            UIManager.Instance.ShowAutoSubtitles(introFullText, introVoiceClip.length);
        }

        PlayInitialAnimation();
        SetUpTheme();
    }

    void OnPlayerFailed()
    {
        if (musicController != null)
            musicController.StopMusic();
        if (AudioManager.Instance != null)
            AudioManager.Instance.Play(AudioManager.Instance.levelFail);
        if (failAnimation != null)
            failAnimation.LevelFailAnimation(GameManager.Instance.LevelFailed);
        else
            UnityEngine.Debug.LogError("levelFailAnimations not set for level manager");
    }

    void OnPlayerSucceeded()
    {
        if (musicController != null)
            musicController.StopMusic();
        if (AudioManager.Instance != null)
            AudioManager.Instance.Play(AudioManager.Instance.levelPass);
        if (passAnimation != null)
            passAnimation.PassLevelAnimation(() => GameManager.Instance.LevelPassed(nextLevelScene)); //lambda needed 
        else
            UnityEngine.Debug.LogError("levelPassAnimations not set for level maanger");
    }

    public void RegisterSegment(PlayerSegmentController segment)
    {
        if (segment != null)
        {
            segment.OnFellOff.AddListener(OnPlayerFailed);
            UnityEngine.Debug.Log($"[LevelManager] Registered {segment.name} for failure events.");
        }
    }
}
