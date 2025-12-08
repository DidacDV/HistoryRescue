using System.Diagnostics;
using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    [SerializeField] private GameObject linkedSystemObject; 
    [SerializeField] private float pressDepth = 0.15f;
    [SerializeField] private float animSpeed = 5f;

    private ISwitchListener linkedSystem;
    private Vector3 upPos;
    private Vector3 downPos;
    private Vector3 targetPos;
    private int objectsOnPlate = 0;

    void Start()
    {
        upPos = transform.localPosition;
        downPos = new Vector3(upPos.x, upPos.y - pressDepth, upPos.z);
        targetPos = upPos;

        if (linkedSystemObject != null)
        {
            linkedSystem = linkedSystemObject.GetComponent<ISwitchListener>();
            if (linkedSystem == null)
            {
                UnityEngine.Debug.LogWarning($"Linked system on {linkedSystemObject.name} doesn't implement ISwitchListener!");
            }
        }
    }

    void Update()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * animSpeed);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Ghost"))
        {
            objectsOnPlate++;
            if (objectsOnPlate == 1)
            {
                targetPos = downPos;
                linkedSystem?.RegisterSwitch(this);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Ghost"))
        {
            objectsOnPlate--;
            if (objectsOnPlate <= 0)
            {
                objectsOnPlate = 0;
                targetPos = upPos;
                linkedSystem?.RemoveSwitch(this);
            }
        }
    }
}