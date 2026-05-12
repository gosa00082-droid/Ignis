using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Collections;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [SerializeField] private Inventory playerInventory;  // перетащи Inventory
    [SerializeField] private ItemDatabase itemDatabase;  // перетащи

    // Добавь в начало класса, в [SerializeField]
    [Header("HUD провала")]
    [SerializeField] private GameObject failMessageCanvas;      // весь Canvas FailMessageCanvas
    [SerializeField] private TMP_Text failMessageText;          // TextMeshProUGUI внутри
    [SerializeField] private CanvasGroup failCanvasGroup;       // CanvasGroup на Canvas или Panel

    private List<ActiveQuest> activeQuests = new List<ActiveQuest>();  // max 2

    // ИСПРАВЛЕНО: Убрали completedQuestIDs — квесты теперь повторяемые

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Скрываем HUD при старте
        if (failMessageCanvas != null)
            failMessageCanvas.SetActive(false);
    }

    private void Update()
    {
        for (int i = activeQuests.Count - 1; i >= 0; i--)
        {
            activeQuests[i].remainingTimeSeconds -= Time.deltaTime; 
            if (activeQuests[i].IsFailed())
            {
                FailQuest(i);
            }
        }
    }

    public bool HasFreeSlot() => activeQuests.Count < 2;

    public void AcceptQuest(QuestData quest)
    {
        if (!HasFreeSlot()) return;

        activeQuests.Add(new ActiveQuest(quest));
        Debug.Log($"Принят заказ: {quest.title}");

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.CheckGoals(quest.questID, 1, GoalType.StartQuest);
        }
    }

    public List<ActiveQuest> GetActiveQuests() => activeQuests;

    public void SubmitQuest(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= activeQuests.Count) return;

        ActiveQuest aq = activeQuests[slotIndex];
        if (aq.IsFailed()) return;

        // Проверяем наличие предмета
        if (playerInventory.GetCount(aq.data.requiredItemID) >= aq.data.requiredAmount)
        {
            playerInventory.RemoveItem(aq.data.requiredItemID, aq.data.requiredAmount);
            playerInventory.AddItem("Gold_Money", aq.data.rewardGold);
            activeQuests.RemoveAt(slotIndex);

            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.CheckGoals(aq.data.questID, 1, GoalType.CompleteQuest);
            }

            Debug.Log($"Сдан заказ: {aq.data.title}. Награда: {aq.data.rewardGold}");
        }
        else
        {
            Debug.Log("Нет нужного предмета для сдачи!");
        }
    }

    private void FailQuest(int slotIndex)
    {
        ActiveQuest aq = activeQuests[slotIndex];
        int penalty = Mathf.RoundToInt(aq.data.rewardGold * (aq.data.failPenaltyPercent / 100f));

        int actualPenalty = Mathf.Min(penalty, playerInventory.GetCount("Gold_Money"));
        playerInventory.RemoveItem("Gold_Money", actualPenalty);

        activeQuests.RemoveAt(slotIndex);

        string msg = $"Провален заказ: {aq.data.title}\nШтраф: {actualPenalty} золота";
        Debug.Log(msg);

        // Показываем HUD прямо здесь
        if (failMessageCanvas != null && failMessageText != null && failCanvasGroup != null)
        {
            failMessageText.text = msg;
            failMessageCanvas.SetActive(true);
            StartCoroutine(ShowFailMessageCoroutine());
        }
    }

    private System.Collections.IEnumerator ShowFailMessageCoroutine()
    {
        failCanvasGroup.alpha = 0f;

        // Fade in
        float time = 0f;
        while (time < 0.5f)
        {
            time += Time.deltaTime;
            failCanvasGroup.alpha = Mathf.Lerp(0f, 1f, time / 0.5f);
            yield return null;
        }
        failCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(3f);

        // Fade out
        time = 0f;
        while (time < 0.5f)
        {
            time += Time.deltaTime;
            failCanvasGroup.alpha = Mathf.Lerp(1f, 0f, time / 0.5f);
            yield return null;
        }
        failCanvasGroup.alpha = 0f;

        failMessageCanvas.SetActive(false);
    }
}