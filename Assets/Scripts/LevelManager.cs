using UnityEngine;

public class LevelManager : MonoBehaviour {
    public string nextLevelScene; // Name of next level scene
    public PlayerController playerCube;

    void Start() {
        // Subscribe to cube events
        playerCube.OnFellOff.AddListener(OnPlayerFailed);
        playerCube.OnReachedVictoryHole.AddListener(OnPlayerSucceeded);
    }

    void OnPlayerFailed() {
        GameManager.Instance.LevelFailed();
    }

    void OnPlayerSucceeded() {
        GameManager.Instance.LevelPassed(nextLevelScene);
    }
}
