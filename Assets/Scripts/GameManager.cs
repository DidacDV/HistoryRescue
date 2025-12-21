using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public int currentLevel = 1;
    public int totalScore = 0;
    public string currentLevelName = "";
    private List<string> seenLevels = new List<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Check initial scene
            if (SceneManager.GetActiveScene().name == "MainMenu" || SceneManager.GetActiveScene().name == "VictoryScreen")
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

    void Update()
    {
        if (Keyboard.current.digit0Key.wasPressedThisFrame) LoadDebugLevel(1);
        if (Keyboard.current.digit1Key.wasPressedThisFrame) LoadDebugLevel(2);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) LoadDebugLevel(3);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) LoadDebugLevel(4);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) LoadDebugLevel(5);
        if (Keyboard.current.digit5Key.wasPressedThisFrame) LoadDebugLevel(6);
        if (Keyboard.current.digit6Key.wasPressedThisFrame) LoadDebugLevel(7);
        if (Keyboard.current.digit7Key.wasPressedThisFrame) LoadDebugLevel(8);
        if (Keyboard.current.digit8Key.wasPressedThisFrame) LoadDebugLevel(9);
        if (Keyboard.current.digit9Key.wasPressedThisFrame) LoadDebugLevel(10);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.StartsWith("Level"))
        {
            currentLevelName = scene.name;
            UnityEngine.Debug.Log($"current level:{currentLevelName}");
        }

        if (scene.name == "MainMenu" || scene.name == "VictoryScreen")
        {
            if (UIManager.Instance != null)
                UIManager.Instance.DisableUI();
        }
        else
        {
            UnityEngine.Debug.Log(scene.name);
            if (scene.name == "Persistent" || scene.name == "VictoryScreen") return;
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
        ClearSubtitleHistory();
        currentLevel = 1;
        totalScore = 0;
        UIManager.Instance.InitUIVars();
        LoadLevel("Level1Scene");
    }

    public void LevelPassed(string nextLevelName)
    {
        UnityEngine.Debug.Log($"Level {currentLevel} passed");
        currentLevel++;
        StartCoroutine(LoadNextLevelCoroutine(nextLevelName));
    }

    public void LevelFailed()
    {
        UnityEngine.Debug.Log("Level failed Restarting...");
        StartCoroutine(RestartLevelCoroutine());
    }

    public void GameCompleted()
    {
        UnityEngine.Debug.Log("game has been completed");
        //StartCoroutine(ReturnToMenuCoroutine()); implement function to go back 
    }

    IEnumerator LoadNextLevelCoroutine(string levelName)
    {
        yield return new WaitForSeconds(1.5f);
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
        UnityEngine.Debug.Log("Returning to Main Menu...");
        SceneManager.LoadScene("MainMenu");
    }

    void LoadDebugLevel(int levelNum)
    {
        currentLevel = levelNum;

        string sceneName = "Level" + currentLevel + "Scene";

        UnityEngine.Debug.Log($"Debug loading: {sceneName}");
        LoadLevel(sceneName);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public bool HasSeenSubtitle(string sceneName)
    {
        return seenLevels.Contains(sceneName);
    }

    public void MarkSubtitleAsSeen(string sceneName)
    {
        if (!seenLevels.Contains(sceneName))
        {
            seenLevels.Add(sceneName);
            UnityEngine.Debug.Log($"<color=cyan>[GameManager]</color> Marked {sceneName} as seen. Total seen: {seenLevels.Count}");
        }
    }

    public void ClearSubtitleHistory()
    {
        seenLevels.Clear();
    }
}