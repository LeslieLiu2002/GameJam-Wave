using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Camera m_camera;
    public float norSpeed; // 正常移动速度
    public float accSpeed; // 加速移动速度
    public float upSpeed;
    public float downSpeed;
    public float gravity; // 向下的力（重力加浮力的作用）
    public float StaminaValue; // 体力条的总值
    public float StaminaCost; // 体力的消耗值
    public float StaminaRecover; // 体力的恢复值
    public float StaminaCurrent; // 当前的体力
    public float StaminaMin; // 允许冲刺的最小值
    public float HelmetValue; // 头盔的可佩戴时间总值
    public float HelmetCost; // 头盔的佩戴消耗值
    public float HelmetRecover; // 头盔的佩戴恢复值
    public float HelmetCurrent; //  当前可佩戴时间
    public float HelmetMin; // 允许佩戴的最小值
    public bool isWearHelmet;

    private CharacterController cc;//自带碰撞体和刚体,但是不带物理引擎
    private float horizontalMove, verticalMove;
    private Vector3 dir;
    private Vector3 velocity;// 被重力控制的向下的速度
    // 利用Physics.CheckSphere来检测是否碰到地面
    public Transform groundCheck;// 检测点的中心位置
    public float checkRadius;// 检测点的半径
    public LayerMask groundLayer;// 需要检测的层级
    private bool isGround;
    private PlayerInputHub pih;
    private bool isDash;
    private bool isAllowHelmet;
    private float moveSpeed;

    void Awake()
    {
        pih = GameObject.Find("GameController").GetComponent<PlayerInputHub>();
    }

    void Start()
    {
        cc = GetComponent<CharacterController>();
        StaminaCurrent = StaminaValue;
        isDash = false;
        moveSpeed = norSpeed;
    }

    void Update()
    {
        isGround = Physics.CheckSphere(groundCheck.position, checkRadius, groundLayer);

        // 按空格垂直向上移动
        if (pih.AscendDown)
        {
            velocity.y = upSpeed;
        }
        if (pih.AscendUp)
        {
            velocity.y = 0;
        }
        // 按左ctrl垂直向下移动
        if (pih.DescendDown)
        {
            velocity.y = -downSpeed;
        }
        if (pih.DescendUp)
        {
            velocity.y = 0;
        }
        // 检测是否到地底
        if (isGround && velocity.y < 0)
        {
            velocity.y = 0f;
        }
        // 按左Shift加速移动
        DashControl();
        ReduceAndRecoverStamina();

        // 佩戴头盔
        HelmetController();
        ReduceAndRecoverHelmet();

        horizontalMove = pih.Horizontal * moveSpeed;
        verticalMove = pih.Vertical * moveSpeed;

        Vector3 camForward = m_camera.transform.forward.normalized;
        Vector3 camRight = m_camera.transform.right.normalized;

        dir = transform.forward * verticalMove + transform.right * horizontalMove;
        // dir = camForward * verticalMove + camRight * horizontalMove;
        cc.Move(dir * Time.deltaTime);

        velocity.y -= gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }

    void DashControl()
    {
        if (pih.Horizontal != 0 || pih.Vertical != 0)
        {
            if (pih.SprintHeld && StaminaCurrent > StaminaMin)
            {
                moveSpeed = accSpeed;
                isDash = true;
            }
            else
            {
                moveSpeed = norSpeed;
                isDash = false;
            }
        }
    }

    void ReduceAndRecoverStamina()
    {
        if (pih.Horizontal != 0 || pih.Vertical != 0)
        {
            if (pih.SprintHeld && StaminaCurrent > 0)
            {
                StaminaCurrent -= StaminaCost * Time.deltaTime;
            }
        }
        if (!isDash && StaminaCurrent < StaminaValue)
        {
            StaminaCurrent += StaminaRecover * Time.deltaTime;
        }
    }

    void HelmetController()
    {
        if (isAllowHelmet)
        {
            if (pih.EDown) isWearHelmet = !isWearHelmet;
        }
        else if (isWearHelmet)
        {
            isWearHelmet = !isWearHelmet;
        }
    }
    void ReduceAndRecoverHelmet()
    {
        if (isWearHelmet)
        {
            if (HelmetCurrent > 0) HelmetCurrent -= HelmetCost * Time.deltaTime;
            else isAllowHelmet = false;
        }
        else
        {
            if (HelmetCurrent > HelmetMin) isAllowHelmet = true;
            if (HelmetCurrent < HelmetValue) HelmetCurrent += HelmetRecover * Time.deltaTime;
        }
    }
}
