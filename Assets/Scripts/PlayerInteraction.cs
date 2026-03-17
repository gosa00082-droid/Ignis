using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private MouseLook mouseLook;

    [Header("Настройки")]
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private float doubleClickTime = 0.3f;
    [SerializeField] private LayerMask interactableLayer = ~0;

    private float lastClickTime = -1f;
    private InteractableObject currentHover;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera == null)
            return;

        HandleHover();

        if (UIManager.Instance != null && UIManager.Instance.currentUI != null)
            return;

        if (mouseLook != null && mouseLook.IsRotating)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            float delta = Time.time - lastClickTime;

            if (delta <= doubleClickTime)
            {
                TryInteract();
            }

            lastClickTime = Time.time;
        }
    }

    private void HandleHover()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayer))
        {
            InteractableObject interactable = hit.collider.GetComponentInParent<InteractableObject>();

            if (interactable != currentHover)
            {
                if (currentHover != null)
                    currentHover.SetHighlight(false);

                currentHover = interactable;

                if (currentHover != null)
                    currentHover.SetHighlight(true);
            }
        }
        else
        {
            if (currentHover != null)
            {
                currentHover.SetHighlight(false);
                currentHover = null;
            }
        }
    }

    private void TryInteract()
    {
        if (currentHover != null)
            currentHover.Interact();
    }
}
