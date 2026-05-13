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
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private WorkbenchItemDatabase prefabDatabase;
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
            Debug.LogError("WorkbenchInventoryBridge: ItemDatabase не назначен!");
        
        if (prefabDatabase == null)
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
        GameObject prefab = prefabDatabase.GetPrefab(itemId);
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
    /// </summary>
    /// <param name="assembly">Корень сборки</param>
    public void SaveAssemblyToInventory(AssemblyRoot assembly)
    {
        if (assembly == null)
        {
            Debug.LogWarning("WorkbenchInventoryBridge: Попытка сохранить null сборку");
            return;
        }

        GameObject assemblyObject = assembly.gameObject;

        // 1. Получить AssemblyRecipeRunner для определения baseItemId
        AssemblyRecipeRunner runner = assemblyObject.GetComponent<AssemblyRecipeRunner>();
        string baseItemId = null;

        if (runner != null && runner.Recipe != null && !string.IsNullOrEmpty(runner.Recipe.resultItemId))
        {
            // Используем resultItemId из рецепта
            baseItemId = runner.Recipe.resultItemId;
        }
        else
        {
            // Если нет runner или resultItemId не задан, используем ID корневой детали
            WorkbenchPart rootPart = assemblyObject.GetComponent<WorkbenchPart>();
            baseItemId = rootPart != null ? rootPart.PartId : "unknown_assembly";
        }

        // 2. Собрать все AttachmentSocket в иерархии
        AttachmentSocket[] sockets = assemblyObject.GetComponentsInChildren<AttachmentSocket>();
        List<AttachedPartData> attachedParts = new List<AttachedPartData>();

        foreach (AttachmentSocket socket in sockets)
        {
            if (socket == null || !socket.HasAttachedObject)
                continue;

            AttachableObject attachedObject = socket.AttachedObject;
            if (attachedObject == null)
                continue;

            // Получить WorkbenchPart прикрепленной детали
            WorkbenchPart childPart = attachedObject.GetComponent<WorkbenchPart>();
            if (childPart == null)
            {
                Debug.LogWarning($"SaveAssemblyToInventory: У прикрепленного объекта {attachedObject.name} нет WorkbenchPart");
                continue;
            }

            // Получить WorkbenchPart родительского объекта (на котором находится сокет)
            WorkbenchPart parentPart = socket.GetComponentInParent<WorkbenchPart>();
            if (parentPart == null)
            {
                Debug.LogWarning($"SaveAssemblyToInventory: У сокета {socket.name} нет родительского WorkbenchPart");
                continue;
            }

            // Создать AttachedPartData
            AttachedPartData partData = new AttachedPartData
            {
                socketId = socket.SocketId,
                partItemId = childPart.PartId,
                insertionProgress = socket.TargetProgress,
                parentPartId = parentPart.PartId
            };

            attachedParts.Add(partData);
        }

        // 3. Собрать теги со всех компонентов
        List<string> tags = CollectTagsFromAssembly(assembly);

        // 4. Определить прогресс завершения
        float completionProgress = 0f;
        if (runner != null && runner.IsCompleted)
        {
            completionProgress = 1f;
        }
        else if (runner != null)
        {
            // Можно добавить более точный расчет прогресса позже
            completionProgress = 0.5f; // Примерное значение для незавершенной сборки
        }

        // Если сборка незавершена, добавляем тег "incomplete"
        if (completionProgress < 1f && !tags.Contains("incomplete"))
        {
            tags.Add("incomplete");
        }

        // 5. Создать AssemblySnapshot
        AssemblySnapshot snapshot = new AssemblySnapshot
        {
            baseItemId = baseItemId,
            parts = attachedParts,
            tags = tags,
            completionProgress = completionProgress
        };

        // 6. Добавить в инвентарь через новый метод AddAssembly
        inventory.AddAssembly(baseItemId, tags, snapshot);

        // 7. Удалить GameObject сборки
        Destroy(assemblyObject);

        Debug.Log($"WorkbenchInventoryBridge: Сборка {baseItemId} сохранена в инвентарь с {attachedParts.Count} деталями и {tags.Count} тегами");
    }

    /// <summary>
    /// Восстанавливает сборку из инвентаря на верстак
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

        AssemblySnapshot snapshot = item.snapshot;
        if (snapshot == null || snapshot.parts == null)
        {
            Debug.LogError("WorkbenchInventoryBridge: AssemblySnapshot отсутствует или поврежден");
            return null;
        }

        // 1. Найти корневой компонент (у которого нет parentPartId в списке деталей)
        // Корневой компонент - это baseItemId из snapshot
        string rootPartId = snapshot.baseItemId;

        // 2. Заспавнить корневой префаб
        GameObject prefab = prefabDatabase.GetPrefab(rootPartId);
        if (prefab == null)
        {
            Debug.LogError($"WorkbenchInventoryBridge: Префаб для корневой детали {rootPartId} не найден");
            return null;
        }

        Vector3 spawnPosition = workbenchSurface != null 
            ? workbenchSurface.transform.position + defaultSpawnOffset 
            : Vector3.zero;

        GameObject rootObject = Instantiate(prefab, spawnPosition, Quaternion.identity);
        rootObject.name = $"{prefab.name} (Restored Assembly)";

        // Настроить компоненты корневого объекта
        SetupSpawnedObject(rootObject, rootPartId);

        // 3. Восстановить все прикрепленные детали
        foreach (AttachedPartData partData in snapshot.parts)
        {
            if (string.IsNullOrEmpty(partData.partItemId) || string.IsNullOrEmpty(partData.socketId))
            {
                Debug.LogWarning("WorkbenchInventoryBridge: AttachedPartData содержит пустые поля");
                continue;
            }

            // Найти родительский GameObject по parentPartId
            GameObject parentObject = FindPartInHierarchy(rootObject, partData.parentPartId);
            if (parentObject == null)
            {
                Debug.LogWarning($"WorkbenchInventoryBridge: Родительский объект с partId={partData.parentPartId} не найден");
                continue;
            }

            // Найти AttachmentSocket на родителе по socketId
            AttachmentSocket socket = FindSocketInObject(parentObject, partData.socketId);
            if (socket == null)
            {
                Debug.LogWarning($"WorkbenchInventoryBridge: Сокет {partData.socketId} не найден на объекте {parentObject.name}");
                continue;
            }

            // Заспавнить префаб детали
            GameObject partPrefab = prefabDatabase.GetPrefab(partData.partItemId);
            if (partPrefab == null)
            {
                Debug.LogError($"WorkbenchInventoryBridge: Префаб для детали {partData.partItemId} не найден");
                continue;
            }

            // Позиционировать деталь в точке входа сокета
            Vector3 partPosition = socket.EntryPoint != null ? socket.EntryPoint.position : socket.transform.position;
            GameObject partObject = Instantiate(partPrefab, partPosition, Quaternion.identity);
            partObject.name = $"{partPrefab.name} (Restored Part)";

            SetupSpawnedObject(partObject, partData.partItemId);

            // Получить AttachableObject
            AttachableObject attachableObject = partObject.GetComponent<AttachableObject>();
            if (attachableObject == null)
            {
                Debug.LogWarning($"WorkbenchInventoryBridge: У детали {partObject.name} нет AttachableObject");
                Destroy(partObject);
                continue;
            }

            // Вызвать socket.TryAttach()
            if (socket.TryAttach(attachableObject))
            {
                // Установить прогресс вставки
                socket.SetProgress(partData.insertionProgress);
                Debug.Log($"WorkbenchInventoryBridge: Деталь {partData.partItemId} прикреплена к сокету {partData.socketId} с прогрессом {partData.insertionProgress}");
            }
            else
            {
                Debug.LogWarning($"WorkbenchInventoryBridge: Не удалось прикрепить деталь {partData.partItemId} к сокету {partData.socketId}");
                Destroy(partObject);
            }
        }

        // 4. Удалить InventoryItem из инвентаря
        inventory.RemoveItemByUniqueId(item.uniqueId);

        Debug.Log($"WorkbenchInventoryBridge: Сборка {rootPartId} восстановлена с {snapshot.parts.Count} деталями");

        // 5. Вернуть GameObject корневого объекта
        return rootObject;
    }

    /// <summary>
    /// Находит объект с указанным PartId в иерархии
    /// </summary>
    private GameObject FindPartInHierarchy(GameObject root, string partId)
    {
        if (root == null || string.IsNullOrEmpty(partId))
            return null;

        WorkbenchPart[] parts = root.GetComponentsInChildren<WorkbenchPart>();
        foreach (WorkbenchPart part in parts)
        {
            if (part.PartId == partId)
                return part.gameObject;
        }

        return null;
    }

    /// <summary>
    /// Находит AttachmentSocket с указанным SocketId в объекте
    /// </summary>
    private AttachmentSocket FindSocketInObject(GameObject obj, string socketId)
    {
        if (obj == null || string.IsNullOrEmpty(socketId))
            return null;

        AttachmentSocket[] sockets = obj.GetComponentsInChildren<AttachmentSocket>();
        foreach (AttachmentSocket socket in sockets)
        {
            if (socket.SocketId == socketId)
                return socket;
        }

        return null;
    }

    /// <summary>
    /// Собирает теги со всех компонентов сборки
    /// </summary>
    private List<string> CollectTagsFromAssembly(AssemblyRoot assembly)
    {
        List<string> tags = new List<string>();
        
        if (assembly == null)
            return tags;

        // Получить все WorkbenchPart в иерархии
        WorkbenchPart[] parts = assembly.GetComponentsInChildren<WorkbenchPart>();
        
        foreach (WorkbenchPart part in parts)
        {
            if (string.IsNullOrEmpty(part.PartId))
                continue;

            // Получить ItemData из базы
            ItemData itemData = itemDatabase.GetItem(part.PartId);
            if (itemData == null)
            {
                Debug.LogWarning($"CollectTagsFromAssembly: ItemData не найден для partId={part.PartId}");
                continue;
            }

            // Добавить теги из ItemData
            if (itemData.tags != null && itemData.tags.Count > 0)
            {
                foreach (string tag in itemData.tags)
                {
                    if (!tags.Contains(tag))
                    {
                        tags.Add(tag);
                    }
                }
            }
        }
        
        Debug.Log($"CollectTagsFromAssembly: Собрано {tags.Count} уникальных тегов: [{string.Join(", ", tags)}]");
        return tags;
    }
}
