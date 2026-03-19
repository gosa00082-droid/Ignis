using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [SerializeField] private float sensitivity = 2f;
    [SerializeField] private float minVerticalAngle = -70f;
    [SerializeField] private float maxVerticalAngle = 60f;
    [SerializeField] private WorkshopCameraModeManager cameraModeManager;

    private float yaw;
    private float pitch;

    public bool IsRotating { get; private set; }
    public bool IsLocked { get; private set; }

    private void Start()
    {
        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = euler.x;
    }

    private void Update()
    {
        // Пока активен режим объекта — поворот камеры полностью запрещён
        if (cameraModeManager != null && cameraModeManager.IsInObjectMode)
        {
            ForceResetState();
            return;
        }

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

    private void StartRotation()
    {
        if (IsLocked)
            return;

        if (cameraModeManager != null && cameraModeManager.IsInObjectMode)
            return;

        IsRotating = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void StopRotation()
    {
        IsRotating = false;

        if (!IsLocked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
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
        IsRotating = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ForceResetState()
    {
        IsRotating = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDisable()
    {
        ForceResetState();
    }

    private void OnEnable()
    {
        ForceResetState();
    }
}