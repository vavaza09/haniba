using UnityEngine;
using System.Collections.Generic;

public class EnvironmentLooper : MonoBehaviour
{
    // 🔒 Exact tile length you provided: 17.94918 * 4
    const float TILE_LENGTH = 17.94918f * 4f; // 71.79672f

    [Header("Environment Loop Tiles")]
    [Tooltip("Environment tiles loop endlessly to match RoadManager movement.")]
    [SerializeField] List<GameObject> prefabs = new List<GameObject>();

    [Header("Spawn/Spacing")]
    [SerializeField] float spacingStep = TILE_LENGTH;
    [SerializeField] float yOffset = 0f;
    [SerializeField] int visibleCount = 12;
    [SerializeField] float recycleThreshold = TILE_LENGTH;

    [Header("Sync")]
    [Tooltip("Reference to the active RoadManager to read its movement speed.")]
    [SerializeField] RoadManager roadManager;

    [Tooltip("Extra speed multiplier (1 = same as road speed).")]
    [Range(0.1f, 2f)] public float speedMultiplier = 1f;

    [Header("Randomize Order")]
    [Tooltip("If true, shuffle tiles once on Awake.")]
    [SerializeField] bool shuffleOrder = false;

    // --- runtime ---
    readonly Queue<Transform> active = new Queue<Transform>();
    int index = 0;

    void Awake()
    {
        spacingStep = TILE_LENGTH;
        if (recycleThreshold <= 0f) recycleThreshold = TILE_LENGTH;

        if (prefabs == null || prefabs.Count == 0)
        {
            Debug.LogWarning("[EnvironmentLooper] No prefabs assigned.");
            enabled = false;
            return;
        }

        if (!roadManager)
        {
            roadManager = FindObjectOfType<RoadManager>();
            if (!roadManager)
            {
                Debug.LogWarning("[EnvironmentLooper] No RoadManager found in scene.");
            }
        }

        if (shuffleOrder && prefabs.Count > 1)
            Shuffle(prefabs);

        // Initial spawn strip
        for (int i = 0; i < visibleCount; i++)
            SpawnNext(initialBuild: true);
    }

    void FixedUpdate()
    {
        // Get real-time speed from RoadManager
        float currentSpeed = 0f;

        if (roadManager != null)
        {
            // Use reflection-safe access to its "moveSpeed" and "isMoving"
            currentSpeed = roadManager.isMoving ? GetRoadSpeed(roadManager) : 0f;
        }

        // Apply multiplier for parallax feel
        float speed = currentSpeed * speedMultiplier;

        // Move tiles
        Vector3 delta = -transform.forward * speed * Time.fixedDeltaTime;
        foreach (Transform t in active)
            t.position += delta;

        // Recycle oldest piece
        if (active.Count == 0) return;
        Transform head = active.Peek();
        float headLocalZ = transform.InverseTransformPoint(head.position).z;

        if (headLocalZ < -recycleThreshold)
        {
            head = active.Dequeue();
            Destroy(head.gameObject);
            SpawnNext(initialBuild: false);
        }
    }

    void SpawnNext(bool initialBuild)
    {
        GameObject prefab = prefabs[index % prefabs.Count];
        index++;

        if (!prefab) return;

        var go = Instantiate(prefab, transform);
        var t = go.transform;
        t.localRotation = Quaternion.identity;

        float z;
        if (initialBuild)
        {
            z = active.Count * TILE_LENGTH;
        }
        else
        {
            Transform last = null;
            foreach (var piece in active) last = piece;

            float lastLocalZ = (last != null)
                ? transform.InverseTransformPoint(last.position).z
                : active.Count * TILE_LENGTH;

            z = lastLocalZ + TILE_LENGTH;
        }

        t.localPosition = new Vector3(0f, yOffset, z);
        active.Enqueue(t);
    }

    void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // Helper — safely reads RoadManager speed (private field) if exposed internally
    float GetRoadSpeed(RoadManager rm)
    {
        // If you can expose "public float CurrentSpeed => currentSpeed;" in RoadManager, use that.
        // Otherwise, fallback to rm.moveSpeed when running.
        var field = typeof(RoadManager).GetField("currentSpeed",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return (field != null) ? (float)field.GetValue(rm) : rm.moveSpeed;
    }
}
