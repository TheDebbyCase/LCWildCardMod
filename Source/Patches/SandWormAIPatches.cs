using GameNetcodeStuff;
using HarmonyLib;
using LCWildCardMod.Utils;
using System;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(SandWormAI))]
    public class SandWormAIPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(nameof(SandWormAI.EatPlayer))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static bool SavePlayer(SandWormAI __instance, ref PlayerControllerB playerScript)
        {
            try
            {
                return !playerScript.SaveIfAny(__instance);
            }
            catch (Exception exception)
            {
                Log.LogError(exception);
            }
            return true;
        }
    }
}