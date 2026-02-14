using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class QuestBoardUI : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private QuestDatabase questDatabase;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private Transform availableContent;
    [SerializeField] private GameObject questSlotPrefab;
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private Image detailsItemIcon;

    [Header("Вкладки")]
    [SerializeField] private Button availableTabButton;
    [SerializeField] private Button activeTabButton;

    [Header("Панели")]
    [SerializeField] private GameObject availablePanel;
    [SerializeField] private GameObject activePanel;
    [SerializeField] private GameObject detailsPanel;

    [Header("Корень интерфейса")]
    [SerializeField] private GameObject questBoardRoot;  // ← перетащи сюда основной Canvas или Panel доски заказов

    [Header("Детали выбранного")]
    [SerializeField] private TMP_Text detailsTitle;
    [SerializeField] private TMP_Text detailsCustomer;
    [SerializeField] private TMP_Text detailsReward;
    [SerializeField] private TMP_Text detailsTime;
    [SerializeField] private TMP_Text detailsDescription;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button backButton;

    [Header("Активные слоты")]
    [SerializeField] private Transform[] activeSlotParents = new Transform[2];
    [SerializeField] private GameObject activeSlotPrefab;

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

    public void ToggleQuestBoard()
    {
        if (questBoardRoot == null)
        {
            Debug.LogError("[QuestBoardUI] questBoardRoot не назначен!");
            return;
        }

        Debug.Log("[QuestBoardUI] Пытаемся открыть доску заказов. Текущий UI в UIManager: " +
                  (UIManager.Instance.currentUI != null ? UIManager.Instance.currentUI.name : "ничего"));

        bool success = UIManager.Instance.TryOpenUI(questBoardRoot);

        if (success)
        {
            Debug.Log("[QuestBoardUI] Успешно открыли questBoardRoot: " + questBoardRoot.name);
            ShowTab(true);
            RefreshAvailableQuests();
            RefreshActiveQuests();
        }
        else
        {
            Debug.LogWarning("[QuestBoardUI] Не удалось открыть — другой UI уже активен или ошибка");
        }
    }

    private void OnDisable()
    {
        detailsPanel.SetActive(false);
        availablePanel.SetActive(false);
        activePanel.SetActive(false);
    }

    private void ShowTab(bool showAvailable)
    {
        availablePanel.SetActive(showAvailable);
        activePanel.SetActive(!showAvailable);
        detailsPanel.SetActive(false);

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

                ItemData reqItem = itemDatabase.GetItem(aq.data.requiredItemID);
                string itemName = (reqItem != null) ? reqItem.itemName : aq.data.requiredItemID;
                slot.transform.Find("RequiredAmount").GetComponent<TMP_Text>().text = $"{aq.data.requiredAmount} x {itemName}";

                Image iconImg = slot.transform.Find("RequiredItemIcon").GetComponent<Image>();
                if (iconImg != null && reqItem != null && reqItem.icon != null)
                    iconImg.sprite = reqItem.icon;

                int slotIndex = i;
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

            if (quests.Count != spawnedActiveSlots.Count)
            {
                RefreshActiveQuests();
                return;
            }

            for (int i = 0; i < quests.Count; i++)
            {
                if (i >= spawnedActiveSlots.Count) continue;

                ActiveQuest aq = quests[i];

                TMP_Text timerText = spawnedActiveSlots[i].transform.Find("Timer")?.GetComponent<TMP_Text>();
                if (timerText != null)
                    timerText.text = $"Осталось: {aq.GetFormattedTime()}";

                Button submitBtn = spawnedActiveSlots[i].transform.Find("SubmitButton")?.GetComponent<Button>();
                if (submitBtn != null)
                {
                    bool canSubmit = playerInventory.GetCount(aq.data.requiredItemID) >= aq.data.requiredAmount;
                    submitBtn.interactable = canSubmit && !aq.IsFailed();
                }
            }
        }
    }

    private void ShowQuestDetails(QuestData quest)
    {
        selectedQuest = quest;
        detailsPanel.SetActive(true);
        availablePanel.SetActive(false);

        ItemData reqItem = itemDatabase.GetItem(quest.requiredItemID);
        if (detailsItemIcon != null && reqItem != null && reqItem.icon != null)
            detailsItemIcon.sprite = reqItem.icon;

        float totalMinutes = quest.timeLimitHours;
        int initMinutes = Mathf.FloorToInt(totalMinutes);
        int initSeconds = Mathf.FloorToInt((totalMinutes - initMinutes) * 60f);
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
        availablePanel.SetActive(true);
        selectedQuest = null;
    }

    private void AcceptSelectedQuest()
    {
        if (selectedQuest != null)
        {
            QuestManager.Instance.AcceptQuest(selectedQuest);
            ShowAvailableList();
            RefreshAvailableQuests();
            ShowTab(false);
        }
    }
}