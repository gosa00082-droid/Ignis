using System;
using System.Collections.Generic;
using UnityEngine;

public enum RecipeAssemblyMode
{
    Ordered,
    Unordered
}

[CreateAssetMenu(menuName = "Workbench/Assembly Recipe", fileName = "NewAssemblyRecipe")]
public class AssemblyRecipe : ScriptableObject
{
    public string recipeId;
    public RecipeAssemblyMode assemblyMode = RecipeAssemblyMode.Ordered;
    public List<AssemblyRecipeStep> steps = new();

    [Header("Результат сборки")]
    [Tooltip("ID предмета, который получается при завершении сборки (например 'sword')")]
    public string resultItemId;
}

[Serializable]
public class AssemblyRecipeStep
{
    public string stepName;

    [Header("����� ����� ������ ��������������")]
    public string socketId;

    [Header("������������ ������")]
    public AttachmentType parentType = AttachmentType.None;
    public string parentPartId;

    [Header("�������� ������")]
    public AttachmentType childType = AttachmentType.None;
    public string childPartId;
}