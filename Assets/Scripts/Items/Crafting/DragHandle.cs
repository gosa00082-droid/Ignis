using UnityEngine;

public class DragHandle : MonoBehaviour
{
    [Header("Скорость вытягивания из сокета")]
    [SerializeField] private float pullOutSpeed = 0.9f;

    private AssemblyRoot activeRoot;
    private AttachableObject ownAttachable;

    private bool isPullDragging;
    private bool isEntryDetachDrag;

    private void Awake()
    {
        ownAttachable = GetComponent<AttachableObject>();

        if (ownAttachable == null)
            ownAttachable = GetComponentInParent<AttachableObject>();
    }

    private void Update()
    {
        HandleRightMousePull();
    }

    private void OnMouseDown()
    {
        if (ownAttachable != null &&
            ownAttachable.IsAttached &&
            ownAttachable.CurrentSocket != null)
        {
            float progress = ownAttachable.CurrentSocket.TargetProgress;

            // Если объект только защелкнулся на входе,
            // ЛКМ оставляем для возможного "увести зажатием"
            if (progress <= 0.001f)
            {
                isEntryDetachDrag = true;
                return;
            }

            // Если уже начали вбивать, но еще не довели до конца,
            // ЛКМ не drag, а клики на вбивание
            if (progress < 1f)
            {
                return;
            }
        }

        activeRoot = GetComponentInParent<AssemblyRoot>();

        if (activeRoot != null)
        {
            activeRoot.BeginDrag();
        }
    }

    private void OnMouseDrag()
    {
        if (isEntryDetachDrag &&
            ownAttachable != null &&
            ownAttachable.IsAttached &&
            ownAttachable.CurrentSocket != null &&
            ownAttachable.CurrentSocket.IsOnlySnappedAtEntry)
        {
            ownAttachable.CurrentSocket.DetachImmediatelyIfOnlySnapped();

            if (activeRoot == null)
            {
                activeRoot = GetComponentInParent<AssemblyRoot>();
                if (activeRoot != null)
                {
                    activeRoot.BeginDrag();
                }
            }

            if (activeRoot != null)
            {
                activeRoot.DragToMouse();
            }

            return;
        }

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

        if (ownAttachable != null && !ownAttachable.IsAttached)
        {
            ownAttachable.TrySnapToNearestSocket();
        }

        activeRoot = null;
        isEntryDetachDrag = false;
    }

    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(1) &&
            ownAttachable != null &&
            ownAttachable.IsAttached &&
            ownAttachable.CurrentSocket != null &&
            ownAttachable.CurrentSocket.TargetProgress > 0.001f)
        {
            isPullDragging = true;
        }
    }

    private void HandleRightMousePull()
    {
        if (!isPullDragging)
            return;

        if (Input.GetMouseButton(1))
        {
            if (ownAttachable != null &&
                ownAttachable.IsAttached &&
                ownAttachable.CurrentSocket != null)
            {
                ownAttachable.CurrentSocket.PullOutStep(pullOutSpeed * Time.deltaTime);
            }
            else
            {
                if (activeRoot == null)
                {
                    activeRoot = GetComponentInParent<AssemblyRoot>();

                    if (activeRoot != null)
                    {
                        activeRoot.BeginDrag();
                    }
                }

                if (activeRoot != null)
                {
                    activeRoot.DragToMouse();
                }
            }
        }

        if (Input.GetMouseButtonUp(1))
        {
            if (activeRoot != null)
            {
                activeRoot.EndDrag();
            }

            if (ownAttachable != null && !ownAttachable.IsAttached)
            {
                ownAttachable.TrySnapToNearestSocket();
            }

            activeRoot = null;
            isPullDragging = false;
        }
    }
}