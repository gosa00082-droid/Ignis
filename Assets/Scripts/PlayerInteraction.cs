using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private MouseLook mouseLook;
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private float doubleClickTime = 0.3f;

    private float lastClickTime = -1f;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera == null)
            return;

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

    private void TryInteract()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            InteractableObject interactable = hit.collider.GetComponentInParent<InteractableObject>();

            if (interactable != null)
            {
                interactable.Interact();
            }
        }
    }
}
