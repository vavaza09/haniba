using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TaxiSeats : MonoBehaviour
{
    [Header("Capacity")]
    [SerializeField, Min(1)] private int capacity = 3;

    [Header("Events")]
    public UnityEvent<Person> OnSeated;       
    public UnityEvent<Person> OnUnseated;     
    public UnityEvent OnSeatFull;             
    public UnityEvent OnSeatFreed;            

   
    private readonly List<Person> _order = new();    
    private readonly HashSet<Person> _set = new();   

    public int Capacity => capacity;
    public int Count => _order.Count;
    public bool HasSpace => _order.Count < capacity;

 
    public IReadOnlyList<Person> CurrentOrder => _order;

    public bool Contains(Person p) => p != null && _set.Contains(p);

    public int GetSeatIndex(Person p) => p != null ? _order.IndexOf(p) : -1;

 
    public bool TrySeat(Person p)
    {
        if (p == null) return false;
        if (!HasSpace) return false;
        if (_set.Contains(p)) return false; 

        _order.Add(p);
        _set.Add(p);

        OnSeated?.Invoke(p);
        if (!HasSpace) OnSeatFull?.Invoke(); 

        return true;
    }


    public bool TryUnseat(Person p)
    {
        if (p == null) return false;
        if (!_set.Contains(p)) return false;

        bool wasFull = !HasSpace; 

        _set.Remove(p);
        _order.Remove(p);

        OnUnseated?.Invoke(p);
        if (wasFull && HasSpace) OnSeatFreed?.Invoke();

        return true;
    }

    
    public void UnseatAll()
    {
       
        var copy = new List<Person>(_order);
        foreach (var p in copy) TryUnseat(p);
    }

  
    public void SetCapacity(int newCapacity)
    {
        if (newCapacity < 1) newCapacity = 1;
        capacity = newCapacity;

        
        while (_order.Count > capacity)
        {
            var last = _order[_order.Count - 1];
            TryUnseat(last);
        }
    }
}
