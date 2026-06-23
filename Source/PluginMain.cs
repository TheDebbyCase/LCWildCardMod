using static BepInEx.BepInDependency;
using static LethalLib.Modules.Items;
using static LethalLib.Modules.MapObjects;
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
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency(LethalLib.Plugin.ModGUID, DependencyFlags.HardDependency)]
    [BepInDependency(LethalCompanyInputUtils.MyPluginInfo.PLUGIN_GUID, DependencyFlags.HardDependency)]
    public class WildCardMod : BaseUnityPlugin
    {
        public const string ModGUID = "deB.WildCard";
        public const string ModName = "WILDCARD Stuff";
        public const string ModVersion = "2.0.8";
        private static ReadOnlyDictionary<string, Type[]> publicHarmonies = null;
        private static ReadOnlyCollection<WildCardItem> publicScrapList = null;
        private static ReadOnlyCollection<WildCardSkin> publicSkinList = null;
        private static ReadOnlyCollection<WildCardMapObject> publicMapObjectList = null;
        private static readonly Dictionary<string, (Harmony, Type[])> harmonies = new Dictionary<string, (Harmony, Type[])>();
        internal static readonly List<WildCardItem> scrapList = new List<WildCardItem>();
        internal static readonly List<WildCardSkin> skinList = new List<WildCardSkin>();
        internal static readonly List<WildCardMapObject> mapObjectList = new List<WildCardMapObject>();
        private bool initialized = false;
        internal ManualLogSource Log => Logger;
        internal static Dictionary<string, (Harmony, Type[])> Harmonies => harmonies;
        public static KeyBinds KeyBinds { get; private set; }
        public static WildCardMod Instance { get; private set; }
        public static WildCardConfig ModConfig { get; private set; }
        public static ReadOnlyDictionary<string, Type[]> HarmonyTypes
        {
            get
            {
                publicHarmonies ??= new ReadOnlyDictionary<string, Type[]>(new Dictionary<string, Type[]>(Harmonies.Select((x) => new KeyValuePair<string, Type[]>(x.Key, x.Value.Item2))));
                return publicHarmonies;
            }
        }
        public static ReadOnlyCollection<WildCardItem> ScrapList
        {
            get
            {
                publicScrapList ??= new ReadOnlyCollection<WildCardItem>(scrapList);
                return publicScrapList;
            }
        }
        public static ReadOnlyCollection<WildCardSkin> SkinList
        {
            get
            {
                publicSkinList ??= new ReadOnlyCollection<WildCardSkin>(skinList);
                return publicSkinList;
            }
        }
        public static ReadOnlyCollection<WildCardMapObject> MapObjectList
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
            try
            {
                LoadFromBundle();
            }
            catch (Exception exception)
            {
                Log.LogError($"{ModName} Failed to Load!");
                Log.LogError(exception);
                return;
            }
            if (!AssetUpdate())
            {
                return;
            }
            Log.LogInfo($"{ModName} Successfully Loaded");
            initialized = true;
        }
        internal bool AssetUpdate(AssetType toUpdate = AssetType.All)
        {
            bool success = true;
            try
            {
                InitializeAssets(toUpdate);
                HandleHarmony(toUpdate);
            }
            catch (Exception exception)
            {
                if (initialized)
                {
                    Log.LogWarning($"{ModName} Failed to Update Assets!");
                    Log.LogWarning(exception);
                }
                else
                {
                    Log.LogError($"{ModName} Failed to Load!");
                    Log.LogError(exception);
                }
                success = false;
            }
            return success;
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
        private void InitializeAssets(AssetType toUpdate)
        {
            if (!initialized)
            {
                ModConfig = new WildCardConfig(Config, scrapList, skinList, mapObjectList);
            }
            ModConfig.ResetReadonlyDicts();
            bool doAll = (int)toUpdate == 7;
            if (doAll || toUpdate.HasFlag(AssetType.Scrap))
            {
                InitializeScraps();
            }
            if (doAll || toUpdate.HasFlag(AssetType.Skin))
            {
                InitializeSkins();
            }
            if (doAll || toUpdate.HasFlag(AssetType.MapObject))
            {
                InitializeMapObjects();
            }
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
                        if (ILifeSaver.IsLifeSaver(item.spawnPrefab, out ILifeSaver disabledLifeSaver))
                        {
                            ILifeSaver.AllLifeSavers.Remove(disabledLifeSaver.GetType().Name);
                        }
                        RemoveScrapFromLevels(item, Levels.LevelTypes.All);
                        scrapItems.Remove(scrapItems.FirstOrDefault((x) => x.origItem == item));
                        Log.LogInfo($"\"{scrapName}\" scrap was disabled!");
                    }
                    continue;
                }
                if (enabledBefore)
                {
                    continue;
                }
                if (ILifeSaver.IsLifeSaver(item.spawnPrefab, out ILifeSaver enabledLifeSaver))
                {
                    ILifeSaver.AllLifeSavers.Add(enabledLifeSaver.GetType().Name, new List<ILifeSaver>());
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
                RegisterScrap(item, scrapLevelWeights, scrapModdedWeights);
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
                            if ((ModConfig.ScrapEnabled.TryGetValue(mapObject.configBools[j], out bool isEnabled) || ModConfig.SkinEnabled.TryGetValue(mapObject.configBools[j], out isEnabled) || ModConfig.MapObjectEnabled.TryGetValue(mapObject.configBools[j], out isEnabled)) && !isEnabled)
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
                        WildUtils.RemoveMapObject(mapObject, Levels.LevelTypes.All);
                        mapObjects.Remove(mapObjects.FirstOrDefault((x) => x.indoorMapHazardType == mapObject));
                        if (mapObject.prefabToSpawn.TryGetComponent(out WildCardProp oldMapObjectScrap))
                        {
                            plainItems.Remove(plainItems.FirstOrDefault((x) => x.item == oldMapObjectScrap.itemProperties));
                        }
                        Log.LogInfo($"\"{mapObjectName}\" map object was disabled!");
                    }
                    continue;
                }
                if (enabledBefore)
                {
                    continue;
                }
                ModifyPrefab(mapObject.prefabToSpawn);
                if (mapObject.prefabToSpawn.TryGetComponent(out WildCardProp mapObjectScrap) && plainItems.Find((x) => x.item == mapObjectScrap.itemProperties) == null)
                {
                    ModifyPrefab(mapObjectScrap.itemProperties.spawnPrefab);
                    RegisterItem(mapObjectScrap.itemProperties);
                }
                WildUtils.RegisterMapObject(mapObject, Levels.LevelTypes.All, mapObject.GetCurveFunc());
                Log.LogDebug($"\"{mapObjectName}\" map object was enabled!");
            }
        }
        private void HandleHarmony(AssetType toUpdate)
        {
            publicHarmonies = null;
            bool doAll = (int)toUpdate == 7;
            if (doAll)
            {
                HarmonyHelper.TogglePatches(ModGUID, true, typeof(NecessaryPatches));
            }
            if (doAll || toUpdate.HasFlag(AssetType.Scrap))
            {
                HarmonyHelper.TogglePatches($"{ModGUID}.cojiro", ModConfig.ScrapEnabled.TryGetValue("Cojiro", out bool cojiroEnabled) && cojiroEnabled, typeof(CojiroPatches));
                HarmonyHelper.TogglePatches($"{ModGUID}.save", ILifeSaver.AnyEnabled, typeof(SavePatches));
                HarmonyHelper.TogglePatches($"{ModGUID}.grace", ILifeSaver.AnyEnabled, typeof(EnemyAIGraceSavePatch));
            }
            if (doAll || toUpdate.HasFlag(AssetType.Skin))
            {
                HarmonyHelper.TogglePatches($"{ModGUID}.skins", WildCardSkin.AnyEnabled, typeof(SkinsPatches));
            }
            //if (doAll || toUpdate.HasFlag(AssetType.MapObject))
            //{

            //}
            HarmonyHelper.TogglePatches($"{ModGUID}.debug", ModConfig.Debug, typeof(DebugPatches));
        }
    }
}