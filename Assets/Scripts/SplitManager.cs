using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class SplitGroup
{
    public List<BaseSwitch> controllingSwitches = new List<BaseSwitch>();
    public GameObject tile1;
    public GameObject tile2;
    [HideInInspector] public int activeSwitchCount = 0;
}

public class SplitManager : MonoBehaviour, ISwitchListener
{
    public PlayerController playerController;
    public GameObject segmentPrefab;
    public GameObject shieldVfxPrefab;
    public float shieldDuration = 1.0f;
    public InputActionReference swapAction;
    [SerializeField] private List<SplitGroup> splitGroups = new List<SplitGroup>();

    [Header("First Split Voice & Subtitles")]
    public bool showSubtitleOnFirstSplit = false;
    public AudioClip splitVoiceClip;
    [TextArea(3, 10)]
    public string splitSubtitleText;
    [SerializeField] private AudioClip swapSound;

    private Dictionary<ISwitchSource, SplitGroup> sourceToGroupMap = new Dictionary<ISwitchSource, SplitGroup>();
    private bool isSplit = false;
    private GameObject segment1, segment2;
    private PlayerSegmentController segment1Controller, segment2Controller;
    private GameObject currentControlledSegment;

    private bool hasPlayedSplitSubtitle = false;

    void Awake()
    {
        foreach (var group in splitGroups)
        {
            foreach (var s in group.controllingSwitches)
            {
                if (s == null) continue;
                s.AddListener(this);
                sourceToGroupMap[s] = group;
                if (s.GetState()) group.activeSwitchCount++;
            }
        }
        if (swapAction?.action != null) swapAction.action.performed += OnSwapPerformed;
    }

    void OnEnable() => swapAction?.action?.Enable();
    void OnDisable() => swapAction?.action?.Disable();

    public void OnSwitchToggled(ISwitchSource source, bool state)
    {
        if (!sourceToGroupMap.TryGetValue(source, out SplitGroup group)) return;

        bool wasActive = group.activeSwitchCount > 0;
        group.activeSwitchCount += state ? 1 : -1;

        if (!wasActive && group.activeSwitchCount > 0 && !isSplit) Split(group);
    }

    void Split(SplitGroup group)
    {
        if (playerController == null || segmentPrefab == null || group.tile1 == null || group.tile2 == null) return;

        var allSwitches = UnityEngine.Object.FindObjectsByType<BaseSwitch>(FindObjectsSortMode.None);
        foreach (var s in allSwitches)
        {
            if (s is PressurePlate p) p.ForceRelease();
            else if (s is CrossPressurePlate cp) cp.ForceRelease();
        }

        InputSystem.actions.FindActionMap("Player")?.Disable();
        InputSystem.actions.FindActionMap("Segments")?.Enable();

        Vector3 p1 = group.tile1.transform.position + new Vector3(0, 0.65f, 0);
        Vector3 p2 = group.tile2.transform.position + new Vector3(0, 0.65f, 0);

        segment1 = Instantiate(segmentPrefab, p1, Quaternion.identity);
        segment2 = Instantiate(segmentPrefab, p2, Quaternion.identity);

        if (segment1.TryGetComponent(out Rigidbody rb1)) { rb1.position = p1; rb1.linearVelocity = Vector3.zero; }
        if (segment2.TryGetComponent(out Rigidbody rb2)) { rb2.position = p2; rb2.linearVelocity = Vector3.zero; }

        segment1Controller = segment1.GetComponent<PlayerSegmentController>();
        segment2Controller = segment2.GetComponent<PlayerSegmentController>();

        LevelManager levelMan = UnityEngine.Object.FindAnyObjectByType<LevelManager>();
        if (levelMan != null)
        {
            levelMan.RegisterSegment(segment1Controller);
            levelMan.RegisterSegment(segment2Controller);
        }

        segment1Controller.OtherSegment = segment2.transform;
        segment2Controller.OtherSegment = segment1.transform;
        segment1Controller.SetControl(true);
        segment2Controller.SetControl(false);
        currentControlledSegment = segment1;

        TriggerShieldVFX(currentControlledSegment);

        if (showSubtitleOnFirstSplit && !hasPlayedSplitSubtitle)
        {
            PlaySplitSubtitle();
        }
        playerController.gameObject.SetActive(false);
        isSplit = true;
    }

    void Update() { if (isSplit) CheckForReconstitution(); }

    private void OnSwapPerformed(InputAction.CallbackContext context) { if (isSplit) SwapControl(); }

    void SwapControl()
    {
        bool isSeg1 = currentControlledSegment == segment1;
        segment1Controller.SetControl(!isSeg1);
        segment2Controller.SetControl(isSeg1);
        currentControlledSegment = isSeg1 ? segment2 : segment1;
        if (swapSound != null)
        {
            AudioManager.Instance.Play(swapSound);
        }
        TriggerShieldVFX(currentControlledSegment);
    }

    void CheckForReconstitution()
    {
        if (segment1 == null || segment2 == null) return;
        Vector3 diff = segment1.transform.position - segment2.transform.position;
        if (Mathf.Abs(diff.y) > 0.15f) return;
        if (Mathf.Abs(new Vector2(diff.x, diff.z).magnitude - 1.0f) > 0.15f) return;
        if (Mathf.Abs(diff.x) < 0.15f || Mathf.Abs(diff.z) < 0.15f) Reconstitute(segment1.transform.position, segment2.transform.position, Mathf.Abs(diff.x) < 0.15f);
    }

    void Reconstitute(Vector3 p1, Vector3 p2, bool rotateForZ)
    {
        InputSystem.actions.FindActionMap("Segments")?.Disable();
        Destroy(segment1); Destroy(segment2);

        Vector3 center = (p1 + p2) / 2f;
        center.y = 0.5f;

        playerController.gameObject.SetActive(true);
        playerController.transform.SetPositionAndRotation(center, rotateForZ ? Quaternion.Euler(90, 0, 0) : Quaternion.Euler(0, 0, 90));
        playerController.ResetState();
        InputSystem.actions.FindActionMap("Player")?.Enable();
        isSplit = false;
    }

    private void TriggerShieldVFX(GameObject target)
    {
        if (shieldVfxPrefab == null || target == null) return;

        GameObject vfx = Instantiate(shieldVfxPrefab, target.transform.position, Quaternion.identity, target.transform);
        vfx.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        Destroy(vfx, shieldDuration);
    }

    private void PlaySplitSubtitle()
    {
        string key = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "_SplitHint";

        if (showSubtitleOnFirstSplit && !GameManager.Instance.HasSeenSubtitle(key))
        {
            if (splitVoiceClip != null)
            {
                AudioManager.Instance.PlayVoice(splitVoiceClip);
                UIManager.Instance.ShowAutoSubtitles(splitSubtitleText, splitVoiceClip.length);
                GameManager.Instance.MarkSubtitleAsSeen(key);
            }
        }
    }
}