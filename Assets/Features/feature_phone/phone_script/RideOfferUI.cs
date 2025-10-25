using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class RideOfferUI : MonoBehaviour
{
    [SerializeField] GameObject phone;
    [SerializeField] bool pauseOnOpen = false;

    [Header("phone_canvas")]
    [SerializeField] Image person_image;
    [SerializeField] TMP_Text person_name;
    [SerializeField] TMP_Text person_rating;
    [SerializeField] TMP_Text price_text;      // Price_frame/Price_Text
    [SerializeField] TMP_Text howfar_text;
    [SerializeField] TMP_Text pickup_text;
    [SerializeField] TMP_Text drop_text;
    [SerializeField] Button accept_button;
    [SerializeField] TMP_Text accept_txt;      // Accept_button/Accept_Txt
    [SerializeField] TMP_Text accept_time;     // Accept_button/Accept_Time
    [SerializeField] Button decline_button;

    [SerializeField] float acceptTimes = 14f;

    RideOffer current;
    float currentEarn;
    Coroutine countdownCo;
    System.Action<RideOffer> onAccept;
    System.Action<RideOffer> onDeclineOrTimeout;

    public void Show(RideOffer offer, float earn, System.Action<RideOffer> acceptCb, System.Action<RideOffer> declineCb)
    {
        current = offer;
        currentEarn = earn;
        onAccept = acceptCb;
        onDeclineOrTimeout = declineCb;

        if (pauseOnOpen) Time.timeScale = 0f;
        phone.SetActive(true);

        if (person_image != null) person_image.sprite = offer.passengerPic;
        if (person_name != null) person_name.text = offer.passengerName;
        if (person_rating != null) person_rating.text = offer.rating.ToString("0.0");
        if (howfar_text != null) howfar_text.text = $"{offer.distance:0.0} km";
        if (pickup_text != null) pickup_text.text = offer.pickup;
        if (drop_text != null) drop_text.text = offer.dropoff;
        if (price_text != null) price_text.text = $"${earn:0}";

        accept_button.onClick.RemoveAllListeners();
        decline_button.onClick.RemoveAllListeners();
        accept_button.onClick.AddListener(Accept);
        decline_button.onClick.AddListener(Decline);

        if (countdownCo != null) 
        { 
            StopCoroutine(countdownCo); 
        }
        countdownCo = StartCoroutine(Countdown());
    }


    IEnumerator Countdown()
    {
        float t = acceptTimes;
        while (t > 0f)
        {
            if (accept_txt) accept_txt.text = "Accept Ride";
            if (accept_time) accept_time.text = $"{Mathf.CeilToInt(t)}s";
            yield return new WaitForSecondsRealtime(1f);
            t -= 1f;
        }
        Timeout();
    }

    void Accept() { if (countdownCo != null) StopCoroutine(countdownCo); Close(); onAccept?.Invoke(current); }
    void Decline() { if (countdownCo != null) StopCoroutine(countdownCo); Close(); onDeclineOrTimeout?.Invoke(current); }
    void Timeout() { Close(); onDeclineOrTimeout?.Invoke(current); }

    void Close()
    {
        phone.SetActive(false);
        if (pauseOnOpen) 
        { 
            Time.timeScale = 1f;
        } 
    }
}
