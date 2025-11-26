using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelInitializer : MonoBehaviour {
    [Header("References")]
    public GameObject[] tiles;  
    public GameObject player;  

    [Header("Animation Settings")]
    public float minRiseSpeed = 0.4f;
    public float maxRiseSpeed = 1f;
    public float delayBeforePlayer = 0.5f;
    public float startYOffset = -3f; //how far below tiles start

    private List<TileAnimData> tileAnimations = new List<TileAnimData>();
    private bool isAnimating = false;
    private float longestAnimationTime = 0f;

    private class TileAnimData {
        public GameObject tile;
        public Vector3 startPosition;
        public Vector3 targetPosition;
        public float speed;
        public float progress;

        public TileAnimData(GameObject tile, Vector3 start, Vector3 target, float speed) {
            this.tile = tile;
            this.startPosition = start;
            this.targetPosition = target;
            this.speed = speed;
            this.progress = 0f;
        }
    }

    void Start() {
        InitializeTileAnimations();
        AnimatePlayer();
    }

    void InitializeTileAnimations() {
        foreach (GameObject tile in tiles) {
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
    }

    void AnimatePlayer() {
        if (player == null) return;

        PlayerAppearanceAnimator playerAnimator = player.GetComponent<PlayerAppearanceAnimator>();
        if (playerAnimator == null) {
            playerAnimator = player.AddComponent<PlayerAppearanceAnimator>();
        }

        //show player after tiles finish
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