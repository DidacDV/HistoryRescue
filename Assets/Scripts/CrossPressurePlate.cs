using UnityEngine;
using DG.Tweening;

public class CrossPressurePlate : MonoBehaviour, ISwitchSource
{
    public interface IStandingChecker
    {
        bool IsStandingUpright();
    }

    [Header("Settings")]
    [SerializeField] private GameObject linkedSystemObject;
    [SerializeField] private float pressDepth = 0.15f;
    [SerializeField] private float animDuration = 0.2f;

    [Header("Standing Check")]
    [SerializeField] private bool requiresUprightStance = true;

    private ISwitchListener linkedSystem;
    private Vector3 upPos;
    private Vector3 downPos;
    private int uprightObjectsOnPlate = 0; // Tracks objects currently standing upright on the plate

    // We don't need to track the state perfectly with a hashset here, 
    // we use the single counter and rely on OnTriggerStay for validation.

    void Start()
    {
        upPos = transform.localPosition;
        downPos = upPos - new Vector3(0, pressDepth, 0);

        if (linkedSystemObject != null)
            linkedSystem = linkedSystemObject.GetComponent<ISwitchListener>();

        if (GetComponent<Collider>() is Collider plateCol)
        {
            plateCol.isTrigger = true;
        }
    }

    // New activation event: Checks state every frame while the player is inside.
    private void OnTriggerStay(Collider other)
    {
        if (!IsValidObject(other)) return;

        // Check 1: Is the plate currently active? (uprightObjectsOnPlate > 0)
        // Check 2: Does the player meet the standing requirement?
        bool isCurrentlyUpright = IsStandingUpright(other);

        if (requiresUprightStance && !isCurrentlyUpright)
        {
            // If the player is on the plate but is NOT upright, do nothing.
            return;
        }

        // If the player is upright (or standing is not required) and we haven't registered them yet:
        if (uprightObjectsOnPlate == 0)
        {
            uprightObjectsOnPlate = 1;

            // Animate Down
            transform.DOKill();
            transform.DOLocalMove(downPos, animDuration).SetEase(Ease.OutQuad);

            linkedSystem?.RegisterSwitch(this);
        }
    }

    // Deactivation event: Player is leaving the tile.
    private void OnTriggerExit(Collider other)
    {
        if (!IsValidObject(other)) return;

        // If the plate was active due to an upright player, deactivate it immediately.
        if (uprightObjectsOnPlate > 0)
        {
            uprightObjectsOnPlate = 0;

            // Animate Up
            transform.DOKill();
            transform.DOLocalMove(upPos, animDuration).SetEase(Ease.OutQuad);

            linkedSystem?.RemoveSwitch(this);
        }
    }

    // Remove the unused OnTriggerEnter
    /* private void OnTriggerEnter(Collider other) { } */

    private bool IsValidObject(Collider other)
    {
        return other.CompareTag("Player") || other.CompareTag("Ghost");
    }

    private bool IsStandingUpright(Collider other)
    {
        IStandingChecker standingChecker = other.GetComponentInParent<IStandingChecker>();

        if (standingChecker != null)
        {
            return standingChecker.IsStandingUpright();
        }
        else
        {
            return other.bounds.size.y > 1.5f;
        }
    }
}