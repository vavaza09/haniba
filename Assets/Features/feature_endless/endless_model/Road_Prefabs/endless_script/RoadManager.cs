using UnityEngine;
using System.Collections.Generic;

public class RoadManager : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] GameObject roadPrefab;
    [SerializeField] int poolSize = 10;

    [Header("Spacing (ignore prefab size)")]
    [SerializeField] float spacingStep = 0.01f;   // small = seamless
    [SerializeField] float yOffset = 0f;

    [Header("Motion")]
    [SerializeField] float moveSpeed = 12f;       // target speed
    [SerializeField] float slowdownRate = 2f;     // how fast to slow/accelerate

    [Header("Runtime Control")]
    [Tooltip("Turn false to simulate parking / stopping the car.")]
    public bool isMoving = true;

    Queue<Transform> pool = new Queue<Transform>();
    float tailLocalZ;
    float currentSpeed = 0f;

    void Awake()
    {
        if (!roadPrefab)
        {
            Debug.LogError("Assign roadPrefab to RoadManager!");
            enabled = false;
            return;
        }

        if (spacingStep <= 0f) spacingStep = 0.01f;

        // Build pool tightly
        for (int i = 0; i < poolSize; i++)
        {
            Transform t = Instantiate(roadPrefab, transform).transform;
            t.localRotation = Quaternion.identity;
            t.localPosition = new Vector3(0f, yOffset, i * spacingStep);
            pool.Enqueue(t);
        }

        tailLocalZ = (poolSize - 1) * spacingStep;
        currentSpeed = moveSpeed;
    }

    void Update()
    {
        // Smooth accelerate / decelerate
        float targetSpeed = isMoving ? moveSpeed : 0f;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * slowdownRate);

        Vector3 delta = -transform.forward * currentSpeed * Time.deltaTime;
        foreach (Transform t in pool)
            t.position += delta;

        // Recycle oldest piece
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

    // --- Optional: external control ---
    public void SetMoving(bool state)
    {
        isMoving = state;
    }
}
