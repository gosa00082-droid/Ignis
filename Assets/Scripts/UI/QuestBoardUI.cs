using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class QuestBoardUI : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private QuestDatabase questDatabase;
    [SerializeField] private ItemDatabase itemDatabase;  // ИСПРАВЛЕНО: Добавлено для получения имени предмета
    [SerializeField] private Transform availableContent;     // Content доступных
    [SerializeField] private GameObject questSlotPrefab;     // префаб слота доступных
    [SerializeField] private Inventory playerInventory;  // для проверки сдачи
    [SerializeField] private Image detailsItemIcon;     // иконка в detailsPanel

    [Header("Вкладки")]
    [SerializeField] private Button availableTabButton;
    [SerializeField] private Button activeTabButton;

    [Header("Панели")]
    [SerializeField] private GameObject availablePanel;
    [SerializeField] private GameObject activePanel;
    [SerializeField] private GameObject detailsPanel;

    [Header("Детали выбранного")]
    [SerializeField] private TMP_Text detailsTitle;
    [SerializeField] private TMP_Text detailsCustomer;
    [SerializeField] private TMP_Text detailsReward;
    [SerializeField] private TMP_Text detailsTime;
    [SerializeField] private TMP_Text detailsDescription;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button backButton;

    [Header("Активные слоты")]
    [SerializeField] private Transform[] activeSlotParents = new Transform[2];  // Slot1 и Slot2
    [SerializeField] private GameObject activeSlotPrefab;  // префаб для активного слота

    [Header("Блокировка")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;

    private QuestData selectedQuest = null;
    private List<GameObject> spawnedActiveSlots = new List<GameObject>();

    private void Start()
    {
        availableTabButton.onClick.AddListener(() => ShowTab(true));
        activeTabButton.onClick.AddListener(() => ShowTab(false));

        acceptButton.onClick.AddListener(AcceptSelectedQuest);
        backButton.onClick.AddListener(ShowAvailableList);

        ShowTab(true);
    }

    public void OpenQuestBoard()
    {
        gameObject.SetActive(true);
        ShowTab(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        playerMovement.enabled = false;
        mouseLook.enabled = false;
    }

    public void CloseQuestBoard()
    {
        gameObject.SetActive(false);
        detailsPanel.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        playerMovement.enabled = true;
        mouseLook.enabled = true;
    }

    private void ShowTab(bool showAvailable)
    {
        availablePanel.SetActive(showAvailable);
        activePanel.SetActive(!showAvailable);

        // ИСПРАВЛЕНО: Скрываем панель деталей при переключении вкладок
        detailsPanel.SetActive(false);

        // Подсветка (замени цвета на свои)
        availableTabButton.image.color = showAvailable ? new Color(0.4f, 0.4f, 0.6f) : Color.gray;
        activeTabButton.image.color = !showAvailable ? new Color(0.4f, 0.4f, 0.6f) : Color.gray;

        if (showAvailable)
            RefreshAvailableQuests();
        else
            RefreshActiveQuests();
    }

    private void RefreshAvailableQuests()
    {
        foreach (Transform child in availableContent) Destroy(child.gameObject);

        foreach (var quest in questDatabase.GetAllQuests())
        {
            CreateAvailableSlot(quest);
        }
    }

    private void CreateAvailableSlot(QuestData quest)
    {
        GameObject slot = Instantiate(questSlotPrefab, availableContent);
        slot.transform.Find("Title").GetComponent<TMP_Text>().text = quest.title;
        slot.transform.Find("Reward").GetComponent<TMP_Text>().text = $"{quest.rewardGold} зол.";

        slot.GetComponent<Button>().onClick.AddListener(() => ShowQuestDetails(quest));
    }

    private void RefreshActiveQuests()
    {
        foreach (var slot in spawnedActiveSlots) Destroy(slot);
        spawnedActiveSlots.Clear();

        var quests = QuestManager.Instance.GetActiveQuests();
        for (int i = 0; i < 2; i++)
        {
            if (i < quests.Count)
            {
                GameObject slot = Instantiate(activeSlotPrefab, activeSlotParents[i]);
                ActiveQuest aq = quests[i];

                slot.transform.Find("Title").GetComponent<TMP_Text>().text = aq.data.title;
                slot.transform.Find("Description").GetComponent<TMP_Text>().text = aq.data.description;

                // ИСПРАВЛЕНО: Заменяем ID на название предмета
                ItemData reqItem = itemDatabase.GetItem(aq.data.requiredItemID);
                string itemName = (reqItem != null) ? reqItem.itemName : aq.data.requiredItemID;
                slot.transform.Find("RequiredAmount").GetComponent<TMP_Text>().text = $"{aq.data.requiredAmount} x {itemName}";

                Image iconImg = slot.transform.Find("RequiredItemIcon").GetComponent<Image>();
                if (iconImg != null && reqItem != null && reqItem.icon != null)
                    iconImg.sprite = reqItem.icon;

                // Icon: slot.transform.Find("RequiredItemIcon").GetComponent<Image>().sprite = reqItem?.icon;

                int slotIndex = i;  // захват
                slot.transform.Find("SubmitButton").GetComponent<Button>().onClick.AddListener(() =>
                {
                    QuestManager.Instance.SubmitQuest(slotIndex);
                    RefreshActiveQuests();
                });

                spawnedActiveSlots.Add(slot);
            }
        }
    }

    private void Update()
    {
        if (activePanel.activeSelf)
        {
            var quests = QuestManager.Instance.GetActiveQuests();

            // Ключевой фикс: если количество активных заказов изменилось (провалился) — сразу перестраиваем UI
            if (quests.Count != spawnedActiveSlots.Count)
            {
                RefreshActiveQuests();
                return; // после перестройки дальше не идём в этом кадре
            }

            // Обновляем существующие слоты
            for (int i = 0; i < quests.Count; i++)
            {
                if (i >= spawnedActiveSlots.Count) continue; // защита

                ActiveQuest aq = quests[i];

                // Таймер
                TMP_Text timerText = spawnedActiveSlots[i].transform.Find("Timer")?.GetComponent<TMP_Text>();
                if (timerText != null)
                    timerText.text = $"Осталось: {aq.GetFormattedTime()}";

                // Кнопка сдачи
                Button submitBtn = spawnedActiveSlots[i].transform.Find("SubmitButton")?.GetComponent<Button>();
                if (submitBtn != null)
                {
                    bool canSubmit = playerInventory.GetCount(aq.data.requiredItemID) >= aq.data.requiredAmount;
                    submitBtn.interactable = canSubmit && !aq.IsFailed();
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            CloseQuestBoard();
    }

    private void ShowQuestDetails(QuestData quest)
    {
        selectedQuest = quest;
        detailsPanel.SetActive(true);
        availablePanel.SetActive(false);

        ItemData reqItem = itemDatabase.GetItem(quest.requiredItemID);
        if (detailsItemIcon != null && reqItem != null && reqItem.icon != null)
            detailsItemIcon.sprite = reqItem.icon;

        // Исправлено: Полный срок в минутах:секундах (игровые минуты, где 1 час = 60 мин)
        float totalMinutes = quest.timeLimitHours;  // часы в минуты
        int initMinutes = Mathf.FloorToInt(totalMinutes);
        int initSeconds = Mathf.FloorToInt((totalMinutes - initMinutes) * 60f);  // дробные минуты в секунды
        detailsTime.text = $"Срок: {initMinutes:00}:{initSeconds:00}";

        detailsTitle.text = quest.title;
        detailsCustomer.text = $"Заказчик: {quest.customerName}";
        detailsReward.text = $"Награда: {quest.rewardGold} золота";
        detailsDescription.text = quest.description;

        acceptButton.interactable = QuestManager.Instance.HasFreeSlot();
    }

    private void ShowAvailableList()
    {
        detailsPanel.SetActive(false);
        availablePanel.SetActive(true);  // ИСПРАВЛЕНО: Возвращаем список доступных
        selectedQuest = null;
    }

    private void AcceptSelectedQuest()
    {
        if (selectedQuest != null)
        {
            QuestManager.Instance.AcceptQuest(selectedQuest);
            ShowAvailableList();
            RefreshAvailableQuests();  // обновить список

            // ИСПРАВЛЕНО: Автопереход на вкладку активных
            ShowTab(false);
        }
    }
}