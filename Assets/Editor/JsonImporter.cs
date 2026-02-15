using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

// ============================================
// СОХРАНИ ЭТОТ ФАЙЛ В: Assets/Editor/JsonImporter.cs
// ============================================

public class JsonImporter : EditorWindow
{
    [MenuItem("Tools/Import Game Data from JSON")]
    public static void ImportAllData()
    {
        string path = EditorUtility.OpenFolderPanel("Select folder with JSON files", "", "");
        if (string.IsNullOrEmpty(path)) return;

        ImportItems(Path.Combine(path, "ItemDatabase.json"));
        ImportRecipes(Path.Combine(path, "RecipeDatabase.json"));
        ImportAlloys(Path.Combine(path, "AlloyDatabase.json"));
        ImportQuests(Path.Combine(path, "QuestDatabase.json"));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("=== IMPORT COMPLETE! ===");
    }

    // === ITEMS ===
    [System.Serializable]
    public class ItemDataJson { public string id, itemName, category, description; public int basePrice, maxStackSize; }
    [System.Serializable]
    public class ItemDatabaseJson { public List<ItemDataJson> items; }

    static void ImportItems(string filePath)
    {
        if (!File.Exists(filePath)) { Debug.LogWarning("ItemDatabase.json not found"); return; }

        string json = File.ReadAllText(filePath);
        var data = JsonUtility.FromJson<ItemDatabaseJson>(json);

        if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder("Assets/Data/Items")) AssetDatabase.CreateFolder("Assets/Data", "Items");

        var database = ScriptableObject.CreateInstance<ItemDatabase>();
        var itemList = new List<ItemData>();

        foreach (var item in data.items)
        {
            var asset = ScriptableObject.CreateInstance<ItemData>();
            asset.id = item.id;
            asset.itemName = item.itemName;
            asset.category = ParseCategory(item.category);
            asset.basePrice = item.basePrice;
            asset.maxStackSize = item.maxStackSize;
            asset.description = item.description;

            AssetDatabase.CreateAsset(asset, $"Assets/Data/Items/{item.id}.asset");
            itemList.Add(asset);
        }

        // Используем рефлексию для установки приватного поля
        var itemsField = typeof(ItemDatabase).GetField("items", BindingFlags.NonPublic | BindingFlags.Instance);
        if (itemsField != null) itemsField.SetValue(database, itemList);

