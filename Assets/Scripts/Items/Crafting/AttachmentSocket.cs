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

    private bool completionRaised;
    private bool isPullingOut;

    public AttachableObject AttachedObject => attachedObject;
    public string SocketId => socketId;
    public Transform EntryPoint => entryPoint;
    public float SnapDistance => snapDistance;
    public bool HasAttachedObject => attachedObject != null;
    public float TargetProgress => targetProgress;
    public bool IsOnlySnappedAtEntry => attachedObject != null && targetProgress <= 0.001f;

    private void Update()
    {
        if (attachedObject == null || entryPoint == null || seatPoint == null)
            return;

        Quaternion entryRotation =
            entryPoint.rotation * Quaternion.Euler(entryRotationOffsetEuler);

        Quaternion seatRotation =
            seatPoint.rotation * Quaternion.Euler(seatRotationOffsetEuler);

        Vector3 desiredPlugPosition = Vector3.Lerp(
            entryPoint.position,
            seatPoint.position,
            targetProgress);

        Quaternion desiredPlugRotation = Quaternion.Slerp(
            entryRotation,
            seatRotation,
            targetProgress);

        attachedObject.MovePlugTowardsPose(
            desiredPlugPosition,
            desiredPlugRotation,
            moveSpeed,
            rotationSpeedDeg);

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
        if (!CanAccept(obj))
            return false;

        attachedObject = obj;
        clicksDone = 0;
        targetProgress = 0f;
        completionRaised = false;
        isPullingOut = false;

        attachedObject.AttachToSocket(this);
        attachedObject.SetParent(transform);

        MergeAssemblyRoots(obj);

        return true;
    }

    public void AdvanceInsertion()
    {
        if (attachedObject == null)
            return;

        if (clicksDone >= clicksToInsert)
            return;

        clicksDone++;
        isPullingOut = false;

        int safeClicks = Mathf.Max(1, clicksToInsert);
        targetProgress = clicksDone / (float)safeClicks;
        targetProgress = Mathf.Clamp01(targetProgress);
    }

    public void PullOutStep(float normalizedDelta)
    {
        if (attachedObject == null)
            return;

        isPullingOut = true;
        targetProgress -= Mathf.Abs(normalizedDelta);
        targetProgress = Mathf.Clamp01(targetProgress);

        int safeClicks = Mathf.Max(1, clicksToInsert);
        clicksDone = Mathf.RoundToInt(targetProgress * safeClicks);
    }

    public void DetachImmediatelyIfOnlySnapped()
    {
        if (attachedObject == null)
            return;

        if (targetProgress > 0.001f)
            return;

        ReleaseAttachedObject();
    }

    private void ReleaseAttachedObject()
    {
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

        EnsureAssemblyRootOnDetached(detached.transform);

        AssemblyEvents.RaiseAssemblyStateChanged();
    }

    private void EnsureAssemblyRootOnDetached(Transform detachedRootTransform)
    {
        if (detachedRootTransform == null)
            return;

        AssemblyRoot existingRoot = detachedRootTransform.GetComponent<AssemblyRoot>();
        if (existingRoot == null)
        {
            existingRoot = detachedRootTransform.gameObject.AddComponent<AssemblyRoot>();
        }

        AssemblyRoot parentRoot = GetComponentInParent<AssemblyRoot>();
        if (parentRoot != null)
        {
            existingRoot.CopySettingsFrom(parentRoot);
        }
    }

    private void MergeAssemblyRoots(AttachableObject childObj)
    {
        AssemblyRoot parentRoot = GetComponentInParent<AssemblyRoot>();
        AssemblyRoot childRoot = childObj.GetComponentInParent<AssemblyRoot>();

        if (childRoot != null && parentRoot != null && childRoot != parentRoot)
        {
            Destroy(childRoot);
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