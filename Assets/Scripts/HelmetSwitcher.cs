using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class HelmetSwitcher : MonoBehaviour
{
    public AnimationCurve showCurve;
    public AnimationCurve hideCurve;
    public float animationSpeed = 1f;
    public GameObject helmetUIPanel;
    public Volume globalVolume;

    private bool isShown;
    private bool isAnimating;
    private PlayerController pc;
    private ExponentialHeightFog visualController;

    private void Awake()
    {
        pc = GameObject.Find("Player").GetComponent<PlayerController>();
        if (globalVolume == null)
            globalVolume = GameObject.Find("Global Volume")?.GetComponent<Volume>();

        if (globalVolume != null && globalVolume.profile != null &&
            globalVolume.profile.TryGet(out visualController))
        {
            visualController.enableHelmetMask.overrideState = true; // 允许被脚本修改
        }
    }

    private void Update()
    {
        if (pc.isWearHelmet)
        {
            if (!isShown && !isAnimating)
            {
                StartCoroutine(ShowPanel());
                visualController.enableHelmetMask.value = pc.isWearHelmet;
                isShown = true;
            }
        }
        else
        {
            if (isShown && !isAnimating)
            {
                StartCoroutine(HidePanel());
                visualController.enableHelmetMask.value = pc.isWearHelmet;
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

