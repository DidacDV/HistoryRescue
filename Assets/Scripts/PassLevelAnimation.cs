using System.Collections.Generic;
using UnityEngine;

public class PassLevelAnimations : MonoBehaviour {
    public GameObject[] manualTiles;
    private GameObject[] allTiles;
    public bool autoDetectTiles = false;

    [Header("Animation Settings")]
    public float minFlySpeed;
    public float maxFlySpeed;
    public float flyDistance;
    public float rotationAmount = 360f;

    [Header("Optional")]
    public bool disablePlayerControl = true;

    private bool isAnimating = false;
    private string TILES_LAYER = "Ground";
    private System.Action onAnimationComplete;
    private Vector3 levelCenter;

    private List<TileAnimData> tileAnimations = new List<TileAnimData>();

    public void PassLevelAnimation(System.Action onComplete = null) {
        onAnimationComplete = onComplete;

        DetectTiles();
        AnimateTilesFlyingAndSpinning();
    }

    private void DetectTiles() {
        if (autoDetectTiles)
            allTiles = TileDetector.GetTilesByLayer(TILES_LAYER);
        else
            allTiles = manualTiles;
    }

    //finds level center position (based on tiles)
    void AnimateTilesFlyingAndSpinning() {
        tileAnimations.Clear();

        Vector3 sumPositions = Vector3.zero;
        int count = 0;
        foreach (GameObject tile in allTiles) {
            if (tile != null) {
                sumPositions += tile.transform.position;
                count++;
            }
        }
        levelCenter = (count > 0) ? sumPositions / count : Vector3.zero;

        foreach (GameObject tile in allTiles) {
            if (tile == null) continue;
            Vector3 startPos = tile.transform.position;
            Vector3 targetPos = new Vector3(startPos.x, startPos.y + flyDistance, startPos.z);
            Vector3 offset = startPos - levelCenter;

            float randomSpeed = Random.Range(minFlySpeed, maxFlySpeed);
            tileAnimations.Add(new TileAnimData(tile, startPos, targetPos, offset, randomSpeed));
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

                float currentY = Mathf.Lerp(data.startPosition.y, data.targetPosition.y, easedProgress);

                Quaternion rotation = Quaternion.Euler(0, rotationAmount * easedProgress, 0);
                Vector3 currentOffset = rotation * data.initialOffset;

                //rotate + go up
                data.tile.transform.position = levelCenter + currentOffset + new Vector3(0, currentY - levelCenter.y, 0);

                if (data.progress >= 1f) {
                    data.tile.transform.position = data.targetPosition;
                    data.tile.SetActive(false);
                }
            }
        }

        if (allFinished) {
            isAnimating = false;
            enabled = false;
            onAnimationComplete?.Invoke();
        }
    }

    private float EaseInCubic(float t) {
        return t * t * t;
    }
}