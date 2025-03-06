using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using LCWildCardMod.Utils;
namespace LCWildCardMod.Config
{
    public class WildCardConfig
    {
        readonly BepInEx.Logging.ManualLogSource log = WildCardMod.Log;
        internal readonly List<ConfigEntry<bool>> isScrapEnabled = new List<ConfigEntry<bool>>();
        internal readonly List<ConfigEntry<string>> scrapSpawnWeights = new List<ConfigEntry<string>>();
        internal readonly List<ConfigEntry<bool>> isSkinEnabled = new List<ConfigEntry<bool>>();
        internal readonly List<ConfigEntry<int>> skinApplyChance = new List<ConfigEntry<int>>();
        internal readonly List<ConfigEntry<bool>> isMapObjectEnabled = new List<ConfigEntry<bool>>();
        internal readonly List<ConfigEntry<bool>> useDefaultMapObjectCurve = new List<ConfigEntry<bool>>();
        internal readonly List<ConfigEntry<int>> mapObjectMinNo = new List<ConfigEntry<int>>();
        internal readonly List<ConfigEntry<int>> mapObjectMaxNo = new List<ConfigEntry<int>>();
        internal readonly ConfigEntry<bool> assortedScrap;
        internal string bonusString = "Affected items include";
        internal WildCardConfig(ConfigFile cfg, List<Item> scrapList, List<Skin> skinList, List<MapObject> mapObjectsList)
        {
            cfg.SaveOnConfigSet = false;
            for (int i = 0; i < scrapList.Count; i++)
            {
                bool defaultEnabled = scrapList[i].spawnPrefab.GetComponent<AdditionalInfo>().defaultEnabled;
                string defaultRarities = scrapList[i].spawnPrefab.GetComponent<AdditionalInfo>().defaultRarities;
                bool isBonus = scrapList[i].spawnPrefab.GetComponent<AdditionalInfo>().isBonus;
                if (isBonus)
                {
                    bonusString = $"{bonusString}, {scrapList[i].itemName}";
                }
                isScrapEnabled.Add(cfg.Bind("Scrap", $"Enable {scrapList[i].itemName}?", defaultEnabled, ""));
                scrapSpawnWeights.Add(cfg.Bind("Scrap", $"Spawn Weights of {scrapList[i].itemName}!", defaultRarities, "For example: All:20,Vanilla:20,Modded:20,Experimentation:20"));
                log.LogDebug($"Added config for {scrapList[i].itemName}");
            }
            assortedScrap = cfg.Bind("Scrap", "Enable/Disable supplementary scrap overall", true, bonusString);
            log.LogDebug("Added config for Assorted Scrap items");
            for (int i = 0; i < skinList.Count; i++)
            {
                bool defaultEnabled = skinList[i].skinEnabled;
                int defaultChance = skinList[i].skinChance;
                isSkinEnabled.Add(cfg.Bind("Skins", $"Enable {skinList[i].skinName}?", defaultEnabled, ""));
                string skinTargetName = "";
                if (skinList[i].targetEnemy != null)
                {
                    skinTargetName = skinList[i].targetEnemy.enemyName;
                }
                else if (skinList[i].targetItem != null)
                {
                    skinTargetName = skinList[i].targetItem.itemName;
                }
                skinApplyChance.Add(cfg.Bind("Skins", $"Weighted chance that {skinTargetName} will spawn as {skinList[i].skinName}", defaultChance, "Must be an integer greater than, or equal to, 0.\nThe chance that no skin will be applied is 100 subtracted by this weight"));
                log.LogDebug($"Added config for {skinList[i].skinName}");
            }
            for (int i = 0; i < mapObjectsList.Count; i++)
            {
                if (mapObjectsList[i].autoHandle)
                {
                    continue;
                }
                else
                {
                    isMapObjectEnabled.Add(cfg.Bind("Map Objects", $"Enable {mapObjectsList[i].mapObjectName}?", true, ""));
                    useDefaultMapObjectCurve.Add(cfg.Bind("Map Objects", $"Use default min and max values for {mapObjectsList[i].mapObjectName}?", true, ""));
                    mapObjectMinNo.Add(cfg.Bind("Map Objects", $"Minimum number of {mapObjectsList[i].mapObjectName} to spawn per level", 0, "This only matters if you set \"Use Default\" to false.\nMust be an integer of 0 or above"));
                    mapObjectMaxNo.Add(cfg.Bind("Map Objects", $"Maximum number of {mapObjectsList[i].mapObjectName} to spawn per level", 1, "This only matters if you set \"Use Default\" to false.\nMust be an integer of 0 or above"));
                    log.LogDebug($"Added config for {mapObjectsList[i].mapObjectName}");
                }
            }
            ClearOrphanedEntries(cfg);
            cfg.Save();
            cfg.SaveOnConfigSet = true;
        }
        static void ClearOrphanedEntries(ConfigFile cfg)
        {
            PropertyInfo orphanedEntriesProp = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries");
            var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(cfg);
            orphanedEntries.Clear();
        }
    }
}