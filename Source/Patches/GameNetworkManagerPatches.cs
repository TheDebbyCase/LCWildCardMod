using HarmonyLib;
using LCWildCardMod.Utils;
using System;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(GameNetworkManager))]
    public class GameNetworkManagerPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(nameof(GameNetworkManager.StartDisconnect))]
        [HarmonyWrapSafe]
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
