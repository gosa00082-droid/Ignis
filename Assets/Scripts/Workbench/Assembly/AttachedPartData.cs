using System;

/// <summary>
/// Данные о прикрепленной детали в сборке
/// </summary>
[Serializable]
public class AttachedPartData
{
    public string socketId;           // ID сокета на котором прикреплена деталь
    public string partItemId;         // ID детали (из WorkbenchPart.PartId)
    public float insertionProgress;   // Прогресс вставки (0-1)
    public string parentPartId;       // ID родительской детали (на которой находится сокет)
}
