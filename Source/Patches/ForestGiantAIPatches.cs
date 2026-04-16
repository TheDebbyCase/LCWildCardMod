using HarmonyLib;
using LCWildCardMod.Utils;
using System.Collections.Generic;
using System.Reflection.Emit;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(ForestGiantAI))]
    public static class ForestGiantAIPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(nameof(ForestGiantAI.OnCollideWithPlayer))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> HaloSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(TranspilerHelper.giantGrabRPC))
                {
                    List<CodeInstruction> newCode = new List<CodeInstruction>();
                    Label newLabel = generator.DefineLabel();
                    newCode.Add(new CodeInstruction(OpCodes.Ldloc_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.haloSave));
                    newCode.Add(new CodeInstruction(OpCodes.Brfalse_S, newLabel));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Ldc_I4_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.switchBehaviour));
                    newCode.Add(new CodeInstruction(OpCodes.Ret));
                    codes[i - 4].labels.Add(newLabel);
                    codes.InsertRange(i - 4, newCode);
                    break;
                }
            }
            return codes;
        }
    }
}