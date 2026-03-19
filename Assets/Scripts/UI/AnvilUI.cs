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
    [SerializeField] private GameObject anvilCanvasRoot;
    [SerializeField] private Transform recipeListContent;
    [SerializeField] private GameObject recipeSlotPrefab;
    [SerializeField] private Transform craftSlotsParent;
    [SerializeField] private GameObject craftSlotPrefab;
    [SerializeField] private Button toolsButton, weaponsButton, decorationsButton;

    [Header("Крафт")]
    [SerializeField] private Button craftButton;
    [SerializeField] private TMP_Text successText;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private TMP_Text recipeNameText;
    [SerializeField] private TMP_Text resultInventoryCountText;

    private RecipeData selectedRecipe = null;
    private int craftCount = 0;

    [Header("Цвета слотов")]
    [SerializeField] private Color availableColor = new Color(1f, 1f, 0.5f, 0.5f);
    [SerializeField] private Color missingColor = new Color(1f, 0.2f, 0.2f, 0.5f);

    [SerializeField] private Button kritsaButton;

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

        toolsButton.onClick.AddListener(() => ShowCategory(CraftCategory.Tools));
        weaponsButton.onClick.AddListener(() => ShowCategory(CraftCategory.Weapons));
        decorationsButton.onClick.AddListener(() => ShowCategory(CraftCategory.Decorations));
        kritsaButton.onClick.AddListener(() => ShowCategory(CraftCategory.Kritsa));



        ShowCategory(CraftCategory.Weapons);

        if (craftButton != null)
        {
            craftButton.onClick.AddListener(TryCraft);
            craftButton.interactable = false;
        }

        if (successText != null) successText.text = "";
        if (errorText != null) errorText.text = "";
    }

    public void OpenKritsaCategory()
    {
        ShowCategory(CraftCategory.Kritsa);
    }

    public void ToggleAnvil()
    {
        if (UIManager.Instance.TryOpenUI(anvilCanvasRoot))
        {
            ShowCategory(currentCategory);
            ClearCraftSlots();
            UpdateCraftSlotAmounts();
            ClearFeedbackTexts();
            ResetCraftCount();
        }
    }

    private void OnDisable()
    {
        ClearRecipeList();
        ClearCraftSlots();
        selectedRecipe = null;
        ClearFeedbackTexts();
        ResetCraftCount();
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

    private void ShowCategory(CraftCategory category)
    {
        currentCategory = category;
        ClearRecipeList();
        ClearCraftSlots();
        ClearFeedbackTexts();
        ResetCraftCount();
        selectedRecipe = null;

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
        ClearCraftSlots();
        ClearFeedbackTexts();
        ResetCraftCount();

        if (recipeNameText != null)
        {
            recipeNameText.text = recipe.recipeName;
            Debug.Log($"Установлено название рецепта: {recipe.recipeName}");
        }

        UpdateResultInventoryCount();
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

            Image icon = slot.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null && req.item.icon != null)
                icon.sprite = req.item.icon;

            TMP_Text nameText = slot.transform.Find("Name")?.GetComponent<TMP_Text>();
            if (nameText != null)
                nameText.text = req.item.itemName;

            TMP_Text amountText = slot.transform.Find("Amount")?.GetComponent<TMP_Text>();
            int have = playerInventory.GetCount(req.item.id);
            if (amountText != null)
                amountText.text = $"{have}/{req.amount}";

            Image bg = slot.GetComponent<Image>();
            if (bg != null)
                bg.color = (have >= req.amount) ? availableColor : missingColor;

            if (have < req.amount)
                canCraft = false;

            spawnedCraftSlots.Add(slot);
        }

        if (craftButton != null)
        {
            craftButton.interactable = canCraft;
            Debug.Log($"Кнопка крафта активна: {canCraft}");
        }

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
                successText.text = "";
            return;
        }

        foreach (var req in selectedRecipe.requiredItems)
        {
            playerInventory.RemoveItem(req.item.id, req.amount);
        }

        playerInventory.AddItem(selectedRecipe.resultItem.id, selectedRecipe.resultAmount);
        UpdateResultInventoryCount();

        if (TutorialManager.Instance != null)
        {
            // Передаем ID созданного предмета
            TutorialManager.Instance.CheckGoals(selectedRecipe.resultItem.id, 1, GoalType.CraftItem);
        }

        craftCount += selectedRecipe.resultAmount;
        if (successText != null)
        {
            successText.text = $"Создано: {selectedRecipe.resultItem.itemName} × {craftCount}";
        }

        if (errorText != null)
            errorText.text = "";

        UpdateCraftSlotAmounts();

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
            int have = playerInventory.GetCount(req.item.id);

            TMP_Text amountText = slot.transform.Find("Amount")?.GetComponent<TMP_Text>();
            if (amountText != null)
            {
                amountText.text = $"{have}/{req.amount}";
            }

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

        if (craftButton != null) craftButton.interactable = false;
        ClearFeedbackTexts();
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