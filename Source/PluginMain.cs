using static BepInEx.BepInDependency;
using BepInEx;
using LethalLib.Modules;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using System;
using BepInEx.Logging;
using HarmonyLib;
using LCWildCardMod.Config;
using LCWildCardMod.Utils;
using LCWildCardMod.Patches;
using LCWildCardMod.Items;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
namespace LCWildCardMod
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency(LethalLib.Plugin.ModGUID, DependencyFlags.HardDependency)]
    [BepInDependency(LethalCompanyInputUtils.MyPluginInfo.PLUGIN_GUID, DependencyFlags.HardDependency)]
    public class WildCardMod : BaseUnityPlugin
    {
        public const string modGUID = "deB.WildCard";
        public const string modName = "WILDCARD Stuff";
        public const string modVersion = "2.0.0";
        private ReadOnlyDictionary<string, Type[]> publicHarmonies;
        private readonly Dictionary<string, (Harmony, Type[])> harmonies = new Dictionary<string, (Harmony, Type[])>();
        private ReadOnlyCollection<WildCardItem> publicScrapList;
        internal List<WildCardItem> scrapList = new List<WildCardItem>();
        private ReadOnlyCollection<WildCardSkin> publicSkinList;
        internal List<WildCardSkin> skinList = new List<WildCardSkin>();
        private ReadOnlyCollection<WildCardMapObject> publicMapObjectList;
        internal List<WildCardMapObject> mapObjectList = new List<WildCardMapObject>();
        internal ManualLogSource Log => Logger;
        internal Dictionary<string, (Harmony, Type[])> Harmonies => harmonies;
        public KeyBinds KeyBinds { get; private set; }
        public static WildCardMod Instance { get; private set; }
        public WildCardConfig ModConfig { get; private set; }
        public ReadOnlyDictionary<string, Type[]> HarmonyTypes
        {
            get
            {
                publicHarmonies ??= new ReadOnlyDictionary<string, Type[]>(new Dictionary<string, Type[]>(Harmonies.Select((x) => new KeyValuePair<string, Type[]>(x.Key, x.Value.Item2))));
                return publicHarmonies;
            }
        }
        public ReadOnlyCollection<WildCardItem> ScrapList
        {
            get
            {
                publicScrapList ??= new ReadOnlyCollection<WildCardItem>(scrapList);
                return publicScrapList;
            }
        }
        public ReadOnlyCollection<WildCardSkin> SkinList
        {
            get
            {
                publicSkinList ??= new ReadOnlyCollection<WildCardSkin>(skinList);
                return publicSkinList;
            }
        }
        public ReadOnlyCollection<WildCardMapObject> MapObjectList
        {
            get
            {
                publicMapObjectList ??= new ReadOnlyCollection<WildCardMapObject>(mapObjectList);
                return publicMapObjectList;
            }
        }
        [SuppressMessage("CodeQuality", "IDE0051")]
        private void Awake()
        {
            Instance = this;
            KeyBinds = new KeyBinds();
            InitializeMethods();
            LoadFromBundle();
            InitializeAssets();
            HandleHarmony();
            HandleEvents();
            Log.LogInfo($"{modName} Successfully Loaded");
        }
        private void InitializeMethods()
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
        private void LoadFromBundle()
        {
            AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "wildcardmod"));
            string[] allAssetPaths = bundle.GetAllAssetNames();
            for (int i = 0; i < allAssetPaths.Length; i++)
            {
                switch (allAssetPaths[i][..allAssetPaths[i].LastIndexOf('/')])
                {
                    case "assets/my creations/scrap items":
                        {
                            scrapList.Add(bundle.LoadAsset<WildCardItem>(allAssetPaths[i]));
                            break;
                        }
                    case "assets/my creations/skins":
                        {
                            skinList.Add(bundle.LoadAsset<WildCardSkin>(allAssetPaths[i]));
                            break;
                        }
                    case "assets/my creations/map objects":
                        {
                            mapObjectList.Add(bundle.LoadAsset<WildCardMapObject>(allAssetPaths[i]));
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
        private void ModifyPrefab(GameObject toModify)
        {
            toModify.GetComponentInChildren<IWildCardBase>()?.Initialize();
            NetworkPrefabs.RegisterNetworkPrefab(toModify);
        }
        internal void InitializeAssets()
        {
            ModConfig ??= new WildCardConfig(Config, scrapList, skinList, mapObjectList);
            InitializeScraps();
            InitializeSkins();
            InitializeMapObjects();
        }
        private void InitializeScraps()
        {
            publicScrapList = null;
            for (int i = 0; i < scrapList.Count; i++)
            {
                WildCardItem item = scrapList[i];
                string scrapName = item.itemName;
                bool enabledBefore = item.enabled;
                item.enabled = ModConfig.ScrapEnabled[scrapName];
                if (!item.enabled)
                {
                    if (enabledBefore)
                    {
                        if (ILifeSaver.IsLifeSaver(item.spawnPrefab, out string disabledLifeSaverName, out _))
                        {
                            ILifeSaver.AllLifeSavers.Remove(disabledLifeSaverName);
                        }
                        LethalLib.Modules.Items.RemoveScrapFromLevels(item, Levels.LevelTypes.All);
                        Log.LogInfo($"\"{scrapName}\" scrap was disabled!");
                    }
                    continue;
                }
                if (enabledBefore)
                {
                    continue;
                }
                if (ILifeSaver.IsLifeSaver(item.spawnPrefab, out string enabledLifeSaverName, out _))
                {
                    ILifeSaver.AllLifeSavers.Add(enabledLifeSaverName, new List<ILifeSaver>());
                }
                Dictionary<Levels.LevelTypes, int> scrapLevelWeights = new Dictionary<Levels.LevelTypes, int>();
                Dictionary<string, int> scrapModdedWeights = new Dictionary<string, int>();
                string[] configScrapStringArray = ModConfig.ScrapSpawnWeights[scrapName].Split(",");
                for (int j = 0; j < configScrapStringArray.Length; j++)
                {
                    string[] pair = configScrapStringArray[j].Split(':');
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
                ModifyPrefab(item.spawnPrefab);
                LethalLib.Modules.Items.RegisterScrap(item, scrapLevelWeights, scrapModdedWeights);
                Log.LogDebug($"\"{scrapName}\" scrap was enabled!");
            }
        }
        private void InitializeSkins()
        {
            publicSkinList = null;
            for (int i = 0; i < skinList.Count; i++)
            {
                WildCardSkin skin = skinList[i];
                string skinName = skin.skinName;
                bool enabledBefore = skin.enabled;
                skin.enabled = ModConfig.SkinEnabled[skinName];
                if (!skin.enabled)
                {
                    if (enabledBefore)
                    {
                        Log.LogInfo($"\"{skinName}\" skin was disabled!");
                    }
                    continue;
                }
                if (enabledBefore)
                {
                    continue;
                }
                Log.LogDebug($"\"{skinName}\" skin was enabled!");
            }
        }
        private void InitializeMapObjects()
        {
            publicMapObjectList = null;
            for (int i = 0; i < mapObjectList.Count; i++)
            {
                WildCardMapObject mapObject = mapObjectList[i];
                string mapObjectName = mapObject.mapObjectName;
                bool enabledBefore = mapObject.enabled;
                if (mapObject.autoHandle)
                {
                    bool configEnabled = true;
                    if (mapObject.configBools != null)
                    {
                        for (int j = 0; j < mapObject.configBools.Length; j++)
                        {
                            if (ModConfig.ScrapEnabled.TryGetValue(mapObject.configBools[j], out bool isEnabled) && !isEnabled)
                            {
                                configEnabled = false;
                                break;
                            }
                            if (ModConfig.SkinEnabled.TryGetValue(mapObject.configBools[j], out isEnabled) && !isEnabled)
                            {
                                configEnabled = false;
                                break;
                            }
                            if (ModConfig.MapObjectEnabled.TryGetValue(mapObject.configBools[j], out isEnabled) && !isEnabled)
                            {
                                configEnabled = false;
                                break;
                            }
                        }
                    }
                    mapObject.enabled = configEnabled;
                }
                else
                {
                    mapObject.enabled = ModConfig.MapObjectEnabled.TryGetValue(mapObjectName, out bool isEnabled) && isEnabled;
                }
                if (!mapObject.enabled)
                {
                    if (enabledBefore)
                    {
                        LethalLib.Modules.MapObjects.RemoveMapObject(mapObject.spawnableMapObject, Levels.LevelTypes.All);
                        Log.LogInfo($"\"{mapObjectName}\" map object was disabled!");
                    }
                    continue;
                }
                if (enabledBefore)
                {
                    continue;
                }
                ModifyPrefab(mapObject.spawnableMapObject.prefabToSpawn);
                if (mapObject.spawnableMapObject.prefabToSpawn.TryGetComponent(out WildCardProp mapObjectScrap) && LethalLib.Modules.Items.plainItems.Find((x) => x.item == mapObjectScrap.itemProperties) == null)
                {
                    ModifyPrefab(mapObjectScrap.itemProperties.spawnPrefab);
                    LethalLib.Modules.Items.RegisterItem(mapObjectScrap.itemProperties);
                }
                LethalLib.Modules.MapObjects.RegisterMapObject(mapObject.spawnableMapObject, Levels.LevelTypes.All, mapObject.GetCurveFunc());
                Log.LogDebug($"\"{mapObjectName}\" map object was enabled!");
            }
        }
        internal void HandleHarmony()
        {
            publicHarmonies = null;
            HarmonyHelper.TogglePatches(modGUID, true, typeof(NecessaryPatches));
            HarmonyHelper.TogglePatches($"{modGUID}.debug", ModConfig.Debug, typeof(DebugPatches));
            HarmonyHelper.TogglePatches($"{modGUID}.skins", WildCardSkin.AnyEnabled, typeof(SkinsPatches));
            HarmonyHelper.TogglePatches($"{modGUID}.cojiro", ModConfig.ScrapEnabled.TryGetValue("Cojiro", out bool cojiroEnabled) && cojiroEnabled, typeof(CojiroPatches));
            HarmonyHelper.TogglePatches($"{modGUID}.save", ILifeSaver.AnyEnabled, typeof(EnemyAIGraceSavePatch), typeof(SavePatches));
        }
        private void HandleEvents()
        {
            
        }
    }
}