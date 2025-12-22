using DG.Tweening;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BreakingTileSimple : MonoBehaviour
{
    public interface IStandingChecker
    {
        bool IsStandingUpright();
        bool IsRolling(); 
    }

    [Header("Settings")]
    public float breakDelay = 0.25f;
    public float tweakDistance = -0.05f;
    public float tweakDuration = 0.08f;
    public float detectionRadius = 0.6f;
    public float detectionHeight = 2.0f; 

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
        _initialLocalPos = transform.localPosition;
    }

    void Update()
    {
        if (_isBreaking || _isBroken) return;

        // Check if there's an upright player on this tile
        IStandingChecker player = FindPlayerOnTile();
        if (player != null && !player.IsRolling() && player.IsStandingUpright())
        {
            UnityEngine.Debug.Log($"[Tile] UPRIGHT player detected on {gameObject.name}. Breaking!");
            StartCoroutine(BreakSequence());
        }
    }

    private IStandingChecker FindPlayerOnTile()
    {
        Vector3 checkCenter = transform.position + Vector3.up * (detectionHeight * 0.5f);
        Vector3 boxSize = new Vector3(detectionRadius * 2f, detectionHeight, detectionRadius * 2f);

        Collider[] hits = Physics.OverlapBox(checkCenter, boxSize * 0.5f);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                IStandingChecker checker = hit.GetComponent<IStandingChecker>();
                if (checker != null)
                {
                    return checker;
                }
            }
        }

        return null;
    }

    IEnumerator BreakSequence()
    {
        _isBreaking = true;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOLocalMoveY(_initialLocalPos.y + tweakDistance, tweakDuration / 2).SetEase(Ease.OutSine));
        seq.Append(transform.DOLocalMoveY(_initialLocalPos.y, tweakDuration / 2).SetEase(Ease.InSine));

        yield return seq.WaitForCompletion();
        yield return new WaitForSeconds(breakDelay);

        _isBroken = true;
        _col.enabled = false;
        transform.DOMoveY(transform.position.y - 10f, 1f).SetEase(Ease.InSine).SetDelay(0.05f);
        Destroy(gameObject, 2.0f);
    }

    void OnDrawGizmosSelected()
    {
        if (_col == null) _col = GetComponent<Collider>();
        if (_col == null) return;

        Bounds b = _col.bounds;
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
        Gizmos.DrawCube(b.center, b.size * 0.9f);
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Vector3 checkCenter = transform.position + Vector3.up * (detectionHeight * 0.5f);
        Vector3 boxSize = new Vector3(detectionRadius * 2f, detectionHeight, detectionRadius * 2f);
        Gizmos.DrawWireCube(checkCenter, boxSize);
    }
}