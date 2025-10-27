using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Person Set", fileName = "PersonSet")]
public class PersonSet : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        [Tooltip("Prefab ที่ติด Person.cs และมี PersonData ใส่ไว้แล้ว")]
        public Person prefab;
    }

    [Tooltip("ลาก Prefab ของแต่ละคนมาไว้ที่นี่")]
    public List<Entry> entries = new();

    private Dictionary<int, Person> _byId;

    private void OnEnable()
    {
        RebuildIndex();
    }

    public void RebuildIndex()
    {
        _byId = new Dictionary<int, Person>();

        foreach (var e in entries)
        {
            if (e?.prefab == null) continue;
            var p = e.prefab;

            if (p.Data == null)
            {
                Debug.LogWarning($"{p.name}: Prefab ไม่มี PersonData อ้างอิงอยู่", p);
                continue;
            }

            int id = p.Id;
            if (_byId.ContainsKey(id))
            {
                Debug.LogWarning($"พบ id ซ้ำใน PersonSet: {id} (Prefab: {p.name})");
                continue;
            }

            _byId[id] = p;
        }
    }
  
    public bool TryGetPrefabById(int id, out Person prefab)
    {
        if (_byId == null) RebuildIndex();
        return _byId.TryGetValue(id, out prefab);
    }

    public IEnumerable<int> AllIds()
    {
        if (_byId == null) RebuildIndex();
        return _byId.Keys;
    }
}
