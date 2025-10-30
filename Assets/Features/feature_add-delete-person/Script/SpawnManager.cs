using UnityEngine;
using System;
public class SpawnManager : MonoBehaviour
{
    // 🔧 ฟังก์ชันลบ Person ตาม id
    public void Despawn(int id)
    {
        if (id < 0)
        {
            Debug.LogWarning("[SpawnManager] Invalid id.");
            return;
        }

        // หา Person ทุกตัวใน Scene (รวม inactive)
        Person[] persons = FindObjectsByType<Person>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (persons == null || persons.Length == 0)
        {
            Debug.LogWarning("[SpawnManager] ไม่มี Person อยู่ในซีนเลย");
            return;
        }

        foreach (var p in persons)
        {
            if (p == null || p.Data == null) continue;

            if (p.Data.id == id)
            {
                Debug.Log($"[SpawnManager] Despawn person id={id}, name={p.Data.displayName}");
                Destroy(p.gameObject);
                return; // จบทันทีเมื่อเจอ
            }
        }

        Debug.LogWarning($"[SpawnManager] ไม่พบ Person id={id} ในซีน");
    }
}
