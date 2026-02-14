using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewAlloyRecipe", menuName = "Ignis/Alloy Recipe")]
public class AlloyRecipe : ScriptableObject
{
    public string alloyName;                // Название сплава
    public ItemData resultItem;             // Слиток при успехе
    public ItemData slagItem;               // Брак при неудаче (slag)
    public List<string> requiredOres = new List<string>();  // ID руд
    [TextArea(3, 6)]
    public string description;              // Описание
    public int coalRequired = 5;            // Константа угля на старте
    public float minTemp = 1200f;           // Нижний край диапазона (°C)
    public float maxTemp = 1600f;           // Верхний край
    public float smeltDuration = 10f;       // Время мини-игры (сек)
}