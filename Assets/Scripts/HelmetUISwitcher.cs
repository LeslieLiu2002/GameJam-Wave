using System.Collections;
using UnityEngine;

public class HelmetUISwitcher : MonoBehaviour
{
    public AnimationCurve showCurve;
    public AnimationCurve hideCurve;
    public float animationSpeed = 1f;
    public GameObject helmetUIPanel;

    private bool isShown;
    private bool isAnimating;
    private PlayerController pc;

    private void Awake()
    {
        pc = GameObject.Find("Player").GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (pc.isWearHelmet)
        {
            if (!isShown && !isAnimating)
            {
                StartCoroutine(ShowPanel());
                isShown = true;
            }
        }
        else
        {
            if (isShown && !isAnimating)
            {
                StartCoroutine(HidePanel());
                isShown = false;
            }
        }
    }

    private IEnumerator ShowPanel()
    {
        isAnimating = true;
        float t = 0f;
        while (t <= 1f)
        {
            helmetUIPanel.transform.localScale = Vector3.one * showCurve.Evaluate(t);
            t += Time.deltaTime * animationSpeed;
            yield return null;
        }
        // 强制收尾，避免浮点误差
        helmetUIPanel.transform.localScale = Vector3.one * showCurve.Evaluate(1f);
        isAnimating = false;
    }

    private IEnumerator HidePanel()
    {
        isAnimating = true;
        float t = 0f;
        while (t <= 1f)
        {
            helmetUIPanel.transform.localScale = Vector3.one * hideCurve.Evaluate(t);
            t += Time.deltaTime * animationSpeed;
            yield return null;
        }
        // 强制收尾，避免浮点误差
        helmetUIPanel.transform.localScale = Vector3.one * hideCurve.Evaluate(1f);
        isAnimating = false;
    }
}

