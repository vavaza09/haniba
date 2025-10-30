using UnityEngine;

public class DebugObjectLength : MonoBehaviour
{
    void Start()
    {
        // Try getting the Renderer first
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Vector3 size = renderer.bounds.size;
            Debug.Log($"[{name}] Renderer size: {size} | Length (X): {size.x}, Height (Y): {size.y}, Depth (Z): {size.z}");
            return;
        }

        // If there's no Renderer, check for a Collider instead
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Vector3 size = col.bounds.size;
            Debug.Log($"[{name}] Collider size: {size} | Length (X): {size.x}, Height (Y): {size.y}, Depth (Z): {size.z}");
            return;
        }

        Debug.LogWarning($"[{name}] has no Renderer or Collider to measure.");
    }
}
