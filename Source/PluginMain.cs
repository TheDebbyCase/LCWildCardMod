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
using static BepInEx.BepInDependency;
using LobbyCompatibility.Enums;
using LobbyCompatibility.Features;
namespace LCWildCardMod
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("BMX.LobbyCompatibility", DependencyFlags.SoftDependency)]
    [BepInDependency("evaisa.lethallib", DependencyFlags.HardDependency)]
    [BepInDependency("com.rune580.LethalCompanyInputUtils", DependencyFlags.HardDependency)]
    public class WildCardMod : BaseUnityPlugin
    {
        internal const string modGUID = "deB.WildCard";
        internal const string modName = "WILDCARD Stuff";
        internal const string modVersion = "0.12.4";
        internal static ManualLogSource Log = null!;
        internal static KeyBinds wildcardKeyBinds;
        internal static SkinsClass skinsClass;
        private static WildCardMod Instance;
        private readonly Harmony harmony = new Harmony(modGUID);
        internal static WildCardConfig ModConfig {get; private set;} = null!;
        private readonly string[] declaredAssetPaths = {"assets/my creations/scrap items", "assets/my creations/skins"};
        public static List<Item> scrapList = new List<Item>();
        public static List<Skin> skinList = new List<Skin>();
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        private void Awake()
        {
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("BMX.LobbyCompatibility"))
            {
                SoftDepHelper.LobCompatRegister();
            }
            wildcardKeyBinds = new KeyBinds();
            Log = Logger;
            if (Instance == null)
            {
                Instance = this;
            }
            Type[] assemblyTypes = Assembly.GetExecutingAssembly().GetTypes();
            for (int i = 0; i < assemblyTypes.Length; i++)
            {
                MethodInfo[] typeMethods = assemblyTypes[i].GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                for (int j = 0; j < typeMethods.Length; j++)
                {
                    object[] methodAttributes = typeMethods[j].GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (methodAttributes.Length > 0)
                    {
                        typeMethods[j].Invoke(null, null);
                    }
                }
            }
            AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "wildcardmod"));
            string[] allAssetPaths = bundle.GetAllAssetNames();
            for (int i = 0; i < allAssetPaths.Length; i++)
            {
                if (declaredAssetPaths.Contains(allAssetPaths[i][..allAssetPaths[i].LastIndexOf("/")]))
                {
                    switch (allAssetPaths[i][..allAssetPaths[i].LastIndexOf("/")])
                    {
                        case "assets/my creations/scrap items":
                            {
                                scrapList.Add(bundle.LoadAsset<Item>(allAssetPaths[i]));
                                break;
                            }
                        case "assets/my creations/skins":
                            {
                                skinList.Add(bundle.LoadAsset<Skin>(allAssetPaths[i]));
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
                    Log.LogWarning($"\"{allAssetPaths[i]}\" is not a known asset path, skipping.");
                }
            }
            ModConfig = new WildCardConfig(base.Config, scrapList, skinList);
            skinsClass = new SkinsClass();
            for (int i = 0; i < scrapList.Count; i++)
            {
                if (scrapList[i].spawnPrefab.GetComponent<AdditionalInfo>().isBonus && !ModConfig.assortedScrap.Value)
                {
                    Log.LogInfo($"\"{scrapList[i].itemName}\" scrap was disabled!");
                    continue;
                }
                if (ModConfig.isScrapEnabled[i].Value)
                {
                    Dictionary<Levels.LevelTypes, int> scrapLevelWeights = new Dictionary<Levels.LevelTypes, int>();
                    Dictionary<string, int> scrapModdedWeights = new Dictionary<string, int>();
                    string[] configScrapStringArray = ModConfig.scrapSpawnWeights[i].Value.Split(",");
                    bool isScrapConfigStringValid = new bool();
                    for (int j = 0; j < configScrapStringArray.Length; j++)
                    {
                        if (configScrapStringArray[j].Contains(":") && int.TryParse(configScrapStringArray[j].Split(":")[1], out _))
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
                        for (int j = 0; j < configScrapStringArray.Length; j++)
                        {
                            if (Levels.LevelTypes.TryParse(configScrapStringArray[j].Split(":")[0], out Levels.LevelTypes configScrapLevel))
                            {
                                scrapLevelWeights.Add(configScrapLevel, int.Parse(configScrapStringArray[j].Split(":")[1]));
                            }
                            else
                            {
                                scrapModdedWeights.Add(configScrapStringArray[j], int.Parse(configScrapStringArray[j].Split(":")[1]));
                            }

                        }
                        NetworkPrefabs.RegisterNetworkPrefab(scrapList[i].spawnPrefab);
                        Utilities.FixMixerGroups(scrapList[i].spawnPrefab);
                        LethalLib.Modules.Items.RegisterScrap(scrapList[i], null, scrapModdedWeights);
                        LethalLib.Modules.Items.RegisterScrap(scrapList[i], scrapLevelWeights);
                        Log.LogDebug($"\"{scrapList[i].itemName}\" scrap was loaded!");
                        for (int j = 0; j < LethalLib.Modules.Items.scrapItems.LastOrDefault().levelRarities.Count; j++)
                        {
                            Log.LogDebug($"LethalLib Scrap Weights \"{LethalLib.Modules.Items.scrapItems.LastOrDefault().levelRarities.ToArray()[j]}\"");
                        }
                    }
                    else
                    {
                        Log.LogWarning($"\"{scrapList[i].itemName}\" scrap was not loaded as its config was not set up correctly!");
                    }
                }
                else
                {
                    Log.LogInfo($"\"{scrapList[i].itemName}\" scrap was disabled!");
                    scrapList.Remove(scrapList[i]);
                    i--;
                }
            }
            for (int i = 0; i < skinList.Count; i++)
            {
                if (ModConfig.isSkinEnabled[i].Value)
                {
                    Log.LogDebug($"\"{skinList[i].skinName}\" skin was loaded!");
                }
                else
                {
                    Log.LogInfo($"\"{skinList[i].skinName}\" skin was disabled!");
                    skinList.Remove(skinList[i]);
                    i--;
                }
            }
            harmony.PatchAll();
            Log.LogInfo("WILDCARD Stuff Successfully Loaded");
        }
    }
}