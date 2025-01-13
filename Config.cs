using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;

namespace LCWildCardMod
{
    public class WildCardConfig
    {
        public readonly List<ConfigEntry<bool>> isScrapEnabled = new List<ConfigEntry<bool>>();
        public readonly List<ConfigEntry<string>> scrapSpawnWeights = new List<ConfigEntry<string>>();
        public WildCardConfig(ConfigFile cfg, List<Item> scrapList)
        {
            cfg.SaveOnConfigSet = false;
            foreach (Item scrapListItem in scrapList)
            {
                isScrapEnabled.Add(cfg.Bind(
                    "Scrap",
                    $"Enable {scrapListItem.itemName}",
                    true,
                    $"Whether or not to allow {scrapListItem.itemName} to spawn!"
                    ));
                scrapSpawnWeights.Add(cfg.Bind(
                    "Scrap",
                    $"Spawn Weights of {scrapListItem.itemName}",
                    "All:20",
                    string.Concat($"Set the spawn weight of {scrapListItem.itemName} for each moon in the format 'Vanilla:20,Modded:30'",
                    "\nUse 'All:' to set the weight for all moons, 'Vanilla:' for only vanilla moons, or 'Modded:' for only modded moons.",
                    "\nSpecific moons can be referenced by typing the name (without the numbers) followed by 'Level' (this works for modded moons also), for example:",
                    "\n'ExperimentationLevel:20'")
                    ));
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

