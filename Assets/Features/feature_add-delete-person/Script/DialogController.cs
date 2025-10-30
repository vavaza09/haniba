using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;



public class DialogController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private DialogUIController ui;
    [SerializeField] private PersonManager personManager;
    [SerializeField] private TaxiSeats seats;

    [Header("Ownership / Integration")]
    [Tooltip("ถ้า true = ทีม Spawn เป็นเจ้าของ instance, ฝั่งเราจะไม่ Destroy ตอนลบ แต่ยิง event ขอให้เขา despawn แทน")]
    [SerializeField] private bool spawnTeamOwnsInstances = true;

    [Header("Debounce / Safety")]
    [SerializeField] private float pickupDebounceSec = 0.25f;

    [Header("Events (ออกไปให้ทีม Spawn/Director)")]
    public UnityEvent<int, bool> OnPickupDecision;  
    public UnityEvent<int> OnRequestDespawn;       
    public UnityEvent<int> OnJobCompleted;          
    public UnityEvent<int> OnGhostRefused;

   
    [Header("Ride timing")]
    [SerializeField] private float rideStartDelay = 5f;  
    [SerializeField] private string rideFirstEntryId = ""; 


    public Person CurrentPassenger { get; private set; }

    private readonly Dictionary<int, PassengerRunState> _run = new();
    private bool _isPlaying = false;
    private float _lastPickupTime = -999f;

    

    
    public void NotifyReachedPickup(Person personAtStop)
    {
        if (personAtStop == null) return;

        // กันโดนยิงซ้ำถี่ ๆ จากคอลลิเดอร์
        if (Time.time - _lastPickupTime < pickupDebounceSec) return;
        _lastPickupTime = Time.time;

        if (_isPlaying) return; // ไม่เปิดซ้อน

        var profile = personAtStop.Data?.dialogProfile;
        var set = profile?.pickupSet;
        if (set == null)
        {
            Debug.LogWarning($"[DialogController] {personAtStop.name} ไม่มี pickupSet");
            // แจ้งว่าไม่รับเพื่อให้ flow ไม่ค้าง
            OnPickupDecision?.Invoke(personAtStop.Data?.id ?? -1, false);
            // ขอ despawn คนรอ (ตามนโยบาย)
            if (spawnTeamOwnsInstances) OnRequestDespawn?.Invoke(personAtStop.Data?.id ?? -1);
            return;
        }

        var node = FindNode(set, set.entryNodeId);
        if (node == null)
        {
            Debug.LogWarning($"[DialogController] pickup entry node ไม่พบ: {set.entryNodeId}");
            OnPickupDecision?.Invoke(personAtStop.Data?.id ?? -1, false);
            if (spawnTeamOwnsInstances) OnRequestDespawn?.Invoke(personAtStop.Data?.id ?? -1);
            return;
        }

        StartCoroutine(CoPlayNode(personAtStop, set, node, isPickup: true));
    }

    
    public void NotifyLeftPickup(Person personAtStop)
    {
        if (_isPlaying) ui.Close(() => _isPlaying = false);
    }

    
    public void NotifyRideTrigger(string rideEntryNodeId)
    {
        if (CurrentPassenger == null || string.IsNullOrEmpty(rideEntryNodeId)) return;

        var profile = CurrentPassenger.Data?.dialogProfile;
        var set = profile?.rideSet;
        if (set == null) return;

        var node = FindNode(set, rideEntryNodeId);
        if (node == null) return;

        if (_isPlaying) return;
        StartCoroutine(CoPlayNode(CurrentPassenger, set, node, isPickup: false));
    }

    
    public void NotifyReachedDropoff(Person personAtDrop)
    {
        if (personAtDrop == null) return;
       
        if (CurrentPassenger != null && personAtDrop != CurrentPassenger) return;

        HandleDropoff(personAtDrop);
    }

    

    private IEnumerator CoPlayNode(Person person, DialogueSet set, DialogueNode node, bool isPickup)
    {
        _isPlaying = true;

        var id = person.Data?.id ?? -1;
        var isGhost = person.IsGhost;
        var rs = GetOrCreateRunState(id);

        // 1) เล่นบรรทัด (ขึ้นเต็มทันที ทีละบรรทัด)
        yield return ui.StartCoroutine(ui.PlayLines(node.lines, onAllDone: null));

        // 2) choices
        var choices = FilterChoices(node, rs, isGhost);
        if (choices.Count > 0)
        {
            bool done = false;
            int picked = -1;

            ui.ShowChoices(choices, idx => { picked = idx; done = true; });
            while (!done) yield return null;

            var choice = choices[Mathf.Clamp(picked, 0, choices.Count - 1)];
            ApplyEffects(choice.effects, person, rs, isPickup);

            // เดินต่อถ้ามี next
            if (!string.IsNullOrEmpty(choice.nextNodeId))
            {
                var next = FindNode(set, choice.nextNodeId);
                if (next != null)
                {
                    yield return StartCoroutine(CoPlayNode(person, set, next, isPickup));
                    yield break;
                }
            }
        }
        else if (!string.IsNullOrEmpty(node.autoNextNodeId))
        {
            var next = FindNode(set, node.autoNextNodeId);
            if (next != null)
            {
                yield return StartCoroutine(CoPlayNode(person, set, next, isPickup));
                yield break;
            }
        }

        ui.Close(() => _isPlaying = false);
    }

    private List<DialogueChoice> FilterChoices(DialogueNode node, PassengerRunState rs, bool isGhost)
    {
        var outList = new List<DialogueChoice>();
        if (node?.choices == null) return outList;

        foreach (var c in node.choices)
            if (DialogEval.AllConditionsPass(c.conditions, rs, isGhost))
                outList.Add(c);
        return outList;
    }

    private DialogueNode FindNode(DialogueSet set, string nodeId)
    {
        if (set == null || set.nodes == null) return null;
        return set.nodes.Find(n => n != null && n.nodeId == nodeId);
    }

    private PassengerRunState GetOrCreateRunState(int personId)
    {
        if (!_run.TryGetValue(personId, out var rs))
        {
            rs = new PassengerRunState(personId);
            _run[personId] = rs;
        }
        return rs;
    }

    // ===== Effects / Hooks =====

    private void ApplyEffects(List<DialogueEffect> effects, Person person, PassengerRunState rs, bool isPickupPhase)
    {
        if (effects == null) return;

        foreach (var e in effects)
        {
            switch (e.type)
            {
                case DialogueEffectType.SetVar:
                    rs.Set(e.key, e.value);
                    break;

                case DialogueEffectType.GameplayHook:
                    HandleGameplayHook(e.key, person, rs, isPickupPhase);
                    break;
            }
        }
    }

    private void HandleGameplayHook(string hook, Person person, PassengerRunState rs, bool isPickupPhase)
    {
        int id = person.Data?.id ?? -1;

        switch (hook)
        {
            case "ACCEPT_PICKUP":
                Debug.Log("ACCEPT");
                if (!seats.HasSpace)
                {
                    OnPickupDecision?.Invoke(id, false);
                    return;
                }

                if (personManager.AddExisting(person))
                {
                    seats.TrySeat(person);
                    rs.accepted = true;
                    CurrentPassenger = person;
                    OnPickupDecision?.Invoke(id, true);

                    // ปิดกล่อง pickup ก่อน แล้วค่อยเปิด ride ต่อทันที
                    ui.Close(() =>
                    {
                        _isPlaying = false; // ปิดสถานะเล่น pickup

                        StartCoroutine(CoStartRideAfterDelay(person));
                    });
                }
                else
                {
                    OnPickupDecision?.Invoke(id, false);
                }
                break;


            case "DECLINE_PICKUP":
                rs.accepted = false;
                OnPickupDecision?.Invoke(id, false);
                if (spawnTeamOwnsInstances) OnRequestDespawn?.Invoke(id); // ให้ทีม Spawn ลบคนรอ
                break;

                // เพิ่ม hook เฉพาะทางได้อีกที่นี่ (EXORCISE_NOW, PAY_TIP, ...)
        }
    }

    private IEnumerator CoStartRideAfterDelay(Person p)
    {
        // หน่วงระหว่างขับรถ (จำลอง 5 วิ)
        float t = rideStartDelay;
        while (t > 0f)
        {
            // ถ้าผู้โดยสารหาย / ลงรถ / ไม่มีใน manager แล้ว → ยกเลิก
            if (p == null) yield break;
            if (!personManager.Contains(p)) yield break;
            if (!seats.Contains(p)) yield break;

            t -= Time.deltaTime;
            yield return null;
        }

        // กันเปิดซ้อน ถ้ามี dialog อื่นเล่นอยู่
        if (_isPlaying) yield break;

        var prof = p.Data?.dialogProfile;
        var ride = prof?.rideSet;
        if (ride == null)
        {
            Debug.LogWarning($"[DialogController] {p.name} ไม่มี rideSet");
            yield break;
        }

        string entryId = string.IsNullOrEmpty(rideFirstEntryId) ? ride.entryNodeId : rideFirstEntryId;
        var node = FindNode(ride, entryId);
        if (node == null)
        {
            Debug.LogWarning($"[DialogController] ride node '{entryId}' ไม่พบ");
            yield break;
        }

        // 👇 เปิด UI ก่อนเริ่มบทพูด
        ui.Open();

        _isPlaying = true;
        yield return StartCoroutine(CoPlayNode(p, ride, node, isPickup: false));
    }


    // ===== Dropoff =====

    private void HandleDropoff(Person p)
    {
        int id = p.Data?.id ?? -1;

        if (!p.IsGhost)
        {
            // มนุษย์: ละจากที่นั่ง + ลบ instance ตามนโยบาย owner
            seats.TryUnseat(p);
            personManager.Remove(p, ownInstance: !spawnTeamOwnsInstances);

            // ถ้าทีม Spawn เป็น owner ให้เราขอ despawn
            if (spawnTeamOwnsInstances) OnRequestDespawn?.Invoke(id);

            if (CurrentPassenger == p) CurrentPassenger = null;

            OnJobCompleted?.Invoke(id);
        }
        else
        {
            
            if (CurrentPassenger == p) CurrentPassenger = null;
            OnGhostRefused?.Invoke(id);
        }
    }
}
