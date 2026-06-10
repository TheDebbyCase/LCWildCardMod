using HarmonyLib;
using LCWildCardMod.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace LCWildCardMod.Patches
{
    internal static class NecessaryPatches
    {
        private static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ShipHasLeft))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        internal static bool StartOfRound_EndRoundInvoke()
        {
            try
            {
                EventsClass.RoundEndInvoke();
            }
            catch (Exception exception)
            {
                Log.LogError(exception);
            }
            return true;
        }
        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.StartDisconnect))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        internal static bool GameNetworkManager_EndRoundInvoke()
        {
            try
            {
                EventsClass.RoundEndInvoke();
            }
            catch (Exception exception)
            {
                Log.LogError(exception);
            }
            return true;
        }
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.waitForMainEntranceTeleportToSpawn))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        internal static void RoundManager_StartRoundInvoke()
        {
            try
            {
                EventsClass.RoundStartInvoke();
            }
            catch (Exception exception)
            {
                Log.LogError(exception);
            }
        }
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.Start))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        internal static void RoundManager_GetEnemies()
        {
            WildUtils.AllEnemies = new ListDict<string, EnemyType>();
            for (int i = 0; i < StartOfRound.Instance.levels.Length; i++)
            {
                SelectableLevel level = StartOfRound.Instance.levels[i];
                List<SpawnableEnemyWithRarity> allEnemies = new List<SpawnableEnemyWithRarity>(RoundManager.Instance.WeedEnemies);
                allEnemies.AddRange(level.Enemies);
                allEnemies.AddRange(level.OutsideEnemies);
                allEnemies.AddRange(level.DaytimeEnemies);
                for (int j = 0; j < allEnemies.Count; j++)
                {
                    EnemyType type = allEnemies[j].enemyType;
                    WildUtils.AllEnemies.Add(type.enemyName, type);
                }
            }
            Dictionary<string, string> enemyNamesDict = new Dictionary<string, string>();
            for (int i = 0; i < WildUtils.AllEnemies.Count; i++)
            {
                EnemyType type = WildUtils.AllEnemies[i];
                if (enemyNamesDict.ContainsKey(type.enemyName))
                {
                    continue;
                }
                string trueName = type.enemyName;
                ScanNodeProperties scanNode = type.enemyPrefab.GetComponentInChildren<ScanNodeProperties>();
                if (scanNode != null)
                {
                    trueName = scanNode.headerText;
                }
                enemyNamesDict.Add(type.enemyName, trueName);
            }
            WildUtils.TrueEnemyNames = new ReadOnlyDictionary<string, string>(enemyNamesDict);

        }
    }
}