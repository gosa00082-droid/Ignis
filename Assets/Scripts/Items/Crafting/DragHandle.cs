using UnityEngine;

public class DragHandle : MonoBehaviour
{
    private AssemblyRoot activeRoot;
    private AttachableObject ownAttachable;

    private void Awake()
    {
        ownAttachable = GetComponent<AttachableObject>();

        if (ownAttachable == null)
            ownAttachable = GetComponentInParent<AttachableObject>();
    }

    private void OnMouseDown()
    {
        // Если деталь уже прикреплена, но еще не добита до конца,
        // то ЛКМ должна идти на "вбивание", а не на drag
        if (ownAttachable != null && ownAttachable.IsAttached && !ownAttachable.IsFullyInserted)
            return;

        activeRoot = GetComponentInParent<AssemblyRoot>();

        if (activeRoot != null)
        {
            activeRoot.BeginDrag();
        }
    }

    private void OnMouseDrag()
    {
        if (activeRoot != null)
        {
            activeRoot.DragToMouse();
        }
    }

    private void OnMouseUp()
    {
        if (activeRoot != null)
        {
            activeRoot.EndDrag();
        }

        // Если это свободная деталь, после отпускания пытаемся ее защелкнуть в ближайший сокет
        if (ownAttachable != null && !ownAttachable.IsAttached)
        {
            ownAttachable.TrySnapToNearestSocket();
        }

        activeRoot = null;
    }
}