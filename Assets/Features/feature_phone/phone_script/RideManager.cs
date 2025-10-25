using UnityEngine;

public class RideManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] RideDatabase database;
    [SerializeField] float defaultRatePerKm = 4.5f;


    [Header("UI")]
    [SerializeField] RideOfferUI offerUI;


    int index;

    private void Start()
    {
        index = 0;
        ShowNext();
    }

    void ShowNext()
    {
        if (database == null || database.offers == null || index >= database.offers.Count)
        {
            return;
        }
        var offer = database.offers[index];
        float earn = offer.GetEarn(defaultRatePerKm);
        offerUI.Show(offer, earn, OnAccepted, OnDeclinedOrTimeout);
    }

    void OnAccepted(RideOffer offer)
    {
        Debug.Log($"Ride accepted: {offer.passengerName}");
        index++;
        ShowNext();
    }

    void OnDeclinedOrTimeout(RideOffer offer)
    {
        Debug.Log($"Ride declined or timed out: {offer.passengerName}");
        index++;
        ShowNext();
    }

}
