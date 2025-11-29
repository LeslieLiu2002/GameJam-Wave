using UnityEngine;
using UnityEngine.UI;

public class StaminaController : MonoBehaviour
{
    public Image m_ref;

    private PlayerController m_pc;

    void Start()
    {
        m_pc = this.GetComponent<PlayerController>();
    }
    void Update()
    {
        if (m_ref == null || m_pc == null) return;

        float ratio = Mathf.Clamp01(m_pc.StaminaCurrent / m_pc.StaminaValue);
        m_ref.fillAmount = ratio;
    }
}
