using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyRadarResponde : MonoBehaviour, IRadarDetectable
{
    [SerializeField] private Transform pulseTransform; // Pulse child on enemy
    [SerializeField] private float pulseRangeMax = 8f;
    [SerializeField] private float pulseRangeSpeed = 12f;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private EnemyChaseController chaseController;

    public float PulseRangeMax => pulseRangeMax;

    private float range;
    private bool isPulsing;
    private List<Collider> alreadyHitPlayers = new List<Collider>();
    private SpriteRenderer pulseRenderer;
    private Color pulseColor;

    void Awake()
    {
        if (pulseTransform == null) pulseTransform = transform.Find("Pulse");
        if (chaseController == null) chaseController = transform.GetComponent<EnemyChaseController>();
        pulseRenderer = pulseTransform.GetComponent<SpriteRenderer>();
        pulseColor = pulseRenderer.color;
        pulseTransform.gameObject.SetActive(false);
    }

    public void OnRadarPing(Vector3 sourcePosition, float sourceRangeMax, float sourceRangeSpeed)
    {
        // Avoid duplicate trigger
        if (!isPulsing) StartCoroutine(CounterPulseRoutine());
    }

    private IEnumerator CounterPulseRoutine()
    {
        isPulsing = true;
        range = 0f;
        alreadyHitPlayers.Clear();
        pulseTransform.gameObject.SetActive(true);

        while (range < pulseRangeMax)
        {
            range += pulseRangeSpeed * Time.deltaTime;
            pulseTransform.localScale = new Vector3(range, range, 1f);

            // Same radius conversion as player radar
            var hits = Physics.OverlapSphere(transform.position, range * 0.5f, playerMask);
            foreach (var hit in hits)
            {
                if (alreadyHitPlayers.Contains(hit)) continue;

                var dir = (hit.transform.position - transform.position).normalized;
                var dst = Vector3.Distance(transform.position, hit.transform.position);
                if (!Physics.Raycast(transform.position, dir, dst, obstacleMask))
                {
                    alreadyHitPlayers.Add(hit);
                    // TODO: handle player detection (e.g. IPlayerDetected or hint)
                    if (chaseController != null)
                    {
                        chaseController.SetTarget(hit.transform);
                    }
                }
            }

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
}
