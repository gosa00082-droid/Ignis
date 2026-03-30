using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WorldDraggableItem : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Camera workCamera;
    [SerializeField] private WorkshopCameraModeManager cameraModeManager;
    [SerializeField] private Transform furnacePoint;
    [SerializeField] private Collider furnaceDragSurfaceCollider;
    [SerializeField] private Collider furnaceDropZoneCollider;

    [Header("Перетаскивание")]
    [SerializeField] private LayerMask dragSurfaceMask;
    [SerializeField] private float dragLiftY = 0.05f;
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float rayDistance = 100f;

    [Header("Плавная доводка в печь")]
    [SerializeField] private float snapMoveSpeed = 2.5f;
    [SerializeField] private float snapRotateSpeed = 360f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    [SerializeField] private float returnSpeed = 4f;

    private bool isReturning = false;

    private bool isDragging = false;
    private bool isInFurnace = false;
    private bool isSnappingToPoint = false;

    private FurnaceDropZone3D currentZone;

    private Vector3 targetPosition;
    private Vector3 dragOffsetXZ;

    private Vector3 homePosition;
    private Quaternion homeRotation;

    private Rigidbody rb;
    private Collider cachedCollider;
    private Coroutine snapCoroutine;

    private void Log(string message)
    {
        if (debugLogs)
            Debug.Log($"[WorldDraggableItem:{name}] {message}", this);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cachedCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        if (workCamera == null)
            workCamera = Camera.main;

        homePosition = transform.position;
        homeRotation = transform.rotation;
        targetPosition = transform.position;

        Log($"Start. homePosition = {homePosition}");
    }

    private void SetFurnaceHelpersEnabled(bool state)
    {
        if (furnaceDragSurfaceCollider != null)
            furnaceDragSurfaceCollider.enabled = state;

        if (furnaceDropZoneCollider != null)
            furnaceDropZoneCollider.enabled = state;

        if (!state)
            currentZone = null;

        Log($"Furnace helpers enabled = {state}");
    }

    private void Update()
    {
        if (isDragging)
        {
            DragUpdate();
        }
        else if (isReturning)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                homePosition,
                returnSpeed * Time.deltaTime
            );

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                homeRotation,
                snapRotateSpeed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, homePosition) < 0.01f)
            {
                transform.position = homePosition;
                transform.rotation = homeRotation;
                isReturning = false;

                Log($"Вернулись в стартовую позицию: {homePosition}");
            }
        }
    }

    private void OnMouseDown()
    {
        Log("OnMouseDown");

        if (cameraModeManager == null)
        {
            Log("cameraModeManager == null");
            return;
        }

        if (!cameraModeManager.IsInObjectMode)
        {
            Log("Не в object mode");
            return;
        }

        if (isSnappingToPoint)
        {
            Log("Сейчас идёт плавная доводка, drag запрещён");
            return;
        }

        if (workCamera == null)
        {
            workCamera = Camera.main;
            if (workCamera == null)
            {
                Log("workCamera == null");
                return;
            }
        }

        if (isInFurnace)
        {
            SetFurnaceHelpersEnabled(true);
            isInFurnace = false;
            Log("Начали вытаскивать объект из печи");
        }

        if (TryGetSurfacePoint(out Vector3 hitPoint))
        {
            Vector3 desired = hitPoint + Vector3.up * dragLiftY;
            dragOffsetXZ = transform.position - desired;
            dragOffsetXZ.y = 0f;
        }
        else
        {
            dragOffsetXZ = Vector3.zero;
            Log("Не нашли поверхность под курсором при начале drag");
        }

        isDragging = true;
    }

    private void ReturnHome()
    {
        isInFurnace = false;
        isReturning = true;
    }

    private void OnMouseUp()
    {
        Log("OnMouseUp");

        if (!isDragging)
            return;

        isDragging = false;

        if (currentZone != null && currentZone.CanAccept(this))
        {
            Log($"Отпустили над зоной {currentZone.name}, плавно ведем в furnacePoint");
            StartSnapToFurnace();
        }
        else
        {
            Log("Отпустили мимо зоны, возвращаем в стартовую позицию");
            ReturnHome();
        }
    }

    private void DragUpdate()
    {
        if (!TryGetSurfacePoint(out Vector3 hitPoint))
            return;

        Vector3 desired = hitPoint + Vector3.up * dragLiftY;
        desired += new Vector3(dragOffsetXZ.x, 0f, dragOffsetXZ.z);

        targetPosition = desired;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );
    }

    private bool TryGetSurfacePoint(out Vector3 point)
    {
        point = Vector3.zero;

        if (workCamera == null)
            return false;

        Ray ray = workCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, dragSurfaceMask))
        {
            point = hit.point;
            return true;
        }

        return false;
    }

    private void StartSnapToFurnace()
    {
        if (furnacePoint == null)
        {
            Log("furnacePoint == null");
            return;
        }

        if (snapCoroutine != null)
            StopCoroutine(snapCoroutine);

        snapCoroutine = StartCoroutine(SnapToPointRoutine(furnacePoint.position, furnacePoint.rotation));
    }

    private IEnumerator SnapToPointRoutine(Vector3 targetPos, Quaternion targetRot)
    {
        isSnappingToPoint = true;

        while (Vector3.Distance(transform.position, targetPos) > 0.01f ||
               Quaternion.Angle(transform.rotation, targetRot) > 1f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                snapMoveSpeed * Time.deltaTime
            );

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                snapRotateSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;

        isInFurnace = true;
        isSnappingToPoint = false;
        snapCoroutine = null;

        SetFurnaceHelpersEnabled(false);

        Log($"Предмет плавно установлен в furnacePoint = {targetPos}");
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