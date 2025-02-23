using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using LCWildCardMod.Utils;
namespace LCWildCardMod.Config
{
    public class WildCardConfig
    {
        BepInEx.Logging.ManualLogSource log = WildCardMod.Log;
        internal readonly List<ConfigEntry<bool>> isScrapEnabled = new List<ConfigEntry<bool>>();
        internal readonly List<ConfigEntry<string>> scrapSpawnWeights = new List<ConfigEntry<string>>();
        internal readonly List<ConfigEntry<bool>> isSkinEnabled = new List<ConfigEntry<bool>>();
        internal readonly List<ConfigEntry<int>> skinApplyChance = new List<ConfigEntry<int>>();
        internal readonly ConfigEntry<bool> assortedScrap;
        internal string bonusString = "Affected items include";
        internal WildCardConfig(ConfigFile cfg, List<Item> scrapList, List<Skin> skinList)
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

            }
            assortedScrap = cfg.Bind("Scrap", "Enable/Disable supplementary scrap overall", true, bonusString);
            for (int i = 0; i < skinList.Count; i++)
            {
                bool defaultEnabled = skinList[i].skinEnabled;
                int defaultChance = skinList[i].skinChance;
                isSkinEnabled.Add(cfg.Bind("Skins", $"Enable {skinList[i].skinName}?", defaultEnabled, ""));
                skinApplyChance.Add(cfg.Bind("Skins", $"Weighted chance that {skinList[i].targetEnemy.enemyName} will spawn as {skinList[i].skinName}", defaultChance, "Must be an integer greater than, or equal to, 0"));
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