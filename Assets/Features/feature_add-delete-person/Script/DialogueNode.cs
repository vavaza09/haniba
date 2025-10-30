using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class DialogueLine
{
    public string speaker;
    [TextArea] public string text;
}

[System.Serializable]
public class DialogueCondition
{
    public string key;
    public string op = "==";
    public string value;
}

public enum DialogueEffectType
{
    None,
    SetVar,
    GameplayHook
}

[System.Serializable]
public class DialogueEffect
{
    public DialogueEffectType type;
    public string key;
    public string value;
}

[System.Serializable]
public class DialogueChoice 
{
    [TextArea] public string text;
    public List<DialogueCondition> conditions;
    public List<DialogueEffect> effects;
    public string nextNodeId;
}

//[CreateAssetMenu(menuName = "Game/Dialog/Node")] 
//public class DialogueNode : ScriptableObject { }
[System.Serializable]
public class DialogueNode
{
    public string nodeId = "start";
    public List<DialogueLine> lines = new();  
    public List<DialogueChoice> choices = new(); 
    public string autoNextNodeId;             
}






