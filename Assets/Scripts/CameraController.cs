using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public Transform player;
    public float mouseSensitivity; // 鼠标灵敏度
    public float headMaxRotationAngle; // 头部垂直转动的最大角度
    private float mouseX, mouseY;
    private float xRotation;
    private float yRotation;
    private PlayerInputHub pih;

    void Awake()
    {
        pih = GameObject.Find("GameController").GetComponent<PlayerInputHub>();
    }

    void Update()
    {
        mouseX = pih.MouseX * mouseSensitivity * Time.deltaTime;
        mouseY = pih.MouseY * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -headMaxRotationAngle, headMaxRotationAngle);

        player.Rotate(Vector3.up * mouseX);

        transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
    }
}
