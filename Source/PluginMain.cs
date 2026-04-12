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
using System.Runtime.CompilerServices;
namespace LCWildCardMod
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("BMX.LobbyCompatibility", DependencyFlags.SoftDependency)]
    [BepInDependency("me.swipez.melonloader.morecompany", DependencyFlags.SoftDependency)]
    [BepInDependency("evaisa.lethallib", DependencyFlags.HardDependency)]
    [BepInDependency("com.rune580.LethalCompanyInputUtils", DependencyFlags.HardDependency)]
    public class WildCardMod : BaseUnityPlugin
    {
        public const string modGUID = "deB.WildCard";
        public const string modName = "WILDCARD Stuff";
        public const string modVersion = "1.2.2";
        public List<Item> scrapList = new List<Item>();
        public List<Skin> skinList = new List<Skin>();
        public List<MapObject> mapObjectsList = new List<MapObject>();
        public List<MapObject> autoMapObjectsList = new List<MapObject>();
        internal ManualLogSource Log { get; private set; } = null!;
        internal KeyBinds KeyBinds { get; private set; } = null!;
        internal static WildCardMod Instance { get; private set; } = null!;
        internal WildCardConfig ModConfig { get; private set; } = null!;
        readonly Harmony harmony = new Harmony(modGUID);
        void Awake()
        {
            Instance = this;
            KeyBinds = new KeyBinds();
            Log = Logger;
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("BMX.LobbyCompatibility"))
            {
                Log.LogDebug("Registering with LobbyCompatibility");
                SoftDepHelper.LobCompatRegister();
            }
            InitializeMethods();
            LoadFromBundle();
            ModConfig = new WildCardConfig(base.Config, scrapList, skinList, mapObjectsList);
            InitializeAssets();
            HandleHarmony();
            Log.LogInfo("WILDCARD Stuff Successfully Loaded");
        }
        void InitializeMethods()
        {
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
        }
        void LoadFromBundle()
        {
            AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "wildcardmod"));
            string[] allAssetPaths = bundle.GetAllAssetNames();
            for (int i = 0; i < allAssetPaths.Length; i++)
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
                            Log.LogWarning($"\"{allAssetPaths[i]}\" is not a known asset path, skipping.");
                            break;
                        }
                }
            }
        }
        void InitializeAssets()
        {
            InitializeScraps();
            InitializeSkins();
            InitializeMapObjects();
        }
        void InitializeScraps()
        {
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
        }
        void InitializeSkins()
        {
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
        }
        void InitializeMapObjects()
        {
            for (int i = 0; i < mapObjectsList.Count; i++)
            {
                if (ModConfig.isMapObjectEnabled[i].Value)
                {
                    if (!ModConfig.useDefaultMapObjectCurve[i].Value)
                    {
                        mapObjectsList[i].curveFunc = MapObjectHelper.MapObjectFunc;
                        Log.LogInfo($"Using config settings for \"{mapObjectsList[i].mapObjectName}\"'s amount curve!");
                    }
                    else
                    {
                        mapObjectsList[i].curveFunc = MapObjectHelper.MapObjectFunc;
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
                    Log.LogInfo($"\"{mapObjectsList[i].mapObjectName}\" map object was disabled!");
                    mapObjectsList.Remove(mapObjectsList[i]);
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
                autoMapObjectsList[i].curveFunc = MapObjectHelper.MapObjectFunc;
                LethalLib.Modules.MapObjects.RegisterMapObject(autoMapObjectsList[i].spawnableMapObject, Levels.LevelTypes.All, autoMapObjectsList[i].curveFunc);
                Log.LogDebug($"\"{autoMapObjectsList[i].mapObjectName}\" is being handled automatically!");
            }
        }
        void HandleHarmony()
        {
            Type[] types = AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly());
            for (int i = 0; i < types.Length; i++)
            {
                Type type = types[i];
                if (type.GetCustomAttributes(typeof(HarmonyAttribute)).Count() > 0)
                {
                    Log.LogDebug($"Running patches of type \"{type}\"");
                    harmony.PatchAll(type);
                }
            }
        }
    }
}