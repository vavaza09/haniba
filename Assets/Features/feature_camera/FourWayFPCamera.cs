using UnityEngine;

public class FourWayFPCamera : MonoBehaviour
{
    public enum Direction { Front = 0, Right = 1, Back = 2, Left = 3 }
    public enum SnapMode { StepInput, FreeAimSnap }

    [Header("References")]
    [Tooltip("ตัวกล้องจริงๆ (เช่น Main Camera). ถ้าเว้นว่างจะใช้ transform ของสคริปต์นี้")]
    public Transform cam;

    [Tooltip("Anchor ทิศหน้า (หันไปทาง forward ของ Anchor นี้)")]
    public Transform frontAnchor;
    [Tooltip("Anchor ทิศขวา")]
    public Transform rightAnchor;
    [Tooltip("Anchor ทิศหลัง")]
    public Transform backAnchor;
    [Tooltip("Anchor ทิศซ้าย")]
    public Transform leftAnchor;

    [Header("Behavior")]
    public SnapMode snapMode = SnapMode.StepInput;
    [Tooltip("ความเร็วการหมุน (deg/sec) เมื่อ Smooth")]
    public float rotateSpeed = 720f;
    [Tooltip("ล็อคทันทีที่สั่งหัน")]
    public bool instantSnap = false;

    [Header("Free-Aim Settings")]
    [Tooltip("เปิดเมาส์ H (Yaw) เพื่อหันอิสระ แล้วดูดล็อคเมื่อใกล้มุมของ Anchor")]
    public bool enableMouseYaw = false;
    [Tooltip("ความไวเมาส์สำหรับ Yaw")]
    public float mouseSensitivity = 2f;
    [Tooltip("มุม (องศา) ที่ถือว่า \"ใกล้พอ\" แล้วดูดล็อคเข้ามุม Anchor")]
    public float captureAngle = 8f;

    [Header("Debug")]
    public Direction current = Direction.Front;

    // internal
    float targetYaw;     // เป้าหมายองศา Y
    float currentYaw;    // องศา Y ปัจจุบัน (เราจะคุมเอง)
    Transform[] anchors;

    void Awake()
    {
        if (cam == null) cam = transform;

        anchors = new Transform[4];
        anchors[(int)Direction.Front] = frontAnchor;
        anchors[(int)Direction.Right] = rightAnchor;
        anchors[(int)Direction.Back] = backAnchor;
        anchors[(int)Direction.Left] = leftAnchor;

        // ตรวจ anchor ต้องครบ
        for (int i = 0; i < 4; i++)
        {
            if (anchors[i] == null)
            {
                Debug.LogError($"[FourWayFPCamera] Anchor {(Direction)i} ยังไม่ถูกเซ็ต");
            }
        }

        // เริ่มที่ Front (หรือทิศที่ตั้งค่าไว้)
        targetYaw = GetYawFromAnchor(current);
        currentYaw = GetYawOf(cam.rotation);
        if (instantSnap) currentYaw = targetYaw;
        ApplyYaw();
    }

    void Update()
    {
        // ---------- อินพุตโหมด Step ----------
        if (snapMode == SnapMode.StepInput)
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                Step(-1);
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                Step(+1);
            if (Input.GetKeyDown(KeyCode.W))
                SetDirection(Direction.Front);
            if (Input.GetKeyDown(KeyCode.S))
                SetDirection(Direction.Back);
        }

        // ---------- เมาส์หันอิสระ + Snap ----------
        if (enableMouseYaw)
        {
            float mouseX = Input.GetAxis("Mouse X");
            currentYaw += mouseX * mouseSensitivity;

            // เช็คว่าใกล้มุม Anchor ไหนที่สุด -> ดูดล็อคถ้าเข้า captureAngle
            Direction nearest = GetNearestDirectionByYaw(currentYaw);
            float nearestYaw = GetYawFromAnchor(nearest);
            float delta = Mathf.DeltaAngle(currentYaw, nearestYaw);

            if (Mathf.Abs(delta) <= captureAngle)
            {
                current = nearest;
                targetYaw = nearestYaw;
            }
        }

        // ---------- เดินทางไปหา targetYaw ----------
        if (instantSnap && !enableMouseYaw)
        {
            currentYaw = targetYaw;
        }
        else
        {
            currentYaw = Mathf.MoveTowardsAngle(currentYaw, targetYaw, rotateSpeed * Time.deltaTime);
        }

        ApplyYaw();
    }

    void Step(int dir) // dir: -1 = ซ้าย, +1 = ขวา
    {
        int idx = (int)current;
        idx = (idx + (dir > 0 ? 1 : -1) + 4) % 4;
        SetDirection((Direction)idx);
    }

    public void SetDirection(Direction d)
    {
        current = d;
        targetYaw = GetYawFromAnchor(d);
        if (instantSnap) currentYaw = targetYaw;
    }

    float GetYawFromAnchor(Direction d)
    {
        Transform a = anchors[(int)d];
        if (a == null) return GetYawOf(cam.rotation);

        // ใช้มุม Y ของ forward ของ Anchor (world)
        Vector3 fwd = a.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
        float yaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
        return yaw;
    }

    Direction GetNearestDirectionByYaw(float yawDeg)
    {
        float bestAbs = 999f;
        Direction best = Direction.Front;

        for (int i = 0; i < 4; i++)
        {
            float y = GetYawFromAnchor((Direction)i);
            float d = Mathf.Abs(Mathf.DeltaAngle(yawDeg, y));
            if (d < bestAbs)
            {
                bestAbs = d;
                best = (Direction)i;
            }
        }
        return best;
    }

    float GetYawOf(Quaternion q)
    {
        Vector3 e = q.eulerAngles;
        return e.y;
    }

    void ApplyYaw()
    {
        // หมุนรอบแกน Y เท่านั้น (มุมมอง FPS ล้วนๆ ไม่แตะ Pitch ในสคริปต์นี้)
        Vector3 e = cam.eulerAngles;
        e.y = currentYaw;
        cam.eulerAngles = e;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // ช่วยวาดเส้นทิศทางของ Anchor
        Gizmos.color = Color.cyan;
        DrawAnchorGizmo(frontAnchor);
        Gizmos.color = Color.green;
        DrawAnchorGizmo(rightAnchor);
        Gizmos.color = Color.yellow;
        DrawAnchorGizmo(backAnchor);
        Gizmos.color = Color.magenta;
        DrawAnchorGizmo(leftAnchor);
    }

    void DrawAnchorGizmo(Transform t)
    {
        if (t == null) return;
        Vector3 p = t.position;
        Vector3 f = t.forward;
        Gizmos.DrawSphere(p, 0.06f);
        Gizmos.DrawLine(p, p + f * 0.7f);
    }
#endif
}
