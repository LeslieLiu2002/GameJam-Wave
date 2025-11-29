using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyChaseController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private float stopDistance = 1.5f; // 防止模型穿模设置的安全距离
    [SerializeField] private float catchDistance = 1.5f; // 必须大于等于stopDistance，否则无法检测到被抓捕
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private EnemyRadarResponde radarResponder;
    [SerializeField] private PauseManager pauseManager;

    private Transform target;
    private Rigidbody rb;
    private float maxChaseDistanceSqr;
    private bool hasCaughtPlayer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (radarResponder == null)
        {
            radarResponder = GetComponent<EnemyRadarResponde>();
        }
        if (pauseManager == null)
        {
            pauseManager = GameObject.Find("GameController").GetComponent<PauseManager>();
        }
        CacheMaxChaseDistance();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        CacheMaxChaseDistance();
    }

    private void CacheMaxChaseDistance()
    {
        maxChaseDistanceSqr = radarResponder != null ? radarResponder.PulseRangeMax * radarResponder.PulseRangeMax : 0f;
    }

    private void FixedUpdate()
    {
        if (target == null || hasCaughtPlayer) return;

        Vector3 toTarget = target.position - transform.position;
        float sqrDist = toTarget.sqrMagnitude;
        float dist = Mathf.Sqrt(sqrDist);

        if (maxChaseDistanceSqr > 0f && sqrDist > maxChaseDistanceSqr)
        {
            target = null;
            return;
        }

        if (Physics.Raycast(transform.position, toTarget.normalized, dist, obstacleMask))
        {
            target = null;
            return;
        }

        if (sqrDist <= catchDistance * catchDistance)
        {
            HandleCaughtPlayer();
            return;
        }

        if (sqrDist <= stopDistance * stopDistance) return;

        Vector3 moveDir = toTarget.normalized;
        Vector3 targetPosition = rb.position + moveDir * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(targetPosition);

        Quaternion targetRotation = Quaternion.LookRotation(moveDir, Vector3.up);
        rb.MoveRotation(Quaternion.Lerp(rb.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
    }

    private void HandleCaughtPlayer()
    {
        hasCaughtPlayer = true;
        target = null;
        rb.linearVelocity = Vector3.zero;

        if (pauseManager == null)
        {
            pauseManager = GameObject.Find("GameController").GetComponent<PauseManager>();
        }
        if (pauseManager != null)
        {
            pauseManager.GameOver();
        }
    }
}
