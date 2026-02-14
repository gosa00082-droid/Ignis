using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopUI : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Inventory inventory;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private GameObject shopCanvasRoot;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Button materialsButton, weaponsButton, toolsButton, otherButton;

    [Header("Детали справа")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemPriceText;
    [SerializeField] private Button plusButton, minusButton, buyButton;

    [Header("Детали справа - доп")]
    [SerializeField] private TMP_Text inventoryCountText;
    [SerializeField] private TMP_InputField quantityInput;

    [Header("Правая панель")]
    [SerializeField] private GameObject rightDetailsPanel;

    [Header("Статус покупки")]
    [SerializeField] private TMP_Text statusText;

    private Color successColor = new Color(0f, 1f, 0f, 1f);
    private Color errorColor = new Color(1f, 0f, 0f, 1f);

    [Header("Счётчик золота")]
    [SerializeField] private TMP_Text goldCounterText;

    [Header("Цвет денег")]
    [SerializeField] private Color moneyColor = new Color(1f, 1f, 0f, 1f);

    private ItemData selectedItem = null;
    private int quantity = 0;
    private ItemCategory currentCategory = ItemCategory.Material;

    private void Start()
    {
        if (statusText != null) statusText.text = "";

        materialsButton.onClick.AddListener(() => ShowCategory(ItemCategory.Material));
        weaponsButton.onClick.AddListener(() => ShowCategory(ItemCategory.Weapon));
        toolsButton.onClick.AddListener(() => ShowCategory(ItemCategory.Tool));
        otherButton.onClick.AddListener(() => ShowCategory(ItemCategory.Other));

        plusButton.onClick.AddListener(() => ChangeQuantity(1));
        minusButton.onClick.AddListener(() => ChangeQuantity(-1));
        buyButton.onClick.AddListener(BuyItem);

        if (quantityInput != null)
        {
            quantityInput.onValueChanged.AddListener(OnQuantityInputChanged);
        }

        ShowCategory(ItemCategory.Material);
        UpdateGoldDisplay();
    }

    public void ToggleShop()
    {
        if (UIManager.Instance.TryOpenUI(shopCanvasRoot))
        {
            ShowCategory(ItemCategory.Material);
            ResetSelection();
            ClearDetails();
            UpdateGoldDisplay();
        }
    }

    private void OnDisable()
    {
        ResetSelection();
        ClearDetails();
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
        ResetSelection();

        var items = itemDatabase.GetItemsByCategory(category);
        foreach (var item in items)
        {
            if (item.basePrice <= 0) continue;
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

        Image icon = slot.transform.Find("Icon")?.GetComponent<Image>();
        if (icon != null && item.icon != null) icon.sprite = item.icon;

        TMP_Text nameText = slot.transform.Find("Name")?.GetComponent<TMP_Text>();
        if (nameText != null) nameText.text = item.itemName;

        TMP_Text priceText = slot.transform.Find("Price")?.GetComponent<TMP_Text>();
        if (priceText != null)
        {
            priceText.text = $"{item.basePrice}";
            priceText.color = moneyColor;
        }

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
        UpdateInventoryCount();
    }

    private void ChangeQuantity(int delta)
    {
        quantity = Mathf.Max(0, quantity + delta);
        if (selectedItem == null) return;

        int maxAffordable = inventory.GetCount("Gold_Money") / selectedItem.basePrice;
        quantity = Mathf.Min(quantity, maxAffordable);

        UpdateQuantityText();

        bool canBuy = quantity > 0 && inventory.GetCount("Gold_Money") >= (selectedItem.basePrice * quantity);
        buyButton.interactable = canBuy;

        if (!canBuy && quantity > 0)
        {
            UpdateStatus("Недостаточно денег!", errorColor);
        }
        else
        {
            UpdateStatus("", Color.white);
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
        UpdateInventoryCount();
    }

    private void ResetSelection()
    {
        selectedItem = null;
        quantity = 0;
        UpdateQuantityText();
        buyButton.interactable = false;
    }

    private void ClearDetails()
    {
        itemIcon.sprite = null;
        itemNameText.text = "";
        itemPriceText.text = "";
        if (statusText != null) statusText.text = "";
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
            ChangeQuantity(0);
        }
        else
        {
            quantityInput.text = quantity.ToString();
        }
    }
}