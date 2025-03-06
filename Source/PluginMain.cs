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
        internal const string modVersion = "0.15.1";
        internal static ManualLogSource Log = null!;
        internal static KeyBinds wildcardKeyBinds;
        internal static SkinsClass skinsClass;
        internal static MapObjectHelper mapClass;
        private static WildCardMod Instance;
        private readonly Harmony harmony = new Harmony(modGUID);
        internal static WildCardConfig ModConfig {get; private set;} = null!;
        private readonly string[] declaredAssetPaths = {"assets/my creations/scrap items", "assets/my creations/skins", "assets/my creations/map objects"};
        public static List<Item> scrapList = new List<Item>();
        public static List<Skin> skinList = new List<Skin>();
        public static List<MapObject> mapObjectsList = new List<MapObject>();
        public static List<MapObject> autoMapObjectsList = new List<MapObject>();
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        private void Awake()
        {
            wildcardKeyBinds = new KeyBinds();
            Log = Logger;
            if (Instance == null)
            {
                Instance = this;
            }
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("BMX.LobbyCompatibility"))
            {
                Log.LogDebug("Registering with LobbyCompatibility");
                SoftDepHelper.LobCompatRegister();
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
                        case "assets/my creations/map objects":
                            {
                                MapObject asset = bundle.LoadAsset<MapObject>(allAssetPaths[i]);
                                if (asset.autoHandle)
                                {
                                    autoMapObjectsList.Add(asset);
                                }
                                else
                                {
                                    mapObjectsList.Add(asset);
                                }
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
            ModConfig = new WildCardConfig(base.Config, scrapList, skinList, mapObjectsList);
            skinsClass = new SkinsClass();
            mapClass = new MapObjectHelper();
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
            mapClass.Init(mapObjectsList, autoMapObjectsList, ModConfig);
            for (int i = 0; i < mapObjectsList.Count; i++)
            {
                if (ModConfig.isMapObjectEnabled[i].Value)
                {
                    if (!ModConfig.useDefaultMapObjectCurve[i].Value)
                    {
                        mapObjectsList[i].curveFunc = mapClass.MapObjectFunc;
                        Log.LogInfo($"Using config settings for \"{mapObjectsList[i].mapObjectName}\"'s amount curve!");
                    }
                    else
                    {
                        mapObjectsList[i].curveFunc = mapClass.MapObjectFunc;
                    }
                    NetworkPrefabs.RegisterNetworkPrefab(mapObjectsList[i].spawnableMapObject.prefabToSpawn);
                    Utilities.FixMixerGroups(mapObjectsList[i].spawnableMapObject.prefabToSpawn);
                    if (mapObjectsList[i].spawnableMapObject.prefabToSpawn.TryGetComponent<GrabbableObject>(out GrabbableObject mapObjectScrap))
                    {
                        LethalLib.Modules.Items.RegisterItem(mapObjectScrap.itemProperties);
                    }
                    LethalLib.Modules.MapObjects.RegisterMapObject(mapObjectsList[i].spawnableMapObject, Levels.LevelTypes.All, mapObjectsList[i].curveFunc);
                    Log.LogDebug($"\"{mapObjectsList[i].mapObjectName}\" map object was loaded!");
                }
                else
                {
                    mapObjectsList.Remove(mapObjectsList[i]);
                    Log.LogInfo($"\"{mapObjectsList[i].mapObjectName}\" map object was disabled!");
                    i--;
                }
            }
            for (int i = 0; i < autoMapObjectsList.Count; i++)
            {
                NetworkPrefabs.RegisterNetworkPrefab(autoMapObjectsList[i].spawnableMapObject.prefabToSpawn);
                Utilities.FixMixerGroups(autoMapObjectsList[i].spawnableMapObject.prefabToSpawn);
                if (autoMapObjectsList[i].spawnableMapObject.prefabToSpawn.TryGetComponent<GrabbableObject>(out GrabbableObject mapObjectScrap))
                {
                    LethalLib.Modules.Items.RegisterItem(mapObjectScrap.itemProperties);
                }
                autoMapObjectsList[i].curveFunc = mapClass.AutoMapObjectFunc;
                LethalLib.Modules.MapObjects.RegisterMapObject(autoMapObjectsList[i].spawnableMapObject, Levels.LevelTypes.All, autoMapObjectsList[i].curveFunc);
                Log.LogDebug($"\"{autoMapObjectsList[i].mapObjectName}\" is being handled automatically!");
            }
            harmony.PatchAll();
            Log.LogInfo("WILDCARD Stuff Successfully Loaded");
        }
    }
}