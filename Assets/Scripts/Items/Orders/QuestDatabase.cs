using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "QuestDatabase", menuName = "Ignis/Quest Database")]
public class QuestDatabase : ScriptableObject
{
    [SerializeField] private List<QuestData> quests = new List<QuestData>();

    private Dictionary<string, QuestData> questDict;

    private void OnEnable()
    {
        questDict = new Dictionary<string, QuestData>();
        foreach (var q in quests)
        {
            if (!string.IsNullOrEmpty(q.questID))
                questDict[q.questID] = q;
        }
    }

    public List<QuestData> GetAllQuests() => quests;

    public QuestData GetQuestById(string id) =>
        questDict.TryGetValue(id, out var q) ? q : null;

    // ИСПРАВЛЕНО: Методы для динамического управления (вызывай из других скриптов, например QuestDatabase.AddQuest(myQuestData);)
    public void AddQuest(QuestData newQuest)
    {
        if (newQuest == null || string.IsNullOrEmpty(newQuest.questID)) return;
        if (questDict.ContainsKey(newQuest.questID)) return;  // не дублируем

        quests.Add(newQuest);
        questDict[newQuest.questID] = newQuest;
    }

    public void RemoveQuest(string questID)
    {
        if (questDict.TryGetValue(questID, out var q))
        {
            quests.Remove(q);
            questDict.Remove(questID);
        }
    }

    public void ClearQuests()
    {
        quests.Clear();
        questDict.Clear();
    }
}