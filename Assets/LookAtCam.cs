using UnityEngine;

public class LookAtCam : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    private void Awake()
    {
        targetCamera = Camera.main;
    }

    private void LateUpdate()
    {

        if (targetCamera == null)
        {
            return;
        }

        if(targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        Vector3 cameraPosition = targetCamera.transform.position;

        cameraPosition.y = transform.position.y;

        transform.LookAt(cameraPosition);

        transform.Rotate(0, 180, 0);
    }
}
