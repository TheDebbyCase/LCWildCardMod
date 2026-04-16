using HarmonyLib;
using LCWildCardMod.Utils;
using System.Collections.Generic;
using System.Reflection.Emit;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(RedLocustBees))]
    public static class RedLocustBeesPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(nameof(RedLocustBees.OnCollideWithPlayer))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> HaloSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            Label? newLabel0 = null;
            Label? newLabel1 = null;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(TranspilerHelper.beeKill) && codes[i - 6].Branches(out _) && !codes[i - 7].Calls(TranspilerHelper.haloSave))
                {
                    List<CodeInstruction> newCode = new List<CodeInstruction>();
                    newLabel0 = generator.DefineLabel();
                    newLabel1 = generator.DefineLabel();
                    newCode.Add(new CodeInstruction(OpCodes.Ldloc_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.fyrusSave));
                    newCode.Add(new CodeInstruction(OpCodes.Brtrue_S, newLabel0.Value));
                    newCode.Add(new CodeInstruction(OpCodes.Ldloc_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.haloSave));
                    newCode.Add(new CodeInstruction(OpCodes.Brtrue_S, newLabel1.Value));
                    codes.InsertRange(i - 5, newCode);
                }
                if (newLabel0.HasValue && newLabel1.HasValue && codes[i].LoadsField(TranspilerHelper.beeZapMode))
                {
                    codes[i - 1].labels.Add(newLabel0.Value);
                    codes[i - 1].labels.Add(newLabel1.Value);
                    break;
                }
            }
            return codes;
        }
    }
}