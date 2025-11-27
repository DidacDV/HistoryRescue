using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitLevelAnimations : MonoBehaviour {
    [Header("References")]
    public GameObject[] manualTiles;  
    public GameObject player;
    public bool autoDetectTiles = true;

    [Header("Animation Settings")]
    public float minRiseSpeed;
    public float maxRiseSpeed;
    public float delayBeforePlayer;
    public float startYOffset; //how far below tiles start

    private List<TileAnimData> tileAnimations = new List<TileAnimData>();
    private GameObject[] allTiles;
    private bool isAnimating = false;
    private float longestAnimationTime = 0f;

    private string TILES_LAYER = "Ground";

    public void PlayLevelStartAnimation(GameObject playerController) {
        player = playerController;
        DetectTilesInLayer(TILES_LAYER);
        AnimateTilesRising();
        AnimatePlayerAppearance();
    }

    private void DetectTilesInLayer(string NameOfLayer) {
        if (autoDetectTiles) {
            allTiles = TileDetector.GetTilesByLayer(NameOfLayer);
        }
        else {
            allTiles = manualTiles;
            Debug.Log($"using {allTiles.Length} manually assigned tiles");
        }
    }

    void AnimateTilesRising() {
        foreach (GameObject tile in allTiles) {
            if (tile == null) continue;

            //store target position (current position)
            Vector3 targetPos = tile.transform.position;

            Vector3 startPos = new Vector3(targetPos.x, targetPos.y + startYOffset, targetPos.z);

            tile.transform.position = startPos;

            float randomSpeed = Random.Range(minRiseSpeed, maxRiseSpeed);

            float animationDuration = 1f / randomSpeed;
            if (animationDuration > longestAnimationTime) {
                longestAnimationTime = animationDuration;
            }

            tileAnimations.Add(new TileAnimData(tile, startPos, targetPos, randomSpeed));
        }

        isAnimating = true;
        enabled = true; //makes update loop run
    }

    void AnimatePlayerAppearance() {
        if (player == null) return;

        PlayerAppearanceAnimator playerAnimator = player.GetComponent<PlayerAppearanceAnimator>();
        if (playerAnimator == null) {
            playerAnimator = player.AddComponent<PlayerAppearanceAnimator>();
        }

        //show player ONLY after tiles finish
        float totalDelay = longestAnimationTime + delayBeforePlayer;
        playerAnimator.ShowAfterDelay(totalDelay);
    }

    void Update() {
        if (!isAnimating) return;

        bool allFinished = true;

        //update all tile animations in one loop
        foreach (TileAnimData data in tileAnimations) {
            if (data.progress < 1f) {
                allFinished = false;

                data.progress += data.speed * Time.deltaTime;

                float easedProgress = EaseOutCubic(Mathf.Clamp01(data.progress));

                data.tile.transform.position = Vector3.Lerp(data.startPosition, data.targetPosition, easedProgress);

                //snap to final position when done
                if (data.progress >= 1f) {
                    data.tile.transform.position = data.targetPosition;
                }
            }
        }

        //stop animating when all tiles are done
        if (allFinished) {
            isAnimating = false;
            enabled = false;
        }
    }

    private float EaseOutCubic(float t) {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}