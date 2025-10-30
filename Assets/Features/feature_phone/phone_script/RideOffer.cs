using UnityEngine;

[CreateAssetMenu(menuName = "Features/Phone/Ride Offer")]
public class RideOffer : ScriptableObject
{
    [Header("Passenger Info")]
    public string passengerName;
    public float rating = 4.5f;
    public Sprite passengerPic;

    [Header("Ride Info")]
    public float distance = 10f;
    public string pickup;
    public string dropoff;

    [Header("Earnings")]
    public bool overrideRatePerKm = false;
    public float ratePerKm = 4.5f;

    [Header("Road")]
    public PersonRoadSet personRoad;
    public GameObject transformPrefab;

    public float GetEarn(float defaultRatePerKm)
    {
        float rate = overrideRatePerKm ? ratePerKm : defaultRatePerKm;
        return distance * rate;
    }
}
