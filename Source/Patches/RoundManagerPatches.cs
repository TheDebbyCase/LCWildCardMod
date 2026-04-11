using HarmonyLib;
using LCWildCardMod.Utils;
using System;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    public class RoundManagerPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(nameof(RoundManager.waitForMainEntranceTeleportToSpawn))]
        [HarmonyPostfix]
        public static void StartRoundInvoke()
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
