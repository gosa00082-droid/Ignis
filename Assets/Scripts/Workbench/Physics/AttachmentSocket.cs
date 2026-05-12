using UnityEngine;

public class AttachmentSocket : MonoBehaviour
{
    [Header("Идентификатор сокета")]
    [SerializeField] private string socketId;

    [Header("Что сюда можно вставить")]
    [SerializeField] private AttachmentType acceptedType = AttachmentType.None;

    [Header("Точки пути")]
    [SerializeField] private Transform entryPoint;
    [SerializeField] private Transform seatPoint;

    [Header("Настройки соединения")]
    [SerializeField] private float snapDistance = 0.6f;
    [SerializeField] private int clicksToInsert = 4;

    [Header("Плавность движения")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float rotationSpeedDeg = 360f;

    [Header("Подстройка поворота")]
    [SerializeField] private Vector3 entryRotationOffsetEuler;
    [SerializeField] private Vector3 seatRotationOffsetEuler;

    [Header("Состояние")]
    [SerializeField] private AttachableObject attachedObject;
    [SerializeField] private int clicksDone;
    [SerializeField, Range(0f, 1f)] private float targetProgress;

    [Header("Игнорировать мышь у принимающего объекта, пока сокет занят не до конца")]
    [SerializeField] private Collider[] hostCollidersToIgnoreWhileBusy;

    [SerializeField] private bool debugLogs = true;

    private int[] hostOriginalLayers;
    private int ignoreRaycastLayer;
    private bool completionRaised;
    private bool isPullingOut;

    public string SocketId => socketId;
    public Transform EntryPoint => entryPoint;
    public float SnapDistance => snapDistance;
    public bool HasAttachedObject => attachedObject != null;
    public float TargetProgress => targetProgress;
    public bool IsOnlySnappedAtEntry => attachedObject != null && targetProgress <= 0.001f;
    public AttachableObject AttachedObject => attachedObject;

    private void Awake()
    {
        ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");

        if (hostCollidersToIgnoreWhileBusy != null)
        {
            hostOriginalLayers = new int[hostCollidersToIgnoreWhileBusy.Length];

            for (int i = 0; i < hostCollidersToIgnoreWhileBusy.Length; i++)
            {
                if (hostCollidersToIgnoreWhileBusy[i] != null)
                    hostOriginalLayers[i] = hostCollidersToIgnoreWhileBusy[i].gameObject.layer;
            }
        }
    }

    private void Update()
    {
        if (attachedObject == null || entryPoint == null || seatPoint == null)
            return;

        Quaternion entryRotation = entryPoint.rotation * Quaternion.Euler(entryRotationOffsetEuler);
        Quaternion seatRotation = seatPoint.rotation * Quaternion.Euler(seatRotationOffsetEuler);

        Vector3 desiredPlugPosition = Vector3.Lerp(entryPoint.position, seatPoint.position, targetProgress);
        Quaternion desiredPlugRotation = Quaternion.Slerp(entryRotation, seatRotation, targetProgress);

        attachedObject.MovePlugTowardsPose(desiredPlugPosition, desiredPlugRotation, moveSpeed, rotationSpeedDeg);

        if (targetProgress >= 1f &&
            attachedObject.IsCloseToPlugPose(desiredPlugPosition, desiredPlugRotation))
        {
            attachedObject.MarkFullyInserted();

            if (!completionRaised)
            {
                completionRaised = true;
                AssemblyEvents.RaiseAttachmentCompleted(this, attachedObject);
            }
        }

        if (isPullingOut &&
            targetProgress <= 0f &&
            attachedObject.IsCloseToPlugPose(entryPoint.position, entryRotation))
        {
            ReleaseAttachedObject();
        }

        UpdateHostRaycastBlocking();
    }

    private void OnDisable()
    {
        if (hostCollidersToIgnoreWhileBusy == null || hostOriginalLayers == null)
            return;

        for (int i = 0; i < hostCollidersToIgnoreWhileBusy.Length; i++)
        {
            Collider col = hostCollidersToIgnoreWhileBusy[i];
            if (col == null)
                continue;

            col.gameObject.layer = hostOriginalLayers[i];
        }
    }

    public bool CanAccept(AttachableObject obj)
    {
        if (obj == null)
            return false;

        if (attachedObject != null)
            return false;

        if (entryPoint == null || seatPoint == null)
            return false;

        if (obj.AttachableType != acceptedType)
            return false;

        AssemblyRecipeRunner runner = GetComponentInParent<AssemblyRecipeRunner>();
        if (runner != null && !runner.CanAcceptStep(this, obj))
            return false;

        return true;
    }

    public bool TryAttach(AttachableObject obj)
    {
        if (debugLogs)
        {
            Debug.Log(
                $"[AttachmentSocket.TryAttach] socket={name}, obj={(obj != null ? obj.name : "null")}, " +
                $"canAccept={(obj != null && CanAccept(obj))}, currentAttached={(attachedObject != null ? attachedObject.name : "null")}");
        }

        if (!CanAccept(obj))
            return false;

        AssemblyRoot hostRootForCheck = AssemblyRoot.FindActiveRoot(transform);
        AssemblyRoot objectRootForCheck = AssemblyRoot.FindActiveRoot(obj.transform);

        if (!IsRootNearWorkbench(hostRootForCheck != null ? hostRootForCheck.transform : null))
        {
            if (debugLogs)
                Debug.Log($"[AttachmentSocket.TryAttach] FAIL host is not near workbench: {name}");
            return false;
        }

        if (!IsRootNearWorkbench(objectRootForCheck != null ? objectRootForCheck.transform : null))
        {
            if (debugLogs)
                Debug.Log($"[AttachmentSocket.TryAttach] FAIL object is not near workbench: {obj.name}");
            return false;
        }

        AssemblyRoot hostRoot = AssemblyRoot.FindActiveRoot(transform);
        if (hostRoot != null && hostRoot.PhysicsItem != null)
        {
            hostRoot.PhysicsItem.SnapUprightToWorkbench();
            Physics.SyncTransforms();
        }

        AssemblyRoot objectRoot = AssemblyRoot.FindActiveRoot(obj.transform);
        if (objectRoot != null && objectRoot.PhysicsItem != null)
        {
            objectRoot.PhysicsItem.SnapUprightToWorkbench();
            Physics.SyncTransforms();
        }

        if (!obj.TryGetEntryRootPose(this, out Vector3 desiredRootPosition, out Quaternion desiredRootRotation))
        {
            if (debugLogs)
                Debug.Log($"[AttachmentSocket.TryAttach] FAIL TryGetEntryRootPose on {obj.name}");
            return false;
        }

        if (!obj.CanOccupyPose(desiredRootPosition, desiredRootRotation, this))
        {
            if (debugLogs)
                Debug.Log($"[AttachmentSocket.TryAttach] FAIL pose blocked for {obj.name}");
            return false;
        }

        obj.transform.SetPositionAndRotation(desiredRootPosition, desiredRootRotation);
        Physics.SyncTransforms();

        attachedObject = obj;
        clicksDone = 0;
        targetProgress = 0f;
        completionRaised = false;
        isPullingOut = false;

        attachedObject.AttachToSocket(this);
        attachedObject.SetParent(transform);

        MergeAssemblyRoots(obj);
        RefreshAssemblyLock();
        UpdateHostRaycastBlocking();
        AssemblyEvents.RaiseAssemblyStateChanged();
        return true;
    }

    public void AdvanceInsertion()
    {
        if (debugLogs)
        {
            Debug.Log(
                $"[AttachmentSocket.AdvanceInsertion] socket={name}, attached={(attachedObject != null ? attachedObject.name : "null")}, " +
                $"clicksDone={clicksDone}, clicksToInsert={clicksToInsert}, progressBefore={targetProgress}");
        }

        if (attachedObject == null)
            return;

        if (clicksDone >= clicksToInsert)
            return;

        clicksDone++;
        isPullingOut = false;

        int safeClicks = Mathf.Max(1, clicksToInsert);
        targetProgress = clicksDone / (float)safeClicks;
        targetProgress = Mathf.Clamp01(targetProgress);
        RefreshAssemblyLock();
    }

    public void PullOutStep(float normalizedDelta)
    {
        if (debugLogs)
        {
            Debug.Log(
                $"[AttachmentSocket.PullOutStep] socket={name}, attached={(attachedObject != null ? attachedObject.name : "null")}, " +
                $"progressBefore={targetProgress}, delta={normalizedDelta}");
        }

        if (attachedObject == null)
            return;

        isPullingOut = true;
        targetProgress -= Mathf.Abs(normalizedDelta);
        targetProgress = Mathf.Clamp01(targetProgress);

        int safeClicks = Mathf.Max(1, clicksToInsert);
        clicksDone = Mathf.RoundToInt(targetProgress * safeClicks);
        RefreshAssemblyLock();
    }

    public void DetachImmediatelyIfOnlySnapped()
    {
        if (debugLogs)
        {
            Debug.Log(
                $"[AttachmentSocket.DetachImmediatelyIfOnlySnapped] socket={name}, attached={(attachedObject != null ? attachedObject.name : "null")}, " +
                $"progress={targetProgress}");
        }

        if (attachedObject == null)
            return;

        if (targetProgress > 0.001f)
            return;

        ReleaseAttachedObject();
    }

    private void ReleaseAttachedObject()
    {
        if (debugLogs)
        {
            Debug.Log(
                $"[AttachmentSocket.ReleaseAttachedObject] socket={name}, releasing={(attachedObject != null ? attachedObject.name : "null")}");
        }

        if (attachedObject == null)
            return;

        AttachableObject detached = attachedObject;
        attachedObject = null;

        clicksDone = 0;
        targetProgress = 0f;
        completionRaised = false;
        isPullingOut = false;

        detached.ClearAttachment();
        detached.transform.SetParent(null, true);

        EnsureDetachedAssemblyRoot(detached.transform);
        AssemblyEvents.RaiseAssemblyStateChanged();
        UpdateHostRaycastBlocking();
        RefreshAssemblyLock();
    }

    private void RefreshAssemblyLock()
    {
        AssemblyRoot root = AssemblyRoot.FindActiveRoot(transform);
        root?.RefreshWorkbenchLock();
    }

    private void UpdateHostRaycastBlocking()
    {
        if (hostCollidersToIgnoreWhileBusy == null || hostOriginalLayers == null)
            return;

        bool shouldIgnoreRaycast = attachedObject != null && targetProgress < 1f;

        for (int i = 0; i < hostCollidersToIgnoreWhileBusy.Length; i++)
        {
            Collider col = hostCollidersToIgnoreWhileBusy[i];
            if (col == null)
                continue;

            col.gameObject.layer = shouldIgnoreRaycast
                ? ignoreRaycastLayer
                : hostOriginalLayers[i];
        }
    }

    private bool IsRootNearWorkbench(Transform root)
    {
        WorkbenchSurface surface = WorkbenchSurface.Instance;
        if (surface == null || root == null)
            return true;

        float lowestY = float.MaxValue;
        bool found = false;

        // 1) Проверяем BottomPoints
        WorkbenchPart[] parts = root.GetComponentsInChildren<WorkbenchPart>(true);
        foreach (WorkbenchPart part in parts)
        {
            if (part == null || part.BottomPoints == null)
                continue;

            foreach (Transform p in part.BottomPoints)
            {
                if (p == null)
                    continue;

                lowestY = Mathf.Min(lowestY, p.position.y);
                found = true;
            }
        }

        // 2) Проверяем реальные коллайдеры тоже
        Collider[] cols = root.GetComponentsInChildren<Collider>(true);
        foreach (Collider c in cols)
        {
            if (c == null || !c.enabled || c.isTrigger)
                continue;

            lowestY = Mathf.Min(lowestY, c.bounds.min.y);
            found = true;
        }

        if (!found)
            return true;

        // Было слишком строго. После падения объект может лечь чуть выше.
        return lowestY <= surface.SurfaceY + 0.08f;
    }

    private void EnsureDetachedAssemblyRoot(Transform detachedRootTransform)
    {
        if (detachedRootTransform == null)
            return;

        AssemblyRoot detachedRoot = detachedRootTransform.GetComponent<AssemblyRoot>();
        detachedRoot?.ReleaseAsStandalone();
    }

    private void MergeAssemblyRoots(AttachableObject childObj)
    {
        AssemblyRoot parentRoot = AssemblyRoot.FindActiveRoot(transform);
        AssemblyRoot childRoot = childObj.GetComponent<AssemblyRoot>();

        if (parentRoot != null && childRoot != null && childRoot != parentRoot)
        {
            parentRoot.AbsorbChildRoot(childRoot);
        }
    }

    private void OnDrawGizmos()
    {
        if (entryPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(entryPoint.position, 0.05f);
        }

        if (seatPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(seatPoint.position, 0.05f);
        }

        if (entryPoint != null && seatPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(entryPoint.position, seatPoint.position);
        }
    }
}
