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

    public bool IsBroken => _isBroken;

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

    public void TriggerBreak(IStandingChecker checker)
    {
        if (_isBreaking || _isBroken) return;

        bool upright = checker != null && checker.IsStandingUpright();
        UnityEngine.Debug.Log($"[Tile] TriggerBreak on {gameObject.name}. Upright check: {upright}");

        if (upright)
        {
            StartCoroutine(BreakSequence());
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

        // BREAK IMMEDIATELY
        _isBroken = true;
        _col.enabled = false;
        UnityEngine.Debug.Log($"[Tile] {gameObject.name} collider disabled. Falling now.");

        transform.DOMoveY(transform.position.y - 10f, 1f).SetEase(Ease.InSine).SetDelay(0.05f);

        Destroy(gameObject, 2.0f);
    }

    bool FinalCheckForPlayer()
    {
        // Check a 1x1 area directly above the tile, ignoring the collider's offset center
        Vector3 checkCenter = transform.position + Vector3.up * 0.5f;
        Vector3 checkHalfExtents = new Vector3(0.4f, 0.5f, 0.4f);

        Collider[] hits = Physics.OverlapBox(checkCenter, checkHalfExtents, Quaternion.identity);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player") && !hit.name.Contains("Ghost"))
            {
                UnityEngine.Debug.Log($"[Tile] Final Check: Player confirmed on {gameObject.name}");
                return true;
            }
        }
        UnityEngine.Debug.LogWarning($"[Tile] Final Check: Player NOT found on {gameObject.name} at {checkCenter}");
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