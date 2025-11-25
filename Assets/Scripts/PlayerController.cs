using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Camera m_camera;
    private CharacterController cc;//自带碰撞体和刚体,但是不带物理引擎
    public float moveSpeed;
    public float upSpeed;
    public float downSpeed;

    private float horizontalMove, verticalMove;
    private Vector3 dir;
    public float gravity;
    private Vector3 velocity;// 被重力控制的向下的速度
    // 利用Physics.CheckSphere来检测是否碰到地面
    public Transform groundCheck;// 检测点的中心位置
    public float checkRadius;// 检测点的半径
    public LayerMask groundLayer;// 需要检测的层级
    private bool isGround;

    void Start()
    {
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        isGround = Physics.CheckSphere(groundCheck.position, checkRadius, groundLayer);

        if (Input.GetKey(KeyCode.Space))
        {
            velocity.y = upSpeed;
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            velocity.y = 0;
        }

        if (Input.GetKey(KeyCode.LeftControl))
        {
            velocity.y = -upSpeed;
        }
        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            velocity.y = 0;
        }

        if (isGround && velocity.y < 0)
        {
            velocity.y = 0f;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            moveSpeed *= 2;
        }
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            moveSpeed /= 2;
        }

        horizontalMove = Input.GetAxis("Horizontal") * moveSpeed;
        verticalMove = Input.GetAxis("Vertical") * moveSpeed;

        Vector3 camForward = m_camera.transform.forward.normalized;
        Vector3 camRight = m_camera.transform.right.normalized;

        dir = transform.forward * verticalMove + transform.right * horizontalMove;
        // dir = camForward * verticalMove + camRight * horizontalMove;
        cc.Move(dir * Time.deltaTime);

        velocity.y -= gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }

}
