using HarmonyLib;
using LCWildCardMod.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(CaveDwellerAI))]
    public static class CaveDwellerAIPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(nameof(CaveDwellerAI.OnCollideWithPlayer))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> HaloSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Branches(out _) && codes[i - 3].IsLdloc() && codes[i - 1].Calls(TranspilerHelper.inequality) && codes[i - 2].opcode.Equals(OpCodes.Ldnull))
                {
                    List<CodeInstruction> newCode = new List<CodeInstruction>();
                    Label newLabel = generator.DefineLabel();
                    newCode.Add(new CodeInstruction(OpCodes.Ldloc_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.haloSave));
                    newCode.Add(new CodeInstruction(OpCodes.Brtrue_S, newLabel));
                    codes.Last().labels.Add(newLabel);
                    codes.InsertRange(i + 1, newCode);
                    break;
                }
            }
            return codes;
        }
    }
}