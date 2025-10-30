using UnityEngine;

public class ObjectTransformer : MonoBehaviour
{
    public RideOffer rideOffer;
    Person person;
    GameObject current;
    //public GameObject object3d;
    SpawnDialogMediator SDM;
    private void OnTriggerEnter(Collider other)
    {
        if (rideOffer == null || rideOffer.transformPrefab == null)
        {
            Debug.LogWarning("[ObjectTransformer] Missing RideOffer or transformPrefab.");
            return;
        }

        current = Instantiate(rideOffer.transformPrefab, transform.position, transform.rotation);
        person = current.GetComponent<Person>();
        SDM.NotifyEnterPickup(person);
        //Destroy(object3d);

    }
}
