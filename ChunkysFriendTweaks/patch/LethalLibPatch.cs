using System.Collections.Generic;
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
        return true;
    }
    
    [HarmonyPatch(typeof(Items), "RegisterScrap" , new []{typeof(Item), typeof(int), typeof(Levels.LevelTypes), typeof(string[])})]
    [HarmonyPrefix]
    public static bool PrefixRegisterScrap2( ref Item spawnableItem, ref int rarity, ref Levels.LevelTypes levelFlags, ref string[] levelOverrides)
    {
        if (MatchesBlacklist(spawnableItem))
        {
            return false;
        }
        spawnableItem = UpdateItemValues(spawnableItem);
        return true;
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
    
    
    private static Item UpdateItemValues(Item spawnableItem)
    {
        var values = Plugin.Instance.Config.Bind("ScrapValues", 
            $"{spawnableItem.itemName} Scrap Values", 
            $"{spawnableItem.minValue},{spawnableItem.maxValue}",
            "The minimum and maximum scrap values for this item, separated by a comma. Lethal Company multiplies all scrap values by 0.4, so a value of 50,100 would mean the item can be worth between 20 and 40.");
        if (string.IsNullOrEmpty(values.Value)) return spawnableItem;
        var valuesArray = values.Value.Split(',')?.Select( int.Parse ).ToArray();
        if (valuesArray == null || valuesArray.Length < 2) return spawnableItem;
        spawnableItem.minValue = valuesArray[0];
        spawnableItem.maxValue = valuesArray[1];
        return spawnableItem;
    }
}
