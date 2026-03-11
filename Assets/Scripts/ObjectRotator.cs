using UnityEngine;

public class ObjectRotator : MonoBehaviour
{
    public Transform targetObject;
    public float rotationSpeed = 100f;
    public float distance = 5f;
    public float minYAngle = -80f;
    public float maxYAngle = 80f;

    private float currentX = 0f;
    private float currentY = 0f;

    void Start()
    {
        if (targetObject == null)
        {
            Debug.LogError("Target Object не назначен! Перетащите объект в инспекторе.");
            return;
        }

        // Устанавливаем начальную позицию камеры
        transform.position = targetObject.position + new Vector3(0, 0, -distance);
        currentX = 0f;
        currentY = 0f;
    }

    void LateUpdate()
    {
        if (targetObject == null) return;

        if (Input.GetMouseButton(0))
        {
            currentX += Input.GetAxis("Mouse X") * rotationSpeed * 0.01f;
            currentY -= Input.GetAxis("Mouse Y") * rotationSpeed * 0.01f;

            // Ограничиваем вертикальный угол
            currentY = Mathf.Clamp(currentY, minYAngle, maxYAngle);
        }

        // Создаем вращение
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

        // Позиционируем камеру вокруг объекта
        Vector3 offset = new Vector3(0, 0, -distance);
        transform.position = targetObject.position + rotation * offset;

        // Камера смотрит на объект
        transform.LookAt(targetObject.position);
    }
}