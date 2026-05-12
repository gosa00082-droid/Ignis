using UnityEngine;

public class DragHandle : MonoBehaviour
{
    [Header("Скорость вытягивания из сокета")]
    [SerializeField] private float pullOutSpeed = 0.9f;

    [Header("Дебаг")]
    [SerializeField] private bool debugLogs = true;

    private AssemblyRoot activeRoot;
    private AttachableObject activeAttachable;
    private bool isPullDragging;

    private void Update()
    {
        HandleRightMousePull();
    }

    private void OnMouseDown()
    {
        Transform clickedTransform = GetRealMouseHitTransform();
        if (clickedTransform == null)
            clickedTransform = transform;

        AttachableObject clickedAttachable = clickedTransform.GetComponentInParent<AttachableObject>();
        activeAttachable = clickedAttachable;

        if (debugLogs)
        {
            Debug.Log(
                $"[DragHandle.OnMouseDown] scriptObject={name}, realHit={clickedTransform.name}, " +
                $"attachable={(clickedAttachable != null ? clickedAttachable.name : "null")}, " +
                $"isAttached={(clickedAttachable != null && clickedAttachable.IsAttached)}, " +
                $"socket={(clickedAttachable != null && clickedAttachable.CurrentSocket != null ? clickedAttachable.CurrentSocket.name : "null")}, " +
                $"progress={(clickedAttachable != null && clickedAttachable.CurrentSocket != null ? clickedAttachable.CurrentSocket.TargetProgress : -1f)}");
        }

        // Если кликнули по детали, которая уже сидит в сокете, но не вставлена до конца,
        // ЛКМ должна не таскать, а выполнять шаг вставления.
        if (clickedAttachable != null &&
            clickedAttachable.IsAttached &&
            clickedAttachable.CurrentSocket != null &&
            clickedAttachable.CurrentSocket.TargetProgress < 1f)
        {
            if (debugLogs)
                Debug.Log($"[DragHandle.OnMouseDown] HandleInsertionClick on {clickedAttachable.name}");

            clickedAttachable.HandleInsertionClick();
            return;
        }

        activeRoot = AssemblyRoot.FindActiveRoot(clickedTransform);

        if (debugLogs)
        {
            Debug.Log(
                $"[DragHandle.OnMouseDown] activeRoot={(activeRoot != null ? activeRoot.name : "null")} " +
                $"from realHit={clickedTransform.name}");
        }

        activeRoot?.BeginDrag(clickedTransform);
    }

    private void OnMouseDrag()
    {
        activeRoot?.DragToMouse();
    }

    private void OnMouseUp()
    {
        activeRoot?.EndDrag();

        if (activeAttachable != null && !activeAttachable.IsAttached)
        {
            if (debugLogs)
                Debug.Log($"[DragHandle.OnMouseUp] TrySnapToNearestSocket on {activeAttachable.name}");

            activeAttachable.TrySnapToNearestSocket();
        }

        activeRoot = null;
        activeAttachable = null;
    }

    private void OnMouseOver()
    {
        if (!Input.GetMouseButtonDown(1))
            return;

        Transform hoveredTransform = GetRealMouseHitTransform();
        if (hoveredTransform == null)
            hoveredTransform = transform;

        AttachableObject hoveredAttachable = hoveredTransform.GetComponentInParent<AttachableObject>();

        if (debugLogs)
        {
            Debug.Log(
                $"[DragHandle.OnMouseOver RMB] scriptObject={name}, realHit={hoveredTransform.name}, " +
                $"attachable={(hoveredAttachable != null ? hoveredAttachable.name : "null")}, " +
                $"isAttached={(hoveredAttachable != null && hoveredAttachable.IsAttached)}, " +
                $"progress={(hoveredAttachable != null && hoveredAttachable.CurrentSocket != null ? hoveredAttachable.CurrentSocket.TargetProgress : -1f)}");
        }

        if (hoveredAttachable != null &&
            hoveredAttachable.IsAttached &&
            hoveredAttachable.CurrentSocket != null)
        {
            activeAttachable = hoveredAttachable;
            isPullDragging = true;
        }
    }

    private void HandleRightMousePull()
    {
        if (!isPullDragging)
            return;

        if (Input.GetMouseButton(1))
        {
            if (activeAttachable != null &&
                activeAttachable.IsAttached &&
                activeAttachable.CurrentSocket != null)
            {
                if (debugLogs)
                {
                    Debug.Log(
                        $"[DragHandle.HandleRightMousePull] attachable={activeAttachable.name}, " +
                        $"progress={activeAttachable.CurrentSocket.TargetProgress}");
                }

                if (activeAttachable.CurrentSocket.TargetProgress <= 0.001f)
                {
                    if (debugLogs)
                        Debug.Log($"[DragHandle.HandleRightMousePull] DetachImmediatelyIfOnlySnapped on {activeAttachable.name}");

                    activeAttachable.CurrentSocket.DetachImmediatelyIfOnlySnapped();

                    Transform hitAfterDetach = GetRealMouseHitTransform();
                    if (hitAfterDetach == null)
                        hitAfterDetach = activeAttachable.transform;

                    if (activeRoot == null)
                    {
                        activeRoot = AssemblyRoot.FindActiveRoot(hitAfterDetach);

                        if (debugLogs)
                        {
                            Debug.Log(
                                $"[DragHandle.HandleRightMousePull] activeRoot after detach = " +
                                $"{(activeRoot != null ? activeRoot.name : "null")}, hitAfterDetach={hitAfterDetach.name}");
                        }

                        activeRoot?.BeginDrag(hitAfterDetach);
                    }

                    activeRoot?.DragToMouse();
                }
                else
                {
                    if (debugLogs)
                        Debug.Log($"[DragHandle.HandleRightMousePull] PullOutStep on {activeAttachable.name}");

                    activeAttachable.CurrentSocket.PullOutStep(pullOutSpeed * Time.deltaTime);
                }
            }
            else
            {
                Transform hoveredTransform = GetRealMouseHitTransform();
                if (hoveredTransform == null && activeAttachable != null)
                    hoveredTransform = activeAttachable.transform;
                if (hoveredTransform == null)
                    hoveredTransform = transform;

                if (activeRoot == null)
                {
                    activeRoot = AssemblyRoot.FindActiveRoot(hoveredTransform);

                    if (debugLogs)
                    {
                        Debug.Log(
                            $"[DragHandle.HandleRightMousePull] free drag activeRoot=" +
                            $"{(activeRoot != null ? activeRoot.name : "null")} from {hoveredTransform.name}");
                    }

                    activeRoot?.BeginDrag(hoveredTransform);
                }

                activeRoot?.DragToMouse();
            }
        }

        if (Input.GetMouseButtonUp(1))
        {
            if (debugLogs)
                Debug.Log("[DragHandle.HandleRightMousePull] RMB UP");

            activeRoot?.EndDrag();

            if (activeAttachable != null && !activeAttachable.IsAttached)
            {
                if (debugLogs)
                    Debug.Log($"[DragHandle.HandleRightMousePull] TrySnapToNearestSocket on {activeAttachable.name}");

                activeAttachable.TrySnapToNearestSocket();
            }

            activeRoot = null;
            activeAttachable = null;
            isPullDragging = false;
        }
    }

    private Transform GetRealMouseHitTransform()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return null;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return hit.collider != null ? hit.collider.transform : null;
        }

        return null;
    }
}