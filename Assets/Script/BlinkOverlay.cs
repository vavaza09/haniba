using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BlinkOverlay : MonoBehaviour
{
    [Header("Overlay")]
    public Image overlayImage;          // optional; auto-created if null
    public Color color = Color.black;
    [Range(0f, 1f)] public float maxAlpha = 1f;

    [Header("Timing")]
    [Tooltip("Seconds to fade to black")]
    public float fadeOut = 0.08f;
    [Tooltip("Seconds to hold fully black before fading back")]
    public float holdBlack = 0.04f;
    [Tooltip("Seconds to fade back to clear")]
    public float fadeIn = 0.10f;

    [Header("Easing")]
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    bool isRunning;

    void Awake()
    {
        EnsureOverlay();
        SetAlpha(0f);
    }

    public void Blink(Action midAction = null)
    {
        if (!gameObject.activeInHierarchy)
        {
            // If disabled somehow, force-enable so the blink can run
            gameObject.SetActive(true);
        }
        StartCoroutine(CoBlink(midAction));
    }

    IEnumerator CoBlink(Action midAction)
    {
        if (isRunning) yield break;
        isRunning = true;

        // Fade to black
        yield return Fade(0f, maxAlpha, fadeOut);

        // Run swap while fully black
        midAction?.Invoke();

        // Optional small hold at black
        if (holdBlack > 0f) yield return new WaitForSeconds(holdBlack);

        // Fade back to clear
        yield return Fade(maxAlpha, 0f, fadeIn);

        isRunning = false;
    }

    IEnumerator Fade(float a, float b, float duration)
    {
        if (duration <= 0f) { SetAlpha(b); yield break; }
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration; // unscaled so it works even if timescale changes
            float k = ease.Evaluate(Mathf.Clamp01(t));
            SetAlpha(Mathf.LerpUnclamped(a, b, k));
            yield return null;
        }
        SetAlpha(b);
    }

    void SetAlpha(float a)
    {
        if (!overlayImage) return;
        var c = color; c.a = a;
        overlayImage.color = c;
    }

    void EnsureOverlay()
    {
        if (overlayImage) return;

        var canvasGO = new GameObject("BlinkCanvas", typeof(Canvas), typeof(CanvasGroup));
        canvasGO.layer = gameObject.layer;
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue; // always on top
        DontDestroyOnLoad(canvasGO);

        var imageGO = new GameObject("BlinkImage", typeof(Image));
        imageGO.transform.SetParent(canvasGO.transform, false);
        var img = imageGO.GetComponent<Image>();
        img.color = new Color(color.r, color.g, color.b, 0f);

        var rt = imageGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        overlayImage = img;
    }
}
