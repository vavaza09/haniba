using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogController_Example : MonoBehaviour
{
    [SerializeField] DialogUIController ui;

    [Header("Mock Data")]
    [TextArea] public string line1 = "สวัสดี ไปวัดป่าขอนแก่นด้วยได้ไหม?";
    [TextArea] public string line2 = "ขอบคุณนะ ระหว่างทางไปคุยกันหน่อยไหม?";

    void Update()
    {
        // กด T เพื่อเทสต์ pickup dialog
        if (Input.GetKeyDown(KeyCode.T))
        {
            StartCoroutine(DemoPickup());
        }
    }

    IEnumerator DemoPickup()
    {
        ui.Open();

        var lines = new List<DialogueLine>
        {
            new DialogueLine{ speaker="Passenger", text=line1 },
            new DialogueLine{ speaker="Passenger", text=line2 },
        };

        yield return ui.StartCoroutine(ui.PlayLines(lines, onAllDone: null));

        var choices = new List<DialogueChoice>
        {
            new DialogueChoice{ text="ขึ้นมาเลย" },
            new DialogueChoice{ text="ขอโทษ ไม่รับ" },
        };

        bool picked = false;
        int pickedIndex = -1;

        ui.ShowChoices(choices, (idx) => {
            picked = true;
            pickedIndex = idx;
        });

        while (!picked) yield return null;

        // ปิด UI
        ui.Close();
        Debug.Log("Picked choice index: " + pickedIndex);
    }
}
