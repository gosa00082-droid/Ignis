using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CeilingCameraController : MonoBehaviour
{
    [Header("Перемещение")]
    [SerializeField] private float moveSpeed = 25f;

    [Header("Приближение колёсиком мыши")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minHeight = 3f;
    [SerializeField] private float maxHeight = 5.5f;

    [Header("Поворот (правая кнопка мыши)")]
    [SerializeField] private float rotationSpeed = 2.5f;

    [Header("Коллизия")]
    [SerializeField] private float collisionRadius = 0.3f; // радиус сферы коллайдера

    private CharacterController controller;
    private float currentHeight = 5.5f;
    private float currentYaw = 0f;
    private float currentPitch = 90f;

    // Для точного возврата курсора после поворота
    private Vector3 savedMousePosition;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        controller.radius = collisionRadius;
        controller.height = 0.1f;           // почти плоский
        controller.skinWidth = 0.05f;
        controller.stepOffset = 0f;

        // Начальная настройка
        transform.position = new Vector3(-16.19f, 5.5f, -0.51f);
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        currentHeight = transform.position.y;
        currentYaw = transform.eulerAngles.y;
    }

    void Update()
    {
        HandleZoom();
        HandleRotation();
        HandleMovement();
        ClampHeight();
    }

    private void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 horizontalForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        Vector3 movement = horizontalForward * v + transform.right * h;
        movement *= moveSpeed * Time.deltaTime;

        // Двигаем через CharacterController — теперь стены и декор будут блокировать!
        controller.Move(movement);
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            currentHeight -= scroll * zoomSpeed;
            currentHeight = Mathf.Clamp(currentHeight, minHeight, maxHeight);
        }
    }

    private void HandleRotation()
    {
        // === ПРАВАЯ КНОПКА НАЖАТА ===
        if (Input.GetMouseButtonDown(1))
        {
            // Сохраняем позицию курсора
            savedMousePosition = Input.mousePosition;

            // Скрываем курсор и лочим его
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

            currentYaw += mouseX;
            currentPitch -= mouseY;
            currentPitch = Mathf.Clamp(currentPitch, 30f, 90f);

            transform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        }

        // === ПРАВАЯ КНОПКА ОТПУЩЕНА ===
        if (Input.GetMouseButtonUp(1))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Возвращаем курсор точно туда, где он был до поворота
            Cursor.SetCursor(null, savedMousePosition, CursorMode.Auto);
        }
    }

    private void ClampHeight()
    {
        Vector3 pos = transform.position;
        pos.y = currentHeight;
        transform.position = pos;
    }

    // Публичный метод, если нужно отключать управление
    public void SetControl(bool control)
    {
        enabled = control;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}