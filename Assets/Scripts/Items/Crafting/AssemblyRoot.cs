using UnityEngine;

public class AssemblyRoot : MonoBehaviour
{
    [Header("Плоскость перетаскивания")]
    [SerializeField] private Transform dragPlaneReference;

    [Header("С чем нельзя пересекаться")]
    [SerializeField] private LayerMask blockingMask;

    [Header("Маленький отступ от стола")]
    [SerializeField] private float surfacePadding = 0.001f;

    [Header("Шаг проверки коллизии")]
    [SerializeField] private float collisionStepDistance = 0.03f;

    private Camera mainCamera;
    private bool isDragging;
    private Vector3 horizontalOffset;

    public Transform DragPlaneReference => dragPlaneReference;
    public LayerMask BlockingMask => blockingMask;
    public float SurfacePadding => surfacePadding;
    public float CollisionStepDistance => collisionStepDistance;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    public void CopySettingsFrom(AssemblyRoot other)
    {
        if (other == null)
            return;

        dragPlaneReference = other.dragPlaneReference;
        blockingMask = other.blockingMask;
        surfacePadding = other.surfacePadding;
        collisionStepDistance = other.collisionStepDistance;
    }

    public void BeginDrag()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("На сцене нет MainCamera.");
            return;
        }

        if (dragPlaneReference == null)
        {
            Debug.LogError($"[{name}] Не назначен DragPlaneReference.");
            return;
        }

        if (TryGetPointOnDragPlane(out Vector3 planePoint))
        {
            isDragging = true;

            horizontalOffset = new Vector3(
                transform.position.x - planePoint.x,
                0f,
                transform.position.z - planePoint.z);
        }
    }

    public void DragToMouse()
    {
        if (!isDragging)
            return;

        if (!TryGetPointOnDragPlane(out Vector3 planePoint))
            return;

        float targetY = CalculateTargetY();
        Vector3 targetPosition = new Vector3(
            planePoint.x + horizontalOffset.x,
            targetY,
            planePoint.z + horizontalOffset.z);

        MoveWithCollision(targetPosition);
    }

    public void EndDrag()
    {
        isDragging = false;
    }

    private void MoveWithCollision(Vector3 targetPosition)
    {
        Vector3 start = transform.position;
        Vector3 delta = targetPosition - start;

        float safeStep = Mathf.Max(0.001f, collisionStepDistance);
        int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / safeStep));

        Vector3 lastSafePosition = start;

        for (int i = 1; i <= steps; i++)
        {
            Vector3 candidate = Vector3.Lerp(start, targetPosition, i / (float)steps);
            Vector3 testDelta = candidate - start;

            if (WouldCollideAt(testDelta))
                break;

            lastSafePosition = candidate;
        }

        transform.position = lastSafePosition;
    }

    private float CalculateTargetY()
    {
        WorkbenchPart[] parts = GetComponentsInChildren<WorkbenchPart>(true);

        bool foundAnyBottomPoint = false;
        float minOffsetY = float.MaxValue;

        foreach (WorkbenchPart part in parts)
        {
            if (part == null || part.BottomPoints == null)
                continue;

            foreach (Transform bottomPoint in part.BottomPoints)
            {
                if (bottomPoint == null)
                    continue;

                float offsetY = bottomPoint.position.y - transform.position.y;

                if (offsetY < minOffsetY)
                {
                    minOffsetY = offsetY;
                    foundAnyBottomPoint = true;
                }
            }
        }

        float planeY = dragPlaneReference.position.y;

        if (!foundAnyBottomPoint)
            return planeY + surfacePadding;

        return planeY - minOffsetY + surfacePadding;
    }

    private bool TryGetPointOnDragPlane(out Vector3 point)
    {
        point = Vector3.zero;

        if (mainCamera == null || dragPlaneReference == null)
            return false;

        Plane dragPlane = new Plane(Vector3.up, dragPlaneReference.position);
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (dragPlane.Raycast(ray, out float enter))
        {
            point = ray.GetPoint(enter);
            return true;
        }

        return false;
    }

    private bool WouldCollideAt(Vector3 delta)
    {
        Collider[] ownColliders = GetComponentsInChildren<Collider>(true);

        foreach (Collider own in ownColliders)
        {
            if (own == null || !own.enabled || own.isTrigger)
                continue;

            if (CheckSingleColliderOverlap(own, delta))
                return true;
        }

        return false;
    }

    private bool CheckSingleColliderOverlap(Collider own, Vector3 delta)
    {
        Collider[] overlaps;

        if (own is BoxCollider box)
        {
            Vector3 center = box.transform.TransformPoint(box.center) + delta;
            Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, Abs(box.transform.lossyScale));

            overlaps = Physics.OverlapBox(
                center,
                halfExtents,
                box.transform.rotation,
                blockingMask,
                QueryTriggerInteraction.Ignore);
        }
        else if (own is SphereCollider sphere)
        {
            Vector3 center = sphere.transform.TransformPoint(sphere.center) + delta;
            float radius = sphere.radius * MaxAbs(sphere.transform.lossyScale);

            overlaps = Physics.OverlapSphere(
                center,
                radius,
                blockingMask,
                QueryTriggerInteraction.Ignore);
        }
        else if (own is CapsuleCollider capsule)
        {
            GetCapsuleWorldData(capsule, delta, out Vector3 p0, out Vector3 p1, out float radius);

            overlaps = Physics.OverlapCapsule(
                p0,
                p1,
                radius,
                blockingMask,
                QueryTriggerInteraction.Ignore);
        }
        else
        {
            Bounds b = own.bounds;
            Vector3 center = b.center + delta;
            Vector3 halfExtents = b.extents;

            overlaps = Physics.OverlapBox(
                center,
                halfExtents,
                own.transform.rotation,
                blockingMask,
                QueryTriggerInteraction.Ignore);
        }

        foreach (Collider hit in overlaps)
        {
            if (hit == null)
                continue;

            if (hit.transform.IsChildOf(transform))
                continue;

            return true;
        }

        return false;
    }

    private void GetCapsuleWorldData(CapsuleCollider capsule, Vector3 delta, out Vector3 p0, out Vector3 p1, out float radius)
    {
        Transform t = capsule.transform;
        Vector3 center = t.TransformPoint(capsule.center) + delta;
        Vector3 lossy = Abs(t.lossyScale);

        Vector3 axis;
        float axisScale;
        float radiusScale;

        switch (capsule.direction)
        {
            case 0:
                axis = t.right;
                axisScale = lossy.x;
                radiusScale = Mathf.Max(lossy.y, lossy.z);
                break;
            case 1:
                axis = t.up;
                axisScale = lossy.y;
                radiusScale = Mathf.Max(lossy.x, lossy.z);
                break;
            default:
                axis = t.forward;
                axisScale = lossy.z;
                radiusScale = Mathf.Max(lossy.x, lossy.y);
                break;
        }

        radius = capsule.radius * radiusScale;
        float height = Mathf.Max(capsule.height * axisScale, radius * 2f);
        float halfSegment = Mathf.Max(0f, height * 0.5f - radius);

        p0 = center + axis * halfSegment;
        p1 = center - axis * halfSegment;
    }

    private Vector3 Abs(Vector3 v)
    {
        return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }

    private float MaxAbs(Vector3 v)
    {
        return Mathf.Max(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }
}