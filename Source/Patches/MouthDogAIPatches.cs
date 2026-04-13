using HarmonyLib;
using LCWildCardMod.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(MouthDogAI))]
    public static class MouthDogAIPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;

        [HarmonyPatch(nameof(MouthDogAI.OnCollideWithPlayer))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> HaloSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].LoadsField(TranspilerHelper.enemyState) && codes[i + 1].opcode.Equals(OpCodes.Ldc_I4_3) && codes[i + 2].Branches(out _))
                {
                    List<CodeInstruction> newCodes = new List<CodeInstruction>();
                    Label newLabel = generator.DefineLabel();
                    newCodes.Add(new CodeInstruction(OpCodes.Ldloc_S, 0));
                    newCodes.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.haloSave));
                    newCodes.Add(new CodeInstruction(OpCodes.Brfalse_S, newLabel));
                    newCodes.Add(new CodeInstruction(OpCodes.Ret));
                    codes[i + 3].labels.Add(newLabel);
                    codes.InsertRange(i + 3, newCodes);
                    break;
                }
            }
            return codes.AsEnumerable();
        }
    }
}