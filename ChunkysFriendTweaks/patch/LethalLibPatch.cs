using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using LethalLib.Modules;
using BepInEx.Configuration;

namespace ChunkysFriendTweaks.patch;

[HarmonyPatch(typeof(Items))]
public class LethalLibPatch
{
    
    [HarmonyPatch(typeof(Items), "RegisterScrap" , new []{typeof(Item), typeof(int), typeof(Levels.LevelTypes)})]
    [HarmonyPrefix]
    public static bool PrefixRegisterScrap( ref Item spawnableItem, ref int rarity, ref Levels.LevelTypes levelFlags)
    {
        if (MatchesBlacklist(spawnableItem))
        {
            return false;
        }
        spawnableItem = UpdateItemValues(spawnableItem);
        Dictionary<Levels.LevelTypes, int>? levelRarities = new Dictionary<Levels.LevelTypes, int> { { levelFlags, rarity } };
        levelRarities = UpdateLevelRarities( spawnableItem, levelRarities);
        //Call the original method and pass the dictionary, so we can still use the custom level weights. This allows for setting multiple levels even if the original has only one. 
        Items.RegisterScrap( spawnableItem, levelRarities);
        return false;
    }
    
    
    [HarmonyPatch(typeof(Items), "RegisterScrap" , new []{typeof(Item), typeof(int), typeof(Levels.LevelTypes), typeof(string[])})]
    [HarmonyPrefix]
    public static bool PrefixRegisterScrap2( ref Item spawnableItem, ref int rarity, ref Levels.LevelTypes levelFlags, ref string[]? levelOverrides)
    {
        if (MatchesBlacklist(spawnableItem))
        {
            return false;
        }
        spawnableItem = UpdateItemValues(spawnableItem);
        Dictionary<Levels.LevelTypes, int>? levelRarities = new Dictionary<Levels.LevelTypes, int> { { levelFlags, rarity } };
        levelRarities = UpdateLevelRarities( spawnableItem, levelRarities);
        var rarityConst = rarity;
        var customLevelRarities = levelOverrides?.ToDictionary(level => level, _ =>rarityConst );
        customLevelRarities = UpdateCustomLevelRarities(spawnableItem, customLevelRarities);
        //Call the original method and pass the dictionary, so we can still use the custom level weights. This allows for setting multiple levels even if the original has only one. 
        Items.RegisterScrap( spawnableItem, levelRarities, customLevelRarities);
        return false;
    }
    
    
    [HarmonyPatch(typeof(Items), "RegisterScrap" , new []{typeof(Item), typeof(Dictionary<Levels.LevelTypes, int>), typeof(Dictionary<string, int>)})]
    [HarmonyPrefix]
    public static bool PrefixRegisterScrap3( ref Item spawnableItem, ref Dictionary<Levels.LevelTypes, int>? levelRarities, ref Dictionary<string, int>? customLevelRarities)
    {
        if (MatchesBlacklist(spawnableItem))
        {
            return false;
        }
        spawnableItem = UpdateItemValues(spawnableItem);
        levelRarities = UpdateLevelRarities( spawnableItem, levelRarities);
        customLevelRarities = UpdateCustomLevelRarities(spawnableItem, customLevelRarities);
        return true;
    }
    
    
    
    private static bool MatchesBlacklist(Item spawnableItem)
    {
        if (Plugin.BlacklistedItems.Contains(spawnableItem.itemName))
        {
            Plugin.Log.LogInfo($"Item {spawnableItem.itemName} is blacklisted, not registering.");
            return true;
        }
        return false;
    }

    private static Dictionary<string, int>? UpdateCustomLevelRarities(Item item, Dictionary<string, int>? levelRarities)
    {
        string safeName = Plugin.SafeName(item.itemName);
        var nameRarityPlanetConfig = Plugin.Instance.Config.Bind("ScrapSpawnWeights", 
            $"{safeName} Custom Level Weights", 
            $"{(levelRarities == null ? "" : string.Join(",", levelRarities.Select( pair => nameof(pair.Key) + ":" + pair.Value)))}",
            "A comma-separated list of custom level names and scrap spawn weights for this item. For example, Experimentation:10,Vow:20 will make the item spawn twice as often on Experimentation as on Vow.");

        if( levelRarities == null ) return levelRarities;
        if (!string.IsNullOrEmpty(nameRarityPlanetConfig.Value))
        {
            var nameRarityPairs = nameRarityPlanetConfig.Value.Split(',').Select(pair => pair.Split(':'));
            try
            {
                foreach (string[] nameRarityPair in nameRarityPairs)
                {
                    nameRarityPair[0] = nameRarityPair[0].Trim();
                    nameRarityPair[1] = nameRarityPair[1].Trim();
                    levelRarities[nameRarityPair[0]] = int.Parse(nameRarityPair[1]);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Error parsing config for LethalLib item {safeName}: {nameRarityPlanetConfig.Value} - {e.Message}\n{e.StackTrace}");
                return levelRarities;
            }
        }
        return levelRarities;
    }

    private static Dictionary<Levels.LevelTypes, int>? UpdateLevelRarities( Item item, Dictionary<Levels.LevelTypes, int>? levelRarities)
    {
        string safeName = Plugin.SafeName(item.itemName);
        var nameRarityPlanetConfig = Plugin.Instance.Config.Bind("ScrapSpawnWeights", 
            $"{safeName} LevelType Weights", 
            $"{(levelRarities == null ? "" : string.Join(",", levelRarities.Select( pair => pair.Key + ":" + pair.Value)))}",
            "A comma-separated list of level names and scrap spawn weights for this item. For example, AssuranceLevel:10,Modded:20 will make the item spawn twice as often on Assurance as on Modded levels.");

        if( levelRarities == null ) return levelRarities;
        if (!string.IsNullOrEmpty(nameRarityPlanetConfig.Value))
        {
            var nameRarityPairs = nameRarityPlanetConfig.Value.Split(',').Select(pair => pair.Split(':'));
            try
            {
                foreach (string[] nameRarityPair in nameRarityPairs)
                {
                    nameRarityPair[0] = nameRarityPair[0].Trim();
                    nameRarityPair[1] = nameRarityPair[1].Trim();
                    var levelType = Enum.Parse<Levels.LevelTypes>(nameRarityPair[0]);
                    levelRarities[levelType] = int.Parse(nameRarityPair[1]);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Error parsing config for LethalLib item {safeName}: {nameRarityPlanetConfig.Value} - {e.Message}\n{e.StackTrace}");
                return levelRarities;
            }
        }
        return levelRarities;
    }
    
    
    private static Item UpdateItemValues(Item spawnableItem)
    {
        string safeName = Plugin.SafeName(spawnableItem.itemName);
        var values = Plugin.Instance.Config.Bind("ScrapValues", 
            $"{safeName} Scrap Values", 
            $"{spawnableItem.minValue},{spawnableItem.maxValue}",
            "The minimum and maximum scrap values for this item, separated by a comma. Lethal Company multiplies all scrap values by 0.4, so a value of 50,100 would mean the item can be worth between 20 and 40.");
        if (string.IsNullOrEmpty(values.Value)) return spawnableItem;
        try
        {
            var valuesArray = values.Value.Split(',')?.Select( int.Parse ).ToArray();
            if (valuesArray == null || valuesArray.Length < 2) return spawnableItem;
            spawnableItem.minValue = valuesArray[0];
            spawnableItem.maxValue = valuesArray[1];
            return spawnableItem;
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"Error parsing config for LethalLib item {safeName}: {values.Value} - {e.Message}\n{e.StackTrace}");
            return spawnableItem;
        }
    }
    
    
    
}
