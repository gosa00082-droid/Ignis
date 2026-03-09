using UnityEngine;

public class CeilingCameraController : MonoBehaviour
{
    [Header("Перемещение (настрой в инспекторе)")]
    [SerializeField] private float moveSpeed = 25f;

    [Header("Управление (настрой в инспекторе)")]
    [SerializeField] private bool lockCursor = true;
    [SerializeField] private bool smoothMovement = true;

    [Header("Поворот (настрой в инспекторе)")]
    [SerializeField] private float rotationSpeed = 2.5f;

    private float mouseX;
    private float mouseY;  // Новый: для поворота по X
    private bool canControl = true;
    private float currentYaw = 0f;
    private float currentPitch = 90f;  // Базовый pitch=90 (смотрит вниз)

    void Start()
    {
        // Начальная позиция и поворот (hardcoded — измени здесь)
        transform.position = new Vector3(-16.19f, 2f, -0.51f);  // Спавн в (-16.19, 2, -0.51)
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

        Vector3 dir = new Vector3(h, 0, v).normalized;

        if (smoothMovement)
        {
            Vector3 target = transform.position +
                             transform.forward * dir.z * moveSpeed * Time.deltaTime +
                             transform.right * dir.x * moveSpeed * Time.deltaTime;

            transform.position = Vector3.Lerp(transform.position, target, 0.15f);
        }
        else
        {
            transform.Translate(dir * moveSpeed * Time.deltaTime, Space.Self);
        }
    }

    private void HandleRotation()
    {
        mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
        mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;  // Новый: поворот по X (pitch)

        // Поворот yaw (Y) с ограничением
        float minYaw = -45f;
        float maxYaw = 45f;
        currentYaw += mouseX;
        currentYaw = Mathf.Clamp(currentYaw, minYaw, maxYaw);

        // Поворот pitch (X) с ограничением "в другую сторону" (от 90 до 130, например, для наклона вверх)
        float minPitch = 90f;    // Базовый (сверху)
        float maxPitch = 130f;   // +40 градусов (наклон "вверх" или в другую сторону — настрой здесь)
        currentPitch += mouseY;  // Изменил знак для "другой стороны" (мышь вверх — наклон вверх)
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

        transform.localRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
    }

    private void ClampPosition()
    {
        Vector3 pos = transform.position;

        // Ограничения (hardcoded — измени здесь)
        float maxX = 2f;   // Камера не дальше x=2
        float fixedY = 2f; // Фиксировано y=2
        float maxZ = 5f;   // Камера не дальше z=5

        pos.y = fixedY;
        pos.x = Mathf.Min(pos.x, maxX);  // Только <= maxX (можно добавить minX = -2f; pos.x = Mathf.Clamp(pos.x, minX, maxX);)
        pos.z = Mathf.Min(pos.z, maxZ);

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