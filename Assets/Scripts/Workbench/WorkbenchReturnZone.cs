using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Физическая зона (триггер) для возврата предметов в инвентарь
/// </summary>
[RequireComponent(typeof(Collider))]
public class WorkbenchReturnZone : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private WorkbenchInventoryBridge bridge;

    [Header("Visual Feedback")]
    [SerializeField] private Renderer zoneRenderer;              // Рендерер для визуальной подсветки (опционально)
    [SerializeField] private Color normalColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);
    [SerializeField] private Color highlightColor = new Color(0.2f, 1f, 0.2f, 0.6f);
    [SerializeField] private string emissionColorProperty = "_EmissionColor";

    [Header("Settings")]
    [SerializeField] private bool debugLogs = true;

    private HashSet<PhysicsItem> itemsInZone = new HashSet<PhysicsItem>();
    private Material zoneMaterial;

    private void Awake()
    {
        // Проверка что коллайдер настроен как триггер
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning("WorkbenchReturnZone: Collider должен быть триггером (isTrigger = true)");
            col.isTrigger = true;
        }

        // Получаем материал для визуальной подсветки
        if (zoneRenderer != null)
        {
            zoneMaterial = zoneRenderer.material;
            SetZoneColor(normalColor);
        }

        // Валидация зависимостей
        if (bridge == null)
        {
            Debug.LogError("WorkbenchReturnZone: WorkbenchInventoryBridge не назначен!");
        }
    }

    private void OnEnable()
    {
        // Подписываемся на события PhysicsItem
        PhysicsItem.OnItemGrabbed += OnItemGrabbed;
        PhysicsItem.OnItemReleased += OnItemReleased;
    }

    private void OnDisable()
    {
        // Отписываемся от событий
        PhysicsItem.OnItemGrabbed -= OnItemGrabbed;
        PhysicsItem.OnItemReleased -= OnItemReleased;
    }

    private void OnTriggerEnter(Collider other)
    {
        PhysicsItem physicsItem = other.GetComponent<PhysicsItem>();
        if (physicsItem == null)
        {
            // Проверяем в родителях
            physicsItem = other.GetComponentInParent<PhysicsItem>();
        }

        if (physicsItem != null)
        {
            itemsInZone.Add(physicsItem);

            if (debugLogs)
                Debug.Log($"WorkbenchReturnZone: Предмет {physicsItem.name} вошел в зону");

            UpdateVisualFeedback();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PhysicsItem physicsItem = other.GetComponent<PhysicsItem>();
        if (physicsItem == null)
        {
            physicsItem = other.GetComponentInParent<PhysicsItem>();
        }

        if (physicsItem != null)
        {
            itemsInZone.Remove(physicsItem);

            if (debugLogs)
                Debug.Log($"WorkbenchReturnZone: Предмет {physicsItem.name} вышел из зоны");

            UpdateVisualFeedback();
        }
    }

    /// <summary>
    /// Обработчик события захвата предмета
    /// </summary>
    private void OnItemGrabbed(PhysicsItem item)
    {
        // Когда предмет захватывают, обновляем визуал если он в зоне
        if (itemsInZone.Contains(item))
        {
            UpdateVisualFeedback();
        }
    }

    /// <summary>
    /// Обработчик события отпускания предмета
    /// </summary>
    private void OnItemReleased(PhysicsItem item)
    {
        // Проверяем находится ли предмет в зоне
        if (!itemsInZone.Contains(item))
            return;

        if (bridge == null)
        {
            Debug.LogError("WorkbenchReturnZone: WorkbenchInventoryBridge не назначен!");
            return;
        }

        if (debugLogs)
            Debug.Log($"WorkbenchReturnZone: Предмет {item.name} отпущен в зоне возврата");

        // Проверяем есть ли AssemblyRoot на предмете
        AssemblyRoot assemblyRoot = item.GetComponent<AssemblyRoot>();
        Debug.Log($"[ReturnZone DEBUG] item={item.name}, assemblyRoot={assemblyRoot != null}");

        if (assemblyRoot != null)
        {
            // Проверяем завершена ли сборка
            AssemblyRecipeRunner runner = item.GetComponent<AssemblyRecipeRunner>();
            if (runner != null) runner.RefreshState();
            Debug.Log($"[ReturnZone DEBUG] runner={runner != null}, IsCompleted={runner?.IsCompleted}, recipe={runner?.Recipe?.recipeId}");

            if (runner != null && !runner.IsCompleted)
            {
                if (debugLogs)
                    Debug.Log($"WorkbenchReturnZone: Сборка {item.name} не завершена, возврат невозможен");
                return;
            }

            // Завершённая сборка — сохраняем
            if (debugLogs)
                Debug.Log($"WorkbenchReturnZone: Сохраняем завершённую сборку {item.name}");

            bridge.SaveAssemblyToInventory(assemblyRoot);
        }
        else
        {
            // Это простой предмет - возвращаем через ReturnItemToInventory
            if (debugLogs)
                Debug.Log($"WorkbenchReturnZone: Возвращаем простой предмет {item.name}");

            bridge.ReturnItemToInventory(item.gameObject);
        }

        // Удаляем из списка (объект будет уничтожен или останется на столе)
        itemsInZone.Remove(item);
        UpdateVisualFeedback();
    }

    /// <summary>
    /// Обновить визуальную подсветку зоны
    /// </summary>
    private void UpdateVisualFeedback()
    {
        if (zoneMaterial == null)
            return;

        // Подсвечиваем зону если в ней есть предметы
        bool hasItems = itemsInZone.Count > 0;
        SetZoneColor(hasItems ? highlightColor : normalColor);
    }

    /// <summary>
    /// Установить цвет зоны
    /// </summary>
    private void SetZoneColor(Color color)
    {
        if (zoneMaterial == null)
            return;

        zoneMaterial.color = color;

        // Если материал поддерживает emission
        if (zoneMaterial.HasProperty(emissionColorProperty))
        {
            Color emissionColor = color * 2f; // Усиливаем для эмиссии
            zoneMaterial.SetColor(emissionColorProperty, emissionColor);
        }
    }

    private void OnDestroy()
    {
        // Очищаем материал
        if (zoneMaterial != null)
        {
            Destroy(zoneMaterial);
        }
    }

    private void OnDrawGizmos()
    {
        // Рисуем границы зоны в редакторе
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
            
            if (col is BoxCollider boxCol)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCol.center, boxCol.size);
            }
            else if (col is SphereCollider sphereCol)
            {
                Gizmos.DrawSphere(transform.position + sphereCol.center, sphereCol.radius);
            }
        }
    }
}
