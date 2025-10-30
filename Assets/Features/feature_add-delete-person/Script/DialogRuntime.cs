using System.Collections.Generic;
public class PassengerRunState
{
    public int personId;
    public bool accepted;                    
    public Dictionary<string, string> vars = new(); 

    public PassengerRunState(int id)
    {
        personId = id;
        vars["branch_key"] = "";             
    }

    public string Get(string key, string fallback = "")
        => (vars != null && vars.TryGetValue(key, out var v)) ? v : fallback;

    public void Set(string key, string value)
    {
        if (vars == null) vars = new();
        vars[key] = value;
    }
}


public static class DialogEval
{
    
    public static bool AllConditionsPass(List<DialogueCondition> conds, PassengerRunState rs, bool isGhost)
    {
        if (conds == null || conds.Count == 0) return true;
        foreach (var c in conds)
        {
            if (!Eval(c, rs, isGhost)) return false;
        }
        return true;
    }

    static bool Eval(DialogueCondition c, PassengerRunState rs, bool isGhost)
    {
        string left = c.key switch
        {
            "branch_key" => rs?.Get("branch_key", "") ?? "",
            "isGhost" => isGhost ? "true" : "false",
            "accepted" => (rs?.accepted ?? false) ? "true" : "false",
            _ => rs?.Get(c.key, "")
        };

        string right = c.value ?? "";
        return Compare(left, c.op, right);
    }

    
    static bool Compare(string l, string op, string r)
    {
        switch (op)
        {
            case "==": return l == r;
            case "!=": return l != r;
            default: return l == r; 
        }
    }
}
