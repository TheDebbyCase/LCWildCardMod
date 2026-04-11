using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(BushWolfEnemy))]
    public static class BushWolfEnemyPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        static MethodInfo targetMethod = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.MeetsStandardPlayerCollisionConditions), new Type[] { typeof(Collider), typeof(bool), typeof(bool) });
        static MethodInfo inequalityMethod = AccessTools.Method(typeof(UnityEngine.Object), "op_Inequality", new Type[] { typeof(UnityEngine.Object), typeof(UnityEngine.Object) });
        static FieldInfo inKillField = AccessTools.Field(typeof(BushWolfEnemy), nameof(BushWolfEnemy.inKillAnimation));
        [HarmonyPatch(nameof(BushWolfEnemy.OnCollideWithPlayer))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> FyrusStarEffect(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(targetMethod) && codes[i + 1].opcode.Equals(OpCodes.Ldnull) && codes[i + 2].Calls(inequalityMethod) && codes[i + 3].Branches(out _))
                {
                    List<CodeInstruction> newLocalDefineCode = new List<CodeInstruction>();
                    LocalBuilder playerLocal = generator.DeclareLocal(typeof(PlayerControllerB));
                    newLocalDefineCode.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    newLocalDefineCode.Add(new CodeInstruction(OpCodes.Ldarg_1));
                    newLocalDefineCode.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    newLocalDefineCode.Add(new CodeInstruction(OpCodes.Ldfld, inKillField));
                    newLocalDefineCode.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
                    newLocalDefineCode.Add(new CodeInstruction(OpCodes.Call, targetMethod));
                    newLocalDefineCode.Add(new CodeInstruction(OpCodes.Stloc_S, (byte)playerLocal.LocalIndex));
                    codes[i] = new CodeInstruction(OpCodes.Ldloc_S, (byte)playerLocal.LocalIndex);
                    codes[i].labels = codes[i - 5].labels;
                    codes.InsertRange(i - 17, newLocalDefineCode);
                    codes.RemoveRange((i + newLocalDefineCode.Count) - 5, 5);
                    break;
                }
            }
            return codes.AsEnumerable();
        }
    }
}