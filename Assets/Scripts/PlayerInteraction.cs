using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactionDistance = Mathf.Infinity;  // Для top-down — бесконечно
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private QuestBoardUI questBoardUI;

    private Camera playerCamera;
    private FurnaceUI furnaceUI;
    private AnvilUI anvilUI;

    private void Awake()
    {
        playerCamera = GetComponent<Camera>();  // Камера на этом объекте
        furnaceUI = FindObjectOfType<FurnaceUI>();
        anvilUI = FindObjectOfType<AnvilUI>();
        shopUI = FindObjectOfType<ShopUI>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableLayer))
            {
                if (hit.collider.CompareTag("Furnace"))
                    furnaceUI.ToggleFurnace();
                else if (hit.collider.CompareTag("Anvil") && anvilUI != null)
                    anvilUI.ToggleAnvil();
                else if (hit.collider.CompareTag("ShopTable") && shopUI != null)
                    shopUI.ToggleShop();
                else if (hit.collider.CompareTag("QuestBoard") && questBoardUI != null)
                    questBoardUI.ToggleQuestBoard();
            }
        }
    }
}