using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using ChunkysFriendTweaks.patch;

namespace ChunkysFriendTweaks;

// 111 is in front of the GUID so that it is (hopefully) loaded before any mods that use LethalLib, as RegisterScrap needs to be patched before anything uses it. 
[BepInPlugin(PluginInfo.GUID, "ChunkysFriendTweaks", "1.0.0")]
[BepInDependency("evaisa.lethallib")]
[BepInDependency("imabatby.lethallevelloader")]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; set; }

    public static ManualLogSource Log => Instance.Logger;

    private readonly Harmony _harmony = new("ChunkysFriendTweaks");

    public static readonly HashSet<string> BlacklistedItems = new();
    
    
    public Plugin()
    {
        Instance = this;
        var blacklist = Config.Bind("General", 
            "Blacklist Item Names", 
            "123",
            "Comma separated list of names of items to add to the blacklist. Blacklisted items will be skipped when being registered with LethalLib or LethalLevelLoader. Use with care.");
        foreach (var item in blacklist.Value.Split(','))
        {
            BlacklistedItems.Add(item.Trim());
        }
        
    }
    
    

    
    private void Awake()
    {
        Log.LogInfo($"Applying patches...");
        ApplyPluginPatch();
        Log.LogInfo($"Patches applied");
    }

    /// <summary>
    /// Applies the patch to the game.
    /// </summary>
    private void ApplyPluginPatch()
    {
        _harmony.PatchAll(typeof(LLLPatch));
        _harmony.PatchAll(typeof(LethalLibPatch));
    }
    
    
    //Makes names safe for config file
    public static string SafeName(string name)
    {
        char[] invalidConfigChars = { '=', '\n', '\t', '\\', '"', '\'', '[', ']' };
        
        foreach (char invalidChar in invalidConfigChars)
        {
            name = name.Replace(invalidChar, '_');
        }

        return name;
    }
}
