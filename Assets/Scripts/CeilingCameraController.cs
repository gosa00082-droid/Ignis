using UnityEngine;

public class CeilingCameraController : MonoBehaviour
{
    [Header("Перемещение (настрой в инспекторе)")]
    [SerializeField] private float moveSpeed = 25f;

    [Header("Управление (настрой в инспекторе)")]
    [SerializeField] private bool lockCursor = true;

    [Header("Поворот (настрой в инспекторе)")]
    [SerializeField] private float rotationSpeed = 2.5f;

    private float mouseX;
    private float mouseY;  // Для поворота по X
    private bool canControl = true;
    private float currentYaw = 0f;
    private float currentPitch = 90f;  // Базовый pitch=90 (смотрит вниз)

    void Start()
    {
        // Начальная позиция и поворот (hardcoded — измени здесь)
        transform.position = new Vector3(-16.19f, 6f, -0.51f);  // Спавн в (-16.19, 2, -0.51)
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);  // Вид сверху вниз

        if (lockCursor)
            Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = !lockCursor;
    }

    void Update()
    {
        if (!canControl) return;

        HandleMovement();
        HandleRotation();
        ClampPosition();
    }

    private void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");  // A/D: влево/вправо
        float v = Input.GetAxis("Vertical");    // W: вперёд (+v), S: назад (-v)

        // Проекция forward на горизонтальную плоскость (XZ) для нормального движения
        Vector3 horizontalForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        Vector3 movement = horizontalForward * v + transform.right * h;
        movement = movement.normalized * moveSpeed * Time.deltaTime;

        // Без Lerp — мгновенная остановка
        transform.position += movement;
    }

    private void HandleRotation()
    {
        mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
        mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;  // Поворот по X (pitch)

        // Поворот yaw (Y) на 360 градусов (без ограничения)
        currentYaw += mouseX;

        // Поворот pitch (X) с ограничением: мышь вниз — до 45, вверх — до 90
        float minPitch = 30f;    // Мин. (опустить мышь вниз)
        float maxPitch = 90f;    // Макс. (поднять мышь вверх)
        currentPitch -= mouseY;  // Инвертированный знак: мышь вниз — pitch уменьшается
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

        transform.localRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
    }

    private void ClampPosition()
    {
        Vector3 pos = transform.position;

        // Ограничения (hardcoded — измени здесь)
        float minX = -21f;   // Мин. X
        float maxX = -10.4f;    // Макс. X
        float fixedY = 6f;   // Фиксировано y=2
        float minZ = -8f;    // Мин. Z
        float maxZ = 7.2f;    // Макс. Z

        pos.y = fixedY;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);

        transform.position = pos;
    }

    public void SetControl(bool control)
    {
        canControl = control;
        Cursor.lockState = control && lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !control;
    }

    public Camera GetCamera() => GetComponent<Camera>();
}