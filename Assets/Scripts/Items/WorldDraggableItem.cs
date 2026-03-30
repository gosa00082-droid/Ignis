using UnityEngine;

public class WorldDraggableItem : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Camera workCamera;
    [SerializeField] private WorkshopCameraModeManager cameraModeManager;

    [Header("Точки")]
    [SerializeField] private Transform shelfPoint;
    [SerializeField] private Transform furnacePoint;

    [Header("Перетаскивание")]
    [SerializeField] private float dragPlaneY = 1.0f;
    [SerializeField] private float dragLiftY = 0.15f;   // насколько приподнимаем объект над плоскостью
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float returnSpeed = 6f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private bool isDragging = false;
    private bool isReturning = false;
    private bool isInFurnace = false;

    private FurnaceDropZone3D currentZone;
    private Plane dragPlane;
    private Vector3 targetPosition;

    private void Log(string message)
    {
        if (debugLogs)
            Debug.Log($"[WorldDraggableItem:{name}] {message}", this);
    }

    private void Start()
    {
        if (workCamera == null)
            workCamera = Camera.main;

        if (workCamera == null)
            Log("ВНИМАНИЕ: workCamera не найдена");

        if (shelfPoint != null)
        {
            transform.position = shelfPoint.position;
            Log($"Стартовая позиция = shelfPoint {shelfPoint.position}");
        }
        else
        {
            Log("ВНИМАНИЕ: shelfPoint не назначен");
        }

        targetPosition = transform.position;
    }

    private void Update()
    {
        if (isDragging)
        {
            DragUpdate();
        }
        else if (isReturning && shelfPoint != null)
        {
            Vector3 returnTarget = shelfPoint.position + Vector3.up * dragLiftY;

            transform.position = Vector3.Lerp(
                transform.position,
                returnTarget,
                returnSpeed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, returnTarget) < 0.02f)
            {
                transform.position = shelfPoint.position;
                isReturning = false;
                Log("Объект вернулся на полку");
            }
        }
    }

    private void OnMouseDown()
    {
        Log("OnMouseDown сработал");

        if (cameraModeManager == null)
        {
            Log("cameraModeManager == null");
            return;
        }

        if (!cameraModeManager.IsInObjectMode)
        {
            Log("Не в object mode, перетаскивание запрещено");
            return;
        }

        if (isInFurnace)
        {
            Log("Объект уже в печи, повторно брать нельзя");
            return;
        }

        if (workCamera == null)
        {
            workCamera = Camera.main;
            if (workCamera == null)
            {
                Log("Не найдена камера для drag");
                return;
            }
        }

        isDragging = true;
        isReturning = false;

        dragPlane = new Plane(Vector3.up, new Vector3(0f, dragPlaneY, 0f));

        Log($"Начали перетаскивание. dragPlaneY={dragPlaneY}, dragLiftY={dragLiftY}");
    }

    private void OnMouseUp()
    {
        Log("OnMouseUp сработал");

        if (!isDragging)
        {
            Log("Но isDragging == false, выходим");
            return;
        }

        isDragging = false;

        if (currentZone != null && currentZone.CanAccept(this))
        {
            Log($"Отпустили над зоной {currentZone.name}, кладем в печь");
            PutIntoFurnace();
        }
        else
        {
            Log("Отпустили мимо зоны, возвращаем на полку");
            ReturnToShelf();
        }
    }

    private void DragUpdate()
    {
        if (workCamera == null)
        {
            Log("DragUpdate: workCamera == null");
            return;
        }

        Ray ray = workCamera.ScreenPointToRay(Input.mousePosition);

        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);

            targetPosition = new Vector3(
                hitPoint.x,
                dragPlaneY + dragLiftY,
                hitPoint.z
            );

            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );
        }
    }

    private void PutIntoFurnace()
    {
        isInFurnace = true;
        isReturning = false;

        if (furnacePoint != null)
        {
            transform.position = furnacePoint.position;
            transform.rotation = furnacePoint.rotation;
            Log($"Объект поставлен в furnacePoint: {furnacePoint.position}");
        }
        else
        {
            Log("ВНИМАНИЕ: furnacePoint не назначен");
        }
    }

    private void ReturnToShelf()
    {
        isInFurnace = false;
        isReturning = true;
        Log("Запущен возврат на полку");
    }

    public void SetCurrentZone(FurnaceDropZone3D zone)
    {
        currentZone = zone;
        Log($"Вошли в drop zone: {zone.name}");
    }

    public void ClearCurrentZone(FurnaceDropZone3D zone)
    {
        if (currentZone == zone)
        {
            currentZone = null;
            Log($"Вышли из drop zone: {zone.name}");
        }
    }
}