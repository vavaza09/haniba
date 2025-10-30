using UnityEngine;
using UnityEngine.Events;

public class SpawnDialogMediator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private DialogController dialog;   

    [Header("Policy")]
    [Tooltip("กันเรียกซ้ำถี่ ๆ จาก collider/trigger")]
    [SerializeField] private float pickupDebounce = 0.25f;

    [Header("Events to SPAWN (commands)")]
    public UnityEvent<int> OnRequestDespawn;  
    public UnityEvent<int> OnJobCompleted;    
    public UnityEvent<int> OnGhostRefused;    

  
    private Person _waitingAtStop;       
    private float _lastPickupTime = -999f;

    // ====== Wiring ======
    void Awake()
    {
        if (!dialog) dialog = FindFirstObjectByType<DialogController>();

       
        dialog.OnPickupDecision.AddListener(HandlePickupDecision);
        dialog.OnRequestDespawn.AddListener(RequestDespawn); 
        dialog.OnJobCompleted.AddListener(JobCompleted);     
        dialog.OnGhostRefused.AddListener(GhostRefused);     
    }

    void OnDestroy()
    {
        if (!dialog) return;
        dialog.OnPickupDecision.RemoveListener(HandlePickupDecision);
        dialog.OnRequestDespawn.RemoveListener(RequestDespawn);
        dialog.OnJobCompleted.RemoveListener(JobCompleted);
        dialog.OnGhostRefused.RemoveListener(GhostRefused);
    }

    // ====== API ที่ "ฝั่ง Spawn" เรียกเข้ามา ======

    /// <summary>รถเข้าโซนรับของคน p → ให้เปิด pickup dialog</summary>
    public void NotifyEnterPickup(Person p)
    {
        if (p == null) return;

        // debounce กันยิงซ้ำถี่ ๆ
        if (Time.time - _lastPickupTime < pickupDebounce) return;
        _lastPickupTime = Time.time;

        // ถ้ามี pickup ของอีกคนเปิดค้างอยู่ ให้ปิดก่อน
        if (_waitingAtStop && _waitingAtStop != p)
        {
            dialog.NotifyLeftPickup(_waitingAtStop);
            _waitingAtStop = null;
        }

        _waitingAtStop = p;
        dialog.NotifyReachedPickup(p);
    }

    /// <summary>รถถอยออก/ออกโซนรับ → ปิด pickup ถ้ายังไม่ได้ตัดสินใจ</summary>
    public void NotifyExitPickup(Person p)
    {
        if (p == null) return;
        if (_waitingAtStop == p)
        {
            dialog.NotifyLeftPickup(p);
            _waitingAtStop = null;
        }
    }

    /// <summary>ถึงจุดส่งของ (คนนั้น ๆ) → ส่งให้ DialogController จัดการลง/ไม่ลง</summary>
    public void NotifyReachedDropoff(Person p)
    {
        if (p == null) return;
        dialog.NotifyReachedDropoff(p);
    }

    // ====== Handlers (จาก DialogController → ส่งกลับหา Spawn) ======

    // ผู้เล่นตัดสินใจตอน pickup
    private void HandlePickupDecision(int personId, bool accepted)
    {
        // ถ้าคือคนที่เรากำลัง track อยู่ → เคลียร์ state
        if (_waitingAtStop && _waitingAtStop.Data && _waitingAtStop.Data.id == personId)
        {
            _waitingAtStop = null;
        }

        if (!accepted)
        {
            // ไม่รับ → ให้ Spawn ลบตัวรอ
            RequestDespawn(personId);
        }
        // ถ้ารับ: ตัวเดิมจะขึ้นรถแล้ว DialogController จะไปเปิด ride ต่อเอง
    }

    private void RequestDespawn(int personId)
    {
        OnRequestDespawn?.Invoke(personId);
    }

    private void JobCompleted(int personId)
    {
        OnJobCompleted?.Invoke(personId);
    }

    private void GhostRefused(int personId)
    {
        OnGhostRefused?.Invoke(personId);
    }
}
