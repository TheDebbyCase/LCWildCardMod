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
        [HarmonyPrefix]
        public static bool SavePlayer(ref PlayerControllerB playerScript)
        {
            try
            {
                return !playerScript.SaveIfAny();
            }
            catch (Exception exception)
            {
                Log.LogError(exception);
            }
            return true;
        }
    }
}