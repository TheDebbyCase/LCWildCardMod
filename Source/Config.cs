using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using LCWildCardMod.Utils;
using UnityEngine;
namespace LCWildCardMod.Config
{
    public class WildCardConfig
    {
        private static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        private static readonly PropertyInfo orphanedEntriesProp = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries");
        internal WildCardConfig(ConfigFile cfg, List<WildCardItem> scrapList, List<WildCardSkin> skinList, List<WildCardMapObject> mapObjectsList)
        {
            config = cfg;
            config.SaveOnConfigSet = false;
            ScrapConfigs(scrapList);
            SkinConfigs(skinList);
            MapObjectConfigs(mapObjectsList);
            debugLogs = config.Bind("Miscellaneous", "Enable Extra Debug Logging?", false);
            ClearOrphanedEntries(config);
            config.Save();
            config.SaveOnConfigSet = true;
            config.SettingChanged += OnSettingChanged;
        }
        private ReadOnlyDictionary<string, bool> publicScrapEnabled;
        private ReadOnlyDictionary<string, string> publicScrapSpawnWeights;
        private ReadOnlyDictionary<string, bool> publicSkinEnabled;
        private ReadOnlyDictionary<string, int> publicSkinApplyChance;
        private ReadOnlyDictionary<string, bool> publicMapObjectEnabled;
        private ReadOnlyDictionary<string, bool> publicDefaultMapObjectCurve;
        private ReadOnlyDictionary<string, Vector2Int> publicMapObjectMinMax;
        private readonly Dictionary<string, ConfigEntry<bool>> isScrapEnabled = new Dictionary<string, ConfigEntry<bool>>();
        private readonly Dictionary<string, ConfigEntry<string>> scrapSpawnWeights = new Dictionary<string, ConfigEntry<string>>();
        private readonly Dictionary<string, ConfigEntry<bool>> isSkinEnabled = new Dictionary<string, ConfigEntry<bool>>();
        private readonly Dictionary<string, ConfigEntry<int>> skinApplyChance = new Dictionary<string, ConfigEntry<int>>();
        private readonly Dictionary<string, ConfigEntry<bool>> isMapObjectEnabled = new Dictionary<string, ConfigEntry<bool>>();
        private readonly Dictionary<string, ConfigEntry<bool>> useDefaultMapObjectCurve = new Dictionary<string, ConfigEntry<bool>>();
        private readonly Dictionary<string, (ConfigEntry<int>, ConfigEntry<int>)> mapObjectMinMax = new Dictionary<string, (ConfigEntry<int>, ConfigEntry<int>)>();
        private readonly ConfigEntry<bool> debugLogs;
        private readonly ConfigFile config;
        public ReadOnlyDictionary<string, bool> ScrapEnabled => publicScrapEnabled;
        public ReadOnlyDictionary<string, string> ScrapSpawnWeights => publicScrapSpawnWeights;
        public ReadOnlyDictionary<string, bool> SkinEnabled => publicSkinEnabled;
        public ReadOnlyDictionary<string, int> SkinApplyChance => publicSkinApplyChance;
        public ReadOnlyDictionary<string, bool> MapObjectEnabled => publicMapObjectEnabled;
        public ReadOnlyDictionary<string, bool> DefaultMapObjectCurve => publicDefaultMapObjectCurve;
        public ReadOnlyDictionary<string, Vector2Int> MapObjectMinMax => publicMapObjectMinMax;
        public bool Debug => debugLogs.Value;
        private static void ClearOrphanedEntries(ConfigFile config)
        {
            (orphanedEntriesProp.GetValue(config) as Dictionary<ConfigDefinition, string>).Clear();
        }
        private void ScrapConfigs(List<WildCardItem> scrapList)
        {
            for (int i = 0; i < scrapList.Count; i++)
            {
                WildCardItem scrap = scrapList[i];
                string itemName = scrap.itemName;
                isScrapEnabled.TryAdd(itemName, config.Bind("Scrap", $"Enable {itemName}?", scrap.defaultEnabled, ""));
                scrapSpawnWeights.TryAdd(itemName, config.Bind("Scrap", $"Spawn Weights of {itemName}!", scrap.defaultRarities, "For example: All:20,Vanilla:20,Modded:20,Experimentation:20"));
                Log.LogDebug($"Added config for {itemName}");
            }
        }
        private void SkinConfigs(List<WildCardSkin> skinList)
        {
            for (int i = 0; i < skinList.Count; i++)
            {
                WildCardSkin skin = skinList[i];
                string skinName = skin.skinName;
                bool defaultEnabled = skin.skinEnabled;
                int defaultChance = skin.skinChance;
                isSkinEnabled.TryAdd(skinName, config.Bind("Skins", $"Enable {skinName}?", defaultEnabled, ""));
                skinApplyChance.TryAdd(skinName, config.Bind("Skins", $"Weighted chance that {skin.target} will spawn as {skinName}", defaultChance, "Must be an integer greater than, or equal to, 0.\nThe chance that no skin will be applied is 100 subtracted by this weight"));
                Log.LogDebug($"Added config for {skinName}");
            }
        }
        private void MapObjectConfigs(List<WildCardMapObject> mapObjectsList)
        {
            for (int i = 0; i < mapObjectsList.Count; i++)
            {
                WildCardMapObject mapObject = mapObjectsList[i];
                if (mapObject.autoHandle)
                {
                    continue;
                }
                string mapObjectName = mapObject.mapObjectName;
                isMapObjectEnabled.TryAdd(mapObjectName, config.Bind("Map Objects", $"Enable {mapObjectName}?", true, ""));
                useDefaultMapObjectCurve.TryAdd(mapObjectName, config.Bind("Map Objects", $"Use default min and max values for {mapObjectName}?", true, ""));
                ConfigEntry<int> mapObjectMin = config.Bind("Map Objects", $"Minimum number of {mapObjectName} to spawn per level", 0, "This only matters if you set \"Use Default\" to false.\nMust be an integer of 0 or above");
                ConfigEntry<int> mapObjectMax = config.Bind("Map Objects", $"Maximum number of {mapObjectName} to spawn per level", 1, "This only matters if you set \"Use Default\" to false.\nMust be an integer of 0 or above");
                mapObjectMinMax.TryAdd(mapObjectName, (mapObjectMin, mapObjectMax));
                Log.LogDebug($"Added config for {mapObjectName}");
            }
        }
        internal void ResetReadonlyDicts()
        {
            publicScrapEnabled = new ReadOnlyDictionary<string, bool>(new Dictionary<string, bool>(isScrapEnabled.Select((x) => new KeyValuePair<string, bool>(x.Key, x.Value.Value))));
            publicScrapSpawnWeights = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(scrapSpawnWeights.Select((x) => new KeyValuePair<string, string>(x.Key, x.Value.Value))));
            publicSkinEnabled = new ReadOnlyDictionary<string, bool>(new Dictionary<string, bool>(isSkinEnabled.Select((x) => new KeyValuePair<string, bool>(x.Key, x.Value.Value))));
            publicSkinApplyChance = new ReadOnlyDictionary<string, int>(new Dictionary<string, int>(skinApplyChance.Select((x) => new KeyValuePair<string, int>(x.Key, x.Value.Value))));
            publicMapObjectEnabled = new ReadOnlyDictionary<string, bool>(new Dictionary<string, bool>(isMapObjectEnabled.Select((x) => new KeyValuePair<string, bool>(x.Key, x.Value.Value))));
            publicDefaultMapObjectCurve = new ReadOnlyDictionary<string, bool>(new Dictionary<string, bool>(useDefaultMapObjectCurve.Select((x) => new KeyValuePair<string, bool>(x.Key, x.Value.Value))));
            publicMapObjectMinMax = new ReadOnlyDictionary<string, Vector2Int>(new Dictionary<string, Vector2Int>(mapObjectMinMax.Select((x) => new KeyValuePair<string, Vector2Int>(x.Key, new Vector2Int(x.Value.Item1.Value, x.Value.Item2.Value)))));
        }
        private void OnSettingChanged(object sender, SettingChangedEventArgs args)
        {
            AssetType selectedType;
            switch (args.ChangedSetting.Definition.Section)
            {
                case "Scrap":
                    {
                        selectedType = AssetType.Scrap;
                        break;
                    }
                case "Skins":
                    {
                        selectedType = AssetType.Skin;
                        break;
                    }
                case "Map Objects":
                    {
                        selectedType = AssetType.MapObject;
                        break;
                    }
                default:
                    {
                        selectedType = AssetType.None;
                        break;
                    }
            }
            WildCardMod.Instance.AssetUpdate(selectedType);
        }
    }
}