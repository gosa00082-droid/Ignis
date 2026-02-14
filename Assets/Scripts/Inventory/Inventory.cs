// Assets/Scripts/Inventory/Inventory.cs
using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    // Отдельный словарь для каждой категории
    private Dictionary<string, int> materials = new Dictionary<string, int>();   // id → количество
    private Dictionary<string, int> weapons = new Dictionary<string, int>();
    private Dictionary<string, int> tools = new Dictionary<string, int>();
    private Dictionary<string, int> consumables = new Dictionary<string, int>();
    private Dictionary<string, int> others = new Dictionary<string, int>();

    [SerializeField] private ItemDatabase itemDatabase;  // ссылка на нашу базу

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

    // Добавить предмет в инвентарь
    public void AddItem(string itemId, int amount = 1)
    {
        ItemData item = itemDatabase.GetItem(itemId);
        if (item == null) return;

        var targetDict = GetDictionaryByCategory(item.category);

        if (targetDict.ContainsKey(itemId))
        {
            targetDict[itemId] += amount;
        }
        else
        {
            targetDict[itemId] = amount;
        }

        // Ограничение стека
        if (targetDict[itemId] > item.maxStackSize)
            targetDict[itemId] = item.maxStackSize;

        Debug.Log($"Добавлено {amount} × {item.itemName}. Всего: {targetDict[itemId]}");
    }

    // Убрать предмет из инвентаря
    public bool RemoveItem(string itemId, int amount = 1)
    {
        ItemData item = itemDatabase.GetItem(itemId);
        if (item == null) return false;

        var dict = GetDictionaryByCategory(item.category);

        if (!dict.ContainsKey(itemId) || dict[itemId] < amount)
            return false;

        dict[itemId] -= amount;
        if (dict[itemId] <= 0)
            dict.Remove(itemId);

        Debug.Log($"Удалено {amount} × {item.itemName}");
        return true;
    }

    // Сколько есть предмета
    public int GetCount(string itemId)
    {
        ItemData item = itemDatabase.GetItem(itemId);
        if (item == null) return 0;

        var dict = GetDictionaryByCategory(item.category);
        return dict.TryGetValue(itemId, out int count) ? count : 0;
    }

    // Вспомогательный метод — выбираем правильный словарь
    private Dictionary<string, int> GetDictionaryByCategory(ItemCategory category)
    {
        switch (category)
        {
            case ItemCategory.Material: return materials;
            case ItemCategory.Weapon: return weapons;
            case ItemCategory.Tool: return tools;
            case ItemCategory.Consumable: return consumables;
            default: return others;
        }
    }

    // Для теста — посмотреть содержимое (можно потом убрать)
    public void DebugPrintInventory()
    {
        Debug.Log("=== Инвентарь ===");
        PrintCategory("Материалы", materials);
        PrintCategory("Оружие", weapons);
        PrintCategory("Инструменты", tools);
        PrintCategory("Расходники", consumables);
        PrintCategory("Прочее", others);
    }

    private void PrintCategory(string name, Dictionary<string, int> dict)
    {
        if (dict.Count == 0) return;
        Debug.Log($"-- {name} --");
        foreach (var kvp in dict)
        {
            ItemData item = itemDatabase.GetItem(kvp.Key);
            Debug.Log($"{item?.itemName ?? kvp.Key} × {kvp.Value}");
        }
    }

    public Dictionary<string, int> GetMaterials()
    {
        return materials;   // возвращаем копию или напрямую
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T)) AddItem("Wooden_Handle", 5);     // замени на свой id
        if (Input.GetKeyDown(KeyCode.I)) DebugPrintInventory();
        if (Input.GetKeyDown(KeyCode.T)) AddItem("Iron_Ore", 5);     // замени на свой id
        if (Input.GetKeyDown(KeyCode.I)) DebugPrintInventory();
        if (Input.GetKeyDown(KeyCode.T)) AddItem("Silver", 5);     // замени на свой id
        if (Input.GetKeyDown(KeyCode.I)) DebugPrintInventory();
        if (Input.GetKeyDown(KeyCode.T)) AddItem("Long_Handler", 5);     // замени на свой id
        if (Input.GetKeyDown(KeyCode.I)) DebugPrintInventory();
        if (Input.GetKeyDown(KeyCode.M)) AddItem("Gold_Money", 15);     // замени на свой id

    }

}

