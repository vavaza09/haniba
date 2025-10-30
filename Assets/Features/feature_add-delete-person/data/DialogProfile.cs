using UnityEngine;

[CreateAssetMenu(fileName = "DialogProfile", menuName = "Game/Dialog/DialogProfile")]
public class DialogProfile : ScriptableObject
{
    public string profileId;
    public DialogueSet pickupSet;
    public DialogueSet rideSet;
}
