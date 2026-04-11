using GameNetcodeStuff;
using HarmonyLib;
using LCWildCardMod.Utils;
using System;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(CadaverBloomAI))]
    public class CadaverBloomAIPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(nameof(CadaverBloomAI.BurstForth))]
        [HarmonyPrefix]
        public static bool SavePlayer(ref PlayerControllerB player, ref bool kill)
        {
            try
            {
                kill = !player.SaveIfAny();
            }
            catch (Exception exception)
            {
                Log.LogError(exception);
            }
            return true;
        }
    }
}