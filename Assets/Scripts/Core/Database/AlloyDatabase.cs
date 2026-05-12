using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AlloyDatabase", menuName = "Ignis/Alloy Database")]
public class AlloyDatabase : ScriptableObject
{
    [SerializeField] private List<AlloyRecipe> recipes = new List<AlloyRecipe>();

    // Найти рецепт по списку ID руд (сортируем для совпадения)
    public AlloyRecipe GetRecipe(List<string> oreIds)
    {
        oreIds.Sort();
        string inputKey = string.Join(",", oreIds);

        foreach (var recipe in recipes)
        {
            List<string> requiredSorted = new List<string>(recipe.requiredOres);
            requiredSorted.Sort();
            string recipeKey = string.Join(",", requiredSorted);

            if (inputKey == recipeKey)
                return recipe;
        }

        return null;  // Нет совпадения
    }
}