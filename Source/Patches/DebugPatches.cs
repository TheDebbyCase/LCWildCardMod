using HarmonyLib;
using LCWildCardMod.Utils;
using System.Collections.Generic;
namespace LCWildCardMod.Patches
{
    internal static class DebugPatches
    {
        private static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(typeof(HarmonyHelper), nameof(HarmonyHelper.ILCodeCheck))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> CheckIL(IEnumerable<CodeInstruction> instructions)
        {
            Log.LogDebug("Checking IL of HarmonyHelper.ILCodeCheck");
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                Log.LogDebug(codes[i]);
            }
            return codes;
        }
    }
}