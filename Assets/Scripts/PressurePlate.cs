using UnityEngine;
using DG.Tweening;

public class PressurePlate : MonoBehaviour, ISwitchSource
{
    [Header("Settings")]
    [SerializeField] private GameObject linkedSystemObject;
    [SerializeField] private float pressDepth = 0.15f;
    [SerializeField] private float animDuration = 0.2f;

    private ISwitchListener linkedSystem;
    private Vector3 upPos;
    private Vector3 downPos;
    private int objectsOnPlate = 0;

    void Start()
    {
        upPos = transform.localPosition;
        downPos = upPos - new Vector3(0, pressDepth, 0);

        if (linkedSystemObject != null)
            linkedSystem = linkedSystemObject.GetComponent<ISwitchListener>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsValidObject(other))
        {
            objectsOnPlate++;
            if (objectsOnPlate == 1)
            {
                // Animate Down
                transform.DOKill();
                transform.DOLocalMove(downPos, animDuration).SetEase(Ease.OutQuad);

                linkedSystem?.RegisterSwitch(this);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsValidObject(other))
        {
            objectsOnPlate--;
            if (objectsOnPlate <= 0)
            {
                objectsOnPlate = 0;

                // Animate Up
                transform.DOKill();
                transform.DOLocalMove(upPos, animDuration).SetEase(Ease.OutQuad);

                linkedSystem?.RemoveSwitch(this);
            }
        }
    }

    private bool IsValidObject(Collider other)
    {
        return other.CompareTag("Player") || other.CompareTag("Ghost");
    }
}