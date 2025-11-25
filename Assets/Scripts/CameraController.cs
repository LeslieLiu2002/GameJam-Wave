using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public Transform player;
    private float mouseX,mouseY;
    public float mouseSensitivity;
    private float xRotation;
    private float yRotation;

    void Update()
    {
        mouseX=Input.GetAxis("Mouse X")*mouseSensitivity*Time.deltaTime;
        mouseY=Input.GetAxis("Mouse Y")*mouseSensitivity*Time.deltaTime;

        xRotation -=mouseY;
        xRotation = Mathf.Clamp(xRotation,-70f,70f);

        player.Rotate(Vector3.up*mouseX);
        
        transform.localRotation=Quaternion.Euler(xRotation,0,0);
    }
}
