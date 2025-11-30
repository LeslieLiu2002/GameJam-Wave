using UnityEngine;
using UnityEngine.UI;

public class HelmetBarController : MonoBehaviour
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

        float ratio = Mathf.Clamp01(m_pc.HelmetCurrent / m_pc.HelmetValue);
        m_ref.fillAmount = ratio;
    }
}
