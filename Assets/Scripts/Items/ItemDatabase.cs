// Assets/Scripts/Items/ItemDatabase.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Ignis/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField]
    private List<ItemData> items = new List<ItemData>();

    // Быстрый поиск по id (будет использоваться очень часто)
    private Dictionary<string, ItemData> itemDict;

    private void OnEnable()
    {
        // Создаём словарь при загрузке ассета
        itemDict = new Dictionary<string, ItemData>();

        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.id))
            {
                Debug.LogWarning($"У предмета {item.name} нет id!");
                continue;
            }

            if (itemDict.ContainsKey(item.id))
            {
                Debug.LogWarning($"Дубликат id: {item.id} у предмета {item.name}");
                continue;
            }

            itemDict[item.id] = item;
        }
    }

    // Получить предмет по уникальному id
    public ItemData GetItem(string id)
    {
        if (itemDict == null || itemDict.Count == 0)
        {
            Debug.LogError("База предметов не инициализирована!");
            return null;
        }

        if (itemDict.TryGetValue(id, out ItemData item))
        {
            return item;
        }

        Debug.LogWarning($"Предмет с id '{id}' не найден в базе");
        return null;
    }

    public ItemData GetItemByName(string name)
    {
        return items.FirstOrDefault(i => i.itemName == name);
    }

    // Получить все предметы определённой категории (удобно для UI)
    public List<ItemData> GetItemsByCategory(ItemCategory category)
    {
        return items.Where(i => i.category == category).ToList();
    }

    // Для отладки — посмотреть сколько всего предметов
    public int TotalItems => items.Count;
}