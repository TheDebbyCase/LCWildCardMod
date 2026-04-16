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
        [HarmonyWrapSafe]
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
    public static class EnemyAISubsPatch
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> GetSubEnemyPlayerCollisions()
        {
            return Assembly.GetAssembly(typeof(EnemyAI)).GetTypes().Where(type => typeof(EnemyAI).IsAssignableFrom(type)).Select((x) => AccessTools.Method(x, nameof(EnemyAI.OnCollideWithPlayer), new Type[] { typeof(Collider) })).Where((x) => x.DeclaringType != typeof(EnemyAI) && x.DeclaringType != typeof(DressGirlAI));
        }
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> FyrusStarEffect(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            bool foundDamage = false;
            int collisionIndex = -1;
            int damageParams = TranspilerHelper.damagePlayer.GetParameters().Length;
            CodeInstruction loadPlayerLocal = null;
            Log.LogDebug($"Patching {original.DeclaringType}.{original.Name}");
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(TranspilerHelper.collision) && codes[i + 1].IsStloc())
                {
                    collisionIndex = i;
                    loadPlayerLocal = TranspilerHelper.StoreToLoad(codes[i + 1]);
                }
                if (loadPlayerLocal != null && codes[i].Calls(TranspilerHelper.damagePlayer))
                {
                    foundDamage = true;
                    int loadPlayer = -1;
                    for (int j = i - damageParams; j > 0; j--)
                    {
                        if (TranspilerHelper.AreLoadEqual(codes[j], loadPlayerLocal))
                        {
                            loadPlayer = j;
                            break;
                        }
                    }
                    if (loadPlayer == -1)
                    {
                        break;
                    }
                    List<CodeInstruction> newCode = new List<CodeInstruction>();
                    Label destination = generator.DefineLabel();
                    loadPlayerLocal.labels = codes[loadPlayer].ExtractLabels();
                    newCode.Add(loadPlayerLocal);
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.fyrusSave));
                    newCode.Add(new CodeInstruction(OpCodes.Brtrue_S, destination));
                    codes[i + 1].labels.Add(destination);
                    codes.InsertRange(loadPlayer, newCode);
                    break;
                }
            }
            if (collisionIndex == -1)
            {
                return codes;
            }
            bool nullCheckExists = false;
            for (int i = collisionIndex; i < codes.Count; i++)
            {
                if (codes[i].Branches(out _))
                {
                    for (int j = i; j > collisionIndex; j--)
                    {
                        if (codes[j].Calls(TranspilerHelper.inequality))
                        {
                            nullCheckExists = true;
                            break;
                        }
                    }
                    break;
                }
            }
            List<CodeInstruction> finalCode = new List<CodeInstruction>();
            if (!nullCheckExists)
            {
                Label nullLabel = generator.DefineLabel();
                finalCode.Add(loadPlayerLocal);
                finalCode.Add(new CodeInstruction(OpCodes.Ldnull));
                finalCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.inequality));
                finalCode.Add(new CodeInstruction(OpCodes.Brfalse_S, nullLabel));
                codes[^1].labels.Add(nullLabel);
            }
            if (!foundDamage)
            {
                Label newLabel = generator.DefineLabel();
                finalCode.Add(loadPlayerLocal);
                finalCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                finalCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.fyrusSave));
                finalCode.Add(new CodeInstruction(OpCodes.Brfalse_S, newLabel));
                finalCode.Add(new CodeInstruction(OpCodes.Ret));
                codes[collisionIndex + 2].labels.Add(newLabel);
            }
            codes.InsertRange(collisionIndex + 2, finalCode);
            return codes;
        }
    }
}