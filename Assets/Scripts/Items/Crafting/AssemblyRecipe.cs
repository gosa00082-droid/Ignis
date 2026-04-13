using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Workbench/Assembly Recipe", fileName = "NewAssemblyRecipe")]
public class AssemblyRecipe : ScriptableObject
{
    public string recipeId;
    public List<AssemblyRecipeStep> steps = new();
}

[Serializable]
public class AssemblyRecipeStep
{
    public string stepName;

    [Header("Какой сокет должен использоваться")]
    public string socketId;

    [Header("Родительская деталь")]
    public AttachmentType parentType = AttachmentType.None;
    public string parentPartId;

    [Header("Дочерняя деталь")]
    public AttachmentType childType = AttachmentType.None;
    public string childPartId;
}