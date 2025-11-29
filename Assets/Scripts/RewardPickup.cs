using UnityEngine;

public class RewardPickup : MonoBehaviour
{
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private PauseManager pauseManager;

    private bool collected;

    private void Awake()
    {
        if (pauseManager == null)
        {
            pauseManager = GameObject.Find("GameController").GetComponent<PauseManager>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        int layerBit = 1 << other.gameObject.layer;
        if ((playerMask.value & layerBit) == 0) return;

        collected = true;
        if (pauseManager != null)
        {
            pauseManager.OnRewardCollected();
        }

        gameObject.SetActive(false);
    }
}
