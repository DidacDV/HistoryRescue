using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;

public class SplitManager : MonoBehaviour, ISwitchListener
{
    [Header("References")]
    public PlayerController playerController;
    public GameObject segmentPrefab;

    [Header("Settings")]
    public InputActionReference SwapAction;

    [Header("Manual Split Offsets (Relative to Block Center)")]
    public Vector3 Segment1Offset = new Vector3(-0.5f, 0.0f, 0.0f);
    public Vector3 Segment2Offset = new Vector3(0.5f, 0.0f, 0.0f);

    private bool isSplit = false;
    private GameObject segment1;
    private GameObject segment2;
    private PlayerSegmentController segment1Controller;
    private PlayerSegmentController segment2Controller;
    private GameObject currentControlledSegment;

    void Awake()
    {
        if (SwapAction != null && SwapAction.action != null)
        {
            SwapAction.action.performed += OnSwapPerformed;
        }
    }

    void OnEnable()
    {
        if (SwapAction != null && SwapAction.action != null)
        {
            SwapAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (SwapAction != null && SwapAction.action != null)
        {
            SwapAction.action.performed -= OnSwapPerformed;
            SwapAction.action.Disable();
        }
    }

    private void OnSwapPerformed(InputAction.CallbackContext context)
    {
        if (isSplit)
        {
            SwapControl();
        }
    }

    public void RegisterSwitch(ISwitchSource plate)
    {
        UnityEngine.Debug.Log("SplitManager: RegisterSwitch called");
        //if (playerController.IsRolling) return;

        if (!isSplit)
        {
            Split();
        }
        else
        {
            CheckForReconstitutionAndCombine();
        }
    }

    public void RemoveSwitch(ISwitchSource plate)
    {
    }

    void Update() {
        if (isSplit)
        {
            CheckForReconstitutionAndCombine();
        }
    }

    void Split()
    {
        if (playerController == null || segmentPrefab == null) return;
        PressurePlate[] allPlates = FindObjectsOfType<PressurePlate>();
        foreach (var plate in allPlates)
        {
            plate.ForceExit();
        }
        // DISABLE PLAYER ACTION MAP
        var playerActionMap = InputSystem.actions.FindActionMap("Player");
        if (playerActionMap != null)
        {
            playerActionMap.Disable();
            UnityEngine.Debug.Log("Disabled Player action map");
        }

        // ENABLE SEGMENTS ACTION MAP
        var segmentsActionMap = InputSystem.actions.FindActionMap("Segments");
        if (segmentsActionMap != null)
        {
            segmentsActionMap.Enable();
            UnityEngine.Debug.Log("Enabled Segments action map");
        }

        Vector3 center = playerController.transform.position;
        Vector3 pos1 = center + Segment1Offset;
        Vector3 pos2 = center + Segment2Offset;

        segment1 = Instantiate(segmentPrefab, pos1, Quaternion.identity);
        segment2 = Instantiate(segmentPrefab, pos2, Quaternion.identity);

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

    void CheckForReconstitutionAndCombine()
    {
        if (segment1 == null || segment2 == null) return;

        Vector3 pos1 = segment1.transform.position;
        Vector3 pos2 = segment2.transform.position;
        Vector3 diff = pos1 - pos2;

        float tolerance = 0.15f;

        if (Mathf.Abs(diff.y) > tolerance)
        {
            return;
        }

        float horizontalDistance = Mathf.Sqrt(diff.x * diff.x + diff.z * diff.z);

        if (Mathf.Abs(horizontalDistance - 1.0f) > tolerance) return;
        
        bool alignedAlongX = Mathf.Abs(diff.z) < tolerance;

        bool alignedAlongZ = Mathf.Abs(diff.x) < tolerance;

        if (alignedAlongX || alignedAlongZ)
        {
            UnityEngine.Debug.Log("RECONSTITUTING NOW!");
            Reconstitute(pos1, pos2, alignedAlongZ);
        }
    }

    void Reconstitute(Vector3 pos1, Vector3 pos2, bool rotateForZ)
    {
        // DISABLE SEGMENTS ACTION MAP
        var segmentsActionMap = InputSystem.actions.FindActionMap("Segments");
        if (segmentsActionMap != null)
        {
            segmentsActionMap.Disable();
            UnityEngine.Debug.Log("Disabled Segments action map");
        }

        // Destroy segments first to avoid interference
        Destroy(segment1);
        Destroy(segment2);
        segment1 = null;
        segment2 = null;
        currentControlledSegment = null;

        // Calculate center position
        Vector3 newCenter = (pos1 + pos2) / 2f;

        // Snap to grid
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

        // ENABLE PLAYER ACTION MAP FIRST (before SetActive)
        var playerActionMap = InputSystem.actions.FindActionMap("Player");
        if (playerActionMap != null)
        {
            playerActionMap.Enable();
            UnityEngine.Debug.Log("Enabled Player action map");
        }

        // NOW reactivate and position player
        playerController.gameObject.SetActive(true);
        playerController.ResetState();

        playerController.transform.position = newCenter;
        playerController.transform.rotation = newRotation;

        isSplit = false;

        UnityEngine.Debug.Log($"SplitManager: Reconstituted player at {newCenter} with rotation {newRotation.eulerAngles}");
    }
}