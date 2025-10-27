using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PersonManager : MonoBehaviour
{
    [Header("Data Source (ScriptableObject)")]
    [SerializeField] private PersonSet personSet;   

    [Header("Limits")]
    [SerializeField, Min(1)] private int maxActive = 3; 

    [Header("Events")]
    public UnityEvent<Person> OnPersonAdded;
    public UnityEvent<int> OnPersonRemoved;
    public UnityEvent OnAddFailedFull;                 
    public UnityEvent<int> OnAddFailedNotFound;        
    public UnityEvent<int> OnAddFailedDuplicate;      

  
    private readonly Dictionary<int, Person> _active = new();

    
    private void Awake()
    {
        if (personSet == null)
        {
            Debug.LogError("PersonManager: ยังไม่ได้อ้าง PersonSet (ScriptableObject).", this);
            return;
        }
        personSet.RebuildIndex(); 
    }

    private void OnValidate()
    {
        if (personSet != null) personSet.RebuildIndex();
    }

    
    public bool HasSpace => _active.Count < maxActive;
    public int ActiveCount => _active.Count;
    public bool ContainsId(int id) => _active.ContainsKey(id);
    public IReadOnlyCollection<int> GetActiveIds() => _active.Keys;

   
    public bool AddById(int id, Vector3 position, Quaternion? rotation = null)
    {
        if (!HasSpace)
        {
            OnAddFailedFull?.Invoke();
            return false;
        }

        if (_active.ContainsKey(id))
        {
            OnAddFailedDuplicate?.Invoke(id);
            return false;
        }

        if (personSet == null || !personSet.TryGetPrefabById(id, out var prefab) || prefab == null)
        {
            OnAddFailedNotFound?.Invoke(id);
            return false;
        }

        var inst = Instantiate(prefab, position, rotation ?? Quaternion.identity);
        _active[id] = inst;
        OnPersonAdded?.Invoke(inst);
        return true;
    }

   
    public bool RemoveById(int id)
    {
        if (!_active.TryGetValue(id, out var inst) || inst == null) return false;

        _active.Remove(id);
        Destroy(inst.gameObject);
        OnPersonRemoved?.Invoke(id);
        return true;
    }

  
    public void RemoveAll()
    {
        var ids = new List<int>(_active.Keys);
        foreach (var id in ids) RemoveById(id);
    }
}