        AssetDatabase.CreateAsset(database, "Assets/Data/ItemDatabase.asset");
        Debug.Log($"Imported {data.items.Count} items");
    }

    static ItemCategory ParseCategory(string cat)
    {
        switch (cat)
        {
            case "Material": return ItemCategory.Material;
            case "Weapon": return ItemCategory.Weapon;
            case "Tool": return ItemCategory.Tool;
            case "Consumable": return ItemCategory.Consumable;
            default: return ItemCategory.Other;
        }
    }

    // === RECIPES ===
    [System.Serializable]
    public class RecipeItemJson { public string itemId; public int amount; }
    [System.Serializable]
    public class RecipeDataJson
    {
        public string recipeName, category, resultItemId;
        public int resultAmount;
        public float craftTime, requiredLevel;
        public List<RecipeItemJson> requiredItems;
    }
    [System.Serializable]
    public class RecipeDatabaseJson { public List<RecipeDataJson> recipes; }

    static void ImportRecipes(string filePath)
    {
        if (!File.Exists(filePath)) { Debug.LogWarning("RecipeDatabase.json not found"); return; }

        string json = File.ReadAllText(filePath);
        var data = JsonUtility.FromJson<RecipeDatabaseJson>(json);

        if (!AssetDatabase.IsValidFolder("Assets/Data/Recipes")) AssetDatabase.CreateFolder("Assets/Data", "Recipes");

        var database = ScriptableObject.CreateInstance<RecipeDatabase>();
        var recipeList = new List<RecipeData>();

        foreach (var recipe in data.recipes)
        {
            var asset = ScriptableObject.CreateInstance<RecipeData>();
            asset.recipeName = recipe.recipeName;
            asset.category = ParseCraftCategory(recipe.category);
            asset.resultItem = FindItemById(recipe.resultItemId);
            asset.resultAmount = recipe.resultAmount;

            asset.requiredItems = new List<RecipeData.RequiredItem>();
            foreach (var req in recipe.requiredItems)
            {
                if (!string.IsNullOrEmpty(req.itemId) && req.amount > 0)
                {
                    asset.requiredItems.Add(new RecipeData.RequiredItem
                    {
                        item = FindItemById(req.itemId),
                        amount = req.amount
                    });
                }
            }

            AssetDatabase.CreateAsset(asset, $"Assets/Data/Recipes/{recipe.recipeName}.asset");
            recipeList.Add(asset);
        }

        var recipesField = typeof(RecipeDatabase).GetField("recipes", BindingFlags.NonPublic | BindingFlags.Instance);
        if (recipesField != null) recipesField.SetValue(database, recipeList);

        AssetDatabase.CreateAsset(database, "Assets/Data/RecipeDatabase.asset");
        Debug.Log($"Imported {data.recipes.Count} recipes");
    }

    static CraftCategory ParseCraftCategory(string cat)
    {
        switch (cat)
        {
            case "Tools": return CraftCategory.Tools;
            case "Weapons": return CraftCategory.Weapons;
            default: return CraftCategory.Decorations;
        }
    }

    static ItemData FindItemById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        var guids = AssetDatabase.FindAssets($"t:ItemData");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null && item.id == id) return item;
        }
        return null;
    }

    // === ALLOYS ===
    [System.Serializable]
    public class AlloyDataJson
    {
        public string alloyName, resultItemId, slagItemId, description;
        public List<string> requiredOres;
        public int coalRequired;
        public float minTemp, maxTemp, smeltDuration;
    }
    [System.Serializable]
    public class AlloyDatabaseJson { public List<AlloyDataJson> alloys; }

    static void ImportAlloys(string filePath)
    {
        if (!File.Exists(filePath)) { Debug.LogWarning("AlloyDatabase.json not found"); return; }

        string json = File.ReadAllText(filePath);
        var data = JsonUtility.FromJson<AlloyDatabaseJson>(json);

        if (!AssetDatabase.IsValidFolder("Assets/Data/Alloys")) AssetDatabase.CreateFolder("Assets/Data", "Alloys");

        var database = ScriptableObject.CreateInstance<AlloyDatabase>();
        var alloyList = new List<AlloyRecipe>();

        foreach (var alloy in data.alloys)
        {
            var asset = ScriptableObject.CreateInstance<AlloyRecipe>();
            asset.alloyName = alloy.alloyName;
            asset.resultItem = FindItemById(alloy.resultItemId);
            asset.slagItem = FindItemById(alloy.slagItemId);
            asset.requiredOres = alloy.requiredOres ?? new List<string>();
            asset.coalRequired = alloy.coalRequired;
            asset.minTemp = alloy.minTemp;
            asset.maxTemp = alloy.maxTemp;
            asset.smeltDuration = alloy.smeltDuration;
            asset.description = alloy.description;

            AssetDatabase.CreateAsset(asset, $"Assets/Data/Alloys/{alloy.alloyName}.asset");
            alloyList.Add(asset);
        }

        var recipesField = typeof(AlloyDatabase).GetField("recipes", BindingFlags.NonPublic | BindingFlags.Instance);
        if (recipesField != null) recipesField.SetValue(database, alloyList);

        AssetDatabase.CreateAsset(database, "Assets/Data/AlloyDatabase.asset");
        Debug.Log($"Imported {data.alloys.Count} alloys");
    }

    // === QUESTS ===
    [System.Serializable]
    public class QuestDataJson
    {
        public string questID, title, description, customerName, requiredItemID;
        public int rewardGold, timeLimitHours, requiredAmount;
        public float failPenaltyPercent;
    }
    [System.Serializable]
    public class QuestDatabaseJson { public List<QuestDataJson> quests; }

    static void ImportQuests(string filePath)
    {
        if (!File.Exists(filePath)) { Debug.LogWarning("QuestDatabase.json not found"); return; }

        string json = File.ReadAllText(filePath);
        var data = JsonUtility.FromJson<QuestDatabaseJson>(json);

        if (!AssetDatabase.IsValidFolder("Assets/Data/Quests")) AssetDatabase.CreateFolder("Assets/Data", "Quests");

        var database = ScriptableObject.CreateInstance<QuestDatabase>();
        var questList = new List<QuestData>();

        foreach (var quest in data.quests)
        {
            var asset = ScriptableObject.CreateInstance<QuestData>();
            asset.questID = quest.questID;
            asset.title = quest.title;
            asset.description = quest.description;
            asset.customerName = quest.customerName;
            asset.requiredItemID = quest.requiredItemID;
            asset.requiredAmount = quest.requiredAmount;
            asset.rewardGold = quest.rewardGold;
            asset.timeLimitHours = quest.timeLimitHours;
            asset.failPenaltyPercent = quest.failPenaltyPercent;

            AssetDatabase.CreateAsset(asset, $"Assets/Data/Quests/{quest.questID}.asset");
            questList.Add(asset);
        }

        var questsField = typeof(QuestDatabase).GetField("quests", BindingFlags.NonPublic | BindingFlags.Instance);
        if (questsField != null) questsField.SetValue(database, questList);

        AssetDatabase.CreateAsset(database, "Assets/Data/QuestDatabase.asset");
        Debug.Log($"Imported {data.quests.Count} quests");
    }
}