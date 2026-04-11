using HarmonyLib;
using LCWildCardMod.Utils;
using System;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    public class StartOfRoundPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(nameof(StartOfRound.ShipHasLeft))]
        [HarmonyPrefix]
        public static bool EndRoundInvoke()
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
    }
}
