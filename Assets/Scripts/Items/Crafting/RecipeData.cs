using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Ignis/Recipe")]
public class RecipeData : ScriptableObject
{
    public string recipeName;           // Имя рецепта (отображается в UI)
    public CraftCategory category;      // Категория для вкладок
    public List<RequiredItem> requiredItems; // Список компонентов
    public ItemData resultItem;         // Что получается
    public int resultAmount = 1;        // Количество результата

    [System.Serializable]
    public class RequiredItem
    {
        public ItemData item;
        public int amount = 1;
    }
}