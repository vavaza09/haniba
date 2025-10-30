using UnityEngine;

public class RoadTrigger : MonoBehaviour
{
    public enum TriggerType
    {
        Slowdown,
        HardStop,
        DestinationStop
    }

    [SerializeField] private TriggerType triggerType;
    [SerializeField] private RoadManager manager;

    private void OnTriggerEnter(Collider other)
    {
        if (!manager) manager = FindFirstObjectByType<RoadManager>();
        if (!manager) return;

        switch (triggerType)
        {
            case TriggerType.Slowdown:
                manager.SetMoving(false);
                break;

            case TriggerType.HardStop:
                manager.ForceStopImmediately();
                // After a pause, resume the default loop again
                //manager.ResumeDefaultLoop();
                gameObject.GetComponent<Collider>().enabled = false;
                break;

            case TriggerType.DestinationStop:
                manager.ForceStopForWait();
                
                break;
        }
    }
}
