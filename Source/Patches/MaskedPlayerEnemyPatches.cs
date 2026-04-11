using GameNetcodeStuff;
using HarmonyLib;
using LCWildCardMod.Utils;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(MaskedPlayerEnemy))]
    public class MaskedPlayerEnemyPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        static MethodInfo haloSaveMethod = AccessTools.Method(typeof(Extensions), nameof(Extensions.SaveIfHalo), new Type[] { typeof(PlayerControllerB) });
        static MethodInfo maskedGlowMethod = AccessTools.Method(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.SetMaskGlow), new Type[] { typeof(bool) });
        static FieldInfo animField = AccessTools.Field(typeof(EnemyAI), nameof(EnemyAI.inSpecialAnimationWithPlayer));
        [HarmonyPatch(nameof(MaskedPlayerEnemy.killAnimation), MethodType.Enumerator)]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> HaloSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(maskedGlowMethod))
                {
                    List<CodeInstruction> newCode = new List<CodeInstruction>();
                    Label newLabel = generator.DefineLabel();
                    newCode.Add(new CodeInstruction(OpCodes.Ldloc_1));
                    newCode.Add(new CodeInstruction(OpCodes.Call, haloSaveMethod));
                    newCode.Add(new CodeInstruction(OpCodes.Brfalse_S, newLabel));
                    newCode.Add(new CodeInstruction(OpCodes.Ldloc_1));
                    newCode.Add(new CodeInstruction(OpCodes.Ldnull));
                    newCode.Add(new CodeInstruction(OpCodes.Stfld, animField));
                    codes[i + 1].labels.Add(newLabel);
                    codes.InsertRange(i + 1, newCode);
                    break;
                }
            }
            return codes.AsEnumerable();
        }
    }
}