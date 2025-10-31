using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EndGamanager : MonoBehaviour
{
    [Header("UI")]
    public Image Win;
    public Image Lose;


    [Header("Settings")]
    [SerializeField] float fadeDuration = 1.5f;  // how long to fade in
    [SerializeField] float holdTime = 6f;        // how long to hold before quit

    [Header("ref")]
    [SerializeField] PersonManager PM;

    private void Awake()
    {
        Win.gameObject.SetActive(false);
        Lose.gameObject.SetActive(false);   
    }
    public void TriggerWin()
    {
        if (Win) StartCoroutine(FadeAndQuit(Win));
    }

    public void TriggerLose()
    {
        if (Lose) StartCoroutine(FadeAndQuit(Lose));
    }

    IEnumerator FadeAndQuit(Image target)
    {
        // make sure only this one is shown
        Win?.gameObject.SetActive(false);
        Lose?.gameObject.SetActive(false);

        target.gameObject.SetActive(true);

        // start fully transparent
        Color c = target.color;
        c.a = 0f;
        target.color = c;

        // fade in
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Clamp01(t / fadeDuration);
            target.color = c;
            yield return null;
        }

        // hold on screen
        yield return new WaitForSeconds(holdTime);

#if UNITY_EDITOR
        // In Editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // In Build
        Application.Quit();
#endif
    }
}