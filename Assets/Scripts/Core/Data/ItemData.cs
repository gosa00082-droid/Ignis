// Assets/Scripts/Items/ItemData.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Item", menuName = "Ignis/Item")]
public class ItemData : ScriptableObject
{
    [Header("Основное")]
    public string itemName = "Новый предмет";      // как будет отображаться игроку
    public string id;                               // уникальный короткий идентификатор

    [Header("Категория и внешний вид")]
    public ItemCategory category;                   // категория предмета
    public Sprite icon;                             // иконка для UI

    [Header("Данные для игры")]
    [Tooltip("Сколько штук считается одним стеком")]
    public int maxStackSize = 99;                   // стек

    [Tooltip("Базовая цена покупки")]
    public int basePrice = 10;                      // базовая цена

    [TextArea(3, 6)]
    public string description = "Описание предмета..."; // описание

    [Header("Workbench System")]
    [Tooltip("Теги компонентов для системы верстака")]
    public List<string> tags = new List<string>();
}