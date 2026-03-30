using UnityEngine;

public class FurnaceDropZone3D : MonoBehaviour
{
    [SerializeField] private bool debugLogs = true;

    private void Log(string message)
    {
        if (debugLogs)
            Debug.Log($"[FurnaceDropZone3D:{name}] {message}", this);
    }

    public bool CanAccept(WorldDraggableItem item)
    {
        bool result = item != null;
        Log($"CanAccept({(item != null ? item.name : "NULL")}) = {result}");
        return result;
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