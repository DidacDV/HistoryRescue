using DG.Tweening;
using UnityEngine;

public class PressurePlate : BaseSwitch
{
    [Header("Settings")]
    [SerializeField] private float pressDepth = 0.15f;
    [SerializeField] private float animDuration = 0.2f;

    [Header("Audio")]
    [SerializeField] private AudioClip pressSound;

    private Vector3 upPos;
    private Vector3 downPos;
    private int objectsOnPlate = 0;
    private bool wasPressed = false;

    void Start()
    {
        upPos = transform.localPosition;
        downPos = upPos - new Vector3(0, pressDepth, 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsValidObject(other))
        {
            objectsOnPlate++;
            if (objectsOnPlate == 1)
            {
                if (!wasPressed)
                {
                    wasPressed = true;
                    AnimateDown();
                    if (pressSound != null)
                    {
                        AudioManager.Instance.Play(pressSound);
                    }
                }
                Toggle();
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
                wasPressed = false;
                AnimateUp();
            }
        }
    }

    public void ForceRelease()
    {
        objectsOnPlate = 0;
        wasPressed = false;
        AnimateUp();
    }

    private void AnimateDown()
    {
        transform.DOKill();
        transform.DOLocalMove(downPos, animDuration).SetEase(Ease.OutQuad);
    }

    private void AnimateUp()
    {
        transform.DOKill();
        transform.DOLocalMove(upPos, animDuration).SetEase(Ease.OutQuad);
    }

    private bool IsValidObject(Collider other)
    {
        return other.CompareTag("Player");
    }
}