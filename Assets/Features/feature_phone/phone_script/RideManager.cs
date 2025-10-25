using UnityEngine;
using System.Collections;

public class RideManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] RideDatabase database;
    [SerializeField] float defaultRatePerKm = 4.5f;


    [Header("UI")]
    [SerializeField] RideOfferUI Phone;
    [SerializeField] GameObject waitPanel;

    //[SerializeField]
    private float minWait = 6f;
    private float maxWait = 9f;

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

        if (waitPanel != null) waitPanel.SetActive(false);

        Phone.gameObject.SetActive(true);
        Phone.Show(offer, earn, OnAccepted, OnDeclinedOrTimeout);
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
        StartCoroutine(WaitAndShowNext());
    }

    IEnumerator WaitAndShowNext()
    {
        if (waitPanel != null)
        {
            waitPanel.SetActive(true);
        }
        float wait = Random.Range(minWait, maxWait);
        float t = 0f;

        while (t < wait)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (waitPanel != null)
        {
            waitPanel.SetActive(false);
        }
        ShowNext();
    }

}
