using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using LCWildCardMod.Utils;
namespace LCWildCardMod.Config
{
    public class WildCardConfig
    {
        internal readonly List<ConfigEntry<bool>> isScrapEnabled = new List<ConfigEntry<bool>>();
        internal readonly List<ConfigEntry<string>> scrapSpawnWeights = new List<ConfigEntry<string>>();
        internal readonly List<ConfigEntry<bool>> isSkinEnabled = new List<ConfigEntry<bool>>();
        internal readonly List<ConfigEntry<int>> skinApplyChance = new List<ConfigEntry<int>>();
        internal readonly ConfigEntry<bool> assortedScrap;
        internal string bonusString = "Affected items include";
        internal WildCardConfig(ConfigFile cfg, List<Item> scrapList, List<Skin> skinList)
        {
            cfg.SaveOnConfigSet = false;
            foreach (Item scrapListItem in scrapList)
            {
                bool defaultEnabled = scrapListItem.spawnPrefab.GetComponent<AdditionalInfo>().defaultEnabled;
                string defaultRarities = scrapListItem.spawnPrefab.GetComponent<AdditionalInfo>().defaultRarities;
                bool isBonus = scrapListItem.spawnPrefab.GetComponent<AdditionalInfo>().isBonus;
                if (isBonus)
                {
                    bonusString = $"{bonusString}, {scrapListItem.itemName}";
                }
                isScrapEnabled.Add(cfg.Bind("Scrap", $"Enable {scrapListItem.itemName}?", defaultEnabled, ""));
                scrapSpawnWeights.Add(cfg.Bind("Scrap", $"Spawn Weights of {scrapListItem.itemName}!", defaultRarities, "For example: All:20,Vanilla:20,Modded:20,Experimentation:20"));
            }
            assortedScrap = cfg.Bind("Scrap", "Enable/Disable supplementary scrap overall", true, bonusString);
            foreach (Skin skinListSkin in skinList)
            {
                bool defaultEnabled = skinListSkin.skinEnabled;
                int defaultChance = skinListSkin.skinChance;
                isSkinEnabled.Add(cfg.Bind("Skins", $"Enable {skinListSkin.skinName}?", defaultEnabled, ""));
                skinApplyChance.Add(cfg.Bind("Skins", $"Chance that {skinListSkin.targetEnemy.enemyName} will spawn as {skinListSkin.skinName}", defaultChance, "Percentage chance, 0 - 100"));
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