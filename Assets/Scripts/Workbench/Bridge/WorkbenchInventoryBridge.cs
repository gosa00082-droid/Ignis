using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Мост между инвентарем и верстаком.
/// Отвечает за спавн предметов из инвентаря на верстак и возврат обратно.
/// </summary>
public class WorkbenchInventoryBridge : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private Inventory inventory;
    [SerializeField] private WorkbenchItemDatabase itemDatabase;
    [SerializeField] private WorkbenchSurface workbenchSurface;

    [Header("Настройки спавна")]
    [SerializeField] private Vector3 defaultSpawnOffset = new Vector3(0, 0.5f, 0);
    [SerializeField] private LayerMask spawnCheckMask;

    private void Awake()
    {
        // Валидация зависимостей
        if (inventory == null)
            Debug.LogError("WorkbenchInventoryBridge: Inventory не назначен!");
        
        if (itemDatabase == null)
            Debug.LogError("WorkbenchInventoryBridge: WorkbenchItemDatabase не назначен!");
        
        if (workbenchSurface == null)
            Debug.LogError("WorkbenchInventoryBridge: WorkbenchSurface не назначен!");
    }

    /// <summary>
    /// Спавнит предмет из инвентаря на верстак
    /// </summary>
    /// <param name="itemId">ID предмета из ItemData</param>
    /// <param name="position">Позиция спавна (если Vector3.zero - используется центр верстака)</param>
    /// <returns>Заспавненный GameObject или null если не удалось</returns>
    public GameObject SpawnItemOnWorkbench(string itemId, Vector3 position = default)
    {
        // Проверка что предмет есть в инвентаре
        if (inventory.GetCount(itemId) <= 0)
        {
            Debug.LogWarning($"WorkbenchInventoryBridge: Предмет {itemId} отсутствует в инвентаре");
            return null;
        }

        // Получить префаб из базы данных
        GameObject prefab = itemDatabase.GetPrefab(itemId);
        if (prefab == null)
        {
            Debug.LogError($"WorkbenchInventoryBridge: Префаб для предмета {itemId} не найден в WorkbenchItemDatabase");
            return null;
        }

        // Определить позицию спавна
        if (position == Vector3.zero && workbenchSurface != null)
        {
            position = workbenchSurface.transform.position + defaultSpawnOffset;
        }

        // Инстанцировать префаб
        GameObject spawnedObject = Instantiate(prefab, position, Quaternion.identity);
        spawnedObject.name = $"{prefab.name} (Spawned)";

        // Настроить компоненты
        SetupSpawnedObject(spawnedObject, itemId);

        // Списать предмет из инвентаря
        inventory.RemoveItem(itemId, 1);

        Debug.Log($"WorkbenchInventoryBridge: Заспавнен предмет {itemId} на позиции {position}");
        return spawnedObject;
    }

    /// <summary>
    /// Настраивает компоненты заспавненного объекта
    /// </summary>
    private void SetupSpawnedObject(GameObject obj, string itemId)
    {
        // Проверить наличие WorkbenchPart
        WorkbenchPart part = obj.GetComponent<WorkbenchPart>();
        if (part == null)
        {
            Debug.LogWarning($"WorkbenchInventoryBridge: У префаба {obj.name} отсутствует компонент WorkbenchPart");
        }

        // Проверить наличие PhysicsItem
        PhysicsItem physicsItem = obj.GetComponent<PhysicsItem>();
        if (physicsItem == null)
        {
            Debug.LogWarning($"WorkbenchInventoryBridge: У префаба {obj.name} отсутствует компонент PhysicsItem");
        }

        // Проверить наличие Rigidbody
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning($"WorkbenchInventoryBridge: У префаба {obj.name} отсутствует Rigidbody");
        }
    }

    /// <summary>
    /// Возвращает простой предмет (не сборку) обратно в инвентарь
    /// </summary>
    /// <param name="obj">GameObject предмета на верстаке</param>
    public void ReturnItemToInventory(GameObject obj)
    {
        if (obj == null)
        {
            Debug.LogWarning("WorkbenchInventoryBridge: Попытка вернуть null объект в инвентарь");
            return;
        }

        // Получить WorkbenchPart для определения itemId
        WorkbenchPart part = obj.GetComponent<WorkbenchPart>();
        if (part == null)
        {
            Debug.LogError($"WorkbenchInventoryBridge: У объекта {obj.name} отсутствует компонент WorkbenchPart");
            return;
        }

        string itemId = part.PartId;
        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogError($"WorkbenchInventoryBridge: У объекта {obj.name} не задан PartId");
            return;
        }

        // Проверить что это не часть сборки
        AssemblyRoot assemblyRoot = part.GetAssemblyRoot();
        if (assemblyRoot != null && assemblyRoot.gameObject != obj)
        {
            Debug.LogWarning($"WorkbenchInventoryBridge: Объект {obj.name} является частью сборки. Используйте SaveAssemblyToInventory()");
            return;
        }

        // Добавить предмет обратно в инвентарь
        inventory.AddItem(itemId, 1);

        // Удалить GameObject
        Destroy(obj);

        Debug.Log($"WorkbenchInventoryBridge: Предмет {itemId} возвращен в инвентарь");
    }

    /// <summary>
    /// Сохраняет сборку (завершенную или незавершенную) в инвентарь
    /// TODO: Реализовать сбор тегов и создание AssemblySnapshot
    /// </summary>
    /// <param name="assembly">Корень сборки</param>
    public void SaveAssemblyToInventory(AssemblyRoot assembly)
    {
        if (assembly == null)
        {
            Debug.LogWarning("WorkbenchInventoryBridge: Попытка сохранить null сборку");
            return;
        }

        Debug.LogWarning("WorkbenchInventoryBridge.SaveAssemblyToInventory(): Метод еще не реализован (заглушка)");
        
        // TODO: Реализовать:
        // 1. Собрать все AttachmentSocket в иерархии
        // 2. Для каждого сокета с прикрепленной деталью создать AttachedPartData
        // 3. Собрать теги со всех компонентов
        // 4. Создать AssemblySnapshot
        // 5. Создать InventoryItem с тегами и snapshot
        // 6. Добавить в инвентарь (когда Inventory будет поддерживать InventoryItem)
        // 7. Удалить GameObject
    }

    /// <summary>
    /// Восстанавливает сборку из инвентаря на верстак
    /// TODO: Реализовать восстановление иерархии из snapshot
    /// </summary>
    /// <param name="item">InventoryItem с сохраненной сборкой</param>
    /// <returns>Восстановленный GameObject или null</returns>
    public GameObject RestoreAssemblyFromInventory(InventoryItem item)
    {
        if (item == null || !item.isAssembly)
        {
            Debug.LogWarning("WorkbenchInventoryBridge: Попытка восстановить не-сборку");
            return null;
        }

        Debug.LogWarning("WorkbenchInventoryBridge.RestoreAssemblyFromInventory(): Метод еще не реализован (заглушка)");
        
        // TODO: Реализовать:
        // 1. Найти корневой компонент (parentPartId == null)
        // 2. Заспавнить корневой префаб
        // 3. Для каждой детали в snapshot.parts:
        //    - Найти родительский компонент
        //    - Найти сокет по socketId
        //    - Заспавнить префаб детали
        //    - Вызвать socket.TryAttach()
        //    - Установить insertionProgress
        // 4. Удалить InventoryItem из инвентаря
        // 5. Вернуть GameObject
        
        return null;
    }

    /// <summary>
    /// Собирает теги со всех компонентов сборки
    /// </summary>
    private List<string> CollectTagsFromAssembly(AssemblyRoot assembly)
    {
        List<string> tags = new List<string>();
        
        // TODO: Реализовать сбор тегов
        // 1. Получить все WorkbenchPart в иерархии
        // 2. Для каждой части получить ItemData из базы
        // 3. Добавить теги из ItemData в общий список
        
        return tags;
    }
}
