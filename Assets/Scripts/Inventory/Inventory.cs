// Assets/Scripts/Inventory/Inventory.cs
using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    // События для уведомления об изменениях инвентаря
    public static event System.Action OnInventoryChanged;

    // Новая структура хранения - список уникальных предметов
    private List<InventoryItem> items = new List<InventoryItem>();

    [SerializeField] private ItemDatabase itemDatabase;  // ссылка на нашу базу

    void Start()
    {
        // ... существующий код ...
        AddItem("r_iron", 10);
        AddItem("Coal", 20);
        AddItem("Gold_Money", 500);

    }

    private void Awake()
    {
        if (itemDatabase == null)
        {
            itemDatabase = Resources.Load<ItemDatabase>("ItemDatabase"); // ← попробуй этот вариант
                                                                         // или просто перетащи вручную в инспекторе — тогда эту строку можно убрать
        }

        if (itemDatabase == null)
        {
            Debug.LogError("База предметов НЕ НАЙДЕНА!");
        }
        else
        {
            Debug.Log($"База загружена. Всего предметов: {itemDatabase.TotalItems}");
        }
    }

    // Добавить предмет в инвентарь (обратная совместимость)
    public void AddItem(string itemId, int amount = 1)
    {
        ItemData item = itemDatabase.GetItem(itemId);
        if (item == null) return;

        int remainingAmount = amount;

        // Пытаемся добавить в существующие стаки
        foreach (InventoryItem invItem in items)
        {
            if (invItem.baseItemId == itemId && 
                (invItem.tags == null || invItem.tags.Count == 0) && 
                !invItem.isAssembly)
            {
                int canAdd = Mathf.Min(remainingAmount, item.maxStackSize - invItem.stackCount);
                if (canAdd > 0)
                {
                    invItem.stackCount += canAdd;
                    remainingAmount -= canAdd;
                }

                if (remainingAmount <= 0)
                    break;
            }
        }

        // Создаем новые стаки если осталось что добавить
        while (remainingAmount > 0)
        {
            int stackSize = Mathf.Min(remainingAmount, item.maxStackSize);
            InventoryItem newItem = new InventoryItem
            {
                uniqueId = System.Guid.NewGuid().ToString(),
                baseItemId = itemId,
                tags = new List<string>(),
                stackCount = stackSize,
                isAssembly = false,
                snapshot = null
            };
            items.Add(newItem);
            remainingAmount -= stackSize;
        }

        Debug.Log($"Добавлено {amount} × {item.itemName}. Всего: {GetCount(itemId)}");

        if (TutorialManager.Instance != null)
        {
            int currentCount = GetCount(itemId);
            TutorialManager.Instance.CheckGoals(itemId, currentCount, GoalType.CollectItem);
        }

        // Уведомляем об изменении инвентаря
        OnInventoryChanged?.Invoke();
    }

    // Убрать предмет из инвентаря (обратная совместимость)
    public bool RemoveItem(string itemId, int amount = 1)
    {
        ItemData item = itemDatabase.GetItem(itemId);
        if (item == null) return false;

        int remainingAmount = amount;

        // Удаляем из существующих стаков
        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i].baseItemId == itemId && !items[i].isAssembly)
            {
                int canRemove = Mathf.Min(remainingAmount, items[i].stackCount);
                items[i].stackCount -= canRemove;
                remainingAmount -= canRemove;

                if (items[i].stackCount <= 0)
                {
                    items.RemoveAt(i);
                }

                if (remainingAmount <= 0)
                    break;
            }
        }

        if (remainingAmount > 0)
        {
            Debug.LogWarning($"Не удалось удалить {amount} × {item.itemName}. Не хватает предметов.");
            return false;
        }

        Debug.Log($"Удалено {amount} × {item.itemName}");
        
        // Уведомляем об изменении инвентаря
        OnInventoryChanged?.Invoke();
        
        return true;
    }

    // Сколько есть предмета (обратная совместимость)
    public int GetCount(string itemId)
    {
        int totalCount = 0;

        foreach (InventoryItem invItem in items)
        {
            if (invItem.baseItemId == itemId && !invItem.isAssembly)
            {
                totalCount += invItem.stackCount;
            }
        }

        return totalCount;
    }

    // Для FurnaceUI - получить предметы по категории
    public List<InventoryItem> GetItemsByCategory(ItemCategory category)
    {
        List<InventoryItem> result = new List<InventoryItem>();

        foreach (InventoryItem invItem in items)
        {
            ItemData itemData = itemDatabase.GetItem(invItem.baseItemId);
            if (itemData != null && itemData.category == category)
            {
                result.Add(invItem);
            }
        }

        return result;
    }

    // Для InventoryUI - получить все предметы
    public List<InventoryItem> GetAllItems()
    {
        return new List<InventoryItem>(items);
    }

    // Для теста — посмотреть содержимое (можно потом убрать)
    public void DebugPrintInventory()
    {
        Debug.Log("=== Инвентарь ===");
        Debug.Log($"Всего уникальных предметов: {items.Count}");
        
        foreach (InventoryItem invItem in items)
        {
            ItemData itemData = itemDatabase.GetItem(invItem.baseItemId);
            string itemName = itemData != null ? itemData.itemName : invItem.baseItemId;
            string tagsStr = invItem.tags != null && invItem.tags.Count > 0 ? $" [Tags: {string.Join(", ", invItem.tags)}]" : "";
            string assemblyStr = invItem.isAssembly ? " [ASSEMBLY]" : "";
            Debug.Log($"{itemName} × {invItem.stackCount}{tagsStr}{assemblyStr}");
        }
    }

    public Dictionary<string, int> GetMaterials()
    {
        // Для обратной совместимости с FurnaceUI (временно)
        Dictionary<string, int> result = new Dictionary<string, int>();
        
        foreach (InventoryItem invItem in items)
        {
            ItemData itemData = itemDatabase.GetItem(invItem.baseItemId);
            if (itemData != null && itemData.category == ItemCategory.Material)
            {
                if (result.ContainsKey(invItem.baseItemId))
                {
                    result[invItem.baseItemId] += invItem.stackCount;
                }
                else
                {
                    result[invItem.baseItemId] = invItem.stackCount;
                }
            }
        }
        
        return result;
    }

    // ========== НОВЫЕ МЕТОДЫ ДЛЯ РАБОТЫ С ТЕГАМИ ==========

    /// <summary>
    /// Добавить сборку в инвентарь с тегами и snapshot
    /// </summary>
    public void AddAssembly(string baseItemId, List<string> tags, AssemblySnapshot snapshot)
    {
        InventoryItem newItem = new InventoryItem
        {
            uniqueId = System.Guid.NewGuid().ToString(),
            baseItemId = baseItemId,
            tags = new List<string>(tags),
            stackCount = 1,
            isAssembly = true,
            snapshot = snapshot
        };
        
        items.Add(newItem);
        
        ItemData itemData = itemDatabase.GetItem(baseItemId);
        string itemName = itemData != null ? itemData.itemName : baseItemId;
        Debug.Log($"Добавлена сборка: {itemName} с тегами [{string.Join(", ", tags)}]");
        
        // Уведомляем об изменении инвентаря
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Получить количество предметов с требуемыми тегами
    /// </summary>
    public int GetCountByTags(List<string> requiredTags)
    {
        if (requiredTags == null || requiredTags.Count == 0)
            return 0;

        int totalCount = 0;

        foreach (InventoryItem invItem in items)
        {
            if (HasAllTags(invItem.tags, requiredTags))
            {
                totalCount += invItem.stackCount;
            }
        }

        return totalCount;
    }

    /// <summary>
    /// Удалить предмет с требуемыми тегами
    /// </summary>
    public bool RemoveItemByTags(List<string> requiredTags, int amount = 1)
    {
        if (requiredTags == null || requiredTags.Count == 0)
            return false;

        int remainingAmount = amount;

        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (HasAllTags(items[i].tags, requiredTags))
            {
                int canRemove = Mathf.Min(remainingAmount, items[i].stackCount);
                items[i].stackCount -= canRemove;
                remainingAmount -= canRemove;

                if (items[i].stackCount <= 0)
                {
                    items.RemoveAt(i);
                }

                if (remainingAmount <= 0)
                    break;
            }
        }

        if (remainingAmount > 0)
        {
            Debug.LogWarning($"Не удалось удалить {amount} предметов с тегами [{string.Join(", ", requiredTags)}]");
            return false;
        }

        Debug.Log($"Удалено {amount} предметов с тегами [{string.Join(", ", requiredTags)}]");
        
        // Уведомляем об изменении инвентаря
        OnInventoryChanged?.Invoke();
        
        return true;
    }

    /// <summary>
    /// Получить предмет по уникальному ID
    /// </summary>
    public InventoryItem GetItemByUniqueId(string uniqueId)
    {
        foreach (InventoryItem invItem in items)
        {
            if (invItem.uniqueId == uniqueId)
            {
                return invItem;
            }
        }

        return null;
    }

    /// <summary>
    /// Удалить предмет по уникальному ID
    /// </summary>
    public bool RemoveItemByUniqueId(string uniqueId)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].uniqueId == uniqueId)
            {
                items.RemoveAt(i);
                Debug.Log($"Удален предмет с uniqueId: {uniqueId}");
                
                // Уведомляем об изменении инвентаря
                OnInventoryChanged?.Invoke();
                
                return true;
            }
        }

        Debug.LogWarning($"Предмет с uniqueId {uniqueId} не найден");
        return false;
    }

    /// <summary>
    /// Проверяет содержит ли список тегов все требуемые теги
    /// </summary>
    private bool HasAllTags(List<string> itemTags, List<string> requiredTags)
    {
        if (itemTags == null || itemTags.Count == 0)
            return false;

        foreach (string requiredTag in requiredTags)
        {
            if (!itemTags.Contains(requiredTag))
                return false;
        }

        return true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M)) AddItem("Gold_Money", 15);     
    }

}

