using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AnvilUI : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private RecipeDatabase recipeDatabase;
    [SerializeField] private GameObject anvilCanvasRoot;  // Перетащи AnvilCanvas
    [SerializeField] private Transform recipeListContent; // Content для списка рецептов (ScrollView)
    [SerializeField] private GameObject recipeSlotPrefab; // Префаб слота рецепта (Button с Text)
    [SerializeField] private Transform craftSlotsParent;  // Grid для слотов компонентов
    [SerializeField] private GameObject craftSlotPrefab;  // Префаб слота компонента (Icon + Name + Amount)
    [SerializeField] private Button toolsButton, weaponsButton, decorationsButton;
    [Header("Блокировка")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;

    // В начало класса, после других [SerializeField]
    [Header("Крафт")]
    [SerializeField] private Button craftButton;
    [SerializeField] private TMP_Text successText;      // ← текст успеха (зелёный)
    [SerializeField] private TMP_Text errorText;        // ← текст ошибок (красный)
    [SerializeField] private TMP_Text recipeNameText;               // Название рецепта
    [SerializeField] private TMP_Text resultInventoryCountText;     // Количество результата в инвентаре

    private RecipeData selectedRecipe = null;   // текущий выбранный рецепт
    private int craftCount = 0;  // сколько раз успешно скрафтили текущий рецепт

    [Header("Цвета слотов")]
    [SerializeField] private Color availableColor = new Color(1f, 1f, 0.5f, 0.5f); // Желтый
    [SerializeField] private Color missingColor = new Color(1f, 0.2f, 0.2f, 0.5f); // Красный

    private CraftCategory currentCategory = CraftCategory.Tools;
    private List<GameObject> spawnedRecipeSlots = new List<GameObject>();
    private List<GameObject> spawnedCraftSlots = new List<GameObject>();
    private bool isCrafting = false;

    private void Start()
    {
        if (recipeDatabase == null)
        {
            Debug.LogError("AnvilUI: RecipeDatabase НЕ НАЗНАЧЕН в инспекторе!");
            return;
        }

        if (recipeNameText != null) recipeNameText.text = "Выберите рецепт";
        if (resultInventoryCountText != null) resultInventoryCountText.text = "";

        // Подключаем вкладки
        toolsButton.onClick.AddListener(() => ShowCategory(CraftCategory.Tools));
        weaponsButton.onClick.AddListener(() => ShowCategory(CraftCategory.Weapons));
        decorationsButton.onClick.AddListener(() => ShowCategory(CraftCategory.Decorations));

        ShowCategory(CraftCategory.Weapons);  // По умолчанию

        if (craftButton != null)
        {
            craftButton.onClick.AddListener(TryCraft);
            craftButton.interactable = false;   // изначально неактивна
        }

        if (successText != null) successText.text = "";
        if (errorText != null) errorText.text = "";
    }

    private void ClearFeedbackTexts()
    {
        if (successText != null) successText.text = "";
        if (errorText != null) errorText.text = "";
    }

    private void ResetCraftCount()
    {
        craftCount = 0;
        if (successText != null) successText.text = "";
        if (recipeNameText != null) recipeNameText.text = "Выберите рецепт";
        UpdateResultInventoryCount();
    }

    public void OpenAnvil()
    {
        anvilCanvasRoot.SetActive(true);
        ShowCategory(currentCategory);  // Обновить список

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        playerMovement.enabled = false;
        mouseLook.enabled = false;
        ClearFeedbackTexts();   // ← добавь
        ResetCraftCount();          // ← добавь
    }

    public void CloseAnvil()
    {
        anvilCanvasRoot.SetActive(false);
        ClearRecipeList();
        ClearCraftSlots();

        selectedRecipe = null;          // ← здесь обязательно

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        playerMovement.enabled = true;
        mouseLook.enabled = true;
        ClearFeedbackTexts();   // ← добавь
        ResetCraftCount();          // ← добавь
    }

    private void ShowCategory(CraftCategory category)
    {
        currentCategory = category;
        ClearRecipeList();
        ClearCraftSlots();
        ClearFeedbackTexts();   // ← добавь
        ResetCraftCount();          // ← добавь

        selectedRecipe = null;          // ← здесь сбрасываем, когда меняем вкладку
        if (craftButton != null) craftButton.interactable = false;

        var recipes = recipeDatabase.GetRecipesByCategory(category);
        foreach (var recipe in recipes)
        {
            GameObject slot = Instantiate(recipeSlotPrefab, recipeListContent);
            TMP_Text nameText = slot.GetComponentInChildren<TMP_Text>();
            if (nameText) nameText.text = recipe.recipeName;

            Button btn = slot.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => ShowCraftSlots(recipe));
            }
            else
            {
                Debug.LogError($"В префабе рецепта {recipe.recipeName} нет компонента Button!", slot);
            }

            spawnedRecipeSlots.Add(slot);
        }
    }

    private void ShowCraftSlots(RecipeData recipe)
    {
        selectedRecipe = recipe;
        ClearCraftSlots();                  // очищаем слоты
        ClearFeedbackTexts();               // очищаем сообщения
        ResetCraftCount();                  // сбрасываем счётчик (и "Выберите рецепт")

        // ← Теперь ПЕРЕЗАПИСЫВАЕМ название рецепта и счёт в инвентаре
        if (recipeNameText != null)
        {
            recipeNameText.text = recipe.recipeName;   // ← вот здесь название появляется
            Debug.Log($"Установлено название рецепта: {recipe.recipeName}");  // для теста в консоли
        }

        UpdateResultInventoryCount();       // показываем сколько результата в инвентаре

        Debug.Log($"Показываем рецепт: {recipe.recipeName}. Компонентов: {recipe.requiredItems.Count}");

        bool canCraft = true;

        foreach (var req in recipe.requiredItems)
        {
            if (req.item == null)
            {
                Debug.LogError($"В рецепте {recipe.recipeName} один из requiredItems имеет item = null!");
                canCraft = false;
                continue;
            }

            Debug.Log($"Создаю слот для: {req.item.itemName} x {req.amount}");

            GameObject slot = Instantiate(craftSlotPrefab, craftSlotsParent);

            // Иконка
            Image icon = slot.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null && req.item.icon != null)
                icon.sprite = req.item.icon;

            // Название
            TMP_Text nameText = slot.transform.Find("Name")?.GetComponent<TMP_Text>();
            if (nameText != null)
                nameText.text = req.item.itemName;

            // Количество: имеющееся / требуемое
            TMP_Text amountText = slot.transform.Find("Amount")?.GetComponent<TMP_Text>();
            int have = playerInventory.GetCount(req.item.id);
            if (amountText != null)
                amountText.text = $"{have}/{req.amount}";

            // Подсветка фона
            Image bg = slot.GetComponent<Image>();
            if (bg != null)
                bg.color = (have >= req.amount) ? availableColor : missingColor;

            if (have < req.amount)
                canCraft = false;

            spawnedCraftSlots.Add(slot);
        }

        // Активируем кнопку крафта
        if (craftButton != null)
        {
            craftButton.interactable = canCraft;
            Debug.Log($"Кнопка крафта активна: {canCraft}");
        }

        // Если сразу не хватает — показываем ошибку (но не перезаписываем успех)
        if (!canCraft && errorText != null)
        {
            errorText.text = "Недостаточно материалов для этого рецепта";
        }
    }

    public void TryCraft()
    {
        Debug.Log("=== TryCraft вызван ===");

        if (isCrafting)
        {
            Debug.Log("TryCraft вызван повторно — игнорируем");
            return;
        }

        isCrafting = true;

        Debug.Log("=== TryCraft начат ===");

        if (selectedRecipe == null) return;

        bool canCraft = true;
        foreach (var req in selectedRecipe.requiredItems)
        {
            int currentCount = playerInventory.GetCount(req.item.id);
            if (currentCount < req.amount)
            {
                canCraft = false;
                break;
            }
        }

        if (!canCraft)
        {
            if (errorText != null)
                errorText.text = "Недостаточно материалов!";
            if (successText != null)
                successText.text = "";  // очищаем успех, если ошибка
            return;
        }

        // Снимаем материалы
        foreach (var req in selectedRecipe.requiredItems)
        {
            playerInventory.RemoveItem(req.item.id, req.amount);
        }

        // Добавляем результат
        playerInventory.AddItem(selectedRecipe.resultItem.id, selectedRecipe.resultAmount);

        // ← Добавь эту строку:
        UpdateResultInventoryCount();

        // Увеличиваем счётчик и обновляем текст успеха
        craftCount += selectedRecipe.resultAmount;  // если resultAmount > 1, тоже учитываем

        if (successText != null)
        {
            successText.text = $"Создано: {selectedRecipe.resultItem.itemName} × {craftCount}";
        }

        // Очищаем ошибку
        if (errorText != null)
            errorText.text = "";

        // Обновляем слоты
        UpdateCraftSlotAmounts();

        // Проверяем повторный крафт
        bool stillCanCraft = true;
        foreach (var req in selectedRecipe.requiredItems)
        {
            if (playerInventory.GetCount(req.item.id) < req.amount)
            {
                stillCanCraft = false;
                break;
            }
        }

        craftButton.interactable = stillCanCraft;

        if (!stillCanCraft && errorText != null)
            errorText.text = "Недостаточно для повторного крафта";

        isCrafting = false;
    }

    private void UpdateCraftSlotAmounts()
    {
        if (selectedRecipe == null) return;

        for (int i = 0; i < spawnedCraftSlots.Count; i++)
        {
            GameObject slot = spawnedCraftSlots[i];
            if (slot == null) continue;

            var req = selectedRecipe.requiredItems[i];

            // Пересчитываем актуальное количество в инвентаре
            int have = playerInventory.GetCount(req.item.id);

            // Обновляем текст количества
            TMP_Text amountText = slot.transform.Find("Amount")?.GetComponent<TMP_Text>();
            if (amountText != null)
            {
                amountText.text = $"{have}/{req.amount}";
            }

            // Обновляем цвет фона
            Image bg = slot.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = (have >= req.amount) ? availableColor : missingColor;
            }
        }

        UpdateResultInventoryCount();
    }

    private void ClearRecipeList()
    {
        foreach (var slot in spawnedRecipeSlots) Destroy(slot);
        spawnedRecipeSlots.Clear();
    }

    private void ClearCraftSlots()
    {
        foreach (var slot in spawnedCraftSlots) Destroy(slot);
        spawnedCraftSlots.Clear();

        // ← ЭТУ СТРОКУ УДАЛИТЬ или закомментировать
        // selectedRecipe = null;

        if (craftButton != null) craftButton.interactable = false;
        ClearFeedbackTexts();   // ← добавь
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) CloseAnvil();
    }

    private void UpdateResultInventoryCount()
    {
        if (resultInventoryCountText == null) return;

        if (selectedRecipe == null || selectedRecipe.resultItem == null)
        {
            resultInventoryCountText.text = "";
            return;
        }

        int count = playerInventory.GetCount(selectedRecipe.resultItem.id);
        resultInventoryCountText.text = $"В инвентаре: {count}";
    }
}