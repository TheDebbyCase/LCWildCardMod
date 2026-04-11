using GameNetcodeStuff;
using HarmonyLib;
using LCWildCardMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(DressGirlAI))]
    public static class DressGirlAIPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        static MethodInfo haloSaveMethod = AccessTools.Method(typeof(Extensions), nameof(Extensions.SaveIfHalo), new Type[] { typeof(PlayerControllerB) });
        static MethodInfo killPlayerMethod = AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer), new Type[] { typeof(Vector3), typeof(bool), typeof(CauseOfDeath), typeof(int), typeof(Vector3), typeof(bool) });
        static FieldInfo stateField = AccessTools.Field(typeof(EnemyAI), nameof(EnemyAI.currentBehaviourStateIndex));
        [HarmonyPatch(nameof(DressGirlAI.OnCollideWithPlayer))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> HaloSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            Label? newLabel = null;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].LoadsField(stateField) && codes[i + 1].opcode.Equals(OpCodes.Ldc_I4_1) && codes[i + 2].Branches(out _))
                {
                    List<CodeInstruction> newCode = new List<CodeInstruction>();
                    newLabel = generator.DefineLabel();
                    newCode.Add(new CodeInstruction(OpCodes.Ldloc_0));
                    newCode.Add(new CodeInstruction(OpCodes.Call, haloSaveMethod));
                    newCode.Add(new CodeInstruction(OpCodes.Brtrue_S, newLabel.Value));
                    codes.InsertRange(i + 3, newCode);
                }
                if (newLabel.HasValue && codes[i].Calls(killPlayerMethod))
                {
                    codes[i + 1].labels.Add(newLabel.Value);
                    break;
                }
            }
            return codes.AsEnumerable();
        }
    }
}