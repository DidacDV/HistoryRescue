using UnityEngine;
using TMPro;
using UnityEngine.UI;
using NUnit.Framework;
using System.Collections.Generic;

public class UIManager : MonoBehaviour {
    public static UIManager Instance;

    [SerializeField] private TextMeshProUGUI movementCountValueText;
    [SerializeField] private Image levelDifficultyImage;
    private int movementCount = 0;

    [Header("Difficulty Settings")]
    [SerializeField] private Sprite[] difficultySprites;
    private int currentDifficultyIndex = 0;

    void Awake() {
        //singleton pattern 
        if (Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
            return;
        }

        UpdateMovementText();
    }


    #region Movement Count
    public void IncrementMovement() {
        movementCount++;
        UpdateMovementText();
    }

    public void ResetMovementCount() {
        movementCount = 0;
        UpdateMovementText();
    }

    private void UpdateMovementText() {
        movementCountValueText.text = movementCount.ToString();
    }

    public int GetMovementCount() {
        return movementCount;
    }

    public void SetMovementCount(int count) {
        movementCount = count;
        UpdateMovementText();
    }
    #endregion

    #region Level Image Difficulty

    public void IncrementDifficultyImage() {
        currentDifficultyIndex = (currentDifficultyIndex + 1) % difficultySprites.Length;
        levelDifficultyImage.sprite = difficultySprites[currentDifficultyIndex];
    }

    public void ResetDifficultyImage() {
        currentDifficultyIndex = 0;
        levelDifficultyImage.sprite = difficultySprites[0];
    }

    public void SetDifficultyImage(int levelIndex) {
        if (levelIndex >= 0 && levelIndex < difficultySprites.Length) {
            currentDifficultyIndex = levelIndex;
            levelDifficultyImage.sprite = difficultySprites[levelIndex];
        }
        else {
            Debug.LogWarning("Invalid level index for difficulty image.");
        }
    }
    #endregion
}