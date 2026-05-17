using UnityEngine;
using System.Collections.Generic;

public enum AttachmentType
{
    None,
    Guard,
    Handle,
    Blade,
    Pommel
}

public class AttachableObject : MonoBehaviour
{
    [Header("Что это за объект")]
    [SerializeField] private AttachmentType attachableType = AttachmentType.None;
    [SerializeField] private bool alignRotationToSocket = true;

    [Header("Точка вставки")]
    [SerializeField] private Transform plugPoint;

    [Header("Состояние")]
    [SerializeField] private bool isAttached;
    [SerializeField] private bool isFullyInserted;

    [Header("Первый толчок")]
    [SerializeField] private float firstInsertDoubleClickWindow = 0.3f;

    [SerializeField] private Vector3 entryRotationOffsetEuler;
    [SerializeField] private Collider[] occupancyColliders;


    [SerializeField] private float doubleClickThreshold = 0.25f;
    private float lastInsertClickTime = -10f;

    private AttachmentSocket currentSocket;
    private float lastEntryClickTime = -999f;

    public Transform PlugPoint => plugPoint != null ? plugPoint : transform;
    public AttachmentType AttachableType => attachableType;
    public bool IsAttached => isAttached;
    public bool IsFullyInserted => isFullyInserted;
    public AttachmentSocket CurrentSocket => currentSocket;

    private void OnMouseDown()
    {
        HandleInsertionClick();
    }

    public bool TryGetEntryRootPose(AttachmentSocket socket, out Vector3 rootPosition, out Quaternion rootRotation)
    {
        rootPosition = transform.position;
        rootRotation = transform.rotation;

        if (socket == null || PlugPoint == null || socket.EntryPoint == null)
            return false;

        Vector3 desiredPlugPosition = socket.EntryPoint.position;

        if (alignRotationToSocket)
        {
            Quaternion desiredPlugRotation =
                socket.EntryPoint.rotation * Quaternion.Euler(entryRotationOffsetEuler);

            rootRotation = desiredPlugRotation * Quaternion.Inverse(PlugPoint.localRotation);
        }
        else
        {
            // Не форсим поворот — оставляем текущий поворот root
            rootRotation = transform.rotation;
        }

        rootPosition = desiredPlugPosition - (rootRotation * PlugPoint.localPosition);
        return true;
    }

