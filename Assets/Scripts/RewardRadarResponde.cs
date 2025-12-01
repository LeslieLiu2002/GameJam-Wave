using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RewardRadarResponde : MonoBehaviour, IRadarDetectable
{
    [SerializeField] private Transform pulseTransform; // Pulse child on reward
    [SerializeField] private float pulseRangeMax = 30f;
    [SerializeField] private float pulseRangeSpeed = 30f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip detectClip;

    public float PulseRangeMax => pulseRangeMax;

    private float range;
    private bool isPulsing;
    private SpriteRenderer pulseRenderer;
    private Color pulseColor;

    void Awake()
    {
        if (pulseTransform == null) pulseTransform = transform.Find("Pulse");
        if (audioSource == null) audioSource = transform.GetComponent<AudioSource>();
        pulseRenderer = pulseTransform.GetComponent<SpriteRenderer>();
        pulseColor = pulseRenderer.color;
        pulseTransform.gameObject.SetActive(false);
    }

    public void OnRadarPing(Vector3 sourcePosition, float sourceRangeMax, float sourceRangeSpeed)
    {
        // Avoid duplicate trigger
        if (!isPulsing)
        {
            TryPlayDetectSound();
            StartCoroutine(CounterPulseRoutine());
        }
    }

    private IEnumerator CounterPulseRoutine()
    {
        isPulsing = true;
        range = 0f;
        pulseTransform.gameObject.SetActive(true);

        while (range < pulseRangeMax)
        {
            range += pulseRangeSpeed * Time.deltaTime;
            pulseTransform.localScale = new Vector3(range, range, 1f);

            // Visual fade (optional)
            pulseColor.a = Mathf.Lerp(1f, 0f, range / pulseRangeMax);
            pulseRenderer.color = pulseColor;

            yield return null;
        }

        pulseTransform.gameObject.SetActive(false);
        pulseColor.a = 1f;
        pulseRenderer.color = pulseColor;
        isPulsing = false;
    }

    private void TryPlayDetectSound()
    {
        if (detectClip == null || audioSource == null) return;
        if (!ReserveAudioSlot(detectClip.length)) return;

        audioSource.PlayOneShot(detectClip);
    }

    private const int MaxConcurrentDetectClips = 2;
    private static readonly List<double> activeClipEndTimes = new List<double>();

    private static bool ReserveAudioSlot(float clipLength)
    {
        PruneExpiredSlots();
        if (activeClipEndTimes.Count >= MaxConcurrentDetectClips) return false;

        activeClipEndTimes.Add(AudioSettings.dspTime + clipLength);
        return true;
    }

    private static void PruneExpiredSlots()
    {
        double now = AudioSettings.dspTime;
        activeClipEndTimes.RemoveAll(endTime => endTime <= now);
    }
}
