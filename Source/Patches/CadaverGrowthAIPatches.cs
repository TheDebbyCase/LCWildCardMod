using HarmonyLib;
using LCWildCardMod.Utils;
using System.Collections.Generic;
using System.Reflection.Emit;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(CadaverGrowthAI))]
    public class CadaverGrowthAIPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(nameof(CadaverGrowthAI.CurePlayer))]
        [HarmonyPrefix]
        public static bool AccurateInfected(CadaverGrowthAI __instance)
        {
            __instance.numberOfInfected--;
            return true;
        }
        [HarmonyPatch(nameof(CadaverGrowthAI.BurstFromPlayer))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SaveFromBurst(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].LoadsField(TranspilerHelper.hasBurst))
                {
                    List<CodeInstruction> newCode = new List<CodeInstruction>();
                    Label resumeBurstLabel = generator.DefineLabel();
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 1));
                    newCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.anySave));
                    newCode.Add(new CodeInstruction(OpCodes.Brfalse_S, resumeBurstLabel));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 1));
                    newCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.cadaverCure));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 1));
                    newCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.cadaverCureRPC));
                    newCode.Add(new CodeInstruction(OpCodes.Ret));
                    codes[i + 2].labels.Add(resumeBurstLabel);
                    codes.InsertRange(i + 2, newCode);
                    break;
                }
            }
            return codes;
        }
    }
}