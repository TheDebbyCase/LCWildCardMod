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
using BepInEx.Configuration;
using LCWildCardMod.Patches;
namespace LCWildCardMod
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("me.swipez.melonloader.morecompany", DependencyFlags.SoftDependency)]
    [BepInDependency("evaisa.lethallib", DependencyFlags.HardDependency)]
    [BepInDependency("com.rune580.LethalCompanyInputUtils", DependencyFlags.HardDependency)]
    public class WildCardMod : BaseUnityPlugin
    {
        public const string modGUID = "deB.WildCard";
        public const string modName = "WILDCARD Stuff";
        public const string modVersion = "1.2.9";
        public List<Item> scrapList = new List<Item>();
        public List<Skin> skinList = new List<Skin>();
        public List<MapObject> mapObjectsList = new List<MapObject>();
        internal ManualLogSource Log => Logger;
        internal KeyBinds KeyBinds { get; private set; } = null!;
        internal static WildCardMod Instance { get; private set; } = null!;
        internal WildCardConfig ModConfig { get; private set; } = null!;
        readonly Harmony harmony = new Harmony(modGUID);
        void Awake()
        {
            Instance = this;
            KeyBinds = new KeyBinds();
            InitializeMethods();
            LoadFromBundle();
            ModConfig = new WildCardConfig(Config, scrapList, skinList, mapObjectsList);
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
                    if (methodAttributes.Length == 0)
                    {
                        continue;
                    }
                    typeMethods[j].Invoke(null, null);
                }
            }
        }
        void LoadFromBundle()
        {
            AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "wildcardmod"));
            string[] allAssetPaths = bundle.GetAllAssetNames();
            for (int i = 0; i < allAssetPaths.Length; i++)
            {
                switch (allAssetPaths[i][..allAssetPaths[i].LastIndexOf('/')])
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
                            mapObjectsList.Add(bundle.LoadAsset<MapObject>(allAssetPaths[i]));
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
                Item item = scrapList[i];
                string scrapName = item.itemName;
                if (!ModConfig.isScrapEnabled[scrapName].Value || (item.spawnPrefab.GetComponent<AdditionalInfo>().isBonus && !ModConfig.assortedScrap.Value))
                {
                    Log.LogInfo($"\"{scrapName}\" scrap was disabled!");
                    scrapList.RemoveAt(i);
                    i--;
                    continue;
                }
                Dictionary<Levels.LevelTypes, int> scrapLevelWeights = new Dictionary<Levels.LevelTypes, int>();
                Dictionary<string, int> scrapModdedWeights = new Dictionary<string, int>();
                string[] configScrapStringArray = ModConfig.scrapSpawnWeights[scrapName].Value.Split(",");
                for (int j = 0; j < configScrapStringArray.Length; j++)
                {
                    string configScrapString = configScrapStringArray[j];
                    string[] pair = configScrapString.Split(':');
                    if (pair.Length != 2)
                    {
                        continue;
                    }
                    if (!int.TryParse(pair[1], out int weight))
                    {
                        continue;
                    }
                    string levelName = pair[0];
                    if (Levels.LevelTypes.TryParse(levelName, out Levels.LevelTypes configScrapLevel))
                    {
                        scrapLevelWeights.Add(configScrapLevel, weight);
                        continue;
                    }
                    scrapModdedWeights.Add(levelName, weight);
                }
                NetworkPrefabs.RegisterNetworkPrefab(item.spawnPrefab);
                Utilities.FixMixerGroups(item.spawnPrefab);
                LethalLib.Modules.Items.RegisterScrap(item, scrapLevelWeights, scrapModdedWeights);
                Log.LogDebug($"\"{scrapName}\" scrap was loaded!");
            }
        }
        void InitializeSkins()
        {
            for (int i = 0; i < skinList.Count; i++)
            {
                Skin skin = skinList[i];
                string skinName = skin.skinName;
                if (ModConfig.isSkinEnabled[skinName].Value)
                {
                    Log.LogDebug($"\"{skinName}\" skin was loaded!");
                }
                else
                {
                    Log.LogInfo($"\"{skinName}\" skin was disabled!");
                    skinList.RemoveAt(i);
                    i--;
                }
            }
        }
        void InitializeMapObjects()
        {
            for (int i = 0; i < mapObjectsList.Count; i++)
            {
                MapObject mapObject = mapObjectsList[i];
                string mapObjectName = mapObject.mapObjectName;
                if (ModConfig.isMapObjectEnabled.TryGetValue(mapObjectName, out ConfigEntry<bool> isEnabled) && !isEnabled.Value)
                {
                    Log.LogInfo($"\"{mapObjectName}\" map object was disabled!");
                    mapObjectsList.RemoveAt(i);
                    i--;
                    continue;
                }
                NetworkPrefabs.RegisterNetworkPrefab(mapObject.spawnableMapObject.prefabToSpawn);
                Utilities.FixMixerGroups(mapObject.spawnableMapObject.prefabToSpawn);
                if (mapObject.spawnableMapObject.prefabToSpawn.TryGetComponent(out GrabbableObject mapObjectScrap))
                {
                    LethalLib.Modules.Items.RegisterItem(mapObjectScrap.itemProperties);
                }
                LethalLib.Modules.MapObjects.RegisterMapObject(mapObject.spawnableMapObject, Levels.LevelTypes.All, mapObject.GetCurveFunc());
                Log.LogDebug($"\"{mapObjectName}\" map object was loaded!");
            }
        }
        void HandleHarmony()
        {
            harmony.PatchAll(typeof(NecessaryPatches));
            if (ModConfig.isSkinEnabled.Any((x) => x.Value.Value))
            {
                harmony.PatchAll(typeof(SkinsPatches));
            }
            if (ModConfig.isScrapEnabled.TryGetValue("Cojiro", out ConfigEntry<bool> cojiroEnabled) && cojiroEnabled.Value)
            {
                harmony.PatchAll(typeof(CojiroPatches));
            }
            if ((ModConfig.isScrapEnabled.TryGetValue("Halo", out ConfigEntry<bool> haloEnabled) && haloEnabled.Value) || (ModConfig.isScrapEnabled.TryGetValue("Fyrus Star", out ConfigEntry<bool> starEnabled) && starEnabled.Value))
            {
                harmony.PatchAll(typeof(EnemyAIFyrusOrHaloGraceSavePatch));
                harmony.PatchAll(typeof(SavePatches));
            }
        }
    }
}