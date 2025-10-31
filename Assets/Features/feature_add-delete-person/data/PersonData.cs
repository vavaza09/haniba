using UnityEngine;

[CreateAssetMenu(menuName = "Game/Person Data")]
public class PersonData : ScriptableObject
{
    public int id;
    public string displayName;
    public PersonKind kind;
    public DialogProfile dialogProfile;
    public PersonRoadSet roadSet;
}