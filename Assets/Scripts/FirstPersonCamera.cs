using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonCamera : MonoBehaviour
{

    public Transform player;
    public float mouseSensitivity = 4f;
    float cameraVerticalRotation = 0f;
        
    // Start is called before the first frame update
    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        //Get mouse position
        float inputX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float inputY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Vertical Rotation
        cameraVerticalRotation -= inputY;
        cameraVerticalRotation = Mathf.Clamp(cameraVerticalRotation, -90f, 90);
        transform.localEulerAngles = Vector3.right * cameraVerticalRotation;

        //Horizontal Rotation
        player.Rotate(Vector3.up * inputX);
    }
}
