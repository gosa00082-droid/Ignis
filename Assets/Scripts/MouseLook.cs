using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [SerializeField] private float sensitivity = 2f;
    [SerializeField] private float minVerticalAngle = -70f;
    [SerializeField] private float maxVerticalAngle = 60f;

    private float yaw;
    private float pitch;

    public bool IsRotating { get; private set; }
    public bool IsLocked { get; set; }

    void Start()
    {
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
    }

    void Update()
    {
        if (IsLocked)
            return;

        if (Input.GetMouseButtonDown(1))
            StartRotation();

        if (Input.GetMouseButtonUp(1))
            StopRotation();

        if (!IsRotating)
            return;

        float mouseX = Input.GetAxis("Mouse X") * sensitivity * 100f * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * 100f * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;

        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void StartRotation()
    {
        if (IsLocked) return;

        IsRotating = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void StopRotation()
    {
        IsRotating = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void LockLook()
    {
        IsLocked = true;
        IsRotating = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void UnlockLook()
    {
        IsLocked = false;
    }
}