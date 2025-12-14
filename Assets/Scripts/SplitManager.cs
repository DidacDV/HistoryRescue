using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class SplitGroup
{
    public List<BaseSwitch> controllingSwitches = new List<BaseSwitch>();
    public Vector3 segment1Position = new Vector3(0.0f, 0.0f, 0.0f);
    public Vector3 segment2Position = new Vector3(0.0f, 0.0f, 0.0f);

    [HideInInspector] public int activeSwitchCount = 0;
}

public class SplitManager : MonoBehaviour, ISwitchListener
{
    [Header("References")]
    public PlayerController playerController;
    public GameObject segmentPrefab;

    [Header("Settings")]
    public InputActionReference swapAction;

    [Header("Split Groups")]
    [SerializeField] private List<SplitGroup> splitGroups = new List<SplitGroup>();

    private Dictionary<ISwitchSource, SplitGroup> sourceToGroupMap = new Dictionary<ISwitchSource, SplitGroup>();
    private bool isSplit = false;
    private GameObject segment1;
    private GameObject segment2;
    private PlayerSegmentController segment1Controller;
    private PlayerSegmentController segment2Controller;
    private GameObject currentControlledSegment;

    void Awake()
    {
        foreach (var group in splitGroups)
        {
            foreach (var switchSource in group.controllingSwitches)
            {
                if (switchSource != null)
                {
                    switchSource.AddListener(this);
                    sourceToGroupMap[switchSource] = group;

                    if (switchSource.GetState())
                    {
                        group.activeSwitchCount++;
                    }
                }
            }
        }

        if (swapAction != null && swapAction.action != null)
        {
            swapAction.action.performed += OnSwapPerformed;
        }
    }

    void OnEnable()
    {
        if (swapAction != null && swapAction.action != null)
        {
            swapAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (swapAction != null && swapAction.action != null)
        {
            swapAction.action.performed -= OnSwapPerformed;
            swapAction.action.Disable();
        }
    }

    private void OnDestroy()
    {
        foreach (var group in splitGroups)
        {
            foreach (var switchSource in group.controllingSwitches)
            {
                if (switchSource != null)
                {
                    switchSource.RemoveListener(this);
                }
            }
        }
    }

    private void OnSwapPerformed(InputAction.CallbackContext context)
    {
        if (isSplit)
        {
            SwapControl();
        }
    }

    public void OnSwitchToggled(ISwitchSource source, bool state)
    {
        if (sourceToGroupMap.TryGetValue(source, out SplitGroup group))
        {
            bool wasActive = group.activeSwitchCount > 0;

            if (state)
            {
                group.activeSwitchCount++;
            }
            else
            {
                group.activeSwitchCount--;
            }

            bool isActive = group.activeSwitchCount > 0;

            if (!wasActive && isActive && !isSplit)
            {
                Split(group);
            }
        }
    }

    void Update()
    {
        if (isSplit)
        {
            CheckForReconstitution();
        }
    }

    void Split(SplitGroup group)
    {
        if (playerController == null || segmentPrefab == null) return;

        BaseSwitch[] allSwitches = FindObjectsOfType<BaseSwitch>();
        foreach (var switchObj in allSwitches)
        {
            if (switchObj is PressurePlate plate)
            {
                plate.ForceRelease();
            }
            else if (switchObj is CrossPressurePlate crossPlate)
            {
                crossPlate.ForceRelease();
            }
        }

        var playerActionMap = InputSystem.actions.FindActionMap("Player");
        if (playerActionMap != null)
        {
            playerActionMap.Disable();
        }

        var segmentsActionMap = InputSystem.actions.FindActionMap("Segments");
        if (segmentsActionMap != null)
        {
            segmentsActionMap.Enable();
        }

        segment1 = Instantiate(segmentPrefab, group.segment1Position, Quaternion.identity);
        segment2 = Instantiate(segmentPrefab, group.segment2Position, Quaternion.identity);

        segment1Controller = segment1.GetComponent<PlayerSegmentController>();
        segment2Controller = segment2.GetComponent<PlayerSegmentController>();
        segment1Controller.OtherSegment = segment2.transform;
        segment2Controller.OtherSegment = segment1.transform;

        currentControlledSegment = segment1;
        segment1Controller.SetControl(true);
        segment2Controller.SetControl(false);

        playerController.gameObject.SetActive(false);
        isSplit = true;
    }

    void SwapControl()
    {
        if (currentControlledSegment == segment1)
        {
            segment1Controller.SetControl(false);
            segment2Controller.SetControl(true);
            currentControlledSegment = segment2;
        }
        else
        {
            segment2Controller.SetControl(false);
            segment1Controller.SetControl(true);
            currentControlledSegment = segment1;
        }
    }

    void CheckForReconstitution()
    {
        if (segment1 == null || segment2 == null) return;

        Vector3 pos1 = segment1.transform.position;
        Vector3 pos2 = segment2.transform.position;
        Vector3 diff = pos1 - pos2;

        float tolerance = 0.15f;

        if (Mathf.Abs(diff.y) > tolerance) return;

        float horizontalDistance = Mathf.Sqrt(diff.x * diff.x + diff.z * diff.z);

        if (Mathf.Abs(horizontalDistance - 1.0f) > tolerance) return;

        bool alignedAlongX = Mathf.Abs(diff.z) < tolerance;
        bool alignedAlongZ = Mathf.Abs(diff.x) < tolerance;

        if (alignedAlongX || alignedAlongZ)
        {
            Reconstitute(pos1, pos2, alignedAlongZ);
        }
    }

    void Reconstitute(Vector3 pos1, Vector3 pos2, bool rotateForZ)
    {
        var segmentsActionMap = InputSystem.actions.FindActionMap("Segments");
        if (segmentsActionMap != null)
        {
            segmentsActionMap.Disable();
        }

        Destroy(segment1);
        Destroy(segment2);
        segment1 = null;
        segment2 = null;
        currentControlledSegment = null;

        Vector3 newCenter = (pos1 + pos2) / 2f;
        newCenter.x = Mathf.Round(newCenter.x * 2f) / 2f;
        newCenter.z = Mathf.Round(newCenter.z * 2f) / 2f;

        Quaternion newRotation;

        if (rotateForZ)
        {
            newRotation = Quaternion.Euler(90, 0, 0);
            newCenter.y = 0.5f;
        }
        else
        {
            newRotation = Quaternion.Euler(0, 0, 90);
            newCenter.y = 0.5f;
        }

        var playerActionMap = InputSystem.actions.FindActionMap("Player");
        if (playerActionMap != null)
        {
            playerActionMap.Enable();
        }

        playerController.gameObject.SetActive(true);
        playerController.ResetState();

        playerController.transform.position = newCenter;
        playerController.transform.rotation = newRotation;

        isSplit = false;
    }
}