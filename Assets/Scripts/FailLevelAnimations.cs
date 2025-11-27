
using System.Collections.Generic;
using UnityEngine;

public class FailLevelAnimations : MonoBehaviour {
    public GameObject[] manualTiles;
    private GameObject[] allTiles;
    public bool autoDetectTiles = false;

    [Header("Animation Settings")]
    public float minFallSpeed = 1f;
    public float maxFallSpeed = 2.5f;
    public float fallDistance = -17f;

    [Header("Optional")]
    public bool disablePlayerControl = true;

    private bool isAnimating = false;
    private string TILES_LAYER = "Ground";
    private System.Action onAnimationComplete;

    private List<TileAnimData> tileAnimations = new List<TileAnimData>();

    public void LevelFailAnimation(System.Action onComplete = null) {
        onAnimationComplete = onComplete; 

        DetectTiles();
        AnimateTilesFalling();
    }

    private void DetectTiles() {
        if (autoDetectTiles)
            allTiles = TileDetector.GetTilesByLayer(TILES_LAYER);
        else
            allTiles = manualTiles;
            Debug.Log($"using {allTiles.Length} manually assigned tiles");
    }

    void AnimateTilesFalling() {
        tileAnimations.Clear();

        foreach (GameObject tile in allTiles) {
            if (tile == null) continue;

            Vector3 startPos = tile.transform.position;
            Vector3 targetPos = new Vector3(startPos.x, startPos.y + fallDistance, startPos.z);

            float randomSpeed = Random.Range(minFallSpeed, maxFallSpeed);
            tileAnimations.Add(new TileAnimData(tile, startPos, targetPos, randomSpeed));
        }

        isAnimating = true;
        enabled = true; 
    }

    void Update() {
        if (!isAnimating) return;

        bool allFinished = true;

        foreach (TileAnimData data in tileAnimations) {
            if (data.progress < 1f) {
                allFinished = false;

                data.progress += data.speed * Time.deltaTime;
                float easedProgress = EaseInCubic(Mathf.Clamp01(data.progress));

                data.tile.transform.position = Vector3.Lerp(data.startPosition, data.targetPosition, easedProgress);

                if (data.progress >= 1f) {
                    data.tile.transform.position = data.targetPosition;
                }
            }
        }

        if (allFinished) {
            isAnimating = false;
            enabled = false;
            onAnimationComplete?.Invoke();
        }
    }

    //falls faster
    private float EaseInCubic(float t) {
        return t * t * t;
    }

}