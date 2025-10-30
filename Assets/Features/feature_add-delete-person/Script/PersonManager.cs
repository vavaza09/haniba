using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PersonManager : MonoBehaviour
{
    [Header("Capacity")]
    [SerializeField, Min(1)] private int capacity = 3;

    [Header("Active")]
    [SerializeField] private List<Person> _active = new();

    [Header("Events")]
    public UnityEvent<Person> OnPersonAdded;    
    public UnityEvent<Person> OnPersonRemoved;   
    public UnityEvent OnFull;                    
    public UnityEvent OnFreed;                  

    public int Capacity => capacity;
    public int ActiveCount => _active.Count;
    public bool HasSpace => _active.Count < capacity;

    public IReadOnlyList<Person> Active => _active;
    public bool Contains(Person p) => p && _active.Contains(p);
    public bool ContainsId(int id) => _active.Exists(p => p && p.Data && p.Data.id == id);

    public IEnumerable<int> ActiveIds()
    {
        foreach (var p in _active)
            if (p && p.Data) yield return p.Data.id;
    }

    public bool AddExisting(Person instance)
    {
        if (instance == null) return false;
        if (_active.Contains(instance)) return false;
        if (!HasSpace) return false;

        _active.Add(instance);

     
        instance.OnAddedToTaxi();

        OnPersonAdded?.Invoke(instance);
        if (!HasSpace) OnFull?.Invoke();
        return true;
    }

    public bool Remove(Person p, bool ownInstance = true)
    {
        if (p == null) return false;

        bool wasFull = !HasSpace;

        if (_active.Remove(p))
        {
            p.OnRemovedFromTaxi();
            OnPersonRemoved?.Invoke(p);

            if (wasFull && HasSpace) OnFreed?.Invoke();

            if (ownInstance)
                Destroy(p.gameObject);

            return true;
        }
        return false;
    }

    public Person GetInstanceById(int id)
        => _active.Find(p => p && p.Data && p.Data.id == id);

    public void RemoveAll(bool ownInstances = true)
    {
        
        var copy = new List<Person>(_active);
        foreach (var p in copy) Remove(p, ownInstances);
    }

    public void SetCapacity(int newCapacity, bool ownInstances = true)
    {
        if (newCapacity < 1) newCapacity = 1;
        capacity = newCapacity;

        while (_active.Count > capacity)
        {
          
            var last = _active[_active.Count - 1];
            Remove(last, ownInstances);
        }
    }
}
