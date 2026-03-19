using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;
using System.Collections;

public class FurnaceUI : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private Transform miniInventoryContent;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private GameObject furnaceCanvasRoot;
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
        Debug.Log("FurnaceUI Start вызван");

        igniteButton.onClick.AddListener(OnIgniteButtonPressed);
        bellowsButton.onClick.AddListener(OnBellowsPressed);
        cancelButton.onClick.AddListener(CancelSmelting);
        bellowsButton.gameObject.SetActive(false);
        cancelButton.gameObject.SetActive(false);
        noCoalWarning.gameObject.SetActive(false);

        // АВТО-ЗАПОЛНЕНИЕ СПИСКА ПЛАВКИ (чтобы не заполнять вручную в инспекторе)
        if (smeltableItemIds.Count == 0)
        {
            smeltableItemIds = new List<string>
        {
            // Руды
            "Iron_Ore", "Copper_Ore", "Tin_Ore", "Silver_Ore", "Gold_Ore", "kr_iron1",
            // Слитки (для переработки в сталь/дамаск)
            "Iron_Ingot", "Steel_Ingot"
        };
            Debug.Log("FurnaceUI: Список плавимых предметов автоматически заполнен.");
        }
    }

    private void OnEnable()
    {
        StartCoroutine(RefreshAfterOpen());
    }

    private IEnumerator RefreshAfterOpen()
    {
        yield return null;

        RefreshMiniInventory();
        UpdatePreview();
        UpdateCoalText();
    }

    public void ToggleFurnace()
    {
        Debug.Log("ToggleFurnace вызван");

        bool opened = UIManager.Instance.TryOpenUI(furnaceCanvasRoot);
        Debug.Log("TryOpenUI результат: " + opened);

        if (opened)
        {
            Debug.Log("Заходим в RefreshMiniInventory");
            RefreshMiniInventory();
            UpdatePreview();
            UpdateCoalText();
        }
    }

    private void OnDisable()
    {
        ReturnOresToInventory();
        ClearFurnaceSlots();
    }

    private void RefreshMiniInventory()
    {
        ClearMiniInventory();
        var materialsDict = playerInventory.GetMaterials();

        Debug.Log("Все id материалов в инвентаре: " + string.Join(", ", materialsDict.Keys));

        foreach (var kvp in materialsDict)
        {
            if (kvp.Value <= 0)
                continue;

            if (!smeltableItemIds.Any(id => string.Equals(id, kvp.Key, StringComparison.OrdinalIgnoreCase)))
                continue;

            ItemData item = itemDatabase.GetItem(kvp.Key);
            if (item == null)
            {
                Debug.LogError("ItemDatabase не нашел предмет с id: " + kvp.Key);
                continue;
            }

            GameObject slot = Instantiate(slotPrefab, miniInventoryContent);

            Image iconImage = slot.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null && item.icon != null)
                iconImage.sprite = item.icon;

            TMP_Text nameText = slot.transform.Find("Name")?.GetComponent<TMP_Text>();
            if (nameText != null)
                nameText.text = item.itemName;

            TMP_Text amountText = slot.transform.Find("Amount")?.GetComponent<TMP_Text>();
            if (amountText != null)
                amountText.text = kvp.Value.ToString();

            DraggableItem draggable = slot.GetComponent<DraggableItem>() ?? slot.AddComponent<DraggableItem>();
            draggable.homeParent = miniInventoryContent;
            draggable.itemId = item.id;

            spawnedSlots.Add(slot);
        }
    }

    private void ClearMiniInventory()
    {
        foreach (var slot in spawnedSlots)
            if (slot != null) Destroy(slot.gameObject);

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

        ReturnOresToInventory();

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
        RefreshMiniInventory();
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