using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;

public class FurnaceUI : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private Transform miniInventoryContent;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private GameObject furnaceCanvasRoot;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;
    [SerializeField] private AlloyDatabase alloyDatabase;

    [Header("Какие руды можно плавить")]
    [SerializeField] private List<string> smeltableItemIds = new List<string>();

    [Header("Настройки")]
    [SerializeField] private float heatPerCoal = 300f;
    [SerializeField] private float temperatureDecay = 50f;
    [SerializeField] private float maxTemperature = 2000f;

    [Header("UI Элементы")]
    [SerializeField] private Button igniteButton;
    [SerializeField] private Button bellowsButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Slider tempBar;
    [SerializeField] private Slider brokBar;
    [SerializeField] private Slider readinessBar;
    [SerializeField] private TMP_Text previewName;
    [SerializeField] private TMP_Text previewDescription;
    [SerializeField] private TMP_Text coalText;
    [SerializeField] private TMP_Text tempLabel;
    [SerializeField] private TMP_Text noCoalWarning;
    [SerializeField] private Image previewIcon;
    [SerializeField] private Transform[] oreSlots = new Transform[4];

    private List<GameObject> spawnedSlots = new List<GameObject>();
    private bool isSmelting = false;
    private bool miniGameActive = false;
    private float currentTemperature = 0f;
    private float brokProgress = 0f;
    private float readinessProgress = 0f;
    private AlloyRecipe currentRecipe = null;
    private float timer = 0f;
    private const float decayInterval = 0.2f;

    private void Start()
    {
        igniteButton.onClick.AddListener(OnIgniteButtonPressed);
        bellowsButton.onClick.AddListener(OnBellowsPressed);
        cancelButton.onClick.AddListener(CancelSmelting);
        bellowsButton.gameObject.SetActive(false);
        cancelButton.gameObject.SetActive(false);
        noCoalWarning.gameObject.SetActive(false);
    }

    public void OpenFurnace()
    {
        furnaceCanvasRoot.SetActive(true);
        RefreshMiniInventory();
        UpdatePreview();
        UpdateCoalText();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        playerMovement.enabled = false;
        mouseLook.enabled = false;
    }

    public void CloseFurnace()
    {
        furnaceCanvasRoot.SetActive(false);

        if (isSmelting && readinessProgress < 0.3f)
        {
            ReturnOresToInventory();
        }

        isSmelting = false;
        miniGameActive = false;
        ClearMiniInventory();
        noCoalWarning.gameObject.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        playerMovement.enabled = true;
        mouseLook.enabled = true;
    }

    private void RefreshMiniInventory()
    {
        ClearMiniInventory();

        var materialsDict = playerInventory.GetMaterials();

        // ← добавь это
        Debug.Log("Все id материалов в инвентаре: " + string.Join(", ", materialsDict.Keys));

        foreach (var kvp in materialsDict)
        {
            if (kvp.Value <= 0) continue;
            if (!smeltableItemIds.Any(id => string.Equals(id, kvp.Key, StringComparison.OrdinalIgnoreCase))) continue;

            ItemData item = itemDatabase.GetItem(kvp.Key);
            if (item == null) continue;

            GameObject slot = Instantiate(slotPrefab, miniInventoryContent);

            Image iconImage = slot.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null && item.icon != null) iconImage.sprite = item.icon;

            TMP_Text nameText = slot.transform.Find("Name")?.GetComponent<TMP_Text>();
            if (nameText != null) nameText.text = item.itemName;

            TMP_Text amountText = slot.transform.Find("Amount")?.GetComponent<TMP_Text>();
            if (amountText != null) amountText.text = kvp.Value.ToString();

            DraggableItem draggable = slot.GetComponent<DraggableItem>() ?? slot.AddComponent<DraggableItem>();
            draggable.homeParent = miniInventoryContent;
            draggable.itemId = item.id;  // ← запоминаем id сразу

            spawnedSlots.Add(slot);
        }
    }

    private void ClearMiniInventory()
    {
        foreach (var slot in spawnedSlots) if (slot != null) Destroy(slot.gameObject);
        spawnedSlots.Clear();
    }

    public void UpdatePreview()
    {
        List<string> loadedOres = new List<string>();
        for (int i = 0; i < 4; i++)
        {
            if (oreSlots[i].childCount > 0)
            {
                DraggableItem slotDraggable = oreSlots[i].GetChild(0).GetComponent<DraggableItem>();
                if (slotDraggable != null && !string.IsNullOrEmpty(slotDraggable.itemId))
                    loadedOres.Add(slotDraggable.itemId);
            }
        }

        currentRecipe = alloyDatabase.GetRecipe(loadedOres);

        if (currentRecipe == null)
        {
            previewName.text = "Неизвестный сплав";
            previewDescription.text = "Такого сплава не бывает";
            previewIcon.gameObject.SetActive(false);
            igniteButton.interactable = false;
        }
        else
        {
            previewName.text = currentRecipe.alloyName;
            previewDescription.text = $"{currentRecipe.description}\nУголь: {currentRecipe.coalRequired}\nДиапазон: {currentRecipe.minTemp}-{currentRecipe.maxTemp}°C\nВремя: {currentRecipe.smeltDuration} сек";
            previewIcon.sprite = currentRecipe.resultItem.icon;
            previewIcon.gameObject.SetActive(true);
            igniteButton.interactable = true;
        }
    }

    public void OnIgniteButtonPressed()
    {
        if (isSmelting || miniGameActive || currentRecipe == null) return;

        if (playerInventory.GetCount("Coal") < currentRecipe.coalRequired)
        {
            noCoalWarning.gameObject.SetActive(true);
            return;
        }

        playerInventory.RemoveItem("Coal", currentRecipe.coalRequired);
        UpdateCoalText();

        isSmelting = true;
        miniGameActive = true;
        ConsumeOresFromSlots();
        currentTemperature = 0f;
        brokProgress = 0f;
        readinessProgress = 0f;
        tempBar.value = 0f;
        tempLabel.text = "0°C";
        brokBar.value = 0f;
        readinessBar.value = 0f;
        DisableSlotDragging(true);
        bellowsButton.gameObject.SetActive(true);
        cancelButton.gameObject.SetActive(true);
        igniteButton.interactable = false;
        noCoalWarning.gameObject.SetActive(false);
    }

    private void OnBellowsPressed()
    {
        currentTemperature += heatPerCoal;
        currentTemperature = Mathf.Min(currentTemperature, maxTemperature);
        tempBar.value = currentTemperature / maxTemperature;
        tempLabel.text = Mathf.RoundToInt(currentTemperature) + "°C";
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) CloseFurnace();

        if (!miniGameActive) return;

        timer += Time.deltaTime;
        if (timer >= decayInterval)
        {
            currentTemperature -= temperatureDecay * decayInterval;
            currentTemperature = Mathf.Max(currentTemperature, 0f);
            tempBar.value = currentTemperature / maxTemperature;
            tempLabel.text = Mathf.RoundToInt(currentTemperature) + "°C";
            timer = 0f;
        }

        float deltaTime = Time.deltaTime / currentRecipe.smeltDuration;

        if (currentTemperature < currentRecipe.minTemp || currentTemperature > currentRecipe.maxTemp)
        {
            brokProgress += deltaTime;
            brokBar.value = brokProgress;
            if (brokProgress >= 1f)
            {
                FinishWithSlag();
            }
        }
        else
        {
            readinessProgress += deltaTime;
            readinessBar.value = readinessProgress;
            if (readinessProgress >= 1f)
            {
                FinishSmelting();
            }
        }

        cancelButton.gameObject.SetActive(readinessProgress < 0.3f);
    }

    private void FinishSmelting()
    {
        miniGameActive = false;
        isSmelting = false;
        playerInventory.AddItem(currentRecipe.resultItem.id, 1);
        previewDescription.text = "Готово: " + currentRecipe.alloyName;
        ResetFurnace();
    }

    private void FinishWithSlag()
    {
        miniGameActive = false;
        isSmelting = false;
        playerInventory.AddItem(currentRecipe.slagItem.id, 1);
        previewDescription.text = "Брак: шлак";
        ResetFurnace();
    }

    private void ResetFurnace()
    {
        bellowsButton.gameObject.SetActive(false);
        cancelButton.gameObject.SetActive(false);
        igniteButton.interactable = true;
        DisableSlotDragging(false);
        ClearFurnaceSlots();
        UpdatePreview();
        tempLabel.text = "0°C";
        brokBar.value = 0f;
        readinessBar.value = 0f;

        RefreshMiniInventory();
    }

    private void CancelSmelting()
    {
        if (!isSmelting || readinessProgress >= 0.3f) return;

        ReturnOresToInventory();  // Возврат руд

        // Полный сброс состояния плавки
        isSmelting = false;
        miniGameActive = false;
        currentTemperature = 0f;
        brokProgress = 0f;
        readinessProgress = 0f;
        tempBar.value = 0f;
        tempLabel.text = "0°C";
        brokBar.value = 0f;
        readinessBar.value = 0f;

        bellowsButton.gameObject.SetActive(false);
        cancelButton.gameObject.SetActive(false);
        igniteButton.interactable = true;

        DisableSlotDragging(false);
        ClearFurnaceSlots();
        UpdatePreview();
        RefreshMiniInventory();  // Обновляем мини-инвентарь (чтобы руды появились сразу)
        previewDescription.text = "";
    }

    private void ConsumeOresFromSlots()
    {
        for (int i = 0; i < 4; i++)
        {
            if (oreSlots[i].childCount > 0)
            {
                GameObject slotObj = oreSlots[i].GetChild(0).gameObject;
                DraggableItem slotDraggable = slotObj.GetComponent<DraggableItem>();
                TMP_Text amountText = slotObj.transform.Find("Amount")?.GetComponent<TMP_Text>();

                if (slotDraggable != null && amountText != null && !string.IsNullOrEmpty(slotDraggable.itemId))
                {
                    playerInventory.RemoveItem(slotDraggable.itemId, 1);
                    int newAmount = int.Parse(amountText.text) - 1;
                    amountText.text = newAmount.ToString();
                    if (newAmount <= 0) Destroy(slotObj);
                }
            }
        }
        UpdatePreview();
    }

    private void ReturnOresToInventory()
    {
        for (int i = 0; i < 4; i++)
        {
            if (oreSlots[i].childCount > 0)
            {
                GameObject slotObj = oreSlots[i].GetChild(0).gameObject;
                DraggableItem slotDraggable = slotObj.GetComponent<DraggableItem>();
                TMP_Text amountText = slotObj.transform.Find("Amount")?.GetComponent<TMP_Text>();

                if (slotDraggable != null && amountText != null && !string.IsNullOrEmpty(slotDraggable.itemId))
                {
                    playerInventory.AddItem(slotDraggable.itemId, 1);
                    int newAmount = int.Parse(amountText.text) + 1;
                    amountText.text = newAmount.ToString();
                }
                Destroy(slotObj);
            }
        }
    }

    private void DisableSlotDragging(bool disable)
    {
        for (int i = 0; i < 4; i++)
        {
            if (oreSlots[i].childCount > 0)
            {
                DraggableItem draggable = oreSlots[i].GetChild(0).GetComponent<DraggableItem>();
                if (draggable != null) draggable.enabled = !disable;
            }
        }
    }

    private void ClearFurnaceSlots()
    {
        for (int i = 0; i < 4; i++)
        {
            if (oreSlots[i].childCount > 0)
                Destroy(oreSlots[i].GetChild(0).gameObject);
        }
    }

    private void UpdateCoalText()
    {
        coalText.text = "Уголь: " + playerInventory.GetCount("Coal");
    }
}