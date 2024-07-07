// CameraController.cs

using UnityEngine;

public class CameraController : MonoBehaviour
{
    private new Transform transform;
    public float acceleration = 40.0f;
    public float maxSpeed = 20.0f;
    public float rotationSpeed = 80.0f;

    private Vector3 speed = new Vector3(0.0f, 0.0f, 0.0f);
    private float rotationX = 0.0f;
    private float rotationY = 0.0f;
    private bool dispatch = true;

    private float MapRotationXToValidRange(float theta)
    {
        int n = (int)Mathf.Floor(theta / 180.0f + 0.5f);
        return n % 2 == 0 ? theta - n * 180.0f : -theta + n * 180.0f;
    }

    private float MapRotationYToValidRange(float theta)
    {
        theta %= 360.0f;
        return theta < 0.0f ? theta + 360.0f : theta;
    }

    void Start()
    {
        Cursor.visible = true;
        transform = GetComponent<Transform>();
        rotationX = MapRotationXToValidRange(transform.rotation.eulerAngles.x);
        rotationY = transform.rotation.eulerAngles.y;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.visible = true;
            dispatch = true;
        }

        if (Input.GetMouseButtonDown(1))
        {
            Cursor.visible = false;
            dispatch = false;

            rotationX = MapRotationXToValidRange(transform.rotation.eulerAngles.x);
            rotationY = transform.rotation.eulerAngles.y;
            speed = Vector3.zero;
        }

        if (!dispatch)
        {
            Cursor.visible = false;

            bool keyDown = false;

            if (Input.GetKey(KeyCode.W))
            {
                keyDown = true;
                speed += Vector3.forward * acceleration * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.S))
            {
                keyDown = true;
                speed += Vector3.back * acceleration * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.A))
            {
                keyDown = true;
                speed += Vector3.left * acceleration * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.D))
            {
                keyDown = true;
                speed += Vector3.right * acceleration * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.Q))
            {
                keyDown = true;
                speed += Vector3.down * acceleration * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.E))
            {
                keyDown = true;
                speed += Vector3.up * acceleration * Time.deltaTime;
            }

            if (speed.magnitude > maxSpeed)
            {
                speed = speed.normalized * maxSpeed;
            }

            if (!keyDown)
            {
                speed -= speed.normalized * Mathf.Min(acceleration * Time.deltaTime, speed.magnitude);
            }

            transform.Translate(speed * Time.deltaTime);

            rotationX -= Input.GetAxis("Mouse Y") * Time.deltaTime * rotationSpeed;
            rotationY += Input.GetAxis("Mouse X") * Time.deltaTime * rotationSpeed;
            
            rotationX = Mathf.Clamp(rotationX, -89.0f, 89.0f);
            rotationY = MapRotationYToValidRange(rotationY);

            transform.rotation = Quaternion.Euler(new Vector3(rotationX, rotationY, 0.0f));
        }
    }
}
