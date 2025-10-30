// DialogUIController.cs (No animation, No typewriter)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DialogUIController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] CanvasGroup root;      

    [Header("Widgets")]
    [SerializeField] TMP_Text nameTag;
    [SerializeField] TMP_Text textBox;
    [SerializeField] Transform choicesParent;            
    [SerializeField] DialogChoiceButton choiceButtonPrefab;

    [SerializeField] UnityEngine.UI.Button advanceButton;


    public bool IsOpen { get; private set; }
    public bool IsShowingChoices { get; private set; }

    Action onAdvanceCurrent;     
    Action<int> onChoicePicked;  

    void Awake()
    {
        HideRoot();
        ClearChoices();
        if (textBox) textBox.text = "";
        if (nameTag) nameTag.text = "";
    }

    // ===== Public API =====
    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        ShowRoot();
    }

    public void Close(Action onClosed = null)
    {
        IsOpen = false;
        HideRoot();
        onClosed?.Invoke();
    }

    public void SetSpeaker(string displayName)
    {
        if (nameTag) nameTag.text = displayName ?? "";
    }

    /*
    public void SetThemeColor(Color c)
    {
        // ไม่มีอนิเมชัน แต่ยังเปลี่ยนสีชื่อได้ถ้าต้องการ
        if (nameTag) nameTag.color = c;
    }
    */

    public IEnumerator PlayLines(List<DialogueLine> lines, Action onAllDone)
    {
        if (lines == null || lines.Count == 0)
        {
            onAllDone?.Invoke();
            yield break;
        }

        Open();
        ClearChoices();

        foreach (var line in lines)
        {
            SetSpeaker(line.speaker);
            if (textBox) textBox.text = line.text ?? "";

            bool advanced = false;
            onAdvanceCurrent = () => advanced = true;

            while (!advanced) yield return null;
        }

        onAllDone?.Invoke();
    }

    public void ShowChoices(List<DialogueChoice> choices, Action<int> onPicked)
    {
        if (choices == null || choices.Count == 0)
        {
            onPicked?.Invoke(-1);
            return;
        }

       
        ClearChoiceButtonsOnly();

        IsShowingChoices = true;
        onChoicePicked = onPicked;

        for (int i = 0; i < choices.Count; i++)
        {
            var choice = choices[i];
            var b = Instantiate(choiceButtonPrefab, choicesParent);
            b.Setup(choice.text, i, HandleChoiceClicked);
        }

        if (advanceButton) advanceButton.interactable = false;
    }

    public void ClearChoices()
    {
        IsShowingChoices = false;
        onChoicePicked = null;
        ClearChoiceButtonsOnly();
        if (advanceButton) advanceButton.interactable = true;
    }

    void ClearChoiceButtonsOnly()
    {
        if (!choicesParent) return;
        for (int i = choicesParent.childCount - 1; i >= 0; i--)
            Destroy(choicesParent.GetChild(i).gameObject);
    }



    public void OnClickAdvance()
    {
        if (IsShowingChoices) return; 
        onAdvanceCurrent?.Invoke();
    }

    void ShowRoot()
    {
        if (!root) return;
        root.alpha = 1f;
        root.interactable = true;
        root.blocksRaycasts = true;
    }

    void HideRoot()
    {
        if (!root) return;
        root.alpha = 0f;
        root.interactable = false;
        root.blocksRaycasts = false;
    }

    void HandleChoiceClicked(int choiceIndex)
    {
        var cb = onChoicePicked;   
        ClearChoices();             
        cb?.Invoke(choiceIndex);    
    }
}
