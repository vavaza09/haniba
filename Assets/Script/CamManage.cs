using UnityEngine;

public class CamManage : MonoBehaviour
{
    [Header("Cameras")]
    public Camera playerCamera;          // your normal camera (has CameraMousePan)
    public Camera mirrorCamera;          // the alt camera to show when zoom starts over the mirror

    [Header("Disble objcct while in mirror mode")]
    [Tooltip("GameObjects to SetActive(false) while mirror mode is on")]
    public GameObject[] disableOnMirrorMode;

    [Header("Mirror detection")]
    public float maxRayDistance = 5f;
    public bool useTag = true;
    public string mirrorTag = "Mirror";
    public LayerMask mirrorLayer;        // used if useTag == false

    [Header("Blink")]
    public BlinkOverlay blink;           // assign in Inspector (optional — will auto-create)

    private CameraMousePan playerPan;    // taken from playerCamera
    private bool mirrorMode;
    private bool transitionBusy;         // prevents double-triggers during blink

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

        // Start with player cam rendering, mirror cam off
        playerCamera.enabled = true;
        mirrorCamera.enabled = false;
        SetHidden(false);

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

    void HandleZoomStart()
    {
        if (transitionBusy || mirrorMode) return;   // only from origin camera

        // Only switch if mouse is currently pointing at the mirror
        if (IsMouseOverMirror())
        {
            transitionBusy = true;
            // Fade to black -> swap -> fade back
            blink.Blink(() =>
            {
                EnterMirrorMode();
            });

            // Clear busy slightly after the fade finishes
            Invoke(nameof(ClearBusyAfterBlink), blink.fadeOut + blink.holdBlack + blink.fadeIn + 0.02f);
        }
    }

    void HandleZoomEnd()
    {
        if (transitionBusy) return;
        if (!mirrorMode) return;

        transitionBusy = true;
        blink.Blink(() =>
        {
            ExitMirrorMode();
        });

        Invoke(nameof(ClearBusyAfterBlink), blink.fadeOut + blink.holdBlack + blink.fadeIn + 0.02f);
    }

    void ClearBusyAfterBlink() => transitionBusy = false;

    void EnterMirrorMode()
    {
        mirrorMode = true;

        // Toggle which camera renders
        playerCamera.enabled = false;
        mirrorCamera.enabled = true;

        SetHidden(true);
    }

    void ExitMirrorMode()
    {
        mirrorMode = false;

        mirrorCamera.enabled = false;
        playerCamera.enabled = true;

        SetHidden(false);
    }

    bool IsMouseOverMirror()
    {
        // Only raycast when on the origin camera (not in mirror mode, not during transition)
        if (mirrorMode || transitionBusy || !playerCamera.enabled) return false;

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

    void SetHidden(bool hidden)
    {
        if (disableOnMirrorMode == null) return;
        foreach (var go in disableOnMirrorMode)
        {
            if (go) go.SetActive(!hidden); // hide while mirrorMode = true
        }
    }
}
