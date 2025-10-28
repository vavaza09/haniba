using UnityEngine;
using System.Collections.Generic;

public class RoadManager : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] GameObject roadPrefab;
    [SerializeField] int poolSize = 10;

    [Header("Spacing (ignores prefab size)")]
    [Tooltip("Distance along local +Z between neighbors. Make small (0.0~0.1) for no visible gaps.")]
    [SerializeField] float spacingStep = 0.01f;   // << set this tiny

    [Header("Motion")]
    [SerializeField] float moveSpeed = 12f;
    [SerializeField] float yOffset = 0f;

    readonly Queue<Transform> pool = new Queue<Transform>();
    float tailLocalZ;

    void Awake()
    {
        if (!roadPrefab) { Debug.LogError("Assign roadPrefab"); enabled = false; return; }
        if (spacingStep <= 0f) spacingStep = 0.01f;

        // Lay out pool tightly using spacingStep (not renderer bounds)
        for (int i = 0; i < poolSize; i++)
        {
            var t = Instantiate(roadPrefab, transform).transform;
            t.localRotation = Quaternion.identity;
            t.localPosition = new Vector3(0f, yOffset, i * spacingStep);
            pool.Enqueue(t);
        }
        tailLocalZ = (poolSize - 1) * spacingStep;
    }

    void Update()
    {
        // Move along -forward
        Vector3 delta = -transform.forward * moveSpeed * Time.deltaTime;
        foreach (var t in pool) t.position += delta;

        // Recycle when the head passed behind by one spacingStep
        Transform head = pool.Peek();
        float headLocalZ = transform.InverseTransformPoint(head.position).z;

        if (headLocalZ < -spacingStep)
        {
            head = pool.Dequeue();
            tailLocalZ += spacingStep;
            head.localPosition = new Vector3(0f, yOffset, tailLocalZ);
            pool.Enqueue(head);
        }
    }
}
