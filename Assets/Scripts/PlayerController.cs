using System;
using System.Collections; 
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider))]
public class PlayerController : MonoBehaviour, BreakingTileSimple.IStandingChecker
{
    public enum Direction { None, Forward, Backward, Left, Right }

    InputAction moveAction; 		// Input action to capture player movement (WASD + cursor keys)

    bool isRolling = false;          // Is the object in the middle of moving?
    Transform pivot;

    bool bFalling = false; 			// Is the object falling?
    bool bLevelPassed = false;      // Has the level been completed?

    public float rotSpeed; 			// Rotation speed in degrees per second
    public float fallSpeed; 		// Fall speed in the Y direction

    LayerMask layerMask; 			// LayerMask to detect raycast hits with ground tiles only

    public AudioClip[] sounds; 		// Sounds to play when the cube rotates
    public AudioClip fallSound; 	// Sound to play when the cube starts falling
    public AudioClip victorySound;  // Sound to play when level is passed

    public UnityEvent OnFellOff;
    public UnityEvent OnReachedVictoryHole;
    Collider m_Collider;
    bool bEventFired = false;

    public Transform ghostPlayer; 
    public Transform ghostPivot;
    public GameObject fallTriggerObject; 
    private Rigidbody playerRigidbody;
    private bool bCommitToFall = false;

    public bool IsStandingUpright()
    {
        return m_Collider.bounds.size.y > 1.5f;
    }

    public float landingGraceTime = 0.1f;
    public bool IsInLandingGrace { get; private set; }
    public bool IsRolling { get { return isRolling; } }
    public bool IsFalling { get { return bFalling; } }

    bool isGrounded()
    {
        if (IsInLandingGrace) return true;

        Vector3 origin = m_Collider.bounds.center;
        float rayDistance = m_Collider.bounds.extents.y + 0.2f;

        RaycastHit hit;
        if (Physics.Raycast(origin, Vector3.down, out hit, rayDistance))
        {
            bool isGroundLayer = ((1 << hit.collider.gameObject.layer) & layerMask) != 0;
            BreakingTileSimple bTile = hit.collider.GetComponentInParent<BreakingTileSimple>();
            bool isValidBreakingTile = bTile != null && !bTile.IsBroken;
            bool isButton = hit.collider.GetComponentInParent<CrossPressurePlate>() != null;

            return isGroundLayer || isValidBreakingTile || isButton;
        }
        return false;
    }

