using UnityEngine;

[RequireComponent(typeof(PhysicsItem))]
public class AssemblyRoot : MonoBehaviour
{
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private bool isActiveRoot = true;

    private PhysicsItem physicsItem;

    public bool IsActiveRoot => isActiveRoot;
    public PhysicsItem PhysicsItem => physicsItem;

    private void Awake()
    {
        physicsItem = GetComponent<PhysicsItem>();
    }

    public static AssemblyRoot FindActiveRoot(Transform from)
    {
        if (from == null)
            return null;

        AssemblyRoot[] roots = from.GetComponentsInParent<AssemblyRoot>(true);
        foreach (AssemblyRoot root in roots)
        {
            if (root != null && root.isActiveRoot)
                return root;
        }

        return null;
    }

    public void RefreshWorkbenchLock()
    {
        if (physicsItem == null)
            physicsItem = GetComponent<PhysicsItem>();

        bool shouldLock = HasIncompleteSocketInAssembly();
        physicsItem?.SetWorkbenchLocked(shouldLock);

        if (debugLogs)
        {
            Debug.Log($"[AssemblyRoot.RefreshWorkbenchLock] root={name}, shouldLock={shouldLock}");
        }
    }

    private bool HasIncompleteSocketInAssembly()
    {
        AttachmentSocket[] sockets = GetComponentsInChildren<AttachmentSocket>(true);

        foreach (AttachmentSocket socket in sockets)
        {
            if (socket == null)
                continue;

            if (socket.HasAttachedObject && socket.TargetProgress < 1f)
                return true;
        }

        return false;
    }

    public void BeginDrag(Transform clickedTransform)
    {
        if (debugLogs)
        {
            Debug.Log(
                $"[AssemblyRoot.BeginDrag] root={name}, clicked={clickedTransform?.name}, " +
                $"isActiveRoot={isActiveRoot}, hasPhysicsItem={physicsItem != null}");
        }

        if (!isActiveRoot || physicsItem == null)
        {
            if (debugLogs)
                Debug.Log($"[AssemblyRoot.BeginDrag] STOP: root inactive or no PhysicsItem on {name}");
            return;
        }

        bool canStart = CanStartDragFrom(clickedTransform);

        if (debugLogs)
        {
            Debug.Log(
                $"[AssemblyRoot.BeginDrag] root={name}, clicked={clickedTransform?.name}, canStartDrag={canStart}");
        }

        if (!canStart)
            return;

        if (debugLogs)
            Debug.Log($"[AssemblyRoot.BeginDrag] START DRAG on root={name}");

        physicsItem.BeginGrab(clickedTransform);
    }

    public void DragToMouse()
    {
        if (!isActiveRoot || physicsItem == null)
            return;

        physicsItem.RefreshGrabTargetFromInput();
    }

    public void EndDrag()
    {
        if (!isActiveRoot || physicsItem == null)
            return;

        physicsItem.EndGrab();
    }

    public void AbsorbChildRoot(AssemblyRoot childRoot)
    {
        if (childRoot == null || childRoot == this)
            return;

        childRoot.isActiveRoot = false;

        if (childRoot.physicsItem == null)
            childRoot.physicsItem = childRoot.GetComponent<PhysicsItem>();

        childRoot.physicsItem?.SetAttachedToParentAssembly(true);
    }

    public void ReleaseAsStandalone()
    {
        isActiveRoot = true;

        if (physicsItem == null)
            physicsItem = GetComponent<PhysicsItem>();

        physicsItem?.SetAttachedToParentAssembly(false);
    }

    private bool CanStartDragFrom(Transform clickedTransform)
    {
        // Если в сборке есть незавершенная вставка — вся сборка закреплена.
        if (HasIncompleteSocketInAssembly())
        {
            if (debugLogs)
                Debug.Log($"[AssemblyRoot.CanStartDragFrom] BLOCK DRAG because assembly has incomplete socket on {name}");
            return false;
        }

        if (clickedTransform == null)
        {
            if (debugLogs)
                Debug.Log($"[AssemblyRoot.CanStartDragFrom] clickedTransform is null on root={name}");
            return true;
        }

        AttachableObject attachable = clickedTransform.GetComponentInParent<AttachableObject>();

        if (attachable == null)
        {
            if (debugLogs)
                Debug.Log($"[AssemblyRoot.CanStartDragFrom] no AttachableObject found from {clickedTransform.name}");
            return true;
        }

        if (debugLogs)
        {
            string socketName = attachable.CurrentSocket != null ? attachable.CurrentSocket.name : "null";
            float progress = attachable.CurrentSocket != null ? attachable.CurrentSocket.TargetProgress : -1f;

            Debug.Log(
                $"[AssemblyRoot.CanStartDragFrom] clicked={clickedTransform.name}, attachable={attachable.name}, " +
                $"isAttached={attachable.IsAttached}, socket={socketName}, progress={progress}");
        }

        if (!attachable.IsAttached || attachable.CurrentSocket == null)
            return true;

        if (attachable.CurrentSocket.TargetProgress < 1f)
        {
            if (debugLogs)
                Debug.Log($"[AssemblyRoot.CanStartDragFrom] BLOCK DRAG because progress < 1 on {attachable.name}");
            return false;
        }

        return true;
    }
}