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
    [HarmonyPatch(typeof(EnemyAI))]
    public static class EnemyAIPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(nameof(EnemyAI.Start))]
        [HarmonyPostfix]
        public static void ChangeAssets(EnemyAI __instance)
        {
            try
            {
                SkinsClass.SetSkin(__instance);
            }
            catch (Exception exception)
            {
                Log.LogError(exception);
            }
        }
    }
    [HarmonyPatch]
    [HarmonyAfter("LCWildCardMod.BushWolfEnemyPatches.FyrusStarEffect")]
    public static class EnemyAISubsPatch
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> GetSubEnemyPlayerCollisions()
        {
            return Assembly.GetAssembly(typeof(EnemyAI)).GetTypes().Where(type => typeof(EnemyAI).IsAssignableFrom(type) && type != typeof(EnemyAI)).Select((x) => AccessTools.Method(x, nameof(EnemyAI.OnCollideWithPlayer), new Type[] { typeof(Collider) }));
        }
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> FyrusStarEffect(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(TranspilerHelper.collision) && codes[i + 1].IsStloc())
                {
                    List<CodeInstruction> newCodes = new List<CodeInstruction>();
                    Label newLabel = generator.DefineLabel();
                    Label nullLabel = generator.DefineLabel();
                    object playerLocalOperand = codes[i + 1].operand;
                    CodeInstruction loadPlayerLocal;
                    if (codes[i + 1].opcode.Equals(OpCodes.Stloc_0))
                    {
                        loadPlayerLocal = new CodeInstruction(OpCodes.Ldloc_S, 0);
                    }
                    else if (codes[i + 1].opcode.Equals(OpCodes.Stloc_1))
                    {
                        loadPlayerLocal = new CodeInstruction(OpCodes.Ldloc_S, 1);
                    }
                    else if (codes[i + 1].opcode.Equals(OpCodes.Stloc_2))
                    {
                        loadPlayerLocal = new CodeInstruction(OpCodes.Ldloc_S, 2);
                    }
                    else if (codes[i + 1].opcode.Equals(OpCodes.Stloc_3))
                    {
                        loadPlayerLocal = new CodeInstruction(OpCodes.Ldloc_S, 3);
                    }
                    else if (codes[i + 1].opcode.Equals(OpCodes.Stloc_S))
                    {
                        loadPlayerLocal = new CodeInstruction(OpCodes.Ldloc_S, playerLocalOperand);
                    }
                    else
                    {
                        loadPlayerLocal = new CodeInstruction(OpCodes.Ldloc, playerLocalOperand);
                    }
                    newCodes.Add(loadPlayerLocal);
                    newCodes.Add(new CodeInstruction(OpCodes.Ldnull));
                    newCodes.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.inequality));
                    newCodes.Add(new CodeInstruction(OpCodes.Brfalse_S, nullLabel));
                    newCodes.Add(loadPlayerLocal);
                    newCodes.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.consumedStar));
                    newCodes.Add(new CodeInstruction(OpCodes.Brfalse_S, newLabel));
                    newCodes.Add(new CodeInstruction(OpCodes.Ret));
                    codes[i + 2].labels.Add(newLabel);
                    codes[i + 2].labels.Add(nullLabel);
                    codes.InsertRange(i + 2, newCodes);
                    break;
                }
            }
            return codes.AsEnumerable();
        }
    }
}