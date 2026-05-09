using System.Collections.Generic;
using UnityEngine;

public class PhysicsItem : MonoBehaviour
{
    private enum ReturnPhase
    {
        None,
        Lift,
        Travel,
        Drop
    }

    [Header("Слои для проверки столкновений")]
    [SerializeField] private LayerMask blockingMask;
    [SerializeField] private float collisionStepDistance = 0.03f;
    [SerializeField] private float surfacePadding = 0.001f;

    [Header("Плавность перемещения")]
    [SerializeField] private float positionLerpSpeed = 18f;
    [SerializeField] private float rotationSpeedDeg = 360f;

    [Header("Подъем колесом")]
    [SerializeField] private float wheelStep = 0.12f;
    [SerializeField] private float minLift = 0f;
    [SerializeField] private float maxLift = 1.5f;

    [Header("Повороты")]
    [SerializeField] private float keyboardRotateSpeed = 120f;
    [SerializeField] private KeyCode rotateLeftKey = KeyCode.Q;
    [SerializeField] private KeyCode rotateRightKey = KeyCode.E;
    [SerializeField] private KeyCode tiltForwardKey = KeyCode.R;
    [SerializeField] private KeyCode tiltBackwardKey = KeyCode.F;

    [Header("Возврат на стол")]
    [SerializeField] private float outOfBoundsDelay = 0.35f;
    [SerializeField] private float returnMoveSpeed = 4.5f;
    [SerializeField] private float returnRotationSpeedDeg = 540f;
    [SerializeField] private float returnSearchStep = 0.18f;
    [SerializeField] private int returnSearchRings = 12;
    [SerializeField] private float workbenchContactTolerance = 0.08f;
    [SerializeField] private float returnClearance = 0.25f;

    [Header("Точка, за которую удобно тянуть предмет")]
    [SerializeField] private Transform dragPivot;

    [Header("Выпрямлять при блокировке на верстаке")]
    [SerializeField] private bool snapUprightWhenWorkbenchLocked = true;

    private Rigidbody rb;
    private Camera mainCamera;
    private WorkbenchSurface surface;

    private bool isGrabbed;
    private bool isReturning;
    private bool isAttachedToParentAssembly;
    private bool isWorkbenchLocked;

    private Vector3 targetGrabPosition;
    private Quaternion targetGrabRotation;
    private Vector3 grabPivotLocalPoint;
    private float liftOffset;
    private Transform activeGrabPivot;

    private Vector3 lastSafePosition;
    private Quaternion lastSafeRotation;
    private float outOfBoundsTimer;

    private Vector3 returnLiftPoint;
    private Vector3 returnTravelPoint;
    private Vector3 returnFinalPoint;
    private Quaternion returnTargetRotation;
    private ReturnPhase returnPhase;

    private float savedMass = 1f;
    private float savedLinearDamping = 0f;
    private float savedAngularDamping = 0.05f;
    private RigidbodyInterpolation savedInterpolation = RigidbodyInterpolation.Interpolate;
    private CollisionDetectionMode savedCollisionDetection = CollisionDetectionMode.ContinuousDynamic;
    private RigidbodyConstraints savedConstraints = RigidbodyConstraints.None;

    public Transform DragPivot => dragPivot != null ? dragPivot : transform;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
        surface = WorkbenchSurface.Instance;

