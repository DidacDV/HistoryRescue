using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider))]
public class PlayerSegmentController : MonoBehaviour
{
    [HideInInspector] public Transform OtherSegment;
    public enum Direction { None, Forward, Backward, Left, Right }

    
    public float rotSpeed = 360f;
    public float fallSpeed = 5f;
    public LayerMask layerMask;
    public UnityEvent OnFellOff;
    private bool hasControl = false;
    private bool isRolling = false;
    private bool bFalling = false;
    private Collider m_Collider;
    private Transform pivot;
    private InputAction moveAction;

    public void SetControl(bool controlStatus)
    {
        hasControl = controlStatus;
    }

    void Awake()
    {
        
        moveAction = InputSystem.actions.FindAction("MoveSegments");
        m_Collider = GetComponent<Collider>();
        layerMask = LayerMask.GetMask("Ground");
        pivot = new GameObject(gameObject.name + "_SegmentPivot").transform;
    }

    void Start()
    {
    }

    void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.Enable();
        }
    }

    void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.Disable();
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

        if (hasControl && !isRolling && !bFalling)
        {
            CheckGroundAndFall();

            if (!bFalling)
            {
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
        else if (bFalling)
        {
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime, Space.World);
            if (transform.position.y < -5.0f)
            {
                OnFellOff?.Invoke();
                Destroy(gameObject);
            }
        }
    }

    private void CheckGroundAndFall()
    {
        float distToGround = m_Collider.bounds.extents.y;

        if (!Physics.Raycast(transform.position, Vector3.down, distToGround + 0.5f, layerMask))
        {
            bFalling = true;
        }
    }

    private IEnumerator RollToDirection(Direction direction)
    {
        isRolling = true;

        float angle = 90f;
        float rollDuration = 90f / rotSpeed;

        Vector3 axis = GetAxis(direction);
        Vector3 directionVector = GetDirectionVector(direction);
        float extent = 0.5f;

        Vector3 pivotPosition = transform.position +
                                (directionVector * extent) +
                                (Vector3.down * extent);

        Vector3 targetCenter = transform.position + directionVector;
        targetCenter.y = 0.5f;
        UIManager.Instance.IncrementMovement();
        if (OtherSegment != null)
        {
            Vector3 partnerCenter = OtherSegment.position;
            partnerCenter.y = 0.5f;

            if (Vector3.Distance(targetCenter, partnerCenter) < 0.1f)
            {
                isRolling = false;
                yield break;
            }
        }

        pivot.position = pivotPosition;

        float elapsedTime = 0f;

        while (elapsedTime < rollDuration)
        {
            elapsedTime += Time.deltaTime;
            float rotationRate = angle * (Time.deltaTime / rollDuration);

            transform.RotateAround(pivotPosition, axis, rotationRate);
            yield return null;
        }

        transform.position = targetCenter;

        transform.rotation = Quaternion.Euler(
            Mathf.Round(transform.eulerAngles.x / 90f) * 90f,
            Mathf.Round(transform.eulerAngles.y / 90f) * 90f,
            Mathf.Round(transform.eulerAngles.z / 90f) * 90f
        );

        isRolling = false;
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
}