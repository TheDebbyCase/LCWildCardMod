using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using LCWildCardMod.Utils;
namespace LCWildCardMod.Config
{
    public class WildCardConfig
    {
        BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        internal readonly Dictionary<string, ConfigEntry<bool>> isScrapEnabled = new Dictionary<string, ConfigEntry<bool>>();
        internal readonly Dictionary<string, ConfigEntry<string>> scrapSpawnWeights = new Dictionary<string, ConfigEntry<string>>();
        internal readonly Dictionary<string, ConfigEntry<bool>> isSkinEnabled = new Dictionary<string, ConfigEntry<bool>>();
        internal readonly Dictionary<string, ConfigEntry<int>> skinApplyChance = new Dictionary<string, ConfigEntry<int>>();
        internal readonly Dictionary<string, ConfigEntry<bool>> isMapObjectEnabled = new Dictionary<string, ConfigEntry<bool>>();
        internal readonly Dictionary<string, ConfigEntry<bool>> useDefaultMapObjectCurve = new Dictionary<string, ConfigEntry<bool>>();
        internal readonly Dictionary<string, (ConfigEntry<int>, ConfigEntry<int>)> mapObjectMinMax = new Dictionary<string, (ConfigEntry<int>, ConfigEntry<int>)>();
        internal ConfigEntry<bool> assortedScrap;
        internal WildCardConfig(ConfigFile cfg, List<Item> scrapList, List<Skin> skinList, List<MapObject> mapObjectsList)
        {
            cfg.SaveOnConfigSet = false;
            ScrapConfigs(cfg, scrapList);
            SkinConfigs(cfg, skinList);
            MapObjectConfigs(cfg, mapObjectsList);
            ClearOrphanedEntries(cfg);
            cfg.Save();
            cfg.SaveOnConfigSet = true;
        }
        void ScrapConfigs(ConfigFile cfg, List<Item> scrapList)
        {
            string bonusString = "Affected items include";
            for (int i = 0; i < scrapList.Count; i++)
            {
                Item scrap = scrapList[i];
                AdditionalInfo additional = scrap.spawnPrefab.GetComponent<AdditionalInfo>();
                bool defaultEnabled = additional.defaultEnabled;
                string defaultRarities = additional.defaultRarities;
                bool isBonus = additional.isBonus;
                if (isBonus)
                {
                    bonusString = $"{bonusString}, {scrapList[i].itemName}";
                }
                isScrapEnabled.Add(scrap.itemName, cfg.Bind("Scrap", $"Enable {scrap.itemName}?", defaultEnabled, ""));
                scrapSpawnWeights.Add(scrap.itemName, cfg.Bind("Scrap", $"Spawn Weights of {scrap.itemName}!", defaultRarities, "For example: All:20,Vanilla:20,Modded:20,Experimentation:20"));
                Log.LogDebug($"Added config for {scrap.itemName}");
            }
            assortedScrap = cfg.Bind("Scrap", "Enable/Disable supplementary scrap overall", true, bonusString);
            Log.LogDebug("Added config for Assorted Scrap items");
        }
        void SkinConfigs(ConfigFile cfg, List<Skin> skinList)
        {
            for (int i = 0; i < skinList.Count; i++)
            {
                Skin skin = skinList[i];
                bool defaultEnabled = skin.skinEnabled;
                int defaultChance = skin.skinChance;
                isSkinEnabled.Add(skin.skinName, cfg.Bind("Skins", $"Enable {skin.skinName}?", defaultEnabled, ""));
                string skinTargetName = "";
                if (skin.target is EnemyType)
                {
                    skinTargetName = (skin.target as EnemyType).enemyName;
                }
                else if (skin.target is Item)
                {
                    skinTargetName = (skin.target as Item).itemName;
                }
                skinApplyChance.Add(skin.skinName, cfg.Bind("Skins", $"Weighted chance that {skinTargetName} will spawn as {skin.skinName}", defaultChance, "Must be an integer greater than, or equal to, 0.\nThe chance that no skin will be applied is 100 subtracted by this weight"));
                Log.LogDebug($"Added config for {skin.skinName}");
            }
        }
        void MapObjectConfigs(ConfigFile cfg, List<MapObject> mapObjectsList)
        {
            for (int i = 0; i < mapObjectsList.Count; i++)
            {
                MapObject mapObject = mapObjectsList[i];
                if (mapObject.autoHandle)
                {
                    continue;
                }
                isMapObjectEnabled.Add(mapObject.mapObjectName, cfg.Bind("Map Objects", $"Enable {mapObject.mapObjectName}?", true, ""));
                useDefaultMapObjectCurve.Add(mapObject.mapObjectName, cfg.Bind("Map Objects", $"Use default min and max values for {mapObject.mapObjectName}?", true, ""));
                ConfigEntry<int> mapObjectMin = cfg.Bind("Map Objects", $"Minimum number of {mapObject.mapObjectName} to spawn per level", 0, "This only matters if you set \"Use Default\" to false.\nMust be an integer of 0 or above");
                ConfigEntry<int> mapObjectMax = cfg.Bind("Map Objects", $"Maximum number of {mapObject.mapObjectName} to spawn per level", 1, "This only matters if you set \"Use Default\" to false.\nMust be an integer of 0 or above");
                mapObjectMinMax.Add(mapObject.mapObjectName, (mapObjectMin, mapObjectMax));
                Log.LogDebug($"Added config for {mapObject.mapObjectName}");
            }
        }
        static void ClearOrphanedEntries(ConfigFile cfg)
        {
            PropertyInfo orphanedEntriesProp = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries");
            Dictionary<ConfigDefinition, string> orphanedEntries = orphanedEntriesProp.GetValue(cfg) as Dictionary<ConfigDefinition, string>;
            orphanedEntries.Clear();
        }
    }
}