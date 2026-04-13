using UnityEngine;

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

    [Header("Точка вставки")]
    [SerializeField] private Transform plugPoint;

    [Header("Состояние")]
    [SerializeField] private bool isAttached;
    [SerializeField] private bool isFullyInserted;

    private AttachmentSocket currentSocket;

    public AttachmentType AttachableType => attachableType;
    public Transform PlugPoint => plugPoint;
    public bool IsAttached => isAttached;
    public bool IsFullyInserted => isFullyInserted;
    public AttachmentSocket CurrentSocket => currentSocket;

    private void OnMouseDown()
    {
        // ЛКМ по уже прикрепленному объекту = "вбиваем" дальше
        if (isAttached && !isFullyInserted && currentSocket != null)
        {
            currentSocket.AdvanceInsertion();
        }
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

        // Локальная позиция PlugPoint относительно корня объекта
        Vector3 plugLocalPosition = transform.InverseTransformPoint(plugPoint.position);

        // Локальный поворот PlugPoint относительно корня объекта
        Quaternion plugLocalRotation = Quaternion.Inverse(transform.rotation) * plugPoint.rotation;

        // Как должен стоять корневой объект, чтобы PlugPoint совпал с целью
        Quaternion desiredRootRotation = targetPlugRotation * Quaternion.Inverse(plugLocalRotation);
        Vector3 desiredRootPosition = targetPlugPosition - desiredRootRotation * plugLocalPosition;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            desiredRootRotation,
            rotationSpeedDeg * Time.deltaTime);

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