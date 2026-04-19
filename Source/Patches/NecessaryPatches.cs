using HarmonyLib;
using LCWildCardMod.Utils;
using System;
namespace LCWildCardMod.Patches
{
    public static class NecessaryPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ShipHasLeft))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static bool StartOfRound_EndRoundInvoke()
        {
            try
            {
                EventsClass.RoundEnded();
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
        public static bool GameNetworkManager_EndRoundInvoke()
        {
            try
            {
                EventsClass.RoundEnded();
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
        public static void RoundManager_StartRoundInvoke()
        {
            try
            {
                EventsClass.RoundStarted();
            }
            catch (Exception exception)
            {
                Log.LogError(exception);
            }
        }
    }
}