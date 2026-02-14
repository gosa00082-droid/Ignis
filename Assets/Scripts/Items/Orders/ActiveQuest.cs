using UnityEngine;

[System.Serializable]
public class ActiveQuest
{
    public QuestData data;
    public float remainingTimeSeconds;  // остаток времени в секундах (реал)

    public ActiveQuest(QuestData questData)
    {
        data = questData;
        // 1 сек реал = 1 мин игра, 60 мин/час → timeLimitHours * 60 сек реал
        remainingTimeSeconds = questData.timeLimitHours * 60f;
    }

    public bool IsFailed() => remainingTimeSeconds <= 0;

    // ИСПРАВЛЕНО: Метод для форматирования времени в MM:SS
    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(remainingTimeSeconds / 60f);
        int seconds = Mathf.FloorToInt(remainingTimeSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
}