using UnityEngine;

public class CamManage : MonoBehaviour
{
    [Header("Cameras")]
    public Camera playerCamera;          // normal camera (has CameraMousePan)
    public Camera mirrorCamera;          // alt camera when zoom starts over mirror
    public Camera sideCamera;            // new: side camera (manual exit only)

    [Header("Disable objects while in mirror mode")]
    [Tooltip("GameObjects to SetActive(false) while mirror mode is on")]
    public GameObject[] disableOnMirrorMode;

    [Header("Disable objects while in side mode")]
    [Tooltip("Optional: GameObjects to SetActive(false) while side mode is on")]
    public GameObject[] disableOnSideMode;

    [Header("Mirror detection")]
    public float maxRayDistance = 5f;
    public bool useTag = true;
    public string mirrorTag = "Mirror";
    public LayerMask mirrorLayer;        // used if useTag == false

    [Header("Side detection")]
    public float sideMaxRayDistance = 5f;
    public bool sideUseTag = true;
    public string sideTag = "SideCam";
    public LayerMask sideLayer;          // used if sideUseTag == false

    [Header("Blink")]
    public BlinkOverlay blink;           // assign in Inspector (optional — auto-create)

    private CameraMousePan playerPan;    // taken from playerCamera

    private bool mirrorMode;
    private bool sideMode;
    private bool transitionBusy;         // prevents double-triggers during blink

    // Permission gate for side cam
    public bool sidePermission;

    void Awake()
    {
        if (!playerCamera) playerCamera = Camera.main;
        if (!playerCamera)
        {
            Debug.LogError("CamManage: playerCamera is not assigned and no Camera.main found.");
            enabled = false; return;
        }

        playerPan = playerCamera.GetComponent<CameraMousePan>();
        if (!playerPan)
        {
            Debug.LogError("CamManage: playerCamera must have CameraMousePan.");
            enabled = false; return;
        }

        if (!mirrorCamera)
        {
            Debug.LogError("CamManage: mirrorCamera is not assigned.");
            enabled = false; return;
        }

        // sideCamera is optional; only used if assigned
        if (!sideCamera)
        {
            Debug.LogWarning("CamManage: sideCamera not assigned. Side mode will be disabled.");
        }

        // Start with player cam rendering, others off
        playerCamera.enabled = true;
        mirrorCamera.enabled = false;
        if (sideCamera) sideCamera.enabled = false;

        SetHidden(disableOnMirrorMode, false);
        SetHidden(disableOnSideMode, false);

        // Find or auto-create a BlinkOverlay if not assigned
        if (!blink)
        {
            blink = FindObjectOfType<BlinkOverlay>();
            if (!blink)
            {
                var go = new GameObject("BlinkOverlay_Auto");
                blink = go.AddComponent<BlinkOverlay>();
            }
        }
    }

    void OnEnable()
    {
        if (playerPan != null)
        {
            playerPan.OnZoomStart += HandleZoomStart;
            playerPan.OnZoomEnd += HandleZoomEnd;
        }
    }

    void OnDisable()
    {
        if (playerPan != null)
        {
            playerPan.OnZoomStart -= HandleZoomStart;
            playerPan.OnZoomEnd -= HandleZoomEnd;
        }
    }

    // ========================
    // Zoom event handlers
    // ========================
    void HandleZoomStart()
    {
        if (transitionBusy) return;
        if (mirrorMode || sideMode) return; // only from origin camera

        // PRIORITY: Side cam first (if allowed), then mirror.
        if (sideCamera && sidePermission && IsMouseOverSide())
        {
            transitionBusy = true;
            blink.Blink(() =>
            {
                EnterSideMode();
            });
            Invoke(nameof(ClearBusyAfterBlink), blink.fadeOut + blink.holdBlack + blink.fadeIn + 0.02f);
            return;
        }

        // Mirror fallback
        if (IsMouseOverMirror())
        {
            transitionBusy = true;
            blink.Blink(() =>
            {
                EnterMirrorMode();
            });
            Invoke(nameof(ClearBusyAfterBlink), blink.fadeOut + blink.holdBlack + blink.fadeIn + 0.02f);
        }
    }

