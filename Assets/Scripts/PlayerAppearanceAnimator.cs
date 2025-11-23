using System.Collections;
using UnityEngine;

//player's entrance animation when a level starts
public class PlayerAppearanceAnimator : MonoBehaviour {
    [Header("Appearance Animation")]
    public float scaleUpDuration = 0.3f;

    private Vector3 originalScale;
    private bool hasAnimated = false;
    private PlayerController playerController;

    void Awake() {
        originalScale = transform.localScale;
        playerController = GetComponent<PlayerController>();
    }

    public void ShowAfterDelay(float delay) {
        if (playerController != null) {
            playerController.enabled = false;
        }
        //invisible by setting scale to zero
        transform.localScale = Vector3.zero;
        StartCoroutine(ShowPlayerAfterDelay(delay));
    }

    //no delay
    public void ShowWithAnimation() {
        if (!hasAnimated) {
            gameObject.SetActive(true);
            StartCoroutine(AnimateAppearance());
        }
    }

    private IEnumerator ShowPlayerAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        if (!hasAnimated) 
            StartCoroutine(AnimateAppearance());
    }

    private IEnumerator AnimateAppearance() {
        hasAnimated = true;

        transform.localScale = Vector3.zero;

        float elapsed = 0f;

        while (elapsed < scaleUpDuration) {
            elapsed += Time.deltaTime;
            float t = elapsed / scaleUpDuration;

            float scale = EaseOutBack(t);
            transform.localScale = originalScale * scale;

            yield return null;
        }

        transform.localScale = originalScale;
        //re enable after animation
        if (playerController != null) {
            playerController.enabled = true;
        }
    }

    private float EaseOutBack(float t) {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    public void ResetAnimation() {
        hasAnimated = false;
        transform.localScale = originalScale;
    }
}