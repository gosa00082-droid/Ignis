using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FurnaceDropZone3D : MonoBehaviour
{
    [SerializeField] private bool debugLogs = true;

    private Collider zoneCollider;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider>();
    }

    private void Log(string message)
    {
        if (debugLogs)
            Debug.Log($"[FurnaceDropZone3D:{name}] {message}", this);
    }

    public bool CanAccept(WorldDraggableItem item)
    {
        bool result = item != null && zoneCollider != null && zoneCollider.enabled;
        Log($"CanAccept: {(item != null ? item.name : "NULL")} => {result}");
        return result;
    }

    public void SetZoneEnabled(bool enabledState)
    {
        if (zoneCollider != null)
        {
            zoneCollider.enabled = enabledState;
            Log($"Zone collider enabled = {enabledState}");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        WorldDraggableItem item = other.GetComponent<WorldDraggableItem>();
        if (item != null)
        {
            Log($"OnTriggerEnter: {item.name}");
            item.SetCurrentZone(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        WorldDraggableItem item = other.GetComponent<WorldDraggableItem>();
        if (item != null)
        {
            Log($"OnTriggerExit: {item.name}");
            item.ClearCurrentZone(this);
        }
    }
}