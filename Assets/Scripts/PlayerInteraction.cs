using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Õ‡ÒÚÓÈÍË")]
    [SerializeField] private float interactionDistance = 100f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("—Ò˚ÎÍË Ì‡ UI Ô‡ÌÂÎË ó œ≈–≈“¿Ÿ» —ﬁƒ¿ »« »≈–¿–’»»!")]
    [SerializeField] private GameObject furnacePanel;
    [SerializeField] private GameObject anvilPanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject questBoardPanel;

    private Camera playerCamera;

    private void Awake()
    {
        playerCamera = GetComponent<Camera>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryOpenUI();
        }
    }

    private void TryOpenUI()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableLayer))
        {
            string tag = hit.collider.tag;

            if (tag == "Furnace" && furnacePanel != null)
                UIManager.Instance.ToggleUI(furnacePanel);

            else if (tag == "Anvil" && anvilPanel != null)
                UIManager.Instance.ToggleUI(anvilPanel);

            else if (tag == "ShopTable" && shopPanel != null)
                UIManager.Instance.ToggleUI(shopPanel);

            else if (tag == "QuestBoard" && questBoardPanel != null)
                UIManager.Instance.ToggleUI(questBoardPanel);
        }
    }
}