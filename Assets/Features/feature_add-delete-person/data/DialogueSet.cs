using UnityEngine;
using System.Collections.Generic;

public enum RideTriggerType { None, OnReachPickup, RideTime, RideDistance, GameEvent}

[System.Serializable]
public class RideTrigger
{
    public RideTriggerType type;
    public float threshold;       
    public string eventKey;       
    public string entryNodeId;    
}

[CreateAssetMenu(menuName = "Game/Dialog/Set")]
public class DialogueSet : ScriptableObject
{
    public string setId;
    public string entryNodeId = "start";
    public List<DialogueNode> nodes = new();
    public List<RideTrigger> rideTriggers = new();
}

