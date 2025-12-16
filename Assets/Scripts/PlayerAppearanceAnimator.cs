using System.Collections;
using System.Timers;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

//player's entrance animation when a level starts
public class PlayerAppearanceAnimator : MonoBehaviour {
    [Header("Player appareance settings")]
    public float startYPlayerOffset = 15f; //how far UP the player starts
    public float fallingSpeed = 0.7f;

    public float targetYPosition; //set to the start position, so the player cube should be left in the correct initial position at the scene
    public Vector3 startPosition;

    private bool hasAnimated = false;
    private PlayerController playerController;

    void Awake() {
        playerController = GetComponent<PlayerController>();
    }

    public void ShowAfterDelay(float delay) {
        if (playerController != null) {
            playerController.enabled = false;
        }

        targetYPosition = transform.position.y;

        startPosition = transform.position;
        transform.position = new Vector3(startPosition.x, startPosition.y + startYPlayerOffset, startPosition.z);
        StartCoroutine(ShowPlayerAfterDelay(delay));
    }

    private IEnumerator ShowPlayerAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        if (!hasAnimated) 
            StartCoroutine(AnimateAppearance());
    }

    private IEnumerator AnimateAppearance() {
        hasAnimated = true;
        var elapsedTime = 0f;

        while (transform.position.y > targetYPosition) {
            elapsedTime += Time.deltaTime;
            transform.Translate(Vector3.down * fallingSpeed * elapsedTime, Space.World);

            //dont go under 
            if (transform.position.y <= targetYPosition) {
                transform.position = new Vector3(startPosition.x, targetYPosition, startPosition.z);
                break;
            }
            yield return null;
        }

        if (transform.position.y <= targetYPosition)
            transform.position = new Vector3(startPosition.x, targetYPosition, startPosition.z);


        //re enable after animation
        if (playerController != null) {
            playerController.enabled = true;
        }
    }

    public void ResetAnimation() {
        hasAnimated = false;
    }
}