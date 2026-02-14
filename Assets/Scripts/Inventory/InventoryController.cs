// Assets/Scripts/Inventory/InventoryController.cs
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    [SerializeField] private GameObject inventoryPanel;     // перетащи InventoryPanel
    [SerializeField] private PlayerMovement playerMovement; // твой скрипт движения игрока
    [SerializeField] private MouseLook mouseLook;           // твой скрипт взгляда

    private bool isOpen = false;

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

        // Курсор
        Cursor.visible = open;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;

        // Блокировка управления
        if (playerMovement != null) playerMovement.enabled = !open;
        if (mouseLook != null) mouseLook.enabled = !open;

        // ← Добавляем обновление инвентаря при открытии
        if (open)
        {
            var inventoryUI = inventoryPanel.GetComponent<InventoryUI>();
            if (inventoryUI != null)
            {
                inventoryUI.Refresh();
            }
        }
    }
}