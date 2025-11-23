using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public int currentLevel = 1;
    public int totalScore = 0;

    //singleton pattern
    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
            return;
        }
    }

    public void StartGame() {
        currentLevel = 1;
        totalScore = 0;
        LoadLevel("Level1");
    }

    public void LevelPassed(string nextLevelName) {
        Debug.Log($"Level {currentLevel} passed");
        currentLevel++;
        StartCoroutine(LoadNextLevelCoroutine(nextLevelName));
    }

    public void LevelFailed() {
        Debug.Log("Level failed Restarting...");
        StartCoroutine(RestartLevelCoroutine());
    }

    public void GameCompleted() {
        Debug.Log("game has been completed");
        //StartCoroutine(ReturnToMenuCoroutine()); implement function to go back 
    }

    IEnumerator LoadNextLevelCoroutine(string levelName) {
        yield return new WaitForSeconds(1.5f);
        LoadLevel(levelName);
    }

    //reloads WHOLE scene, meaning the level initiation animation is also called + player etc etc
    IEnumerator RestartLevelCoroutine() {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void LoadLevel(string levelName) {
        SceneManager.LoadScene(levelName);
    }
}