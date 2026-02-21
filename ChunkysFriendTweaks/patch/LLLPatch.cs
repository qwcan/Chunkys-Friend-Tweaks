using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LethalLevelLoader;

namespace ChunkysFriendTweaks.patch;

[HarmonyPatch(typeof(PatchedContent))]
public class LLLPatch
{

    [HarmonyPatch(typeof(PatchedContent))]
    [HarmonyPatch("CustomExtendedItems", MethodType.Getter)]
    [HarmonyPostfix]
    public static void ModifyItems(ref List<ExtendedItem> __result)
    {
        for (var i = __result.Count - 1; i >= 0; i--)
        {
            var extendedItem = __result[i];
            if (MatchesBlacklist(extendedItem))
            {
                __result.RemoveAt(i);
            }
            else
            {
                __result[i] = UpdateItem(extendedItem);
            }
            
        }
    }


    private static bool MatchesBlacklist(ExtendedItem extendedItem)
    {
        if (Plugin.BlacklistedItems.Contains(extendedItem.name))
        {
            Plugin.Log.LogInfo($"Item {extendedItem.name} is blacklisted, not registering.");
            return true;
        }
        return false;
    }
    
    private static ExtendedItem UpdateItem(ExtendedItem extendedItem)
    {
        return UpdateItemValues(UpdateItemWeights(extendedItem));
    }
    
    
    private static ExtendedItem UpdateItemValues(ExtendedItem extendedItem)
    {
        var values = Plugin.Instance.Config.Bind("ScrapValues", 
            $"{extendedItem.name}_ScrapValues", 
            $"{extendedItem.Item.minValue},{extendedItem.Item.maxValue}",
            "The minimum and maximum scrap values for this item, separated by a comma. Lethal Company multiplies all scrap values by 0.4, so a value of 50,100 would mean the item can be worth between 20 and 40.");
        
        if (string.IsNullOrEmpty(values.Value)) return extendedItem;
        var valuesArray = values.Value.Split(',')?.Select( int.Parse ).ToArray();
        if (valuesArray == null || valuesArray.Length < 2) return extendedItem;
        extendedItem.Item.minValue = valuesArray[0];
        extendedItem.Item.maxValue = valuesArray[1];
        return extendedItem;
    }

    private static ExtendedItem UpdateItemWeights(ExtendedItem extendedItem)
    {
        string safeName = Plugin.SafeName(extendedItem.name);
        UpdateList( safeName, extendedItem.LevelMatchingProperties.planetNames, 
            "Planet Weights",
             "A comma-separated list of moon names and scrap spawn weights for this item. For example, Experimentation:10,Vow:20 will make the item spawn twice as often on Experimentation as on Vow.");
        
        UpdateList( safeName, extendedItem.LevelMatchingProperties.levelTags, 
            "Level Tag Weights",
            "A comma-separated list of level tags and scrap spawn weights for this item. For example, Vanilla:20 will make the item spawn with a weight of 20 on Vanilla levels.");
        
        UpdateList( safeName, extendedItem.LevelMatchingProperties.currentWeather, 
            "Current Weather Weights",
            "A comma-separated list of current weather and scrap spawn weights for this item. For example, Rainy:100 will make the item spawn with a weight of 100 when the current weather is Rainy.");
        
        //No idea if these two work or not
        UpdateList( safeName, extendedItem.LevelMatchingProperties.modNames, 
            "Mod Name Weights",
            "A comma-separated list of mod names and scrap spawn weights for this item. For example, MinecraftInteriors:100 will make the item spawn with a weight of 100 on levels from the MinecraftInteriors mod.");

        UpdateList( safeName, extendedItem.LevelMatchingProperties.authorNames, 
            "Author Name Weights",
            "A comma-separated list of author names and scrap spawn weights for this item. For example, qwcan:20 will make the item spawn with a weight of 20 on levels by the author qwcan.");
        
        return extendedItem;
    }
    
    private static void UpdateList(string itemName, List<StringWithRarity> stringWithRarityList, String configName, String description)
    {
        var nameRarityDict = stringWithRarityList.ToDictionary(planet => planet.Name, planet => planet.Rarity);
        var nameRarityPlanetConfig = Plugin.Instance.Config.Bind("ScrapSpawnWeights", 
            $"{itemName} {configName}", 
            $"{string.Join(",", nameRarityDict.Select( pair => pair.Key + ":" + pair.Value))}",
            description);

        if (!string.IsNullOrEmpty(nameRarityPlanetConfig.Value))
        {
            var nameRarityPairs = nameRarityPlanetConfig.Value.Split(',').Select(pair => pair.Split(':'));
            foreach (string[] nameRarityPair in nameRarityPairs)
            {
                nameRarityPair[0] = nameRarityPair[0].Trim();
                nameRarityPair[1] = nameRarityPair[1].Trim();
                var planetRarityPair = stringWithRarityList.FirstOrDefault( planet => planet.Name.Trim() == nameRarityPair[0]);
                try
                {
                    if (planetRarityPair == null)
                    {
                        //Add a new planet if it's in the config but not the item
                        stringWithRarityList.Add( new StringWithRarity(nameRarityPair[0], int.Parse(nameRarityPair[1])));
                    }
                    else
                    {
                        //Otherwise just update the rarity. This should update the item in the live list of rarities
                        planetRarityPair.Rarity = int.Parse(nameRarityPair[1]);
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Error parsing config for LLL item{itemName}: {nameRarityPlanetConfig.Value} - {e.Message}\n{e.StackTrace}");
                }
            }
        }
    }
    
    
}
