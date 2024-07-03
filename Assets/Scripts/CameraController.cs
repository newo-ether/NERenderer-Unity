using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    private new Transform transform;
    public float moveSpeed = 10.0f;
    public float rotationSpeed = 40.0f;
    private float rotationX = 0.0f;
    private float rotationY = 0.0f;

    void Start()
    {
        Cursor.visible = false;
        transform = GetComponent<Transform>();
        rotationX = transform.rotation.eulerAngles.x;
        rotationY = transform.rotation.eulerAngles.y;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.Translate(Vector3.back * moveSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.A))
        {
            transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.Q))
        {
            transform.Translate(Vector3.down * moveSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.E))
        {
            transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);
        }

        rotationX -= Input.GetAxis("Mouse Y") * Time.deltaTime * rotationSpeed;
        rotationY += Input.GetAxis("Mouse X") * Time.deltaTime * rotationSpeed;
        rotationX = Mathf.Clamp(rotationX, -89.9f, 89.0f);

        transform.rotation = Quaternion.Euler(new Vector3(rotationX, rotationY, 0.0f));
    }
}
