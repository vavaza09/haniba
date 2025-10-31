using UnityEngine;
using System;

[RequireComponent(typeof(Camera))]
public class CameraMousePan : MonoBehaviour
{
    [Header("Pan settings")]
    public float maxPanAngleX = 15f;
    public float maxPanAngleY = 10f;
    public float smoothing = 8f;
    public float panSpeed = 1f;
    public bool invertY = false;
    public bool lockPanX = false;
    public bool lockPanY = false;

    [Header("Zoom settings")]
    [Tooltip("ระยะที่ Zoom เข้าเมื่อคลิ้กขวาค้าง")]
    public float zoomedFOV = 35f;
    [Tooltip("ความเร็วในการ zoom (ยิ่งมากยิ่งไว)")]
    public float zoomSpeed = 6f;

    public event Action OnZoomStart;
    public event Action OnZoomEnd;
    public bool IsZooming { get; private set; }

    private Quaternion originLocalRotation;
    private Camera cam;
    private float defaultFOV;

    void Start()
    {
        originLocalRotation = transform.localRotation;
        cam = GetComponent<Camera>();
        defaultFOV = cam.fieldOfView;
    }

    void LateUpdate()
    {
        // --- Mouse Pan ---
        Vector2 mouse = Input.mousePosition;
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        Vector2 norm = new Vector2(
            Mathf.Clamp((mouse.x - screenCenter.x) / screenCenter.x, -1f, 1f),
            Mathf.Clamp((mouse.y - screenCenter.y) / screenCenter.y, -1f, 1f)
        );

        float targetYaw = lockPanX ? 0f : (norm.x * maxPanAngleX);
        float targetPitch = lockPanY ? 0f : ((invertY ? -1f : 1f) * norm.y * maxPanAngleY);

        Quaternion targetRotation = originLocalRotation * Quaternion.Euler(-targetPitch, targetYaw, 0f);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * smoothing * panSpeed);

        // --- Right-Click Zoom state changes (events) ---
        if (Input.GetMouseButtonDown(1))
        {
            IsZooming = true;
            OnZoomStart?.Invoke();
        }
        if (Input.GetMouseButtonUp(1))
        {
            IsZooming = false;
            OnZoomEnd?.Invoke();
        }

        // --- Apply FOV each frame based on current state ---
        float targetFov = IsZooming ? zoomedFOV : defaultFOV;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * zoomSpeed);
    }

    public void ResetOriginToCurrent()
    {
        originLocalRotation = transform.localRotation;
    }
}
