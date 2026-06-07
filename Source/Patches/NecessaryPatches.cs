using HarmonyLib;
using LCWildCardMod.Utils;
using System;
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
            WildUtils.GetEnemies();
        }
    }
}