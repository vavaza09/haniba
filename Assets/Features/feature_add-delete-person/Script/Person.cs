using System;
using UnityEngine;

public class Person : MonoBehaviour
{
    [SerializeField] private PersonData data;   
    public PersonData Data => data;
    public int Id => data != null ? data.id : -1;
    public bool IsGhost => data != null && data.kind == PersonKind.Ghost;

    void Awake()
    {
        if (data == null)
            Debug.LogError($"{name}: PersonData is not assigned on prefab!", this);
        DebugUtils.LogAllValues(data);
    }

    public string DialogKey => data != null ? data.dialogKey : "";
}
