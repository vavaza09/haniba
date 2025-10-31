using UnityEngine;

public class TestAPI : MonoBehaviour
{
    [Tooltip("Reference to DashAndBodySim on your car.")]
    public DashAndBodySim dashSim;

    [Header("Keybinds")]
    public KeyCode toggleDriveKey = KeyCode.Space;

    bool isDriving = false;

    void Update()
    {
        if (!dashSim) return;

        // SPACE toggles between drive and stop
        if (Input.GetKeyDown(toggleDriveKey))
        {
            isDriving = !isDriving;

            if (isDriving)
            {
                dashSim.CarGo();
                Debug.Log("Car started moving.");
            }
            else
            {
                dashSim.CarIdle();
                Debug.Log("Car stopped.");
            }
        }
    }
}
