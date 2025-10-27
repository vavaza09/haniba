using UnityEngine;

[CreateAssetMenu(menuName = "Game/Person Data")]
public class PersonData : ScriptableObject
{
    public int id;
    public string displayName;
    public PersonKind kind;

    [Header("Dialog Linking")]
    public string dialogKey;

    [Header("Visual (optional)")]
    public GameObject prefabOverride;
}