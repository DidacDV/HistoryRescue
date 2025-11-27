using UnityEngine;

public class LevelManager : MonoBehaviour {
    public string nextLevelScene; //name of next level scene
    public PlayerController playerCube;

    [Header("Animation Components")]
    public InitLevelAnimations initAnimation;
    public FailLevelAnimations failAnimation;

    void SetUpPlayerEvents() {
        if (playerCube != null) {
            playerCube.OnFellOff.AddListener(OnPlayerFailed);
            playerCube.OnReachedVictoryHole.AddListener(OnPlayerSucceeded);
        }
        else
            Debug.LogError("player not set in level manager");
    }

    void PlayInitialAnimation() {
        if (initAnimation != null)
            initAnimation.PlayLevelStartAnimation(playerCube.gameObject);
        else
            Debug.LogError("levelInitAnimations not set for level manager");
    }

    void Start() {
        SetUpPlayerEvents();
        PlayInitialAnimation();
    }

    void OnPlayerFailed() {
        if (failAnimation != null)
            failAnimation.LevelFailAnimation(GameManager.Instance.LevelFailed);
        else
            Debug.LogError("levelFailAnimations not set for level manager");
    }

    void OnPlayerSucceeded() {
        GameManager.Instance.LevelPassed(nextLevelScene);
    }
}
