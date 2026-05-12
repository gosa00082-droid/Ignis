using UnityEngine;
using System.Collections.Generic;

public enum PostSmeltAction
{
    None,
    OfferRefiningChoice
}

[CreateAssetMenu(fileName = "NewAlloyRecipe", menuName = "Ignis/Alloy Recipe")]
public class AlloyRecipe : ScriptableObject
{
    [Header("Основное")]
    public string alloyName;

    [Header("Результат")]
    public ItemData resultItem;
    public ItemData slagItem;

    [Header("Требования")]
    public List<string> requiredOres = new List<string>();

    [TextArea(3, 6)]
    public string description;

    [Header("Параметры плавки")]
    public int coalRequired = 5;
    public float minTemp = 1200f;
    public float maxTemp = 1600f;
    public float smeltDuration = 10f;

    [Header("Действие после плавки")]
    public PostSmeltAction postSmeltAction = PostSmeltAction.None;
}