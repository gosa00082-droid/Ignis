using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "RecipeDatabase", menuName = "Ignis/Recipe Database")]
public class RecipeDatabase : ScriptableObject
{
    [SerializeField] private List<RecipeData> recipes = new List<RecipeData>();

    public List<RecipeData> GetRecipesByCategory(CraftCategory category)
    {
        return recipes.Where(r => r.category == category).ToList();
    }
}