// Assets/Scripts/Player/PlayerInteraction.cs
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;   // слой Interactable
    [SerializeField] private ShopUI shopUI;  // Перетащи ShopUI из сцены
    [SerializeField] private QuestBoardUI questBoardUI;

    private Camera playerCamera;

    // Ссылки на UI-менеджеры
    private FurnaceUI furnaceUI;
    private AnvilUI anvilUI;

    // Флаги открытых интерфейсов
    private bool isFurnaceOpen = false;
    private bool isAnvilOpen = false;
    private bool isShopOpen = false;
    private bool isQuestBoardOpen = false;

    private void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();

        // Находим нужные скрипты (лучше перетащить в инспекторе, но FindObjectOfType — запасной вариант)
        furnaceUI = FindObjectOfType<FurnaceUI>();
        anvilUI = FindObjectOfType<AnvilUI>();
        shopUI = FindObjectOfType<ShopUI>();

        if (shopUI == null) Debug.LogError("ShopUI не найден!");
        if (furnaceUI == null) Debug.LogError("FurnaceUI не найден в сцене!");
        if (anvilUI == null) Debug.LogWarning("AnvilUI не найден — интерфейс наковальни работать не будет.");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            RaycastHit hit;
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward,
                               out hit, interactionDistance, interactableLayer))
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