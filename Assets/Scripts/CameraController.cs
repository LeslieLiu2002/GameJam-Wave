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

    void Update()
    {
        mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -headMaxRotationAngle, headMaxRotationAngle);

        player.Rotate(Vector3.up * mouseX);

        transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
    }
}
