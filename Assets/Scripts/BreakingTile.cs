using DG.Tweening;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BreakingTileSimple : MonoBehaviour
{
    public interface IStandingChecker
    {
        bool IsStandingUpright();
    }

    [Header("Settings")]
    public float breakDelay = 0.5f;
    public float tweakDistance = -0.05f;
    public float tweakDuration = 0.08f;
    public float uprightHeightThreshold = 1.0f;
    public float playerGroundCheckDistance = 0.15f;
    public LayerMask groundLayer = 0;

    [Header("Visuals")]
    public Color alertColor = new Color(1f, 0.5f, 0f, 1f);

    Renderer _renderer;

    Collider _col;
    bool _isBreaking = false;
    bool _isBroken = false;
    Vector3 _initialLocalPos;

    void Awake()
    {
        _col = GetComponent<Collider>();
        if (_col == null)
        {
            enabled = false;
            return;
        }

        _renderer = GetComponent<Renderer>();
        if (_renderer != null && _renderer.material != null)
        {
            _renderer.material.color = alertColor;
        }

        _initialLocalPos = transform.localPosition;

        if (groundLayer == 0)
            groundLayer = LayerMask.GetMask("Ground");
    }

    void Update()
    {
        if (_isBreaking || _isBroken) return;

        Bounds b = _col.bounds;
        Vector3 overlapHalfExtents = b.extents * 0.9f;

        Collider[] hits = Physics.OverlapBox(
            b.center,
            overlapHalfExtents,
            Quaternion.identity,
            -1,
            QueryTriggerInteraction.Collide
        );

        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (hit == _col) continue;

            if (!hit.CompareTag("Player"))
            {
                continue;
            }

            IStandingChecker standingChecker = hit.GetComponentInParent<IStandingChecker>();
            bool isUpright;

            if (standingChecker != null)
            {
                isUpright = standingChecker.IsStandingUpright();
            }
            else
            {
                float playerHeight = hit.bounds.size.y;
                isUpright = playerHeight >= uprightHeightThreshold;
            }

            Vector3 playerPos = hit.transform.position;
            Vector3 rayStart = new Vector3(playerPos.x, hit.bounds.min.y + 0.01f, playerPos.z);
            bool grounded = Physics.Raycast(rayStart, Vector3.down, playerGroundCheckDistance, groundLayer);

            if (isUpright && !grounded)
            {
                StartCoroutine(BreakSequence());
                return;
            }
        }
    }

    IEnumerator BreakSequence()
    {
        _isBreaking = true;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOLocalMoveY(_initialLocalPos.y + tweakDistance, tweakDuration / 2).SetEase(Ease.OutSine));
        seq.Append(transform.DOLocalMoveY(_initialLocalPos.y, tweakDuration / 2).SetEase(Ease.InSine));

        yield return seq.WaitForCompletion();

        yield return new WaitForSeconds(breakDelay);

        if (!FinalCheckForPlayer())
        {
            _isBreaking = false;
            yield break;
        }

        _isBroken = true;
        _col.enabled = false;

        transform.DOMoveY(transform.position.y - 10f, 1f).SetEase(Ease.InSine).SetDelay(0.05f);

        Destroy(gameObject, 2.0f);
    }

    bool FinalCheckForPlayer()
    {
        Bounds b = _col.bounds;
        Vector3 overlapHalfExtents = b.extents * 0.9f;

        Collider[] hits = Physics.OverlapBox(
            b.center,
            overlapHalfExtents,
            Quaternion.identity,
            -1,
            QueryTriggerInteraction.Collide
        );

        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (hit == _col) continue;
            if (!hit.CompareTag("Player")) continue;

            IStandingChecker standingChecker = hit.GetComponentInParent<IStandingChecker>();
            bool isUpright;

            if (standingChecker != null)
            {
                isUpright = standingChecker.IsStandingUpright();
            }
            else
            {
                float playerHeight = hit.bounds.size.y;
                isUpright = playerHeight >= uprightHeightThreshold;
            }

            Vector3 playerPos = hit.transform.position;
            Vector3 rayStart = new Vector3(playerPos.x, hit.bounds.min.y + 0.01f, playerPos.z);
            bool grounded = Physics.Raycast(rayStart, Vector3.down, playerGroundCheckDistance, groundLayer);

            if (isUpright && !grounded)
            {
                return true;
            }
        }

        return false;
    }

    void OnDrawGizmosSelected()
    {
        if (_col == null) _col = GetComponent<Collider>();
        if (_col == null) return;

        Bounds b = _col.bounds;
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
        Gizmos.DrawCube(b.center, b.size * 0.9f);
    }
}