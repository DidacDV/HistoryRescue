using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class CrossPressurePlate : BaseSwitch
{
    public interface IStandingChecker
    {
        bool IsStandingUpright();
    }

    [Header("Settings")]
    [SerializeField] private float pressDepth = 0.15f;
    [SerializeField] private float animDuration = 0.2f;

    [Header("Standing Check")]
    [SerializeField] private bool requiresUprightStance = true;

    private Vector3 upPos;
    private Vector3 downPos;
    private Dictionary<Collider, bool> pressingObjects = new Dictionary<Collider, bool>();
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
            pressingObjects[other] = IsStandingUpright(other);
            CheckToggle();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!pressingObjects.ContainsKey(other)) return;

        bool wasUpright = pressingObjects[other];
        bool isUpright = IsStandingUpright(other);

        if (wasUpright != isUpright)
        {
            pressingObjects[other] = isUpright;
            CheckToggle();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (pressingObjects.ContainsKey(other))
        {
            pressingObjects.Remove(other);
            if (pressingObjects.Count == 0)
            {
                wasPressed = false;
                AnimateUp();
            }
        }
    }

    private void CheckToggle()
    {
        int validCount = 0;
        foreach (var kvp in pressingObjects)
        {
            if (!requiresUprightStance || kvp.Value)
            {
                validCount++;
            }
        }

        if (validCount > 0)
        {
            if (!wasPressed)
            {
                wasPressed = true;
                AnimateDown();
            }
            Toggle();
        }
    }

    public void ForceRelease()
    {
        pressingObjects.Clear();
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

    private bool IsStandingUpright(Collider other)
    {
        IStandingChecker standingChecker = other.GetComponentInParent<IStandingChecker>();
        if (standingChecker != null)
        {
            return standingChecker.IsStandingUpright();
        }
        return other.bounds.size.y > 1.5f;
    }
}