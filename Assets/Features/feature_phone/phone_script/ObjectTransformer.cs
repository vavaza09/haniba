using UnityEngine;

public class ObjectTransformer : MonoBehaviour
{
    public RideOffer rideOffer; 

    private void OnTriggerEnter(Collider other)
    {
        if (rideOffer == null || rideOffer.transformPrefab == null)
        {
            Debug.LogWarning("[ObjectTransformer] Missing RideOffer or transformPrefab.");
            return;
        }

        Instantiate(rideOffer.transformPrefab, transform.position, transform.rotation);
        Destroy(gameObject);
    }
}
