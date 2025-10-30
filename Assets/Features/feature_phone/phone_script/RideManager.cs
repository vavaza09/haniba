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

        // hide passenger, show accept_pas
        Phone.Hide();
        if (waitPanel) waitPanel.SetActive(false);

        if (acceptUI)
        {
            acceptUI.Show(offer, earn);
            // start a watcher that waits for your other code to SetReady(true)
            StartCoroutine(WaitForAcceptReadyThenGoWaiting());
        }
    }

    IEnumerator WaitForAcceptReadyThenGoWaiting()
    {
        // wait until other code calls acceptUI.SetReady(true)
        while (acceptUI != null && !acceptUI.readyToRequestNext)
            yield return null;

        if (acceptUI) acceptUI.Hide();

        // now show waiting for 6–9s
        if (waitPanel)
        {
            waitPanel.SetActive(true);
            waitPanel.transform.SetAsLastSibling();
        }

        float wait = Random.Range(minWait, maxWait);
        yield return new WaitForSecondsRealtime(wait);

        if (waitPanel) waitPanel.SetActive(false);

        // advance to next passenger
        index++;
        ShowPassenger();
    }

    void OnDeclinedOrTimeout(RideOffer offer)
    {
        // passenger panel already hidden by RideOfferUI
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
