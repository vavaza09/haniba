using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeatBinder : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PersonManager personManager;
    [SerializeField] private List<Transform> seatAnchors = new(); 

    [Header("Placement")]
    [SerializeField] private bool parentToSeat = true; 

    private readonly Dictionary<Person, Coroutine> _moving = new();

    private void OnEnable()
    {
        if (personManager != null)
        {
            personManager.OnPersonAdded.AddListener(OnPersonAdded);
            personManager.OnPersonRemoved.AddListener(OnPersonRemoved);
        }
        RefreshAll();
    }

    private void OnDisable()
    {
        if (personManager != null)
        {
            personManager.OnPersonAdded.RemoveListener(OnPersonAdded);
            personManager.OnPersonRemoved.RemoveListener(OnPersonRemoved);
        }
        StopAllCoroutines();
        _moving.Clear();
    }

    void OnPersonAdded(Person p) => RefreshAll();
    void OnPersonRemoved(int id) => RefreshAll();

    public void RefreshAll()
    {
        if (personManager == null || seatAnchors == null || seatAnchors.Count == 0) return;

        var order = personManager.GetActiveOrder();
        int count = Mathf.Min(order.Count, seatAnchors.Count);

        var targetList = new List<Person>(count);
        for (int i = 0; i < count; i++)
        {
            int id = order[i];
            if (TryGetActiveById(id, out var person))
                targetList.Add(person);
        }

        for (int i = 0; i < seatAnchors.Count; i++)
        {
            if (i < targetList.Count)
            {
                PlaceAtSeat(targetList[i], seatAnchors[i]);
            }
        }
    }

    bool TryGetActiveById(int id, out Person person)
    {
        person = personManager.GetInstanceById(id);
        return person != null;
    }

    void PlaceAtSeat(Person person, Transform seat)
    {
        if (!person || !seat) return;

        
        if (_moving.TryGetValue(person, out var co) && co != null)
        {
            StopCoroutine(co);
            _moving.Remove(person);
        }

        if (parentToSeat)
        {
            
            person.transform.SetParent(seat, worldPositionStays: true);
            person.transform.position = seat.position;
            person.transform.rotation = seat.rotation;
        }

        else
        {
            person.transform.position = seat.position;
            person.transform.rotation = seat.rotation;
            if (parentToSeat)
            {
                person.transform.localPosition = Vector3.zero;
                person.transform.localRotation = Quaternion.identity;
            }
        }
    }
}
