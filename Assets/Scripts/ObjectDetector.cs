using System.Collections.Generic;
using UnityEngine;

public class ObjectDetector : MonoBehaviour
{
    public LayerMask targetMask;
    public LayerMask obstacleMask;
    public float detectRadius;
    public List<Transform> targets;


    void Start()
    {
        targets = new List<Transform>();
    }


    void Update()
    {
        targets.Clear();
        Collider[] targetsInRadius = Physics.OverlapSphere(transform.position, detectRadius, targetMask);

        for (int i = 0; i < targetsInRadius.Length; i++)
        {
            Transform target = targetsInRadius[i].transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            float dstToTarget = Vector3.Distance(transform.position, target.position);

            if (!Physics.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
            {
                targets.Add(target);
            }
        }

    }
}
