using UnityEngine;
using System.Collections.Generic;

// Типы целей
public enum GoalType
{
    None,           // Нет цели (просто текст)
    CollectItem,    // Собрать предмет
    CraftItem,      // Скрафтить предмет
    CompleteQuest,  // Сдать квест
    Interact        // Взаимодействовать с объектом (по Тегу)
}

// Класс одной цели (подзадачи)
[System.Serializable]
public class TutorialGoal
{
    public GoalType type;           // Тип цели
    public string targetID;         // ID предмета или квеста (или Тег для Interact)
    public int requiredAmount = 1;  // Сколько нужно
    [TextArea] public string description; // Текст для игрока (например, "Найди 3 железа")

    [HideInInspector] public bool isCompleted = false; // Статус (меняет менеджер)
}

// Сам шаг обучения (ScriptableObject)
[CreateAssetMenu(fileName = "New Tutorial Step", menuName = "Ignis/Tutorial Step")]
public class TutorialStep : ScriptableObject
{
    [Header("Текст слайда")]
    [TextArea(3, 10)] public string dialogueText; // Текст на панели

    [Header("Цели и подзадачи")]
    public List<TutorialGoal> goals = new List<TutorialGoal>(); // Список целей

    // Проверка, выполнены ли все цели в этом шаге
    public bool IsComplete()
    {
        foreach (var goal in goals)
        {
            if (!goal.isCompleted) return false;
        }
        return true;
    }
}