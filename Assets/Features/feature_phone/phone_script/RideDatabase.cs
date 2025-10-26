using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Features/Phone/Ride Database")]    
public class RideDatabase : ScriptableObject
{
    public List<RideOffer> offers = new List<RideOffer>();
}
