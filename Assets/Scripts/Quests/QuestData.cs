using UnityEngine;

[CreateAssetMenu(fileName = "NewQuest", menuName = "Ignis/Quest")]
public class QuestData : ScriptableObject
{
    public string questID;              // уникальный id
    public string title;                // название
    [TextArea(3, 6)]
    public string description;          // описание
    public string customerName;         // заказчик

    public int rewardGold;              // награда
    public float timeLimitHours;        // срок в игровых часах

    [Header("Что нужно сдать")]
    public string requiredItemID;       // id предмета
    public int requiredAmount = 1;

    [Header("Штраф")]
    public float failPenaltyPercent = 30f;  // % от награды
}