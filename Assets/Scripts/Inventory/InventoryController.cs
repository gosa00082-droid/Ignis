using UnityEngine;

public class InventoryController : MonoBehaviour
{
    [SerializeField] private GameObject inventoryPanel;
    // УДАЛИТЬ: [SerializeField] private PlayerMovement playerMovement; 
    [SerializeField] private CeilingCameraController ceilingCamera;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            UIManager.Instance.ToggleUI(inventoryPanel);

            if (inventoryPanel.activeSelf)
            {
                var inventoryUI = inventoryPanel.GetComponent<InventoryUI>();
                if (inventoryUI != null) inventoryUI.Refresh();
            }
        }
    }

    private void ToggleInventory(bool open)
    {
        inventoryPanel.SetActive(open);

        if (ceilingCamera != null)
            ceilingCamera.SetControl(!open);

        // УДАЛИТЬ: if (playerMovement != null) playerMovement.enabled = !open;

        if (open)
        {
            var inventoryUI = inventoryPanel.GetComponent<InventoryUI>();
            if (inventoryUI != null)
                inventoryUI.Refresh();
        }
    }
}