using HarmonyLib;
using LCWildCardMod.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(DressGirlAI))]
    public static class DressGirlAIPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(nameof(DressGirlAI.OnCollideWithPlayer))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> HaloSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            Label? newLabel = null;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].LoadsField(TranspilerHelper.enemyState) && codes[i + 1].opcode.Equals(OpCodes.Ldc_I4_1) && codes[i + 2].Branches(out _))
                {
                    List<CodeInstruction> newCode = new List<CodeInstruction>();
                    newLabel = generator.DefineLabel();
                    newCode.Add(new CodeInstruction(OpCodes.Ldloc_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.anySave));
                    newCode.Add(new CodeInstruction(OpCodes.Brtrue_S, newLabel.Value));
                    codes.InsertRange(i + 3, newCode);
                }
                if (newLabel.HasValue && codes[i].Calls(TranspilerHelper.killPlayer))
                {
                    List<CodeInstruction> newCode = new List<CodeInstruction>();
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Ldc_I4_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Stfld, TranspilerHelper.timesSeenByPlayer));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Ldc_I4_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.switchBehaviourLocal));
                    codes[i + 1].labels.Add(newLabel.Value);
                    codes.InsertRange(i + 1, newCode);
                    break;
                }
            }
            return codes;
        }
    }
}