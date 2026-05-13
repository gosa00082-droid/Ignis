using System;
using System.Collections.Generic;

/// <summary>
/// Снимок состояния сборки для сохранения в инвентарь
/// </summary>
[Serializable]
public class AssemblySnapshot
{
    public string baseItemId;                    // Базовый ID результата (например "sword")
    public List<AttachedPartData> parts;         // Список всех прикрепленных деталей
    public List<string> tags;                    // Теги всех компонентов
    public float completionProgress;             // Прогресс завершения (0-1)

    public AssemblySnapshot()
    {
        parts = new List<AttachedPartData>();
        tags = new List<string>();
        completionProgress = 0f;
    }
}
