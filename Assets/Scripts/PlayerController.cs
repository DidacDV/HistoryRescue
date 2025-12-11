using System;
using System.Collections; 
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider))]
public class PlayerController : MonoBehaviour
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

    bool IsStandingUpright() { return m_Collider.bounds.size.y > 1.5f; }

    bool isGrounded()
    {
        float distToGround = m_Collider.bounds.extents.y;
        return Physics.Raycast(transform.position, Vector3.down, distToGround + 0.1f, layerMask);
    }

    void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        layerMask = LayerMask.GetMask("Ground");
        m_Collider = GetComponent<Collider>();

        pivot = new GameObject("RotationPivot").transform;
        ghostPivot = new GameObject("GhostPivot").transform;
        if (ghostPlayer == null)
        {
            ghostPlayer = new GameObject("GhostPlayer_TEMP").transform;
            ghostPlayer.gameObject.SetActive(false);
        }

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
        
        if (bFalling)
        {
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime, Space.World);
            if (transform.position.y < -5.0f) HandleFalling();
        }
        else if (!isRolling)
        {

            if (!isGrounded())
            {
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

        isRolling = true;

        UIManager.Instance.IncrementMovement();

        float angle = 90f;
        float rollDuration = 90f / rotSpeed;

        Vector3 axis = GetAxis(direction);
        Vector3 directionVector = GetDirectionVector(direction);
        Vector2 pivotOffset = GetPivotOffset(direction);

        Vector3 pivotPosition = transform.position +
                                (directionVector * pivotOffset.x) +
                                (Vector3.down * pivotOffset.y);

        pivot.position = pivotPosition;

        CopyTransformData(transform, ghostPlayer);

        ghostPlayer.RotateAround(pivotPosition, axis, angle);

        float elapsedTime = 0f;

        while (elapsedTime < rollDuration)
        {
            elapsedTime += Time.deltaTime;

            float rotationRate = angle * (Time.deltaTime / rollDuration);

            transform.RotateAround(pivotPosition, axis, rotationRate);
            yield return null;
        }

        transform.rotation = ghostPlayer.rotation;

        Vector3 finalPos = ghostPlayer.position;

        float snappedX = Mathf.Round(finalPos.x * 2) / 2f;
        float snappedZ = Mathf.Round(finalPos.z * 2) / 2f;

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
    }

}