    void HandleZoomEnd()
    {
        if (transitionBusy) return;

        // Mirror mode auto-exits on unzoom (old behavior)
        if (mirrorMode)
        {
            transitionBusy = true;
            blink.Blink(() =>
            {
                ExitMirrorMode();
            });
            Invoke(nameof(ClearBusyAfterBlink), blink.fadeOut + blink.holdBlack + blink.fadeIn + 0.02f);
        }

        // Side mode: DO NOTHING on unzoom.
        // Must call ExitSideMode() / ExitSideModeWithBlink() manually.
    }

    void ClearBusyAfterBlink() => transitionBusy = false;

    // ========================
    // Mode switches
    // ========================
    void EnterMirrorMode()
    {
        mirrorMode = true;
        sideMode = false;

        playerCamera.enabled = false;
        if (sideCamera) sideCamera.enabled = false;
        mirrorCamera.enabled = true;

        SetHidden(disableOnSideMode, false);
        SetHidden(disableOnMirrorMode, true);
    }

    void ExitMirrorMode()
    {
        mirrorMode = false;

        mirrorCamera.enabled = false;
        // Go back to player cam unless side mode is on (it isn't here)
        playerCamera.enabled = true;

        SetHidden(disableOnMirrorMode, false);
    }

    void EnterSideMode()
    {
        if (!sideCamera) return;

        sideMode = true;
        mirrorMode = false;

        playerCamera.enabled = false;
        mirrorCamera.enabled = false;
        sideCamera.enabled = true;

        SetHidden(disableOnMirrorMode, false);
        SetHidden(disableOnSideMode, true);
    }

    public void ExitSideMode()
    {
        if (!sideMode) return;

        sideMode = false;

        if (sideCamera) sideCamera.enabled = false;
        // Return to player cam (explicit exit only)
        playerCamera.enabled = true;

        SetHidden(disableOnSideMode, false);
    }

    public void ExitSideModeWithBlink()
    {
        if (transitionBusy || !sideMode) return;

        transitionBusy = true;
        blink.Blink(() =>
        {
            ExitSideMode();
        });
        Invoke(nameof(ClearBusyAfterBlink), blink.fadeOut + blink.holdBlack + blink.fadeIn + 0.02f);
    }

    // ========================
    // Permission API for side cam
    // ========================
    public void SetSidePermission(bool allowed)
    {
        sidePermission = allowed;
    }

    public bool GetSidePermission() => sidePermission;

    // ========================
    // Detection
    // ========================
    bool IsMouseOverMirror()
    {
        // Only raycast when on the origin camera
        if (mirrorMode || sideMode || transitionBusy || !playerCamera.enabled) return false;

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        bool hitSomething = useTag
            ? Physics.Raycast(ray, out hit, maxRayDistance, ~0, QueryTriggerInteraction.Ignore)
            : Physics.Raycast(ray, out hit, maxRayDistance, mirrorLayer, QueryTriggerInteraction.Ignore);

        if (!hitSomething) return false;

        if (useTag)
            return hit.collider.CompareTag(mirrorTag);
        else
            return ((1 << hit.collider.gameObject.layer) & mirrorLayer) != 0;
    }

    bool IsMouseOverSide()
    {
        // Only raycast when on the origin camera
        if (mirrorMode || sideMode || transitionBusy || !playerCamera.enabled) return false;

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        bool hitSomething = sideUseTag
            ? Physics.Raycast(ray, out hit, sideMaxRayDistance, ~0, QueryTriggerInteraction.Ignore)
            : Physics.Raycast(ray, out hit, sideMaxRayDistance, sideLayer, QueryTriggerInteraction.Ignore);

        if (!hitSomething) return false;

        if (sideUseTag)
            return hit.collider.CompareTag(sideTag);
        else
            return ((1 << hit.collider.gameObject.layer) & sideLayer) != 0;
    }

    // ========================
    // Helpers
    // ========================
    void SetHidden(GameObject[] list, bool hidden)
    {
        if (list == null) return;
        foreach (var go in list)
        {
            if (go) go.SetActive(!hidden);
        }
    }
}