using System.Collections;
using UnityEngine;

public class DialogTestDriver : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private DialogController dialog;   // ใส่ตัวที่อัปเดตไว้
    [SerializeField] private Person waitingPerson;      // คนที่ “ยืนรอ” ในซีน
    [SerializeField] private TaxiSeats seats;           // ตัวคุมที่นั่ง
    [SerializeField] private PersonManager personManager;

    [Header("Test Flow")]
    [SerializeField] private float rideDelaySeconds = 5f;     // ข้อ 3: ดีเลย์ 5 วิ
    [SerializeField] private string rideEntryNodeId = "halfway_chat"; // node ใน rideSet

    void Start()
    {
        // สมัครฟังอีเวนต์จาก DialogController
        dialog.OnPickupDecision.AddListener(OnPickupDecision);
        dialog.OnRequestDespawn.AddListener(OnRequestDespawn);
        dialog.OnJobCompleted.AddListener(OnJobCompleted);
        dialog.OnGhostRefused.AddListener(OnGhostRefused);

        // เริ่มเทสต์: จำลอง “ถึงจุดรับ”
        if (waitingPerson != null)
            dialog.NotifyReachedPickup(waitingPerson);
        else
            Debug.LogError("TestDriver: waitingPerson ยังไม่ถูกเซ็ต");
    }

    void OnDestroy()
    {
        dialog.OnPickupDecision.RemoveListener(OnPickupDecision);
        dialog.OnRequestDespawn.RemoveListener(OnRequestDespawn);
        dialog.OnJobCompleted.RemoveListener(OnJobCompleted);
        dialog.OnGhostRefused.RemoveListener(OnGhostRefused);
    }

    // ===== Event handlers =====

    // ผู้เล่นตัดสินใจรับ/ไม่รับ
    void OnPickupDecision(int personId, bool accepted)
    {
        Debug.Log($"[Test] PickupDecision id={personId}, accepted={accepted}");

        if (!accepted)
        {
            // ไม่รับ → จบเคสนี้
            return;
        }

        // รับแล้ว → รอ 5 วิ แล้วเปิด ride node ตามที่กำหนด
        StartCoroutine(CoRideThenDropoff());
    }

    // ขอให้ despawn (ตอนปฏิเสธ หรือหลังส่งมนุษย์ลง ถ้า Spawn-team เป็น owner)
    void OnRequestDespawn(int personId)
    {
        Debug.Log($"[Test] RequestDespawn id={personId}");

        // ในโหมดเทสต์นี้ เราถือว่าซีนนี้ owner อยู่ที่เราเอง → ทำลาย waitingPerson ถ้ายังอยู่
        if (waitingPerson != null && waitingPerson.Data != null && waitingPerson.Data.id == personId)
        {
            Destroy(waitingPerson.gameObject);
            waitingPerson = null;
        }
        else
        {
            // หรือหาใน scene คร่าว ๆ (กรณีเป็น CurrentPassenger ที่เพิ่งลง)
            var inst = personManager.GetInstanceById(personId);
            if (inst != null) Destroy(inst.gameObject);
        }
    }

    // ส่งมนุษย์ลงสำเร็จ
    void OnJobCompleted(int personId)
    {
        Debug.Log($"[Test] JobCompleted id={personId}");
    }

    // ผีไม่ลงที่จุดหมาย
    void OnGhostRefused(int personId)
    {
        Debug.Log($"[Test] GhostRefused id={personId} (ค้างบนรถ)");
    }

    // ===== Test sequence =====
    IEnumerator CoRideThenDropoff()
    {
        // ข้อ 3: ดีเลย์ 5 วินาที แล้วเปิด ride dialog
        yield return new WaitForSeconds(rideDelaySeconds);

        var current = dialog.CurrentPassenger;
        if (current == null)
        {
            Debug.LogWarning("[Test] ไม่มี CurrentPassenger หลังรับ – ยกเลิก");
            yield break;
        }

        dialog.NotifyRideTrigger(rideEntryNodeId);

        // รอเพิ่มอีกหน่อยให้ผู้เล่นเลือกใน ride dialog (ปรับได้ตามใจ)
        yield return new WaitForSeconds(3f);

        // ข้อ 4: ถึงที่ส่ง → แจ้ง dropoff
        dialog.NotifyReachedDropoff(dialog.CurrentPassenger);
    }
}
