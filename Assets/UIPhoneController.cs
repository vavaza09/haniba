using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIPhoneController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject openUI;
    public GameObject acceptUI;
    public GameObject doneUI;
    public GameObject failUI;

    [Header("Shortcut Text")]
    public TMP_Text shortcutText;

    private bool isOpen = false;
    private Vector3 originalScaleOpen;

    private void Start()
    {
        originalScaleOpen = openUI.transform.localScale;

        openUI.SetActive(false);
        acceptUI.SetActive(false);
        doneUI.SetActive(false);
        failUI.SetActive(false);

        UpdateShortcutText();
    }

    private bool isAnimating = false; // ✅ ตัวแปรล็อก Animation

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isAnimating) return; // ❌ ไม่ทำอะไรถ้า Animation กำลังเล่น

            if (!isOpen)
                StartCoroutine(OpenPhone());
            else
                ClosePhone();
        }
    }


    private void UpdateShortcutText()
    {
        if (shortcutText != null)
        {
            shortcutText.text = isOpen ? "PHONE (CLOSE)" : "PHONE (OPEN)";
        }
    }

    IEnumerator SmoothScale(GameObject target, Vector3 start, Vector3 end, float duration)
    {
        float t = 0f;
        RectTransform rect = target.GetComponent<RectTransform>();

        if (!target.activeSelf) target.SetActive(true);

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);

            // Ease Out Effect
            float eased = 1f - Mathf.Pow(1f - progress, 3f);

            rect.localScale = Vector3.Lerp(start, end, eased);
            yield return null;
        }

        rect.localScale = end;
    }

    IEnumerator SmoothFade(Image img, float start, float end, float duration)
    {
        float t = 0f;
        Color c = img.color;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);

            // Ease linear fade
            c.a = Mathf.Lerp(start, end, progress);
            img.color = c;
            yield return null;
        }

        c.a = end;
        img.color = c;
    }

    IEnumerator OpenPhone()
    {
        isOpen = true;
        isAnimating = true; // ✅ เริ่มล็อก
        UpdateShortcutText();

        Transform logo = openUI.transform.Find("logo");
        if (logo != null)
        {
            logo.localScale = Vector3.zero;
            logo.gameObject.SetActive(false);
        }

        openUI.SetActive(true);
        openUI.transform.localScale = Vector3.zero;
        yield return StartCoroutine(SmoothScale(openUI, Vector3.zero, originalScaleOpen, 0.3f));

        if (logo != null)
        {
            logo.localScale = Vector3.zero;
            logo.gameObject.SetActive(true);
            yield return StartCoroutine(SmoothScale(logo.gameObject, Vector3.zero, Vector3.one, 0.3f));
        }

        yield return new WaitForSeconds(0.25f);

        Image acceptImg = acceptUI.GetComponent<Image>();
        acceptUI.SetActive(true);
        acceptImg.color = new Color(acceptImg.color.r, acceptImg.color.g, acceptImg.color.b, 0f);
        yield return StartCoroutine(SmoothFade(acceptImg, 0f, 1f, 0.3f));

        Button acceptBtn = acceptUI.transform.Find("accept_frame").GetComponent<Button>();
        acceptBtn.onClick.RemoveAllListeners();
        acceptBtn.onClick.AddListener(() => StartCoroutine(ShowDoneUI()));

        isAnimating = false; // ✅ Animation OpenPhone จบ
    }


    IEnumerator ShowDoneUI()
    {
        isAnimating = true; // ✅ เริ่มล็อก
        openUI.SetActive(false);

        Transform logo = doneUI.transform.Find("logo");
        if (logo != null)
        {
            logo.localScale = Vector3.zero;
            logo.gameObject.SetActive(false);
        }


        doneUI.transform.localScale = Vector3.zero;
        doneUI.SetActive(true);

        TMP_Text earnText = doneUI.transform.Find("logo/earn_value").GetComponent<TMP_Text>();
        earnText.text = "00.00฿";

        Image doneImg = doneUI.GetComponent<Image>();
        Color c = doneImg.color;
        c.a = 0f;
        doneImg.color = c;

        yield return StartCoroutine(SmoothScale(doneUI, Vector3.zero, originalScaleOpen, 0.3f));

        if (logo != null)
        {
            logo.gameObject.SetActive(true);
            yield return StartCoroutine(SmoothScale(logo.gameObject, Vector3.zero, Vector3.one, 0.3f));
        }

        yield return new WaitForSeconds(0.25f);

        yield return StartCoroutine(SmoothFade(doneImg, 0f, 1f, 0.3f));

        //Counting Animation ui
        float targetValue = 68.80f;
        if (earnText != null)
        {
            yield return StartCoroutine(CountUpText(earnText, 0f, targetValue, 1f));
        }

        yield return new WaitForSeconds(1.7f);

        
        UpdateShortcutText();

        openUI.SetActive(false);
        acceptUI.SetActive(false);

        yield return StartCoroutine(SmoothScale(doneUI, originalScaleOpen, Vector3.zero, 0.3f));
        doneUI.SetActive(false);

        yield return new WaitForSeconds(.7f);
        isAnimating = false; // ✅ Animation ShowDoneUI จบ
        isOpen = false;
    }

    IEnumerator CountUpText(TMP_Text text, float start, float end, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);
            float value = Mathf.Lerp(start, end, progress);
            text.text = value.ToString("F2") + "฿"; // แสดงทศนิยม 2 ตำแหน่ง
            yield return null;
        }
        text.text = end.ToString("F2") + "฿"; // ให้ตรงกับ target
    }



    private void ClosePhone()
    {
        isOpen = false;
        UpdateShortcutText();

        openUI.SetActive(false);
        acceptUI.SetActive(false);
        doneUI.SetActive(false);
        failUI.SetActive(false);
    }
}
