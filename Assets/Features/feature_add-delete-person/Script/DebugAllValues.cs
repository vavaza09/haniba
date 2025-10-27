using UnityEngine;
using System.Reflection;

public static class DebugUtils
{
    public static void LogAllValues(object obj)
    {
        if (obj == null)
        {
            Debug.LogWarning("Object is null!");
            return;
        }

        System.Type type = obj.GetType();
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        Debug.Log($"==== Values in {type.Name} ====");
        foreach (FieldInfo field in fields)
        {
            object value = field.GetValue(obj);
            Debug.Log($"{field.Name}: {value}");
        }
        Debug.Log("===============================");
    }
}

