using HarmonyLib;
using LCWildCardMod.Utils;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
namespace LCWildCardMod.Patches
{
    internal static class DebugPatches
    {
        private static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(typeof(HarmonyHelper), nameof(HarmonyHelper.ILCodeCheck))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> CheckIL(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            MethodBase.GetCurrentMethod().LogIL(original, codes);
            return codes;
        }
    }
}