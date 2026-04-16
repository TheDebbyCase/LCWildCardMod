using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using LCWildCardMod.Utils;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(MaskedPlayerEnemy))]
    public class MaskedPlayerEnemyPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(nameof(MaskedPlayerEnemy.killAnimation), MethodType.Enumerator)]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> HaloSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].LoadsField(TranspilerHelper.specialAnim) && codes[i + 1].StoresField(TranspilerHelper.maskedLastPlayer))
                {
                    List<CodeInstruction> newCode = new List<CodeInstruction>();
                    Label newLabel = generator.DefineLabel();
                    newCode.Add(new CodeInstruction(OpCodes.Ldloc_S, 1));
                    newCode.Add(new CodeInstruction(OpCodes.Ldfld, TranspilerHelper.specialAnim));
                    newCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.haloSave));
                    newCode.Add(new CodeInstruction(OpCodes.Brfalse_S, newLabel));
                    newCode.Add(new CodeInstruction(OpCodes.Ldloc_S, 1));
                    newCode.Add(new CodeInstruction(OpCodes.Callvirt, TranspilerHelper.cancelSpecialAnim));
                    newCode.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
                    newCode.Add(new CodeInstruction(OpCodes.Ret));
                    codes[i - 2].labels.Add(newLabel);
                    codes.InsertRange(i - 2, newCode);
                    break;
                }
            }
            return codes;
        }
    }
}