        CacheRigidbodyDefaults();

        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        // Не кешируем позицию в Awake, т.к. surface может быть null
        // CacheSafePose();
    }

    private void Start()
    {
        if (surface == null)
            surface = WorkbenchSurface.Instance;

        if (mainCamera == null)
            mainCamera = Camera.main;

        // Инициализируем безопасную позицию в Start, когда surface точно инициализирован
        if (surface != null)
        {
            lastSafePosition = transform.position;
            lastSafeRotation = transform.rotation;
            Debug.Log($"[PhysicsItem.Start] Initialized safe pose for {name}: pos={lastSafePosition}, surface.SurfaceY={surface.SurfaceY}");
        }
        else
        {
            Debug.LogError($"[PhysicsItem.Start] WorkbenchSurface.Instance is NULL for {name}!");
        }
    }

    private void Update()
    {
        if (surface == null)
            surface = WorkbenchSurface.Instance;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (isGrabbed)
        {
            UpdateGrabTargetFromInput();
        }
        else if (!isReturning && !isAttachedToParentAssembly)
        {
            MonitorBoundsAndSafePose();
        }
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        if (isGrabbed)
        {
            MoveGrabbedBody();
        }
        else if (isReturning)
        {
            MoveReturningBody();
        }
    }

    public void BeginGrab(Transform clickedTransform = null)
    {
        if (isWorkbenchLocked)
            return;

        if (isAttachedToParentAssembly || surface == null)
            return;

        CreateRigidbodyIfMissing();
        if (rb == null)
            return;

        isReturning = false;
        returnPhase = ReturnPhase.None;
        isGrabbed = true;
        outOfBoundsTimer = 0f;

        activeGrabPivot = ResolveGrabPivot(clickedTransform);
        grabPivotLocalPoint = transform.InverseTransformPoint(activeGrabPivot.position);
        
        float lowestY = GetLowestWorldYForPose(transform.position, transform.rotation);
        liftOffset = Mathf.Clamp(
            lowestY - surface.SurfaceY - surfacePadding,
            minLift,
            maxLift);

        Debug.Log($"[PhysicsItem.BeginGrab] {name}: lowestY={lowestY}, surfaceY={surface.SurfaceY}, liftOffset={liftOffset}, minLift={minLift}, maxLift={maxLift}");

        targetGrabRotation = transform.rotation;
        targetGrabPosition = transform.position;

        ZeroBodyVelocity();
        rb.useGravity = false;
        rb.isKinematic = true;

        UpdateGrabTargetFromInput();
    }

    public void RefreshGrabTargetFromInput()
    {
        UpdateGrabTargetFromInput();
    }

    public void EndGrab()
    {
        if (!isGrabbed || rb == null)
            return;

        isGrabbed = false;
        activeGrabPivot = null;

        if (isWorkbenchLocked)
        {
            ZeroBodyVelocity();
            rb.useGravity = false;
            rb.isKinematic = true;
            return;
        }

        rb.isKinematic = false;
        rb.useGravity = true;
        ZeroBodyVelocity();
        CacheSafePose();
    }

    public void SetAttachedToParentAssembly(bool attached)
    {
        isGrabbed = false;
        isReturning = false;
        returnPhase = ReturnPhase.None;
        isAttachedToParentAssembly = attached;
        outOfBoundsTimer = 0f;

        if (attached)
        {
            if (rb != null)
            {
                ZeroBodyVelocity();
                rb.useGravity = false;
                rb.isKinematic = true;
                rb.detectCollisions = false;

                CacheRigidbodyDefaults();
                Destroy(rb);
                rb = null;
            }
        }
        else
        {
            CreateRigidbodyIfMissing();
            if (rb == null)
                return;

            rb.detectCollisions = true;
            rb.useGravity = !isWorkbenchLocked;
            rb.isKinematic = isWorkbenchLocked;
            ZeroBodyVelocity();

            CacheSafePose();
        }
    }

    public void SetWorkbenchLocked(bool locked)
    {
        isWorkbenchLocked = locked;

        if (rb == null)
            return;

        if (locked)
        {
            ZeroBodyVelocity();
            rb.useGravity = false;
            rb.isKinematic = true;
        }
        else if (!isGrabbed && !isAttachedToParentAssembly && !isReturning)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            ZeroBodyVelocity();
        }
    }

    public void SnapUprightToWorkbench()
    {
        if (!snapUprightWhenWorkbenchLocked)
            return;

        Vector3 euler = transform.eulerAngles;
        Quaternion uprightRotation = Quaternion.Euler(0f, euler.y, 0f);
        Vector3 uprightPosition = ApplySurfaceHeight(transform.position, uprightRotation);

        transform.SetPositionAndRotation(uprightPosition, uprightRotation);
        Physics.SyncTransforms();

        if (rb != null)
            ZeroBodyVelocity();
    }

    private void UpdateGrabTargetFromInput()
    {
        if (!isGrabbed || surface == null || mainCamera == null)
            return;

        if (!surface.TryGetMousePoint(mainCamera, out Vector3 mousePoint))
            return;

        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > 0.01f)
        {
            liftOffset = Mathf.Clamp(liftOffset + wheel * wheelStep, minLift, maxLift);
        }

        float yawInput = 0f;
        if (Input.GetKey(rotateLeftKey)) yawInput -= 1f;
        if (Input.GetKey(rotateRightKey)) yawInput += 1f;

        float pitchInput = 0f;
        if (Input.GetKey(tiltForwardKey)) pitchInput += 1f;
        if (Input.GetKey(tiltBackwardKey)) pitchInput -= 1f;

        if (Mathf.Abs(yawInput) > 0.01f)
        {
            targetGrabRotation = Quaternion.AngleAxis(
                yawInput * keyboardRotateSpeed * Time.deltaTime,
                Vector3.up) * targetGrabRotation;
        }

        if (Mathf.Abs(pitchInput) > 0.01f)
        {
            Vector3 localRight = targetGrabRotation * Vector3.right;
            targetGrabRotation = Quaternion.AngleAxis(
                pitchInput * keyboardRotateSpeed * Time.deltaTime,
                localRight) * targetGrabRotation;
        }

        Vector3 desiredRootPosition = new Vector3(mousePoint.x, transform.position.y, mousePoint.z) -
                                      (targetGrabRotation * grabPivotLocalPoint);

        targetGrabPosition = ApplySurfaceHeight(desiredRootPosition, targetGrabRotation, liftOffset);
    }

    private void MoveGrabbedBody()
    {
        Vector3 current = rb.position;
        Vector3 desired = Vector3.Lerp(
            current,
            targetGrabPosition,
            1f - Mathf.Exp(-positionLerpSpeed * Time.fixedDeltaTime));

        Vector3 safe = GetSafePosition(current, desired);

        Quaternion desiredRot = Quaternion.RotateTowards(
            rb.rotation,
            targetGrabRotation,
            rotationSpeedDeg * Time.fixedDeltaTime);

        rb.MovePosition(safe);
        rb.MoveRotation(desiredRot);
    }

    private void MoveReturningBody()
    {
        Vector3 phaseTarget;

        switch (returnPhase)
        {
            case ReturnPhase.Lift:
                phaseTarget = returnLiftPoint;
                break;
            case ReturnPhase.Travel:
                phaseTarget = returnTravelPoint;
                break;
            case ReturnPhase.Drop:
                phaseTarget = returnFinalPoint;
                break;
            default:
                phaseTarget = returnFinalPoint;
                break;
        }

        Vector3 nextPos = Vector3.MoveTowards(
            rb.position,
            phaseTarget,
            returnMoveSpeed * Time.fixedDeltaTime);

        Quaternion nextRot = Quaternion.RotateTowards(
            rb.rotation,
            returnTargetRotation,
            returnRotationSpeedDeg * Time.fixedDeltaTime);

        rb.MovePosition(nextPos);
        rb.MoveRotation(nextRot);

        if (Vector3.Distance(rb.position, phaseTarget) <= 0.01f)
        {
            switch (returnPhase)
            {
                case ReturnPhase.Lift:
                    returnPhase = ReturnPhase.Travel;
                    break;
                case ReturnPhase.Travel:
                    returnPhase = ReturnPhase.Drop;
                    break;
                case ReturnPhase.Drop:
                    FinishReturn();
                    break;
            }
        }
    }

    private void FinishReturn()
    {
        isReturning = false;
        returnPhase = ReturnPhase.None;

        rb.isKinematic = false;
        rb.useGravity = true;
        ZeroBodyVelocity();

        transform.SetPositionAndRotation(returnFinalPoint, returnTargetRotation);
        Physics.SyncTransforms();
        CacheSafePose();
    }

    private void MonitorBoundsAndSafePose()
    {
        if (surface == null || rb == null)
            return;

        if (surface.IsInsideWorkingArea(transform.position) && IsNearWorkbenchNow())
        {
            CacheSafePose();
        }

        if (surface.IsOutOfBounds(transform.position))
        {
            outOfBoundsTimer += Time.deltaTime;
            if (outOfBoundsTimer >= outOfBoundsDelay)
            {
                StartReturn();
            }
        }
        else
        {
            outOfBoundsTimer = 0f;
        }
    }

    private void StartReturn()
    {
        if (isReturning || isGrabbed || isAttachedToParentAssembly || rb == null)
            return;

        if (surface == null)
        {
            Debug.LogError($"[PhysicsItem.StartReturn] surface is NULL on {name}!");
            return;
        }

        Debug.Log($"[PhysicsItem.StartReturn] Starting return for {name}, lastSafePosition={lastSafePosition}, currentPosition={transform.position}");

        isReturning = true;
        outOfBoundsTimer = 0f;

        activeGrabPivot = null;
        returnTargetRotation = lastSafeRotation;
        if (!FindReturnDestination(out returnFinalPoint, out returnTargetRotation))
        {
            returnFinalPoint = ApplySurfaceHeight(lastSafePosition, lastSafeRotation);
            activeGrabPivot = null;
            returnTargetRotation = lastSafeRotation;
            Debug.LogWarning($"[PhysicsItem.StartReturn] Could not find return destination, using lastSafePosition: {returnFinalPoint}");
        }
        else
        {
            Debug.Log($"[PhysicsItem.StartReturn] Found return destination: {returnFinalPoint}");
        }

        float cruiseY = Mathf.Max(transform.position.y, returnFinalPoint.y, GetReturnCruiseHeight());
        returnLiftPoint = new Vector3(transform.position.x, cruiseY, transform.position.z);
        returnTravelPoint = new Vector3(returnFinalPoint.x, cruiseY, returnFinalPoint.z);
        returnPhase = ReturnPhase.Lift;

        Debug.Log($"[PhysicsItem.StartReturn] Return path: Lift={returnLiftPoint}, Travel={returnTravelPoint}, Final={returnFinalPoint}");

        ZeroBodyVelocity();
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    private bool FindReturnDestination(out Vector3 destination, out Quaternion rotation)
    {
        rotation = lastSafeRotation;
        destination = ApplySurfaceHeight(lastSafePosition, rotation);

        Vector3 searchCenter = surface != null && surface.IsInsideWorkingArea(lastSafePosition)
            ? lastSafePosition
            : transform.position;

        searchCenter.y = destination.y;

        if (surface != null && surface.IsInsideWorkingArea(destination) && CanOccupyReturnPose(destination, rotation))
            return true;

        for (int ring = 0; ring <= returnSearchRings; ring++)
        {
            float radius = ring * returnSearchStep;
            int samples = ring == 0 ? 1 : Mathf.Max(8, ring * 8);

            for (int i = 0; i < samples; i++)
            {
                float angle = (Mathf.PI * 2f * i) / samples;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                Vector3 candidate = searchCenter + offset;
                candidate = ApplySurfaceHeight(candidate, rotation);

                if (surface != null && !surface.IsInsideWorkingArea(candidate))
                    continue;

                if (CanOccupyReturnPose(candidate, rotation))
                {
                    destination = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    private float GetReturnCruiseHeight()
    {
        float highest = surface != null ? surface.SurfaceY + 0.2f : transform.position.y;

        Collider[] all = FindObjectsOfType<Collider>();
        foreach (Collider c in all)
        {
            if (c == null || !c.enabled || c.isTrigger)
                continue;

            if (c.transform.IsChildOf(transform))
                continue;

            if (!IsLayerInMask(c.gameObject.layer, blockingMask))
                continue;

            if (c.GetComponentInParent<WorkbenchSurface>() != null)
                continue;

            highest = Mathf.Max(highest, c.bounds.max.y);
        }

        float ownTop = GetOwnMaxY();
        highest = Mathf.Max(highest, ownTop);

        return highest + returnClearance;
    }

    private float GetOwnMaxY()
    {
        float maxY = transform.position.y;
        Collider[] own = GetComponentsInChildren<Collider>(true);
        foreach (Collider c in own)
        {
            if (c == null || !c.enabled || c.isTrigger)
                continue;

            maxY = Mathf.Max(maxY, c.bounds.max.y);
        }
        return maxY;
    }

    private bool IsNearWorkbenchNow()
    {
        if (surface == null)
            return true;

        float lowest = GetLowestWorldYForPose(transform.position, transform.rotation);
        return lowest <= surface.SurfaceY + workbenchContactTolerance;
    }

    private void CacheSafePose()
    {
        if (surface == null)
        {
            Debug.LogWarning($"[PhysicsItem.CacheSafePose] surface is NULL for {name}, skipping cache");
            return;
        }

        if (!surface.IsInsideWorkingArea(transform.position))
        {
            Debug.LogWarning($"[PhysicsItem.CacheSafePose] {name} is outside working area at {transform.position}, skipping cache");
            return;
        }

        lastSafePosition = transform.position;
        lastSafeRotation = transform.rotation;
        Debug.Log($"[PhysicsItem.CacheSafePose] Cached safe pose for {name}: pos={lastSafePosition}");
    }

    private Transform ResolveGrabPivot(Transform clickedTransform)
    {
        if (clickedTransform != null)
        {
            PhysicsItem[] items = clickedTransform.GetComponentsInParent<PhysicsItem>(true);
            foreach (PhysicsItem item in items)
            {
                if (item != null)
                    return item.DragPivot;
            }
        }

        return DragPivot;
    }

    private Vector3 ApplySurfaceHeight(Vector3 desiredRootPosition, Quaternion desiredRootRotation, float extraLift = 0f)
    {
        if (surface == null)
            return desiredRootPosition;

        float lowestY = GetLowestWorldYForPose(desiredRootPosition, desiredRootRotation);
        float targetLowestY = surface.SurfaceY + surfacePadding + extraLift;

        desiredRootPosition.y += targetLowestY - lowestY;
        return desiredRootPosition;
    }

    private float GetLowestWorldYForPose(Vector3 rootPosition, Quaternion rootRotation)
    {
        WorkbenchPart[] parts = GetComponentsInChildren<WorkbenchPart>(true);

        bool foundBottomPoint = false;
        float minY = float.MaxValue;

        foreach (WorkbenchPart part in parts)
        {
            if (part == null || part.BottomPoints == null)
                continue;

            foreach (Transform bottomPoint in part.BottomPoints)
            {
                if (bottomPoint == null)
                    continue;

                Vector3 localPoint = transform.InverseTransformPoint(bottomPoint.position);
                Vector3 worldPoint = rootPosition + (rootRotation * localPoint);

                if (worldPoint.y < minY)
                {
                    minY = worldPoint.y;
                    foundBottomPoint = true;
                }
            }
        }

        Collider[] allColliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider c in allColliders)
        {
            if (c == null || !c.enabled || c.isTrigger)
                continue;

            if (c.bounds.min.y < minY)
            {
                minY = c.bounds.min.y;
                foundBottomPoint = true;
            }
        }

        if (!foundBottomPoint)
        {
            Debug.LogWarning($"[PhysicsItem.GetLowestWorldYForPose] No bottom points or colliders found for {name}, using rootPosition.y={rootPosition.y}");
            return rootPosition.y;
        }

        Debug.Log($"[PhysicsItem.GetLowestWorldYForPose] {name}: minY={minY}, foundBottomPoint={foundBottomPoint}, colliderCount={allColliders.Length}");
        return minY;
    }

    private bool CanOccupyReturnPose(Vector3 targetPosition, Quaternion targetRotation)
    {
        Collider[] ownColliders = GetComponentsInChildren<Collider>(true);
        Vector3 oldPos = transform.position;
        Quaternion oldRot = transform.rotation;

        transform.SetPositionAndRotation(targetPosition, targetRotation);
        Physics.SyncTransforms();

        bool blocked = false;

        foreach (Collider own in ownColliders)
        {
            if (own == null || !own.enabled || own.isTrigger)
                continue;

            Collider[] overlaps = Physics.OverlapBox(
                own.bounds.center,
                own.bounds.extents,
                own.transform.rotation,
                blockingMask,
                QueryTriggerInteraction.Ignore);

            foreach (Collider hit in overlaps)
            {
                if (hit == null)
                    continue;

                if (hit.transform.IsChildOf(transform))
                    continue;

                if (hit.GetComponentInParent<WorkbenchSurface>() != null)
                    continue;

                if (Physics.ComputePenetration(
                    own, own.transform.position, own.transform.rotation,
                    hit, hit.transform.position, hit.transform.rotation,
                    out _, out float dist) && dist > 0.001f)
                {
                    blocked = true;
                    break;
                }
            }

            if (blocked)
                break;
        }

        transform.SetPositionAndRotation(oldPos, oldRot);
        Physics.SyncTransforms();

        return !blocked;
    }

    private Vector3 GetSafePosition(Vector3 start, Vector3 target)
    {
        Vector3 delta = target - start;
        float safeStep = Mathf.Max(0.001f, collisionStepDistance);
        int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / safeStep));

        Vector3 lastSafe = start;

        for (int i = 1; i <= steps; i++)
        {
            Vector3 candidate = Vector3.Lerp(start, target, i / (float)steps);
            Vector3 testDelta = candidate - start;

            if (WouldCollideAt(testDelta))
                break;

            lastSafe = candidate;
        }

        return lastSafe;
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

            if (rb != null && hit.attachedRigidbody == rb)
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

    private void CacheRigidbodyDefaults()
    {
        if (rb == null)
            return;

        savedMass = rb.mass;
        savedLinearDamping = rb.linearDamping;
        savedAngularDamping = rb.angularDamping;
        savedInterpolation = rb.interpolation;
        savedCollisionDetection = rb.collisionDetectionMode;
        savedConstraints = rb.constraints;
    }

    private void CreateRigidbodyIfMissing()
    {
        if (rb != null)
            return;

        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        rb.mass = savedMass;
        rb.linearDamping = savedLinearDamping;
        rb.angularDamping = savedAngularDamping;
        rb.interpolation = savedInterpolation;
        rb.collisionDetectionMode = savedCollisionDetection;
        rb.constraints = savedConstraints;
    }

    private void ZeroBodyVelocity()
    {
        if (rb == null)
            return;

        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private bool IsLayerInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
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