    public bool CanOccupyPose(Vector3 targetPosition, Quaternion targetRotation, AttachmentSocket socket)
    {
        if (occupancyColliders == null || occupancyColliders.Length == 0)
            occupancyColliders = GetComponentsInChildren<Collider>(true);

        AssemblyRoot hostRoot = AssemblyRoot.FindActiveRoot(socket.transform);

        HashSet<Collider> ignore = new HashSet<Collider>();

        foreach (Collider c in occupancyColliders)
        {
            if (c != null)
                ignore.Add(c);
        }

        if (hostRoot != null)
        {
            Collider[] hostCols = hostRoot.GetComponentsInChildren<Collider>(true);
            foreach (Collider c in hostCols)
            {
                if (c != null)
                    ignore.Add(c);
            }
        }

        Vector3 oldPos = transform.position;
        Quaternion oldRot = transform.rotation;

        transform.SetPositionAndRotation(targetPosition, targetRotation);
        Physics.SyncTransforms();

        bool blocked = false;

        foreach (Collider own in occupancyColliders)
        {
            if (own == null || !own.enabled || own.isTrigger)
                continue;

            Bounds b = own.bounds;
            Collider[] overlaps = Physics.OverlapBox(
                b.center,
                b.extents,
                Quaternion.identity,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            foreach (Collider other in overlaps)
            {
                if (other == null || ignore.Contains(other) || other.isTrigger)
                    continue;

                // Игнорируем сам верстак/стол, иначе snap на поверхности будет считаться blocked.
                if (other.GetComponentInParent<WorkbenchSurface>() != null)
                    continue;

                if (Physics.ComputePenetration(
                    own, own.transform.position, own.transform.rotation,
                    other, other.transform.position, other.transform.rotation,
                    out Vector3 dir, out float dist))
                {
                    if (dist > 0.001f)
                    {
                        blocked = true;
                        break;
                    }
                }
            }

            if (blocked)
                break;
        }

        transform.SetPositionAndRotation(oldPos, oldRot);
        Physics.SyncTransforms();

        return !blocked;
    }

    public void HandleInsertionClick()
    {
        if (!IsAttached || CurrentSocket == null)
            return;

        if (CurrentSocket.TargetProgress >= 1f)
            return;

        // Первый толчок только двойным кликом
        if (CurrentSocket.TargetProgress <= 0.001f)
        {
            float now = Time.time;

            if (now - lastInsertClickTime <= doubleClickThreshold)
            {
                CurrentSocket.AdvanceInsertion();
                lastInsertClickTime = -10f;
            }
            else
            {
                lastInsertClickTime = now;
            }

            return;
        }

        // Дальше уже обычные клики
        CurrentSocket.AdvanceInsertion();
    }

    public void TrySnapToNearestSocket()
    {
        if (isAttached)
            return;

        if (plugPoint == null)
        {
            Debug.LogError($"[{name}] Не назначен PlugPoint.");
            return;
        }

        AttachmentSocket[] allSockets = FindObjectsOfType<AttachmentSocket>();

        AttachmentSocket bestSocket = null;
        float bestDistance = float.MaxValue;

        foreach (AttachmentSocket socket in allSockets)
        {
            if (!socket.CanAccept(this))
                continue;

            float distance = Vector3.Distance(plugPoint.position, socket.EntryPoint.position);

            if (distance <= socket.SnapDistance && distance < bestDistance)
            {
                bestDistance = distance;
                bestSocket = socket;
            }
        }

        if (bestSocket != null)
        {
            bestSocket.TryAttach(this);
        }
    }

    public void AttachToSocket(AttachmentSocket socket)
    {
        currentSocket = socket;
        isAttached = true;
        isFullyInserted = false;
    }

    public void MarkFullyInserted()
    {
        isFullyInserted = true;
    }

    public void ClearAttachment()
    {
        currentSocket = null;
        isAttached = false;
        isFullyInserted = false;
        transform.SetParent(null, true);
    }

    public void SetParent(Transform newParent)
    {
        transform.SetParent(newParent, true);
    }

    public void MovePlugTowardsPose(
    Vector3 targetPlugPosition,
    Quaternion targetPlugRotation,
    float moveSpeed,
    float rotationSpeedDeg)
    {
        if (plugPoint == null)
            return;

        Vector3 plugLocalPosition = transform.InverseTransformPoint(plugPoint.position);
        Quaternion plugLocalRotation = Quaternion.Inverse(transform.rotation) * plugPoint.rotation;

        Quaternion desiredRootRotation;

        if (alignRotationToSocket)
        {
            desiredRootRotation = targetPlugRotation * Quaternion.Inverse(plugLocalRotation);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                desiredRootRotation,
                rotationSpeedDeg * Time.deltaTime);
        }
        else
        {
            desiredRootRotation = transform.rotation;
        }

        Vector3 desiredRootPosition = targetPlugPosition - desiredRootRotation * plugLocalPosition;

        transform.position = Vector3.MoveTowards(
            transform.position,
            desiredRootPosition,
            moveSpeed * Time.deltaTime);
    }

    public bool IsCloseToPlugPose(
        Vector3 targetPlugPosition,
        Quaternion targetPlugRotation,
        float positionTolerance = 0.01f,
        float angleTolerance = 1f)
    {
        if (plugPoint == null)
            return false;

        float posDelta = Vector3.Distance(plugPoint.position, targetPlugPosition);
        float rotDelta = Quaternion.Angle(plugPoint.rotation, targetPlugRotation);

        return posDelta <= positionTolerance && rotDelta <= angleTolerance;
    }
}