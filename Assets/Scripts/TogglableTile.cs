using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TogglableTile : MonoBehaviour
{
    [Header("Settings")]
    public float duration = 0.5f;
    public Ease easeType = Ease.InOutQuad;
    public bool startVisible = true;

    [Header("Hinge Physics")]
    public Vector3 pivotOffset;
    public Vector3 rotationAxis = Vector3.right;
    public float rotationAngle = 90f;

    private Vector3 _startPos;
    private Quaternion _startRot;
    private Vector3 _worldPivot;
    private float _currentT = 0f;
    private Collider _col;

    private void Awake()
    {
        _col = GetComponent<Collider>();
        _startPos = transform.position;
        _startRot = transform.rotation;
        _worldPivot = _startPos + transform.TransformDirection(pivotOffset);
    }

    private void Start()
    {
        float initialT = startVisible ? 0f : 1f;
        ApplyRotation(initialT);

        if (_col != null) _col.enabled = startVisible;
    }

    public void SetTileState(bool isVisible)
    {
        transform.DOKill();

        float targetT = isVisible ? 0f : 1f;

        DOVirtual.Float(_currentT, targetT, duration, ApplyRotation)
            .SetEase(easeType)
            .OnComplete(() => {
                if (_col != null) _col.enabled = isVisible;
            });

        if (isVisible && _col != null) _col.enabled = true;
    }

    private void ApplyRotation(float t)
    {
        _currentT = t;
        transform.position = _startPos;
        transform.rotation = _startRot;
        transform.RotateAround(_worldPivot, rotationAxis, rotationAngle * t);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 pivot = UnityEngine.Application.isPlaying ? _worldPivot : transform.position + transform.TransformDirection(pivotOffset);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pivot, 0.05f);
        Gizmos.DrawLine(transform.position, pivot);
    }
}