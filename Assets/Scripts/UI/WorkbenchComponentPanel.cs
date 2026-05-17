using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Выдвижная UI панель для выбора компонентов из инвентаря для спавна на верстак
/// </summary>
public class WorkbenchComponentPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform panelTransform;       // RectTransform панели для анимации
    [SerializeField] private Transform contentParent;            // Content внутри ScrollView
    [SerializeField] private GameObject itemButtonPrefab;        // Префаб кнопки предмета
    [SerializeField] private Toggle showAllToggle;               // Чекбокс "Показывать готовые предметы"

    [Header("Dependencies")]
    [SerializeField] private Inventory inventory;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private WorkbenchItemDatabase prefabDatabase;
    [SerializeField] private WorkbenchInventoryBridge bridge;

    [Header("Animation Settings")]
    [SerializeField] private float hiddenPositionX = -1150f;     // Позиция когда панель скрыта
    [SerializeField] private float visiblePositionX = -770f;     // Позиция когда панель видна
    [SerializeField] private float edgeDetectionWidth = 50f;     // Ширина зоны для активации (пиксели от левого края)
    [SerializeField] private float animationSpeed = 800f;        // Скорость анимации (пикселей в секунду)

    private List<GameObject> spawnedButtons = new List<GameObject>();
    private bool isMouseOverPanel = false;
    private bool isMouseNearEdge = false;
    private bool isPanelActive = false;

    private void Start()
    {
        // Подписываемся на изменение чекбокса
        if (showAllToggle != null)
        {
            showAllToggle.onValueChanged.AddListener(OnShowAllToggleChanged);
        }

        // Скрываем панель при старте (устанавливаем позицию за экраном)
        if (panelTransform != null)
        {
            Vector2 pos = panelTransform.anchoredPosition;
            pos.x = hiddenPositionX;
            panelTransform.anchoredPosition = pos;
        }
    }

    private void OnEnable()
    {
        // Вызывается когда UIManager включает Canvas
        ActivatePanel();
        
        // Подписываемся на изменения инвентаря
        Inventory.OnInventoryChanged += OnInventoryChanged;
    }

    private void OnDisable()
    {
        // Вызывается когда UIManager выключает Canvas
        DeactivatePanel();
        
        // Отписываемся от изменений инвентаря
        Inventory.OnInventoryChanged -= OnInventoryChanged;
    }

    private void Update()
    {
        if (!isPanelActive)
            return;

        // Проверяем позицию мыши относительно экрана
        Vector2 mousePos = Input.mousePosition;
        isMouseNearEdge = mousePos.x < edgeDetectionWidth;

        // Проверяем находится ли мышь над панелью
        if (panelTransform != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                panelTransform, 
                mousePos, 
                null, 
                out Vector2 localPoint
            );
            isMouseOverPanel = panelTransform.rect.Contains(localPoint);
        }

        // Анимация панели
        AnimatePanel();
    }

    private void AnimatePanel()
    {
        if (panelTransform == null)
            return;

        // Определяем целевую позицию
        float targetX = (isMouseNearEdge || isMouseOverPanel) ? visiblePositionX : hiddenPositionX;

        // Плавно двигаем панель к целевой позиции
        Vector2 currentPos = panelTransform.anchoredPosition;
        float newX = Mathf.MoveTowards(currentPos.x, targetX, animationSpeed * Time.deltaTime);
        
        panelTransform.anchoredPosition = new Vector2(newX, currentPos.y);
    }

    /// <summary>
    /// Активировать панель (вызывается когда UIManager включает Canvas)
    /// </summary>
    public void ActivatePanel()
    {
        isPanelActive = true;
        RefreshItems();
        
        Debug.Log("WorkbenchComponentPanel: Панель активирована");
    }

    /// <summary>
    /// Деактивировать панель (вызывается когда UIManager выключает Canvas)
    /// </summary>
    public void DeactivatePanel()
    {
        isPanelActive = false;
        
        // Сбрасываем позицию панели за экран
        if (panelTransform != null)
        {
            Vector2 pos = panelTransform.anchoredPosition;
            pos.x = hiddenPositionX;
            panelTransform.anchoredPosition = pos;
        }
        
        Debug.Log("WorkbenchComponentPanel: Панель деактивирована");
    }

    /// <summary>
    /// Обновить список предметов
    /// </summary>
    public void RefreshItems()
    {
        ClearButtons();

        if (inventory == null || itemDatabase == null || prefabDatabase == null)
        {
            Debug.LogError("WorkbenchComponentPanel: Не все зависимости назначены!");
            Debug.LogError($"  inventory = {inventory}, itemDatabase = {itemDatabase}, prefabDatabase = {prefabDatabase}");
            return;
        }

        bool showAll = showAllToggle != null && showAllToggle.isOn;

        // Получаем все предметы из инвентаря
        List<InventoryItem> allItems = inventory.GetAllItems();

        Debug.Log($"WorkbenchComponentPanel.RefreshItems: Всего предметов в инвентаре: {allItems.Count}, showAll = {showAll}");

        // Группируем по baseItemId для отображения
        Dictionary<string, int> itemCounts = new Dictionary<string, int>();

        foreach (InventoryItem invItem in allItems)
        {
            bool hasPrefab = prefabDatabase.HasPrefab(invItem.baseItemId);
            Debug.Log($"  - {invItem.baseItemId}, stackCount={invItem.stackCount}, isAssembly={invItem.isAssembly}, hasPrefab={hasPrefab}");

            // Проверяем есть ли префаб для этого предмета
            if (!hasPrefab)
                continue;

            // Фильтрация: если showAll выключен, пропускаем сборки и готовые предметы
            if (!showAll && invItem.isAssembly)
                continue;

            // Фильтрация: если showAll выключен, пропускаем предметы с тегами готовых изделий
            if (!showAll && HasFinishedItemTags(invItem.tags))
                continue;

            // Добавляем в словарь для группировки
            if (itemCounts.ContainsKey(invItem.baseItemId))
            {
                itemCounts[invItem.baseItemId] += invItem.stackCount;
            }
            else
            {
                itemCounts[invItem.baseItemId] = invItem.stackCount;
            }
        }

        // Создаем кнопки для каждого уникального предмета
        foreach (var kvp in itemCounts)
        {
            CreateItemButton(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// Проверяет содержит ли список тегов теги готовых изделий
    /// </summary>
    private bool HasFinishedItemTags(List<string> tags)
    {
        if (tags == null || tags.Count == 0)
            return false;

        // Список тегов готовых изделий (можно расширить)
        string[] finishedItemTags = { "sword", "axe", "pickaxe", "hammer", "dagger", "spear" };

        foreach (string tag in tags)
        {
            foreach (string finishedTag in finishedItemTags)
            {
                if (tag.Equals(finishedTag, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Создать кнопку для предмета
    /// </summary>
    private void CreateItemButton(string itemId, int count)
    {
        if (itemButtonPrefab == null || contentParent == null)
            return;

        ItemData itemData = itemDatabase.GetItem(itemId);
        if (itemData == null)
            return;

        // Инстанцируем кнопку
        GameObject buttonObj = Instantiate(itemButtonPrefab, contentParent);
        spawnedButtons.Add(buttonObj);

        // Настраиваем визуал
        Image iconImage = buttonObj.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage != null && itemData.icon != null)
        {
            iconImage.sprite = itemData.icon;
        }

        TMP_Text amountText = buttonObj.transform.Find("AmountText")?.GetComponent<TMP_Text>();
        if (amountText != null)
        {
            amountText.text = $"x{count}";
        }

        TMP_Text nameText = buttonObj.transform.Find("NameText")?.GetComponent<TMP_Text>();
        if (nameText != null)
        {
            nameText.text = itemData.itemName;
        }

        // Настраиваем обработчик клика
        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            string capturedItemId = itemId; // Захватываем переменную для замыкания
            button.onClick.AddListener(() => OnItemButtonClicked(capturedItemId));
        }
    }

    /// <summary>
    /// Обработчик клика на кнопку предмета
    /// </summary>
    private void OnItemButtonClicked(string itemId)
    {
        if (bridge == null)
        {
            Debug.LogError("WorkbenchComponentPanel: WorkbenchInventoryBridge не назначен!");
            return;
        }

        // Спавним предмет на верстак
        GameObject spawnedObject = bridge.SpawnItemOnWorkbench(itemId);

        if (spawnedObject != null)
        {
            Debug.Log($"WorkbenchComponentPanel: Заспавнен предмет {itemId}");
            // RefreshItems() вызовется автоматически через событие OnInventoryChanged
        }
    }

    /// <summary>
    /// Обработчик изменения чекбокса "Показывать все"
    /// </summary>
    private void OnShowAllToggleChanged(bool isOn)
    {
        RefreshItems();
    }

    /// <summary>
    /// Обработчик изменения инвентаря
    /// </summary>
    private void OnInventoryChanged()
    {
        // Обновляем список предметов только если панель активна
        if (isPanelActive)
        {
            RefreshItems();
        }
    }

    /// <summary>
    /// Очистить все кнопки
    /// </summary>
    private void ClearButtons()
    {
        foreach (GameObject button in spawnedButtons)
        {
            if (button != null)
                Destroy(button);
        }
        spawnedButtons.Clear();
    }

    private void OnDestroy()
    {
        // Отписываемся от событий
        if (showAllToggle != null)
        {
            showAllToggle.onValueChanged.RemoveListener(OnShowAllToggleChanged);
        }
    }
}
