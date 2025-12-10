using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public int currentLevel = 1;
    public int totalScore = 0;
    public string currentLevelName = "";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Check initial scene
            if (SceneManager.GetActiveScene().name == "MainMenu")
            {
                if (UIManager.Instance != null)
                    UIManager.Instance.DisableUI();
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.StartsWith("Level"))
        {
            currentLevelName = scene.name;
            Debug.Log($"current level:{currentLevelName}");
        }

        if (scene.name == "MainMenu")
        {
            if (UIManager.Instance != null)
                UIManager.Instance.DisableUI();
        }
        else
        {
            if (scene.name == "Persistent") return;
            if (UIManager.Instance != null)
                UIManager.Instance.EnableUI();
        }
    }

    public void StartGame()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            if (UIManager.Instance != null)
                UIManager.Instance.DisableUI();
        }
        currentLevel = 1;
        totalScore = 0;
        UIManager.Instance.InitUIVars();
        LoadLevel("Level1Scene");
    }

    public void LevelPassed(string nextLevelName)
    {
        Debug.Log($"Level {currentLevel} passed");
        currentLevel++;
        StartCoroutine(LoadNextLevelCoroutine(nextLevelName));
    }

    public void LevelFailed()
    {
        Debug.Log("Level failed Restarting...");
        StartCoroutine(RestartLevelCoroutine());
    }

    public void GameCompleted()
    {
        Debug.Log("game has been completed");
        //StartCoroutine(ReturnToMenuCoroutine()); implement function to go back 
    }

    IEnumerator LoadNextLevelCoroutine(string levelName)
    {
        yield return new WaitForSeconds(1.5f);
        UIManager.Instance.IncrementDifficultyImage();
        LoadLevel(levelName);
    }

    IEnumerator RestartLevelCoroutine()
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(currentLevelName);
    }

    void LoadLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }

    public void ReturnToMainMenu()
    {
        Debug.Log("Returning to Main Menu...");
        SceneManager.LoadScene("MainMenu");
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}