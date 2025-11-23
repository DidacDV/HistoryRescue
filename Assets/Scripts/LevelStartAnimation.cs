using UnityEngine;

public class LevelStartAnimation : MonoBehaviour {
    [Header("Animation Settings")]
    public float riseSpeed = 3f;           //speed of the rising animation
    public float targetYPosition = -0.05f;  //final Y position (matches your tile spawn height)
    public float startYOffset = -3f;        //how far below to start

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool isAnimating = false;
    private float animationProgress = 0f;

    public void AnimateRise(float delay = 0f) {
        targetPosition = transform.position;
        startPosition = new Vector3(targetPosition.x, targetPosition.y + startYOffset, targetPosition.z);
        transform.position = startPosition;
        if (delay > 0f) {
            Invoke(nameof(StartAnimation), delay);
        }
        else {
            StartAnimation();
        }
    }

    private void StartAnimation() {
        isAnimating = true;
        animationProgress = 0f;
    }

    void Update() {
        if (isAnimating) {
            animationProgress += riseSpeed * Time.deltaTime;
            float easedProgress = EaseOutCubic(Mathf.Clamp01(animationProgress));

            transform.position = Vector3.Lerp(startPosition, targetPosition, easedProgress);

            if (animationProgress >= 1f) {
                isAnimating = false;
                transform.position = targetPosition; 
            }
        }
    }
    private float EaseOutCubic(float t) {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}