using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LethalLevelLoader;
using LethalLevelLoader.AssetBundles;
using UnityEngine;

namespace ChunkysFriendTweaks.patch;

public class LLLPatch
{

    [HarmonyPatch(typeof(ExtendedMod), "RegisterExtendedContent", new []{typeof(ExtendedItem)} )]
    [HarmonyPrefix]
    public static bool RegisterExtendedContentPatch(ref ExtendedMod __instance, ref ExtendedItem extendedItem)
    {
        if (__instance.ModName == "LethalCompany" || __instance.AuthorName == "Zeekerss")
        {
            //Skip vanilla stuff. ContentType isn't set here for some reason.
            return true;
        }

        if (extendedItem.LevelMatchingProperties is null)
        {
            //Should just be vanilla stuff? Idk anymore
            Plugin.Log.LogWarning($"Skipping {extendedItem.name} (missing LevelMatchingProperties).");
            return true;
        }
        if (MatchesBlacklist(extendedItem))
        {
            return false;
        }
        extendedItem = UpdateItem(extendedItem);
        return true;
    }
    
    
    private static bool MatchesBlacklist(ExtendedItem extendedItem)
    {
        string safeName = Plugin.SafeName(extendedItem.name);
        if (Plugin.BlacklistedItems.Contains( safeName ))
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
        string safeName = Plugin.SafeName(extendedItem.name);
        var values = Plugin.Instance.Config.Bind("ScrapValues", 
            $"{safeName} Scrap Values", 
            $"{extendedItem.Item.minValue},{extendedItem.Item.maxValue}",
            "The minimum and maximum scrap values for this item, separated by a comma. Lethal Company multiplies all scrap values by 0.4, so a value of 50,100 would mean the item can be worth between 20 and 40.");
        
        if (string.IsNullOrEmpty(values.Value)) return extendedItem;
        var valuesArray = values.Value.Split(',')?.Select( int.Parse ).ToArray();
        if (valuesArray == null || valuesArray.Length < 2) return extendedItem;
        extendedItem.Item.minValue = valuesArray[0];
        extendedItem.Item.maxValue = valuesArray[1];
        Plugin.Log.LogInfo($"Item {safeName} scrap values updated to {extendedItem.Item.minValue} - {extendedItem.Item.maxValue}");
        return extendedItem;
    }

    private static ExtendedItem UpdateItemWeights(ExtendedItem extendedItem)
    {
        string safeName = Plugin.SafeName(extendedItem.name);
        //Make a copy so we don't update the original (it can be shared between items)
        LevelMatchingProperties orig = extendedItem.LevelMatchingProperties;
        extendedItem.LevelMatchingProperties = LevelMatchingProperties.Create(extendedItem);
        extendedItem.LevelMatchingProperties.ApplyValues(orig.modNames, orig.authorNames, orig.levelTags, orig.currentRoutePrice, orig.currentWeather, orig.planetNames);
        extendedItem.LevelMatchingProperties.planetNames = UpdateList( safeName, extendedItem.LevelMatchingProperties.planetNames, 
            "Planet Weights",
             "A comma-separated list of moon names and scrap spawn weights for this item. For example, Experimentation:10,Vow:20 will make the item spawn twice as often on Experimentation as on Vow. A blank value will use the mod's default settings");
        
        extendedItem.LevelMatchingProperties.levelTags = UpdateList( safeName, extendedItem.LevelMatchingProperties.levelTags, 
            "Level Tag Weights",
            "A comma-separated list of level tags and scrap spawn weights for this item. For example, Vanilla:20 will make the item spawn with a weight of 20 on Vanilla levels. A blank value will use the mod's default settings");
        
        extendedItem.LevelMatchingProperties.currentWeather = UpdateList( safeName, extendedItem.LevelMatchingProperties.currentWeather, 
            "Current Weather Weights",
            "A comma-separated list of current weather and scrap spawn weights for this item. For example, Rainy:100 will make the item spawn with a weight of 100 when the current weather is Rainy. A blank value will use the mod's default settings");
       
        //TODO dungeon weights
        return extendedItem;
    }
    
    private static List<StringWithRarity> UpdateList(string itemName, List<StringWithRarity> stringWithRarityList, String configName, String description)
    {
        List<StringWithRarity> result = new List<StringWithRarity>();
        foreach (var stringWithRarity in stringWithRarityList)
        {
            //Make a copy so we don't update the original (it can be shared between items)
            result.Add(new StringWithRarity(stringWithRarity.Name, stringWithRarity.Rarity));
        }
        var nameRarityDict = result.ToDictionary(planet => planet.Name, planet => planet.Rarity);
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
                var planetRarityPair = result.FirstOrDefault( planet => planet.Name.Trim() == nameRarityPair[0]);
                try
                {
                    if (planetRarityPair == null)
                    {
                        //Add a new planet if it's in the config but not the item
                        result.Add( new StringWithRarity(nameRarityPair[0], int.Parse(nameRarityPair[1])));
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
        return result;
    }
}
