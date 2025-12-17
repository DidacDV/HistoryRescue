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
    public bool IsStandingUpright() { return m_Collider.bounds.size.y > 1.5f; }

    public bool IsRolling { get { return isRolling; } }

    bool isGrounded()
    {
        float distToGround = m_Collider.bounds.extents.y;
        return Physics.Raycast(transform.position, Vector3.down, distToGround + 0.1f, layerMask);
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

            // NOTE: We temporarily disable the layerMask here to see EVERYTHING beneath the corner.
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 1.0f))
            {
                if (hit.collider.CompareTag("LevelPass"))
                {
                    victoryHits++; // Count hits on the victory hole
                    validHits++;   // Count it as a 'safe' hit for the 4-corner count
                    Debug.DrawRay(rayOrigin, Vector3.down * 1.0f, Color.yellow, 2.0f); // Yellow for victory
                }
                // Check if we hit the actual ground layer
                else if (((1 << hit.collider.gameObject.layer) & layerMask) != 0)
                {
                    validHits++;
                    Debug.DrawRay(rayOrigin, Vector3.down * 1.0f, Color.green, 2.0f);
                }
                else
                {
                    // Hit something, but it's not Ground and not LevelPass. This is UNSAFE.
                    Debug.DrawRay(rayOrigin, Vector3.down * 1.0f, Color.red, 2.0f);
                }
            }
            else
            {
                // Ray hit NOTHING! (Empty space) -> UNSAFE
                Debug.DrawRay(rayOrigin, Vector3.down * 1.0f, Color.red, 2.0f);
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
        // Initialize everything in Awake BEFORE OnEnable is called
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

        if (fallTriggerObject != null)
        {
            fallTriggerObject.SetActive(false);
        }
    }

    void Start()
    {
    }

    void OnEnable()
    {
        if (moveAction != null) moveAction.Enable();
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
            if (!isGrounded())
            {
                UnityEngine.Debug.Log("Player not grounded after reconstitution!");
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

        if (fallTriggerObject != null)
        {
            float currentHalfHeight = m_Collider.bounds.extents.y;

            Vector3 topCenter = transform.position + (transform.up * currentHalfHeight);

            fallTriggerObject.transform.position = topCenter;
            fallTriggerObject.transform.parent = this.transform;

            Rigidbody fallTriggerRB = fallTriggerObject.GetComponent<Rigidbody>();
            if (fallTriggerRB != null)
            {
                fallTriggerRB.mass = 500f;
                fallTriggerRB.isKinematic = false;
                fallTriggerRB.useGravity = true;
            }
            fallTriggerObject.SetActive(true);
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = false;
            playerRigidbody.useGravity = true;
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

            // COMMIT POINT: Once we reach 50% of the roll (45 degrees)
            if (!committed && progress >= 0.8f && !isSafe)
            {
                committed = true;
                Debug.Log("Roll halfway done. Committing movement to physics for landing/fall.");

                // Activate physics control. Whether it lands or falls off is now determined by Unity's physics.
                StartFallingFromUnsafeRoll();
                isRolling = false;
                yield break; // Exit the coroutine
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
    }

    public void CopyTransformData(Transform source, Transform target)
    {
        target.position = source.position;
        target.rotation = source.rotation;
        target.localScale = source.localScale;
    }

}