using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "WorkbenchItemDatabase", menuName = "Ignis/Workbench Item Database")]
public class WorkbenchItemDatabase : ScriptableObject
{
    public List<ItemPrefabMapping> mappings = new List<ItemPrefabMapping>();
    
    // Получить префаб по ID предмета
    public GameObject GetPrefab(string itemId)
    {
        var mapping = mappings.FirstOrDefault(m => m.itemId == itemId);
        return mapping?.prefab;
    }
    
    // Проверить есть ли префаб для предмета
    public bool HasPrefab(string itemId)
    {
        return mappings.Any(m => m.itemId == itemId);
    }
    
    // Валидация (вызывается в редакторе)
    private void OnValidate()
    {
        // Проверка на дубликаты itemId
        var duplicates = mappings.GroupBy(m => m.itemId)
                                  .Where(g => g.Count() > 1)
                                  .Select(g => g.Key);
        
        if (duplicates.Any())
        {
            Debug.LogWarning($"WorkbenchItemDatabase: Найдены дубликаты itemId: {string.Join(", ", duplicates)}");
        }
    }
}
