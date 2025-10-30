using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AcceptedPassengerUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] GameObject accept_panel; // Phone_Screen(accept_pas)

    [Header("Refs")]
    [SerializeField] Image person_image;
    [SerializeField] TMP_Text person_name;
    [SerializeField] TMP_Text person_rating;
    [SerializeField] TMP_Text price_text;
    [SerializeField] TMP_Text pickup_text;
    [SerializeField] TMP_Text drop_text;
    [SerializeField] TMP_Text howfar_text;

    // other systems set this when ready to move to waiting requests
    public bool readyToRequestNext { get; private set; }

    public void Show(RideOffer offer, float earn)
    {
        if (person_image) person_image.sprite = offer.passengerPic;
        if (person_name) person_name.text = offer.passengerName;
        if (person_rating) person_rating.text = offer.rating.ToString("0.0");
        if (price_text) price_text.text = $"{earn:0}";
        if (pickup_text) pickup_text.text = offer.pickup;
        if (drop_text) drop_text.text = offer.dropoff;
        if (howfar_text) howfar_text.text = $"{offer.distance:0.0} km";

        readyToRequestNext = false;
        if (accept_panel) accept_panel.SetActive(true);
        transform.SetAsLastSibling(); // ensure on top
    }

    public void Hide()
    {
        if (accept_panel) accept_panel.SetActive(false);
        readyToRequestNext = false;
    }

    // call this from your other code when it’s time to move on
    public void SetReady(bool value) => readyToRequestNext = value;
}
