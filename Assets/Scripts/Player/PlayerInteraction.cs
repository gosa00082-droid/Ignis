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
        // Если любой интерфейс открыт — по повторному E закрываем
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (isFurnaceOpen)
            {
                furnaceUI.CloseFurnace();
                isFurnaceOpen = false;
                Debug.Log("Печь закрыта по повторному E");
                return;
            }

            if (isShopOpen)
            {
                shopUI.CloseShop();
                isShopOpen = false;
                Debug.Log("Магазин закрыт по повторному E");
                return;
            }

            if (isAnvilOpen)
            {
                anvilUI.CloseAnvil();
                isAnvilOpen = false;
                Debug.Log("Наковальня закрыта по повторному E");
                return;
            }

            if (isQuestBoardOpen) 
            { 
                questBoardUI.CloseQuestBoard(); 
                isQuestBoardOpen = false; 
                return; 
            }

            RaycastHit hit;
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward,
                               out hit, interactionDistance, interactableLayer))
            {
                if (hit.collider.CompareTag("Furnace"))
                {
                    furnaceUI.OpenFurnace();
                    isFurnaceOpen = true;
                    Debug.Log("Печь открыта");
                }
                else if (hit.collider.CompareTag("Anvil"))
                {
                    if (anvilUI != null)
                    {
                        anvilUI.OpenAnvil();
                        isAnvilOpen = true;
                        Debug.Log("Наковальня открыта");
                    }
                }
                else if (hit.collider.CompareTag("ShopTable"))
                {
                    if (shopUI != null)
                    {
                        shopUI.OpenShop();
                        isShopOpen = true;
                        Debug.Log("Магазин открыт");
                    }
                }
                else if (hit.collider.CompareTag("QuestBoard"))
                {
                    if (questBoardUI != null)
                    {
                        questBoardUI.OpenQuestBoard();
                        isQuestBoardOpen = true;
                        Debug.Log("Доска заказов открыта");
                    }
                }
            }
        }
    }
}