using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AreaSoundTrigger : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip clip;
    public bool stopOnExit = true; // Stop playback when player exits the area

    private void Reset()
    {
        var c = GetComponent<Collider>();
        c.isTrigger = true;
    }

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
            audioSource.playOnAwake = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || audioSource == null)
            return;

        if (clip != null)
            audioSource.clip = clip;

        audioSource.Stop(); // Ensure state switches immediately
        audioSource.Play();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") || audioSource == null)
            return;

        if (stopOnExit)
            audioSource.Stop();
    }
}
