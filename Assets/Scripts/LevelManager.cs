using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class LevelManager : MonoBehaviour {
    public string nextLevelScene; //name of next level scene
    public LevelTheme currentTheme; //NULL if the current level keeps the same font, otherwise the new font to be set in the UI

    public SpriteRenderer backgroundSprite;

    public PlayerController playerCube;

    [Header("Animation Components")]
    public InitLevelAnimations initAnimation;
    public FailLevelAnimations failAnimation;
    public PassLevelAnimations passAnimation;

    void SetUpPlayerEvents() {
        if (playerCube != null) {
            playerCube.OnFellOff.AddListener(OnPlayerFailed);
            playerCube.OnReachedVictoryHole.AddListener(OnPlayerSucceeded);
        }
        else
            Debug.LogError("player not set in level manager");
    }

    void PlayInitialAnimation() {
        Debug.Log("playing level start animation");
        if (initAnimation != null)
            initAnimation.PlayLevelStartAnimation(playerCube.gameObject);
        else
            Debug.LogError("levelInitAnimations not set for level manager");
    }

    void SetUpTheme()
    {
        if (currentTheme != null)
        {
            UIManager.Instance.ApplyTheme(currentTheme);
            if (currentTheme.backgroundImage != null)
                backgroundSprite.sprite = currentTheme.backgroundImage;
        }
        else
            return;
    }

    void Start() {
        SetUpPlayerEvents();
        PlayInitialAnimation();
        SetUpTheme();
    }

    void OnPlayerFailed() {
        if (failAnimation != null)
            failAnimation.LevelFailAnimation(GameManager.Instance.LevelFailed);
        else
            Debug.LogError("levelFailAnimations not set for level manager");
    }

    void OnPlayerSucceeded() {
        if (passAnimation != null)
            passAnimation.PassLevelAnimation(() => GameManager.Instance.LevelPassed(nextLevelScene)); //lambda needed 
        else
            Debug.LogError("levelPassAnimations not set for level maanger");
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
