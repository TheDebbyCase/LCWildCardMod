using BepInEx;
using LethalLib.Modules;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using LCWildCardMod.Config;
using LCWildCardMod.Utils;
namespace LCWildCardMod
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    [BepInDependency("com.rune580.LethalCompanyInputUtils", BepInDependency.DependencyFlags.HardDependency)]
    public class WildCardMod : BaseUnityPlugin
    {
        private const string modGUID = "deB.WildCard";
        private const string modName = "WILDCARD Stuff";
        private const string modVersion = "0.9.4";
        internal static ManualLogSource Log = null!;
        internal static KeyBinds wildcardKeyBinds;
        private static WildCardMod Instance;
        private readonly Harmony harmony = new Harmony(modGUID);
        internal static WildCardConfig ModConfig {get; private set;} = null!;
        private readonly string[] declaredAssetPaths = {"assets/my creations/scrap items"};
        private void Awake()
        {
            wildcardKeyBinds = new KeyBinds();
            Log = Logger;
            if (Instance == null)
            {
                Instance = this;
            }
            Type[] assemblyTypes = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type currentType in assemblyTypes)
            {
                MethodInfo[] typeMethods = currentType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (MethodInfo currentMethod in typeMethods)
                {
                    object[] methodAttributes = currentMethod.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (methodAttributes.Length > 0)
                    {
                        currentMethod.Invoke(null, null);
                    }
                }
            }
            AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "wildcardmod"));
            List<Item> scrapList = new List<Item>();
            foreach (string assetPath in bundle.GetAllAssetNames())
            {
                if (declaredAssetPaths.Contains(assetPath[..assetPath.LastIndexOf("/")]))
                {
                    switch (assetPath[..assetPath.LastIndexOf("/")])
                    {
                        case "assets/my creations/scrap items":
                            {
                                scrapList.Add(bundle.LoadAsset<Item>(assetPath));
                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }
                }
                else
                {
                    Log.LogWarning($"\"{assetPath}\" is not a known asset path, skipping.");
                }
            }
            ModConfig = new WildCardConfig(base.Config, scrapList);
            for (int i = 0; i < scrapList.Count; i++)
            {
                if (scrapList[i].spawnPrefab.GetComponent<AdditionalInfo>().isBonus && !ModConfig.assortedScrap.Value)
                {
                    Log.LogInfo($"{scrapList[i].itemName} was disabled!");
                    continue;
                }
                if (ModConfig.isScrapEnabled[i].Value)
                {
                    Dictionary<Levels.LevelTypes, int> scrapLevelWeights = new Dictionary<Levels.LevelTypes, int>();
                    Dictionary<string, int> scrapModdedWeights = new Dictionary<string, int>();
                    string[] configScrapStringArray = ModConfig.scrapSpawnWeights[i].Value.Split(",");
                    bool isScrapConfigStringValid = new bool();
                    foreach (string configScrapStringCheck in configScrapStringArray)
                    {
                        if ((configScrapStringCheck.Contains(":") && int.TryParse(configScrapStringCheck.Split(":")[1], out _)))
                        {
                            isScrapConfigStringValid = true;
                        }
                        else
                        {
                            isScrapConfigStringValid = false;
                            break;
                        }
                    }
                    if (isScrapConfigStringValid)
                    {
                        foreach (string configScrapString in configScrapStringArray)
                        {
                            if (Levels.LevelTypes.TryParse(configScrapString.Split(":")[0], out Levels.LevelTypes configScrapLevel))
                            {
                                scrapLevelWeights.Add(configScrapLevel, int.Parse(configScrapString.Split(":")[1]));
                            }
                            else
                            {
                                scrapModdedWeights.Add(configScrapString, int.Parse(configScrapString.Split(":")[1]));
                            }
                        }
                        NetworkPrefabs.RegisterNetworkPrefab(scrapList[i].spawnPrefab);
                        Utilities.FixMixerGroups(scrapList[i].spawnPrefab);
                        LethalLib.Modules.Items.RegisterScrap(scrapList[i], null, scrapModdedWeights);
                        LethalLib.Modules.Items.RegisterScrap(scrapList[i], scrapLevelWeights);
                        Log.LogDebug($"{scrapList[i].itemName} was loaded!");
                        foreach (KeyValuePair<Levels.LevelTypes, int> debugRarities in LethalLib.Modules.Items.scrapItems.LastOrDefault().levelRarities)
                        {
                            Log.LogDebug($"LethalLib Registered Weights {debugRarities}");
                        }
                    }
                    else
                    {
                        Log.LogWarning($"{scrapList[i].itemName} was not loaded as its config was not set up correctly!");
                    }
                }
                else
                {
                    Log.LogInfo($"{scrapList[i].itemName} was disabled!");
                }
            }
            harmony.PatchAll();
            Log.LogInfo("WILDCARD Stuff Successfully Loaded");
        }
    }
}