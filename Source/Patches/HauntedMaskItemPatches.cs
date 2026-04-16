using HarmonyLib;
using LCWildCardMod.Utils;
using System.Collections.Generic;
using System.Reflection.Emit;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(HauntedMaskItem))]
    public static class HauntedMaskItemPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(nameof(HauntedMaskItem.FinishAttaching))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> HaloSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(TranspilerHelper.allowDeath) && codes[i + 1].Branches(out Label? label))
                {
                    List<CodeInstruction> newCodes = new List<CodeInstruction>();
                    newCodes.Add(new CodeInstruction(OpCodes.Ldc_I4_S, 0));
                    newCodes.Add(new CodeInstruction(OpCodes.Ceq));
                    newCodes.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                    newCodes.Add(new CodeInstruction(OpCodes.Ldfld, TranspilerHelper.maskHeldBy));
                    newCodes.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.haloSave));
                    newCodes.Add(new CodeInstruction(OpCodes.Or));
                    codes[i + 1] = new CodeInstruction(OpCodes.Brfalse_S, label.Value);
                    codes.InsertRange(i + 1, newCodes);
                    break;
                }
            }
            return codes;
        }
    }
}