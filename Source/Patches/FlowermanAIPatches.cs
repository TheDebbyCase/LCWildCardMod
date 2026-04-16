using HarmonyLib;
using LCWildCardMod.Utils;
using System.Collections.Generic;
using System.Reflection.Emit;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(FlowermanAI))]
    public static class FlowermanAIPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(nameof(FlowermanAI.OnCollideWithPlayer))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> HaloSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = codes.Count - 3; i >= 0; i--)
            {
                if (codes[i].opcode.Equals(OpCodes.Ldnull) && codes[i - 1].IsLdloc() && codes[i + 1].Calls(TranspilerHelper.inequality) && codes[i + 2].Branches(out _))
                {
                    List<CodeInstruction> newCode = new List<CodeInstruction>();
                    Label newLabel = generator.DefineLabel();
                    newCode.Add(new CodeInstruction(OpCodes.Ldloc_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.haloSave));
                    newCode.Add(new CodeInstruction(OpCodes.Brfalse_S, newLabel));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Ldc_I4_S, 1));
                    newCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.switchBehaviour));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Ldfld, TranspilerHelper.enemyAgent));
                    newCode.Add(new CodeInstruction(OpCodes.Ldc_R4, 0f));
                    newCode.Add(new CodeInstruction(OpCodes.Callvirt, TranspilerHelper.setSpeed));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Ldc_R4, 0f));
                    newCode.Add(new CodeInstruction(OpCodes.Stfld, TranspilerHelper.brackenEvade));
                    newCode.Add(new CodeInstruction(OpCodes.Ret));
                    codes[i + 3].labels.Add(newLabel);
                    codes.InsertRange(i + 3, newCode);
                    break;
                }
            }
            return codes;
        }
    }
}