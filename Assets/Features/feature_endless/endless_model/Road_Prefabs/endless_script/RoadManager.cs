using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RoadManager : MonoBehaviour, IHasDefaultRun
{
    // 🔒 Exact tile length you gave: 17.94918 * 4
    const float TILE_LENGTH = 17.94918f * 4f; // 71.79672f

    [Header("Default Loop Tiles")]
    [Tooltip("These tiles loop endlessly when no special ride tiles are active.")]
    [SerializeField] List<GameObject> defaultLoopPrefabs = new List<GameObject>();

    [Header("Spawn/Spacing (auto-locked to tile length)")]
    [SerializeField] float spacingStep = TILE_LENGTH; // will be forced to TILE_LENGTH in Awake
    [SerializeField] float yOffset = 0f;
    [SerializeField] int visibleCount = 20;

    [Header("Recycle")]
    [Tooltip("Recycle when the HEAD tile has passed behind this Z by this amount (in local space).")]
    [SerializeField] float recycleThreshold = TILE_LENGTH; // safe default: one tile length

    [Header("Motion")]
    [SerializeField] public float moveSpeed = 8f;
    [SerializeField] float slowdownRate = 2f;

    [Header("Resume Boost (only when resuming default loop)")]
    [SerializeField] float resumeAccelRate = 10f;
    [SerializeField] float resumeAccelTime = 0.6f;

    [Header("Runtime")]
    public bool isMoving = true;

    readonly Queue<Transform> active = new Queue<Transform>();
    readonly Queue<GameObject> injectOnce = new Queue<GameObject>();

    private bool allowDefaultLoop = true;
    private float currentSpeed = 0f;
    private int defaultIndex = 0;
    private Coroutine resumeBoostCo;

    [SerializeField] private RideManager rm;


    void Awake()
    {
        // Lock spacing and recycle precisely to the tile size you specified
        spacingStep = TILE_LENGTH;
        if (recycleThreshold <= 0f) recycleThreshold = TILE_LENGTH;

        if (defaultLoopPrefabs == null || defaultLoopPrefabs.Count == 0)
            Debug.LogWarning("[RoadManager] No defaultLoopPrefabs set!");

        // Build initial strip at exact Z slots: 0, L, 2L, 3L, ...
        for (int i = 0; i < visibleCount; i++)
            SpawnNextPiece(initialBuild: true);

        currentSpeed = moveSpeed;
    }

    void FixedUpdate()
    {
        // Smooth accel/decel
        float targetSpeed = isMoving ? moveSpeed : 0f;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.fixedDeltaTime * slowdownRate);

        // Move everything backwards along -forward (endless runner style)
        Vector3 delta = -transform.forward * currentSpeed * Time.fixedDeltaTime;
        foreach (Transform t in active) t.position += delta;

        // If nothing active, try spawn once
        if (active.Count == 0)
        {
            if (injectOnce.Count > 0 || allowDefaultLoop)
                SpawnNextPiece(initialBuild: true);
            if (active.Count == 0) return;
        }

        // Recycle based on HEAD tile's local Z passing behind threshold
        Transform head = active.Peek();
        float headLocalZ = transform.InverseTransformPoint(head.position).z;
        bool shouldRecycle = headLocalZ < -recycleThreshold;
        bool canSpawnNext = (injectOnce.Count > 0) || allowDefaultLoop;

        if (shouldRecycle && canSpawnNext)
        {
            head = active.Dequeue();
            Destroy(head.gameObject); // swap to pooling if needed
            SpawnNextPiece(initialBuild: false);
        }
    }

    void SpawnNextPiece(bool initialBuild)
    {
        GameObject prefab = null;

        if (injectOnce.Count > 0)
        {
            prefab = injectOnce.Dequeue();
        }
        else if (allowDefaultLoop)
        {
            if (defaultLoopPrefabs.Count == 0) return;
            prefab = defaultLoopPrefabs[defaultIndex % defaultLoopPrefabs.Count];
            defaultIndex++;
        }
        else
        {
            return;
        }

        if (!prefab) return;

        var go = Instantiate(prefab, transform);
        var t = go.transform;
        t.localRotation = Quaternion.identity;

        // Exact placement:
        // - For the very first build, place by slot index i * TILE_LENGTH (no accumulation drift).
        // - For continuous spawns, place exactly after the current LAST tile's local Z + TILE_LENGTH.
        float z;

        if (initialBuild)
        {
            // slot index is current count (0..visibleCount-1) BEFORE enqueue
            z = active.Count * TILE_LENGTH;
        }
        else
        {
            // find last tile's local Z
            Transform last = null;
            foreach (var piece in active) last = piece;
            float lastLocalZ = (last != null)
                ? transform.InverseTransformPoint(last.position).z
                : active.Count * TILE_LENGTH; // fallback, should not happen

            z = lastLocalZ + TILE_LENGTH;
        }

        t.localPosition = new Vector3(0f, yOffset, z);
        active.Enqueue(t);
    }

    public void SetMoving(bool state) => isMoving = state;

    public void ForceStopImmediately()
    {
        isMoving = false;
        currentSpeed = 0f;
        //SDM.NotifyEnterPickup();

    }

    public void ForceStopForWait()
    {
        isMoving = false;
        currentSpeed = 0f;
        if (rm) rm.StartWaitingThenNext();
    }

    public void DeclineOnLoad()
    {
        rm.StartWaitingThenNext();
        ResumeDefaultLoop();
    }

    public void BeginRide(PersonRoadSet set)
    {
        if (set == null) return;

        allowDefaultLoop = false; // stop default spawning (we'll inject)
        InjectTilesOnce(new[] { set.firstTileOnce, set.busStopTileOnce });
        isMoving = true;
    }

    public void ResumeDefaultLoop()
    {
        // Re-anchor tail Z to the last active piece's local Z, so next spawn is tight
        if (active.Count > 0)
        {
            Transform last = null;
            foreach (var piece in active) last = piece;
            if (last != null)
            {
                // Optional snap of LAST to the nearest slot to remove drift entirely:
                float lastLocalZ = transform.InverseTransformPoint(last.position).z;
                float snapped = Mathf.Round(lastLocalZ / TILE_LENGTH) * TILE_LENGTH;
                Vector3 lp = last.localPosition; lp.z = snapped; last.localPosition = lp;
            }
        }

        allowDefaultLoop = true;

        // start from rest + boost to feel snappy
        currentSpeed = 0f;
        isMoving = true;

        if (resumeBoostCo != null) StopCoroutine(resumeBoostCo);
        resumeBoostCo = StartCoroutine(BoostResumeAcceleration());
    }

    IEnumerator BoostResumeAcceleration()
    {
        float original = slowdownRate;
        slowdownRate = resumeAccelRate;
        float elapsed = 0f;

        while (elapsed < resumeAccelTime && isMoving && currentSpeed < moveSpeed * 0.98f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        slowdownRate = original;
        resumeBoostCo = null;
    }

    public void ShowDestinationAndStop(PersonRoadSet set)
    {
        if (set == null) return;
        InjectTileOnce(set.destinationTileOnce);
    }

    public void InjectTileOnce(GameObject prefab)
    {
        if (prefab) injectOnce.Enqueue(prefab);
    }

    public void InjectTilesOnce(IEnumerable<GameObject> prefabs)
    {
        if (prefabs == null) return;
        foreach (var p in prefabs)
            if (p) injectOnce.Enqueue(p);
    }
}
