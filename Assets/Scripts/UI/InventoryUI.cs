// Assets/Scripts/Inventory/InventoryUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Inventory inventory;           // ссылка на наш инвентарь
    [SerializeField] private ItemDatabase itemDatabase;     // база предметов
    [SerializeField] private GameObject slotPrefab;         // префаб слота
    [SerializeField] private Transform contentParent;       // Content внутри ScrollView

    [Header("Вкладки")]
    [SerializeField] private Button materialsButton;
    [SerializeField] private Button weaponsButton;
    [SerializeField] private Button toolsButton;
    [SerializeField] private Button otherButton;


    private ItemCategory currentCategory = ItemCategory.Material;

    private void Start()
    {
        // Подключаем кнопки вкладок
        materialsButton.onClick.AddListener(() => ShowCategory(ItemCategory.Material));
        weaponsButton.onClick.AddListener(() => ShowCategory(ItemCategory.Weapon));
        toolsButton.onClick.AddListener(() => ShowCategory(ItemCategory.Tool));
        otherButton.onClick.AddListener(() => ShowCategory(ItemCategory.Other));

        // По умолчанию показываем материалы
        ShowCategory(ItemCategory.Material);

        // Начальное выделение
        HighlightButton(materialsButton);
    }

    public void ShowCategory(ItemCategory category)
    {
        Button targetButton = GetButtonForCategory(category);
        if (targetButton != null) HighlightButton(targetButton);

        currentCategory = category;
        ClearSlots();

        // Получаем все предметы этой категории
        List<ItemData> items = itemDatabase.GetItemsByCategory(category);

        foreach (ItemData item in items)
        {
            int count = inventory.GetCount(item.id);
            if (count <= 0) continue;  // не показываем предметы с нулевым количеством

            CreateSlot(item, count);
        }
    }

    private void ClearSlots()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
    }

    private void CreateSlot(ItemData item, int amount)
    {
        GameObject slot = Instantiate(slotPrefab, contentParent);

        // Иконка
        Image icon = slot.transform.Find("Icon")?.GetComponent<Image>();
        if (icon != null && item.icon != null)
            icon.sprite = item.icon;

        // Количество
        TextMeshProUGUI amountText = slot.transform.Find("Amount")?.GetComponent<TextMeshProUGUI>();
        if (amountText != null)
            amountText.text = amount.ToString();

        // Название
        TextMeshProUGUI nameText = slot.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
        if (nameText != null)
            nameText.text = item.itemName;
    }

    // Вызывать при открытии инвентаря
    public void Refresh()
    {
        ShowCategory(currentCategory);
    }

    // Добавь в конец класса InventoryUI.cs эти поля и методы

    [Header("Визуальное выделение вкладок")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);    // обычный фон
    [SerializeField] private Color selectedColor = new Color(0.4f, 0.4f, 0.6f, 1f);  // выделенный

    private Button currentActiveButton = null;

    private void HighlightButton(Button button)
    {
        if (currentActiveButton != null)
        {
            var img = currentActiveButton.GetComponent<Image>();
            if (img != null) img.color = normalColor;
        }

        currentActiveButton = button;

        var activeImg = button.GetComponent<Image>();
        if (activeImg != null) activeImg.color = selectedColor;
    }

    private Button GetButtonForCategory(ItemCategory category)
    {
        switch (category)
        {
            case ItemCategory.Material: return materialsButton;
            case ItemCategory.Weapon: return weaponsButton;
            case ItemCategory.Tool: return toolsButton;
            case ItemCategory.Other: return otherButton;
            default: return materialsButton; // запасной вариант
        }
    }
}