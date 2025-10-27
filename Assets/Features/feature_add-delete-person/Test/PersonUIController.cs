using UnityEngine;
using TMPro; // ถ้าใช้ TextMeshPro

public class PersonUIController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] PersonManager personManager;
    [SerializeField] TMP_InputField idInput;
    [SerializeField] Transform spawnAnchor;   // จุดเกิด (วาง Empty ไว้ในฉาก)
    [SerializeField] TMP_Text toast;          // ไว้แจ้งเตือน (optional)

    Vector3 SpawnPos => spawnAnchor ? spawnAnchor.position : Vector3.zero;

    // ปุ่ม Add (อ่าน id จาก input)
    public void AddFromInput()
    {
        if (!TryGetId(out int id)) return;

        bool ok = personManager.AddById(id, SpawnPos);
        if (!ok) ShowFailReason(id);
        else Show("Added " + id);
    }

    // ปุ่ม Remove (อ่าน id จาก input)
    public void RemoveFromInput()
    {
        if (!TryGetId(out int id)) return;

        bool ok = personManager.RemoveById(id);
        Show(ok ? "Removed " + id : "No active " + id);
    }

    // ปุ่ม Add แบบมีพารามิเตอร์ (ใช้กับ UnityEvent ใส่ int ตรงๆ ได้)
    public void AddByIdParam(int id)
    {
        bool ok = personManager.AddById(id, SpawnPos);
        if (!ok) ShowFailReason(id);
        else Show("Added " + id);
    }

    // ปุ่ม Remove แบบมีพารามิเตอร์
    public void RemoveByIdParam(int id)
    {
        bool ok = personManager.RemoveById(id);
        Show(ok ? "Removed " + id : "No active " + id);
    }

    bool TryGetId(out int id)
    {
        id = 0;
        if (idInput == null || string.IsNullOrWhiteSpace(idInput.text))
        {
            Show("กรุณาใส่ ID");
            return false;
        }
        if (!int.TryParse(idInput.text, out id))
        {
            Show("ID ต้องเป็นตัวเลข");
            return false;
        }
        return true;
    }

    void ShowFailReason(int id)
    {
        // ยิงตามอีเวนต์ก็ได้ แต่สรุปสั้นๆให้ผู้เล่นรู้สาเหตุ
        if (!personManager.HasSpace) { Show("เต็มแล้ว (limit)"); return; }
        if (personManager.ContainsId(id)) { Show("ID นี้อยู่บนซีนแล้ว"); return; }
        Show("ไม่พบ ID ใน PersonSet");
    }

    void Show(string msg)
    {
        if (toast) toast.text = msg;
        else Debug.Log(msg);
    }
}
