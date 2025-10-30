using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogChoiceButton : MonoBehaviour
{
    [SerializeField] TMP_Text label;
    [SerializeField] Button button;

    int _index;
    Action<int> _onClicked;

    public void Setup(string text, int index, Action<int> onClicked)
    {
        _index = index;
        _onClicked = onClicked;
        if (label) label.text = text ?? "";
        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onClicked?.Invoke(_index));
        }
    }
}
