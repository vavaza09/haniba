using UnityEngine;
using System.Collections;               // added for IEnumerator/Coroutine
using System.Collections.Generic;

public class RoadManager : MonoBehaviour
{
    [Header("Default Loop Tiles")]
    [Tooltip("These tiles loop endlessly when no special ride tiles are active.")]
    [SerializeField] List<GameObject> defaultLoopPrefabs = new List<GameObject>();

    [Header("Spawn/Spacing")]
    [SerializeField] float spacingStep = 10f;       // distance between pieces
    [SerializeField] float yOffset = 0f;
    [SerializeField] int visibleCount = 20;         // number of tiles active
    [SerializeField] float recycleThreshold = 10f;  // when to recycle oldest piece

    [Header("Motion")]
    [SerializeField] float moveSpeed = 8f;
    [SerializeField] float slowdownRate = 2f;       // global lerp rate used everywhere

    [Header("Resume Boost (only when resuming default loop)")]
    [SerializeField] float resumeAccelRate = 10f;   // temporary faster ramp
    [SerializeField] float resumeAccelTime = 0.6f;  // how long to keep the fast ramp

    [Header("Runtime")]
    public bool isMoving = true;

    readonly Queue<Transform> active = new Queue<Transform>();
    readonly Queue<GameObject> injectOnce = new Queue<GameObject>();

    private bool allowDefaultLoop = true;
    private float tailLocalZ;
    private float currentSpeed = 0f;
    private int defaultIndex = 0;

    private Coroutine resumeBoostCo;


    void Awake()
    {
        if (spacingStep <= 0f) spacingStep = 10f;
        if (recycleThreshold <= 0f) recycleThreshold = spacingStep;

        if (defaultLoopPrefabs == null || defaultLoopPrefabs.Count == 0)
            Debug.LogWarning("[RoadManager] No defaultLoopPrefabs set!");

        // Fill initial road
        for (int i = 0; i < visibleCount; i++)
            SpawnNextPiece(initialBuild: true);

        currentSpeed = moveSpeed;
    }


    void FixedUpdate()
    {
        // Smooth acceleration/deceleration with a single rate (temporarily boosted on resume)
        float targetSpeed = isMoving ? moveSpeed : 0f;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.fixedDeltaTime * slowdownRate);

        // Move all active tiles
        Vector3 delta = -transform.forward * currentSpeed * Time.fixedDeltaTime;
        foreach (Transform t in active)
            t.position += delta;

        // Safety guard: if queue empty, try to spawn once
        if (active.Count == 0)
        {
            if (injectOnce.Count > 0 || allowDefaultLoop)
                SpawnNextPiece(initialBuild: true);
            if (active.Count == 0) return;
        }

        // Check oldest piece
        Transform head = active.Peek();
        float headLocalZ = transform.InverseTransformPoint(head.position).z;

        bool shouldRecycle = headLocalZ < -recycleThreshold;
        bool canSpawnNext = (injectOnce.Count > 0) || allowDefaultLoop;

        // Recycle only if we can spawn a replacement
        if (shouldRecycle && canSpawnNext)
        {
            head = active.Dequeue();
            Destroy(head.gameObject); // pooling can replace this later
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

        if (initialBuild)
        {
            t.localPosition = new Vector3(0f, yOffset, active.Count * spacingStep);
            tailLocalZ = (active.Count) * spacingStep;
        }
        else
        {
            tailLocalZ += spacingStep;
            t.localPosition = new Vector3(0f, yOffset, tailLocalZ);
        }

        active.Enqueue(t);
    }


    public void SetMoving(bool state) => isMoving = state;

    public void ForceStopImmediately()
    {
        isMoving = false;
        currentSpeed = 0f;
    }

    public void BeginRide(PersonRoadSet set)
    {
        if (set == null) return;

        allowDefaultLoop = false; // stop default spawning
        InjectTilesOnce(new[] { set.firstTileOnce, set.busStopTileOnce });
        isMoving = true;
    }


    public void ResumeDefaultLoop()
    {
        // Re-anchor tail to the last active piece so spacing is tight
        if (active.Count > 0)
        {
            Transform last = null;
            foreach (var piece in active) last = piece;
            if (last != null)
                tailLocalZ = transform.InverseTransformPoint(last.position).z;
        }

        allowDefaultLoop = true;

        // start from rest
        currentSpeed = 0f;
        isMoving = true;

        // kick a short acceleration boost (only affects the resume moment)
        if (resumeBoostCo != null) StopCoroutine(resumeBoostCo);
        resumeBoostCo = StartCoroutine(BoostResumeAcceleration());
    }

    IEnumerator BoostResumeAcceleration()
    {
        float original = slowdownRate;
        slowdownRate = resumeAccelRate; // faster ramp
        float elapsed = 0f;

        // keep the boost for a short window, or until we're basically at target speed
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
