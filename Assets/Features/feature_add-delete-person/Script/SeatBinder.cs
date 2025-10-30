using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeatBinder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TaxiSeats seats;             
    [SerializeField] private List<Transform> seatAnchors; 

    [Header("Placement")]
    [SerializeField] private bool parentToSeat = true;    

    [Header("Physics (optional)")]
    [SerializeField] private bool disablePhysicsWhenSeated = true; 
    [SerializeField] private bool restorePhysicsWhenUnseated = true;

    
 

    private void Reset()
    {
        // auto try find TaxiSeats บน parent เดียวกัน (เผื่อกด Add Component ใน Inspector)
        if (seats == null) seats = GetComponentInParent<TaxiSeats>();
    }

    private void OnEnable()
    {
        if (seats == null)
        {
            Debug.LogError("SeatBinder: TaxiSeats reference is missing.", this);
            return;
        }

        seats.OnSeated.AddListener(HandleSeated);
        seats.OnUnseated.AddListener(HandleUnseated);
        seats.OnSeatFull.AddListener(RefreshAll);
        seats.OnSeatFreed.AddListener(RefreshAll);

        RefreshAll();
    }

    private void OnDisable()
    {
        if (seats != null)
        {
            seats.OnSeated.RemoveListener(HandleSeated);
            seats.OnUnseated.RemoveListener(HandleUnseated);
            seats.OnSeatFull.RemoveListener(RefreshAll);
            seats.OnSeatFreed.RemoveListener(RefreshAll);
        }

        StopAllCoroutines();
    }

    private void HandleSeated(Person p)
    {
        
        RefreshAll();
    }

    private void HandleUnseated(Person p)
    {
        if (restorePhysicsWhenUnseated) RestorePhysics(p);
        if (parentToSeat && p && p.transform.parent == FindAnchorOf(p))
            p.transform.SetParent(null, true);

        RefreshAll();
    }

    
    public void RefreshAll()
    {
        if (seats == null || seatAnchors == null || seatAnchors.Count == 0) return;

        var order = seats.CurrentOrder;
        int count = Mathf.Min(order.Count, seatAnchors.Count);

       
        for (int i = 0; i < count; i++)
        {
            var person = order[i];
            var anchor = seatAnchors[i];

            if (person == null || anchor == null) continue;
            PlaceAtSeat(person, anchor);
        }

       
        if (order.Count > seatAnchors.Count)
        {
            Debug.LogWarning($"SeatBinder: มีผู้โดยสาร {order.Count} คน แต่มีที่นั่งให้แค่ {seatAnchors.Count}. คนเกินจะไม่ได้ถูกจัดวาง.", this);
        }
    }

    private void PlaceAtSeat(Person person, Transform seat)
    {
        if (!person || !seat) return;

        if (disablePhysicsWhenSeated) DisablePhysics(person);

        if (parentToSeat)
        {
            person.transform.SetParent(seat, worldPositionStays: false);
            person.transform.localPosition = Vector3.zero;
            person.transform.localRotation = Quaternion.identity;
        }
        else
        {
            person.transform.position = seat.position;
            person.transform.rotation = seat.rotation;
        }
    }

    private Transform FindAnchorOf(Person p)
    {
        var order = seats?.CurrentOrder;
        if (order == null) return null;
        int idx = new List<Person>(order).IndexOf(p);
        if (idx < 0 || idx >= seatAnchors.Count) return null;
        return seatAnchors[idx];
    }

    private void DisablePhysics(Person p)
    {
        if (!p) return;
        foreach (var col in p.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        foreach (var rb in p.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void RestorePhysics(Person p)
    {
        if (!p) return;
        foreach (var col in p.GetComponentsInChildren<Collider>(true))
            col.enabled = true;

        foreach (var rb in p.GetComponentsInChildren<Rigidbody>(true))
            rb.isKinematic = false;
    }
}
