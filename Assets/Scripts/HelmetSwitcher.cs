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
    public ScannerController scannerController;
    public GameObject spotLight;

    [Header("Audio")]
    public AudioSource helmetAudioSource;
    public AudioClip helmetOnClip;
    public AudioClip helmetOffClip;

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
            visualController.enableHelmetMask.overrideState = true; // allow script control
        }

        if (helmetAudioSource == null)
            helmetAudioSource = GetComponent<AudioSource>();

        helmetAudioSource.playOnAwake = false;
        // helmetAudioSource.loop = false;

    }

    private void Update()
    {
        if (pc.isWearHelmet)
        {
            if (!isShown && !isAnimating)
            {
                PlayHelmetAudio(helmetOnClip);
                StartCoroutine(ShowPanel());
                spotLight.SetActive(false);
                scannerController.Open();
                visualController.enableHelmetMask.value = pc.isWearHelmet;
                isShown = true;
            }
        }
        else
        {
            if (isShown && !isAnimating)
            {
                PlayHelmetAudio(helmetOffClip);
                StartCoroutine(HidePanel());
                spotLight.SetActive(true);
                scannerController.Close();
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
        // Snap final value to avoid floating point drift
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
        // Snap final value to avoid floating point drift
        helmetUIPanel.transform.localScale = Vector3.one * hideCurve.Evaluate(1f);
        isAnimating = false;
    }

    private void PlayHelmetAudio(AudioClip clip)
    {
        if (helmetAudioSource == null || clip == null)
            return;

        helmetAudioSource.Stop();
        helmetAudioSource.clip = clip;
        helmetAudioSource.Play();
    }
}
