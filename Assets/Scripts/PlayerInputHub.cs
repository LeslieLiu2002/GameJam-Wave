using UnityEngine;

// 用于处理用户的输入，键盘鼠标等
public class PlayerInputHub : MonoBehaviour
{
    // 鼠标移动
    public float MouseX { get; private set; }
    public float MouseY { get; private set; }

    // 轴向移动
    public float Horizontal { get; private set; }
    public float Vertical { get; private set; }

    // 垂直移动
    public bool AscendDown { get; private set; }   // Space 按下瞬间
    public bool AscendUp { get; private set; }   // Space 抬起瞬间
    public bool DescendDown { get; private set; }   // LeftCtrl 按下瞬间
    public bool DescendUp { get; private set; }   // LeftCtrl 抬起瞬间

    // 冲刺
    public bool SprintDown { get; private set; }   // Shift 按下瞬间
    public bool SprintUp { get; private set; }   // Shift 抬起瞬间
    public bool SprintHeld { get; private set; } // Shift 一直按下

    // 交互（开关 UI 等）
    public bool EDown { get; private set; }  // E 按下瞬间

    void Update()
    {
        MouseX = Input.GetAxis("Mouse X");
        MouseY = Input.GetAxis("Mouse Y");

        Horizontal = Input.GetAxis("Horizontal");
        Vertical = Input.GetAxis("Vertical");

        AscendDown = Input.GetKeyDown(KeyCode.Space);
        AscendUp = Input.GetKeyUp(KeyCode.Space);
        DescendDown = Input.GetKeyDown(KeyCode.LeftControl);
        DescendUp = Input.GetKeyUp(KeyCode.LeftControl);

        SprintDown = Input.GetKeyDown(KeyCode.LeftShift);
        SprintUp = Input.GetKeyUp(KeyCode.LeftShift);
        SprintHeld = Input.GetKey(KeyCode.LeftShift);

        EDown = Input.GetKeyDown(KeyCode.E);
    }
}
