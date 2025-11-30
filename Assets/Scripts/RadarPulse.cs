using System.Collections.Generic;
using UnityEngine;

public class RadarPulse : MonoBehaviour
{
    public float rangeMax = 10f;
    public float rangeSpeed = 10f;
    public float fadeRange;
    public LayerMask enemytMask;
    public LayerMask obstacleMask;
    public LayerMask rewardMask;
    [SerializeField] private Transform pfRadarPing;

    private Transform pulseTransform;
    private float range;
    private List<Collider> alreadyPingedColliderList = new List<Collider>();
    private SpriteRenderer pulseSpriteRenderer;
    private Color pulseColor;

    private void Awake()
    {
        pulseTransform = transform.Find("Pulse");
        pulseSpriteRenderer = pulseTransform.GetComponent<SpriteRenderer>();
        pulseColor = pulseSpriteRenderer.color;
    }
    private void Update()
    {
        range += rangeSpeed * Time.deltaTime;
        if (range > rangeMax)
        {
            range = 0f;
            alreadyPingedColliderList.Clear();
        }
        pulseTransform.localScale = new Vector3(range, range, 0);

        // 检测奖励和敌人
        var combinedMask = enemytMask | rewardMask;
        Collider[] targetsInRadius = Physics.OverlapSphere(transform.position, range / 2, combinedMask);
        foreach (Collider collider in targetsInRadius)
        {
            if (!alreadyPingedColliderList.Contains(collider))
            {
                Transform target = collider.transform;
                Vector3 dirToTarget = (target.position - transform.position).normalized;
                float dstToTarget = Vector3.Distance(transform.position, target.position);
                if (!Physics.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
                {
                    alreadyPingedColliderList.Add(collider);
                    Quaternion rot = Quaternion.Euler(-90f, 0, 0);
                    Transform radarPingTransform = null;
                    RadarPing radarPing = null;
                    radarPingTransform = Instantiate(pfRadarPing, collider.transform.position, rot);

                    radarPing = radarPingTransform.GetComponent<RadarPing>();

                    if (collider.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                        radarPing.SetColor(new Color(1, 0, 0));// 红色
                    if (collider.gameObject.layer == LayerMask.NameToLayer("Reward"))
                        radarPing.SetColor(new Color(1, 1, 0));// 黄色
                    radarPing.SetDisappearTimer(rangeMax / rangeSpeed);

                    //  激活敌人和奖励的探测波
                    var detectable = collider.GetComponent<IRadarDetectable>();
                    if (detectable != null)
                    {
                        detectable.OnRadarPing(transform.position, rangeMax, rangeSpeed);
                    }
                }
            }
        }


        if (range > rangeMax - fadeRange)
        {
            pulseColor.a = Mathf.Lerp(0f, 1f, (rangeMax - range) / fadeRange);
        }
        else
        {
            pulseColor.a = 1f;
        }
        pulseSpriteRenderer.color = pulseColor;
    }
}
