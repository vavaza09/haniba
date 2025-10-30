using UnityEngine;

public class ObjectTransformer : MonoBehaviour
{
    public RideOffer rideOffer;
    Person person;
    GameObject current;
    //public GameObject object3d;
    SpawnDialogMediator SDM;
    [SerializeField] string playerTag = "Player";
    Collider cl;

    void Awake()
    {
        // หา Mediator อัตโนมัติถ้าไม่ถูกตั้งไว้
        if (SDM == null)
            SDM = FindFirstObjectByType<SpawnDialogMediator>();

    }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;

            if (rideOffer == null || rideOffer.transformPrefab == null)
            {
                Debug.LogWarning("[ObjectTransformer] Missing RideOffer or transformPrefab.");
                return;
            }
            cl = GetComponent<Collider>();
            current = Instantiate(rideOffer.transformPrefab, transform.position, transform.rotation);
            Debug.Log(current);
            person = current.GetComponent<Person>();
            Debug.Log(person);
            Debug.Log(cl);
            SDM.NotifyEnterPickup(person);
            cl.isTrigger = false;

    }
}
