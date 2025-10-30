using UnityEngine;
using System.Collections;

public class RideManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] RideDatabase database;
    [SerializeField] float defaultRatePerKm = 4.5f;

    [Header("UI")]
    [SerializeField] RideOfferUI Phone;          // passenger panel
    [SerializeField] GameObject waitPanel;       // waiting panel
    [SerializeField] AcceptedPassengerUI acceptUI; // accept_pas panel

    [Header("Waiting")]
    [SerializeField] float minWait = 6f;
    [SerializeField] float maxWait = 9f;

    [SerializeField] RoadManager roadManager;

    int index;

    void Start()
    {
        index = 0;
        if (waitPanel) waitPanel.SetActive(false);
        if (acceptUI) acceptUI.Hide();
        ShowPassenger();
    }

    void ShowPassenger()
    {
        if (database == null || database.offers == null || database.offers.Count == 0) return;
        if (index >= database.offers.Count) index = 0;

        if (waitPanel) waitPanel.SetActive(false);
        if (acceptUI) acceptUI.Hide();

        var offer = database.offers[index];
        float earn = offer.GetEarn(defaultRatePerKm);

        Phone.gameObject.SetActive(true);
        Phone.transform.SetAsLastSibling();
        Phone.Show(offer, earn, OnAccepted, OnDeclinedOrTimeout);
    }

    void OnAccepted(RideOffer offer)
    {
        float earn = offer.GetEarn(defaultRatePerKm);

        Phone.Hide();
        if (waitPanel) waitPanel.SetActive(false);

        // >>> Begin the person's road sequence
        if (roadManager && offer.personRoad)
            roadManager.BeginRide(offer.personRoad);

        if (acceptUI)
        {
            acceptUI.Show(offer, earn);
            StartCoroutine(WaitForAcceptReadyThenGoWaiting());
        }
    }

    IEnumerator WaitForAcceptReadyThenGoWaiting()
    {
        while (acceptUI != null && !acceptUI.readyToRequestNext)
            yield return null;

        if (acceptUI) acceptUI.Hide();

        // Passenger is in  resume default loop
        if (roadManager) roadManager.ResumeDefaultLoop();

        // Show waiting page for next requests as you already do...
        if (waitPanel)
        {
            waitPanel.SetActive(true);
            waitPanel.transform.SetAsLastSibling();
        }
        float wait = Random.Range(minWait, maxWait);
        yield return new WaitForSecondsRealtime(wait);
        if (waitPanel) waitPanel.SetActive(false);

        index++;
        ShowPassenger();
    }

    void OnDeclinedOrTimeout(RideOffer offer)
    {
        // passenger panel already hidden by RideOfferUI
        StartCoroutine(ShowWaitingThenNext());
    }

    public void StartWaitingThenNext()
    {
        StartCoroutine(ShowWaitingThenNext());
    }

    IEnumerator ShowWaitingThenNext()
    {
        if (acceptUI) acceptUI.Hide();

        if (waitPanel)
        {
            waitPanel.SetActive(true);
            waitPanel.transform.SetAsLastSibling();
        }

        float wait = Random.Range(minWait, maxWait);
        yield return new WaitForSecondsRealtime(wait);

        if (waitPanel) waitPanel.SetActive(false);

        index++;
        ShowPassenger();
    }
}
