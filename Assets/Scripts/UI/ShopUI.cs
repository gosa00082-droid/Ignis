using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopUI : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Inventory inventory;           // Перетащи Inventory с игрока
    [SerializeField] private ItemDatabase itemDatabase;     // Перетащи ItemDatabase.asset
    [SerializeField] private GameObject shopCanvasRoot;     // Сам Canvas
    [SerializeField] private Transform contentParent;       // Content для списка предметов (ScrollView)
    [SerializeField] private GameObject slotPrefab;         // Префаб слота (как в InventoryUI)
    [SerializeField] private Button materialsButton, weaponsButton, toolsButton, otherButton;

    [Header("Детали справа")]
    [SerializeField] private Image itemIcon;                // Иконка выбранного
    [SerializeField] private TMP_Text itemNameText;         // Название
    [SerializeField] private TMP_Text itemPriceText;        // Цена за 1
    [SerializeField] private Button plusButton, minusButton, buyButton;

    [Header("Детали справа - доп")]
    [SerializeField] private TMP_Text inventoryCountText;   // Перетащи InventoryCountText
    [SerializeField] private TMP_InputField quantityInput;  // Перетащи QuantityInput вместо quantityText

    [Header("Блокировка")]
    [SerializeField] private PlayerMovement playerMovement; // Перетащи с игрока
    [SerializeField] private MouseLook mouseLook;           // Перетащи с камеры

    [Header("Правая панель")]
    [SerializeField] private GameObject rightDetailsPanel;  // Перетащи RightDetailsPanel (весь панель справа)

    [Header("Статус покупки")]
    [SerializeField] private TMP_Text statusText;  // Перетащи StatusText из инспектора

    private Color successColor = new Color(0f, 1f, 0f, 1f);  // Зелёный
    private Color errorColor = new Color(1f, 0f, 0f, 1f);    // Красный

    [Header("Счётчик золота")]
    [SerializeField] private TMP_Text goldCounterText;      // Перетащи GoldCounter из инспектора

    [Header("Цвет денег")]
    [SerializeField] private Color moneyColor = new Color(1f, 1f, 0f, 1f);  // жёлтый по умолчанию

    private ItemData selectedItem = null;
    private int quantity = 0;  // Начальное количество
    private ItemCategory currentCategory = ItemCategory.Material;

    private void Start()
    {
        if (statusText != null) statusText.text = "";

        // Подключаем вкладки
        materialsButton.onClick.AddListener(() => ShowCategory(ItemCategory.Material));
        weaponsButton.onClick.AddListener(() => ShowCategory(ItemCategory.Weapon));
        toolsButton.onClick.AddListener(() => ShowCategory(ItemCategory.Tool));
        otherButton.onClick.AddListener(() => ShowCategory(ItemCategory.Other));

        // Кнопки +/-
        plusButton.onClick.AddListener(() => ChangeQuantity(1));
        minusButton.onClick.AddListener(() => ChangeQuantity(-1));
        buyButton.onClick.AddListener(BuyItem);

        // Событие ввода
        if (quantityInput != null)
        {
            quantityInput.onValueChanged.AddListener(OnQuantityInputChanged);
        }

        ShowCategory(ItemCategory.Material);  // По умолчанию

        UpdateGoldDisplay();
    }

    public void OpenShop()
    {
        shopCanvasRoot.SetActive(true);
        ShowCategory(currentCategory);  // Обновить список
        if (rightDetailsPanel != null) rightDetailsPanel.SetActive(false);
        UpdateGoldDisplay();  // счётчик золота остаётся видимым
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        playerMovement.enabled = false;
        mouseLook.enabled = false;
        ResetSelection();
        UpdateGoldDisplay();

    }

    public void CloseShop()
    {
        shopCanvasRoot.SetActive(false);
        if (rightDetailsPanel != null) rightDetailsPanel.SetActive(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        playerMovement.enabled = true;
        mouseLook.enabled = true;
        ResetSelection();
        UpdateGoldDisplay();

    }

    private void UpdateStatus(string message, Color color)
    {
        if (statusText == null) return;
        statusText.text = message;
        statusText.color = color;
        UpdateGoldDisplay();

    }

    private void UpdateGoldDisplay()
    {
        if (goldCounterText == null) return;

        int gold = inventory.GetCount("Gold_Money");
        Debug.Log($"Монеты: {gold}");
        goldCounterText.text = $"Золото: {gold}";
        goldCounterText.color = moneyColor;
    }

    private void ShowCategory(ItemCategory category)
    {
        currentCategory = category;
        ClearSlots();
        if (rightDetailsPanel != null) rightDetailsPanel.SetActive(false);
        ResetSelection();  // на всякий
        var items = itemDatabase.GetItemsByCategory(category);
        foreach (var item in items)
        {
            if (item.basePrice <= 0) continue;  // Не продаём бесплатные
            CreateSlot(item);
        }
        UpdateGoldDisplay();
    }

    private void ClearSlots()
    {
        foreach (Transform child in contentParent) Destroy(child.gameObject);
        UpdateGoldDisplay();

    }

    private void CreateSlot(ItemData item)
    {
        GameObject slot = Instantiate(slotPrefab, contentParent);

        // Иконка
        Image icon = slot.transform.Find("Icon")?.GetComponent<Image>();
        if (icon != null && item.icon != null) icon.sprite = item.icon;

        // Название
        TMP_Text nameText = slot.transform.Find("Name")?.GetComponent<TMP_Text>();
        if (nameText != null) nameText.text = item.itemName;

        // Цена — жёлтым
        TMP_Text priceText = slot.transform.Find("Price")?.GetComponent<TMP_Text>();
        if (priceText != null)
        {
            priceText.text = $"{item.basePrice}";
            priceText.color = moneyColor;           // ← жёлтый цвет
        }

        // Кнопка выбора
        Button button = slot.GetComponent<Button>();
        if (button != null) button.onClick.AddListener(() => SelectItem(item));
    }

    private void SelectItem(ItemData item)
    {
        if (rightDetailsPanel != null) rightDetailsPanel.SetActive(true);

        ClearDetails();

        selectedItem = item;
        itemIcon.sprite = item.icon;
        itemNameText.text = item.itemName;
        itemPriceText.text = $"Цена: {item.basePrice} золота за 1";
        itemPriceText.color = moneyColor;

        quantity = 1;
        UpdateQuantityText();
        ChangeQuantity(0);

        UpdateInventoryCount();  // ← Добавь
    }

    private void ChangeQuantity(int delta)
    {
        quantity = Mathf.Max(0, quantity + delta);  // ← Теперь можно до 0
        if (selectedItem == null) return;

        int maxAffordable = inventory.GetCount("Gold_Money") / selectedItem.basePrice;
        quantity = Mathf.Min(quantity, maxAffordable);

        UpdateQuantityText();

        // Проверяем и обновляем кнопку + статус
        bool canBuy = quantity > 0 && inventory.GetCount("Gold_Money") >= (selectedItem.basePrice * quantity);
        buyButton.interactable = canBuy;

        if (!canBuy && quantity > 0)
        {
            UpdateStatus("Недостаточно денег!", errorColor);
        }
        else
        {
            UpdateStatus("", Color.white);  // Очищаем, если ок
        }
    }

    private void UpdateQuantityText()
    {
        if (quantityInput != null)
            quantityInput.text = quantity.ToString();
    }

    private void BuyItem()
    {
        if (selectedItem == null || quantity <= 0) return;
        int totalCost = selectedItem.basePrice * quantity;
        if (inventory.GetCount("Gold_Money") < totalCost)
        {
            UpdateStatus("Недостаточно денег!", errorColor);
            return;
        }

        inventory.RemoveItem("Gold_Money", totalCost);
        inventory.AddItem(selectedItem.id, quantity);
        Debug.Log($"Куплено {quantity} x {selectedItem.itemName}");

        UpdateGoldDisplay();
        UpdateStatus("Куплено!", successColor);

        quantity = 0;
        UpdateQuantityText();
        buyButton.interactable = false;

        UpdateInventoryCount();  // ← Добавь, чтобы обновить после добавления в инвентарь
    }

    private void ResetSelection()
    {
        selectedItem = null;
        quantity = 0;
        UpdateQuantityText();
        buyButton.interactable = false;

        // НЕ очищаем здесь правую панель — пусть остаётся после покупки
        // itemIcon.sprite = null;
        // itemNameText.text = "";
        // itemPriceText.text = "";

        // Статус тоже НЕ очищаем здесь — очищаем только при выборе нового предмета или закрытии
        // if (statusText != null) statusText.text = "";
    }

    private void ClearDetails()
    {
        itemIcon.sprite = null;
        itemNameText.text = "";
        itemPriceText.text = "";
        if (statusText != null) statusText.text = "";
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) CloseShop();
    }

    private void UpdateInventoryCount()
    {
        if (selectedItem == null || inventoryCountText == null) return;
        int count = inventory.GetCount(selectedItem.id);
        inventoryCountText.text = $"В инвентаре: {count}";
    }

    private void OnQuantityInputChanged(string input)
    {
        if (int.TryParse(input, out int newQuantity))
        {
            quantity = newQuantity;
            ChangeQuantity(0);  // Проверяем лимиты и статус
        }
        else
        {
            quantityInput.text = quantity.ToString();  // Восстанавливаем старое, если не число
        }
    }
}