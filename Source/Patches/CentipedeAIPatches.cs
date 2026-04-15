using HarmonyLib;
using LCWildCardMod.Utils;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(CentipedeAI))]
    public static class CentipedeAIPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(nameof(CentipedeAI.DamagePlayerOnIntervals))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> HaloSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = codes.Count - 1; i >= 0; i--)
            {
                if (codes[i].Calls(TranspilerHelper.damagePlayer))
                {
                    List<CodeInstruction> newCode = new List<CodeInstruction>();
                    Label destination = generator.DefineLabel();
                    CodeInstruction first = new CodeInstruction(OpCodes.Ldarg_S, 0);
                    codes[i - 11].MoveLabelsTo(first);
                    newCode.Add(first);
                    newCode.Add(new CodeInstruction(OpCodes.Ldfld, TranspilerHelper.clingPlayer));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.fyrusSave));
                    newCode.Add(new CodeInstruction(OpCodes.Brtrue_S, destination));
                    codes[i + 1].labels.Add(destination);
                    codes.InsertRange(i - 11, newCode);
                    break;
                }
            }
            return codes;
        }
    }
}