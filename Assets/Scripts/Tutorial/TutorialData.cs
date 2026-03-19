using UnityEngine;
using System.Collections.Generic;

// Типы целей
public enum GoalType
{
    None,
    CollectItem,
    CraftItem,
    StartQuest,
    CompleteQuest,
    Interact
}

[System.Serializable]
public class TutorialGoal
{
    public GoalType type;
    public string targetID;
    public int requiredAmount = 1;
    [TextArea] public string description;
    [HideInInspector] public bool isCompleted = false;
}

[CreateAssetMenu(fileName = "New Tutorial Step", menuName = "Ignis/Tutorial Step")]
public class TutorialStep : ScriptableObject
{
    [Header("Настройки Шага")]
    public bool isFinalStep = false; // Галочка: последний ли это шаг?

    [Header("Слайды (Тексты диалогов)")]
    // Сюда пишем тексты. Размер массива = кол-во слайдов.
    [TextArea(3, 10)] public List<string> slides = new List<string>();

    [Header("Цели (Выполняются для завершения шага)")]
    public List<TutorialGoal> goals = new List<TutorialGoal>();

    // Проверка выполнения всех целей
    public bool IsGoalsComplete()
    {
        if (goals.Count == 0) return true; // Если целей нет — считаем выполненным сразу

        foreach (var goal in goals)
        {
            if (!goal.isCompleted) return false;
        }
        return true;
    }
}