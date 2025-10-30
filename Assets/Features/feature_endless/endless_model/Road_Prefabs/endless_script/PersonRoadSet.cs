using UnityEngine;

[CreateAssetMenu(menuName = "Road/Person Road Set")]
public class PersonRoadSet : ScriptableObject
{
    [Header("Sequence")]
    public GameObject firstTileOnce;     // plays once after accept
    public GameObject busStopTileOnce;   // has your two triggers
    public GameObject destinationTileOnce; // shown when arriving, then stop the car
}