    bool IsMoveSafe(Transform ghost)
    {
        BoxCollider b = ghost.GetComponent<BoxCollider>();
        Vector3[] localCorners = GetLocalCorners(b);
        Vector3[] worldCorners = new Vector3[8];

        float minY = float.MaxValue;
        for (int i = 0; i < 8; i++)
        {
            worldCorners[i] = ghost.TransformPoint(localCorners[i]);
            if (worldCorners[i].y < minY) minY = worldCorners[i].y;
        }

        var bottomCorners = new System.Collections.Generic.List<Vector3>();
        Vector3 bottomCenter = Vector3.zero;

        foreach (var p in worldCorners)
        {
            if (Mathf.Abs(p.y - minY) < 0.05f)
            {
                bottomCorners.Add(p);
                bottomCenter += p;
            }
        }

        if (bottomCorners.Count != 4) return false;
        bottomCenter /= 4f;

        int validHits = 0;
        int victoryHits = 0; // NEW: Track hits on the victory hole
        float inset = 0.15f;

        foreach (var p in bottomCorners)
        {
            Vector3 dirToCenter = (bottomCenter - p).normalized;
            Vector3 testPoint = p + (dirToCenter * inset);
            Vector3 rayOrigin = testPoint + Vector3.up * 0.1f;

            RaycastHit hit;

            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 1.0f))
            {
                BreakingTileSimple bTile = hit.collider.GetComponentInParent<BreakingTileSimple>();
                bool isLevelPass = hit.collider.CompareTag("LevelPass");
                bool isBreakingTile = bTile != null && !bTile.IsBroken;
                bool isGroundLayer = ((1 << hit.collider.gameObject.layer) & layerMask) != 0;

                if (isLevelPass || isBreakingTile || isGroundLayer)
                {
                    validHits++;
                    if (isLevelPass) victoryHits++;
                }
            }
        }

        // FINAL CHECK:

        // If the cube is positioned completely over the LevelPass object (4 hits), 
        // it must be safe, regardless of other tiles.
        if (victoryHits == 4)
        {
            Debug.Log("Move is safe: Landing perfectly on Victory Hole.");
            return true;
        }

        // If we rely on solid ground, all four corner rays must hit solid ground or the LevelPass object.
        if (validHits == 4)
        {
            Debug.Log("Move is safe: Landing on solid ground.");
            return true;
        }

        // If we didn't meet the 4-hit requirement, the move is unsafe.
        return false;
    }

    void Awake()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        layerMask = LayerMask.GetMask("Ground");
        m_Collider = GetComponent<Collider>();
        playerRigidbody = GetComponent<Rigidbody>();
        pivot = new GameObject("RotationPivot").transform;
        ghostPivot = new GameObject("GhostPivot").transform;

        if (ghostPlayer == null)
        {
            GameObject ghostObj = new GameObject("GhostPlayer_TEMP");
            ghostPlayer = ghostObj.transform;
            BoxCollider ghostCol = ghostObj.AddComponent<BoxCollider>();
            BoxCollider playerCol = GetComponent<BoxCollider>();
            if (playerCol != null)
            {
                ghostCol.size = playerCol.size;
                ghostCol.center = playerCol.center;
            }
            ghostCol.isTrigger = true;
            ghostObj.SetActive(true);
        }
    }

    void Start()
    {
    }

    void OnEnable()
    {
        if (moveAction != null) moveAction.Enable();
        StartCoroutine(LandingGrace());
    }

    void OnDisable()
    {
        if (moveAction != null) moveAction.Disable();
    }

    public void ResetState()
    {
        StopAllCoroutines();
        isRolling = false;
        bFalling = false;
        bLevelPassed = false;
        bEventFired = false;
        Debug.Log("PlayerController state reset!");
    }


    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Time.timeScale == 0)
                UIManager.Instance.OnResumeGamePress();
            else
                UIManager.Instance.OnEnterPause();
            return;
        }

        //game is paused
        if (Time.timeScale == 0) return;

        if (bCommitToFall)
        {
            if (transform.position.y < -5.0f) HandleFalling();
            return; // Let physics handle position/rotation
        }

        if (bFalling)
        {
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime, Space.World);
            if (transform.position.y < -5.0f) HandleFalling();
        }
        else if (!isRolling)
        {

            if (!isGrounded() && !bCommitToFall)
            {
                // Only fall if we are actually below the map or truly in the void
                UnityEngine.Debug.Log("[Physics] Ground lost check.");
                CheckVictoryHole();
                if (!bLevelPassed)
                {
                    bFalling = true;
                    if (fallSound) AudioSource.PlayClipAtPoint(fallSound, transform.position, 1.5f);
                }
                return;
            }
            Vector2 input = moveAction.ReadValue<Vector2>();



            if (input.sqrMagnitude > 0.5f)
            {
                Direction dir = Direction.None;

                if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                {
                    dir = input.x > 0 ? Direction.Right : Direction.Left;
                }
                else
                {
                    dir = input.y > 0 ? Direction.Forward : Direction.Backward;
                }

                if (dir != Direction.None)
                {
                    StartCoroutine(RollToDirection(dir));
                }
            }
        }
    }

    void StartFallingFromUnsafeRoll()
    {
        bCommitToFall = true;
        bFalling = true;

        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = false;
            playerRigidbody.useGravity = true;

            Vector3 center = m_Collider.bounds.center;
            BoxCollider b = GetComponent<BoxCollider>();
            Vector3[] localCorners = GetLocalCorners(b);
            Vector3 combinedunsupportedOffset = Vector3.zero;
            int unsupportedCount = 0;

            foreach (Vector3 localCorner in localCorners)
            {
                if (localCorner.y > 0) continue;
                Vector3 worldPoint = transform.TransformPoint(localCorner);
                RaycastHit hit;
                bool supported = Physics.Raycast(
                    worldPoint + Vector3.up * 0.1f,
                    Vector3.down,
                    out hit,
                    0.5f
                );

                if (supported)
                {
                    var bTile = hit.collider.GetComponent<BreakingTileSimple>();
                    bool validBreakingTile = bTile != null && !bTile.IsBroken;

                    bool isGround = ((1 << hit.collider.gameObject.layer) & layerMask) != 0;

                    supported = isGround || validBreakingTile;
                }

                if (!supported)
                {
                    combinedunsupportedOffset += (worldPoint - center);
                    unsupportedCount++;
                }
            }

            if (unsupportedCount > 0)
            {
                combinedunsupportedOffset.y = 0;
                Vector3 torqueAxis = Vector3.Cross(combinedunsupportedOffset.normalized, Vector3.up);
                playerRigidbody.AddTorque(torqueAxis * 20f, ForceMode.Impulse);
                playerRigidbody.AddForce(Vector3.down * 10f, ForceMode.Impulse);
            }
        }

        if (fallSound) AudioSource.PlayClipAtPoint(fallSound, transform.position, 1.5f);
    }

    // Check if we're over the victory hole and in correct position
    void CheckVictoryHole()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 2.0f))
        {
            if (hit.collider.CompareTag("LevelPass"))
            {
                if (IsStandingUpright())
                {
                    Debug.Log("CORRECT POSITION! Falling into victory hole");
                    bFalling = true;
                    bLevelPassed = true;
                    if (victorySound != null) { } // AudioSource.PlayClipAtPoint(victorySound, transform.position, 1.5f);
                }
            }
        }
    }

    void HandleFalling()
    {
        if (!bEventFired && transform.position.y < -5.0f)
        {
            bEventFired = true;
            if (bLevelPassed) OnReachedVictoryHole?.Invoke();
            else OnFellOff?.Invoke();
        }
    }

    Vector3[] GetLocalCorners(BoxCollider b)
    {
        Vector3 c = b.center;
        Vector3 e = b.size * 0.5f;

        return new Vector3[] {
            c + new Vector3(e.x, e.y, e.z),
            c + new Vector3(e.x, e.y, -e.z),
            c + new Vector3(e.x, -e.y, e.z),
            c + new Vector3(e.x, -e.y, -e.z),
            c + new Vector3(-e.x, e.y, e.z),
            c + new Vector3(-e.x, e.y, -e.z),
            c + new Vector3(-e.x, -e.y, e.z),
            c + new Vector3(-e.x, -e.y, -e.z)
        };
    }

    private Vector3 GetAxis(Direction direction)
    {
        switch (direction)
        {
            case Direction.Left: return Vector3.forward;
            case Direction.Right: return Vector3.back;
            case Direction.Forward: return Vector3.right;
            case Direction.Backward: return Vector3.left;
            default: return Vector3.zero;
        }
    }

    private Vector3 GetDirectionVector(Direction direction)
    {
        switch (direction)
        {
            case Direction.Left: return Vector3.left;
            case Direction.Right: return Vector3.right;
            case Direction.Forward: return Vector3.forward;
            case Direction.Backward: return Vector3.back;
            default: return Vector3.zero;
        }
    }

    private Vector2 GetPivotOffset(Direction direction)
    {
        Bounds bounds = m_Collider.bounds;
        Vector2 offset = Vector2.zero;

        offset.y = bounds.extents.y;

        if (direction == Direction.Left || direction == Direction.Right)
            offset.x = bounds.extents.x;
        else
            offset.x = bounds.extents.z;

        return offset;
    }

    private IEnumerator RollToDirection(Direction direction)
    {
        if (isRolling) yield break;

        float angle = 90f;
        float rollDuration = 90f / rotSpeed;

        Vector3 axis = GetAxis(direction);
        Vector3 directionVector = GetDirectionVector(direction);
        Vector2 pivotOffset = GetPivotOffset(direction);

        Vector3 pivotPosition = transform.position +
                                (directionVector * pivotOffset.x) +
                                (Vector3.down * pivotOffset.y);

        pivot.position = pivotPosition;

        // 1. Setup the ghost to check the FINAL position/rotation
        CopyTransformData(transform, ghostPlayer);

        // Rotate the ghost 90 degrees to simulate the final landing spot
        ghostPlayer.RotateAround(pivotPosition, axis, angle);

        Physics.SyncTransforms();

        // 2. Perform the PRE-ROLL SAFETY CHECK on the final ghost position
        bool isSafe = IsMoveSafe(ghostPlayer);
        if (!isSafe)
        {
            // Log the failure, but continue the roll animation.
            Debug.LogWarning("Pre-Roll FAILURE detected! Final landing is unstable. Roll will proceed and commit to fall mid-way.");
        }

        // Safety check passed: store final ghost state for perfect snapping later
        Quaternion finalRotation = ghostPlayer.rotation;
        Vector3 finalPos = ghostPlayer.position;

        // Restore the ghost back to original state to prevent issues if it's reused
        CopyTransformData(transform, ghostPlayer);

        float elapsedTime = 0f;
        bool committed = false;

        UIManager.Instance.IncrementMovement();
        isRolling = true;
        bCommitToFall = false;

        while (elapsedTime < rollDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / rollDuration;
            float rotationRate = angle * (Time.deltaTime / rollDuration);

            transform.RotateAround(pivotPosition, axis, rotationRate);

            if (!committed && progress >= 0.7f && !isSafe)
            {
                committed = true;
                StartFallingFromUnsafeRoll();
                isRolling = false;
                yield break;
            }

            yield return null;
        }

        // 3. Post-Roll Snapping: Use the pre-calculated final state for perfect snap

        // Snap rotation
        transform.rotation = finalRotation;

        // Snap position (using the pre-calculated finalPos)
        float snappedX = Mathf.Round(finalPos.x * 2) / 2f;
        float snappedZ = Mathf.Round(finalPos.z * 2) / 2f;

        // Recalculate current height based on final rotation to handle rotation changes
        float currentHeight = 1.0f;
        Vector3 size = transform.lossyScale;

        if (Mathf.Abs(Vector3.Dot(transform.right, Vector3.up)) > 0.9f) currentHeight = size.x;
        else if (Mathf.Abs(Vector3.Dot(transform.up, Vector3.up)) > 0.9f) currentHeight = size.y;
        else if (Mathf.Abs(Vector3.Dot(transform.forward, Vector3.up)) > 0.9f) currentHeight = size.z;

        transform.position = new Vector3(snappedX, currentHeight / 2f, snappedZ);
        isRolling = false;

        CheckVictoryHole();

        RaycastHit groundHit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out groundHit, 1.5f))
        {
            BreakingTileSimple bTile = groundHit.collider.GetComponentInParent<BreakingTileSimple>();
            if (bTile != null)
            {
                bTile.TriggerBreak(this);
            }
        }

        StartCoroutine(LandingGrace());
    }

    IEnumerator LandingGrace()
    {
        IsInLandingGrace = true;
        yield return new WaitForSeconds(landingGraceTime);
        IsInLandingGrace = false;
    }

    public void CopyTransformData(Transform source, Transform target)
    {
        target.position = source.position;
        target.rotation = source.rotation;
        target.localScale = source.localScale;
    }

}