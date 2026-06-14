using static LCWildCardMod.Utils.HarmonyHelper;
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
    internal static class EnemyAIGraceSavePatch
    {
        [HarmonyTargetMethods]
        internal static IEnumerable<MethodBase> GetEachEnemyPlayerCollision()
        {
            Assembly assembly = Assembly.GetAssembly(typeof(EnemyAI));
            Type[] types = assembly.GetTypes();
            IEnumerable<Type> filteredTypes = types.Where(type => typeof(EnemyAI).IsAssignableFrom(type));
            IEnumerable<MethodInfo> filteredMethods = filteredTypes.Select((x) => x.GetMethod(nameof(EnemyAI.OnCollideWithPlayer)));
            IEnumerable<MethodInfo> finalMethods = filteredMethods.Where((x) => x.DeclaringType != typeof(EnemyAI) && x.DeclaringType != typeof(DressGirlAI));
            return finalMethods;
        }
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        [HarmonyAfter("deB.WildCard.save")]
        internal static IEnumerable<CodeInstruction> GraceSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            bool foundDamage = false;
            int collisionIndex = -1;
            int damageParams = damagePlayer.GetParameters().Length;
            CodeInstruction loadPlayerLocal = null;
            for (int i = 0; i < codes.Count; i++)
            {
                if (loadPlayerLocal == null && codes[i].Calls(collision) && codes[i + 1].IsStloc())
                {
                    collisionIndex = i;
                    loadPlayerLocal = StoreToLoad(codes[i + 1]);
                }
                if (collisionIndex == -1 || !codes[i].Calls(damagePlayer))
                {
                    continue;
                }
                foundDamage = true;
                int loadPlayer = -1;
                for (int j = i - damageParams; j >= 0; j--)
                {
                    if (!AreLoadEqual(codes[j], loadPlayerLocal))
                    {
                        continue;
                    }
                    loadPlayer = j;
                    break;
                }
                if (loadPlayer == -1)
                {
                    break;
                }
                Label destination = generator.DefineLabel();
                CodeInstruction newLoadPlayerLocal = new CodeInstruction(loadPlayerLocal);
                codes[loadPlayer].MoveLabelsTo(newLoadPlayerLocal);
                LocalBuilder vectorLocal = generator.DeclareLocal(typeof(Vector3));
                List<CodeInstruction> newCode = new List<CodeInstruction>
                {
                    newLoadPlayerLocal,
                    new CodeInstruction(OpCodes.Ldc_I4_S, (int)CauseOfDeath.Unknown),
                    new CodeInstruction(OpCodes.Ldloca_S, vectorLocal),
                    new CodeInstruction(OpCodes.Initobj, typeof(Vector3)),
                    new CodeInstruction(OpCodes.Ldloc_S, vectorLocal),
                    new CodeInstruction(OpCodes.Ldarg_S, 0),
                    new CodeInstruction(OpCodes.Call, savePlayerGrace),
                    new CodeInstruction(OpCodes.Brtrue_S, destination)
                };
                codes[i + 1].labels.Add(destination);
                codes.InsertRange(loadPlayer, newCode);
                break;
            }
            if (collisionIndex == -1)
            {
                if (WildCardMod.ModConfig.Debug)
                {
                    WildCardMod.Instance.Log.LogDebug($"{original.DeclaringType.FullName}.{original.Name} does not use a local to store player collision result!");
                }
                return codes;
            }
            int inverseNullCheckIndex = -1;
            bool nullCheckExists = false;
            for (int i = collisionIndex; i < codes.Count; i++)
            {
                if (!codes[i].Branches(out Label? inequalityLabel))
                {
                    continue;
                }
                for (int j = i; j > collisionIndex; j--)
                {
                    if (!codes[j].Calls(inequality))
                    {
                        continue;
                    }
                    if (codes[^1].labels.Contains(inequalityLabel.Value))
                    {
                        inverseNullCheckIndex = i + 1;
                    }
                    else if (codes[i + 1].opcode.Equals(OpCodes.Ret))
                    {
                        inverseNullCheckIndex = i + 2;
                    }
                    nullCheckExists = true;
                    break;
                }
                break;
            }
            int insertAt = collisionIndex + 2;
            List<CodeInstruction> finalCode = new List<CodeInstruction>();
            if (!nullCheckExists)
            {
                Label nullLabel = generator.DefineLabel();
                finalCode.Add(loadPlayerLocal);
                finalCode.Add(new CodeInstruction(OpCodes.Ldnull));
                finalCode.Add(new CodeInstruction(OpCodes.Call, inequality));
                finalCode.Add(new CodeInstruction(OpCodes.Brfalse_S, nullLabel));
                codes[^1].labels.Add(nullLabel);
            }
            if (!foundDamage)
            {
                LocalBuilder vectorLocal = generator.DeclareLocal(typeof(Vector3));
                Label newLabel = generator.DefineLabel();
                finalCode.Add(loadPlayerLocal);
                finalCode.Add(new CodeInstruction(OpCodes.Ldc_I4_S, (int)CauseOfDeath.Unknown));
                finalCode.Add(new CodeInstruction(OpCodes.Ldloca_S, vectorLocal));
                finalCode.Add(new CodeInstruction(OpCodes.Initobj, typeof(Vector3)));
                finalCode.Add(new CodeInstruction(OpCodes.Ldloc_S, vectorLocal));
                finalCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                finalCode.Add(new CodeInstruction(OpCodes.Call, savePlayerGrace));
                finalCode.Add(new CodeInstruction(OpCodes.Brtrue_S, newLabel));
                if (inverseNullCheckIndex != -1 && finalCode.Count > 0)
                {
                    insertAt = inverseNullCheckIndex;
                    codes[insertAt].MoveLabelsTo(finalCode[0]);
                }
                codes[^1].labels.Add(newLabel);
            }
            codes.InsertRange(insertAt, finalCode);
            if (WildCardMod.ModConfig.Debug)
            {
                MethodBase.GetCurrentMethod().LogIL(original, codes);
            }
            return codes;
        }
    }
    internal static class SavePatches
    {
        [HarmonyPatch(typeof(CadaverGrowthAI), nameof(CadaverGrowthAI.CurePlayer))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        internal static bool CadaverGrowthAI_CureMore(CadaverGrowthAI __instance)
        {
            if (__instance.playerInfections[(int)GameNetworkManager.Instance.localPlayerController.playerClientId].infected)
            {
                __instance.numberOfInfected--;
            }
            HUDManager.Instance.cadaverFilter = 0f;
            SoundManager.Instance.alternateEarsRinging = false;
            SoundManager.Instance.earsRingingTimer = 0f;
            return true;
        }
        [HarmonyPatch(typeof(DepositItemsDesk), nameof(DepositItemsDesk.CollisionDetect))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        internal static bool DepositItemsDesk_Save(DepositItemsDesk __instance)
        {
            if (!__instance.attacking)
            {
                return true;
            }
            return !ILifeSaver.TrySave(GameNetworkManager.Instance.localPlayerController);
        }
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DamagePlayer))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        internal static bool PlayerControllerB_SaveDamage(PlayerControllerB __instance, ref CauseOfDeath causeOfDeath, ref Vector3 force)
        {
            return !ILifeSaver.TrySaveGraceOnly(__instance, causeOfDeath, force);
        }
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        internal static bool PlayerControllerB_SaveKill(PlayerControllerB __instance, ref Vector3 bodyVelocity, ref bool spawnBody, ref CauseOfDeath causeOfDeath)
        {
            if (causeOfDeath == (int)CauseOfDeath.Unknown || !spawnBody || !ILifeSaver.TrySave(__instance, causeOfDeath, bodyVelocity))
            {
                return true;
            }
            return false;
        }
        [HarmonyPatch(typeof(SandWormAI), nameof(SandWormAI.EatPlayer))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        internal static bool SandWormAI_Save(SandWormAI __instance, ref PlayerControllerB playerScript)
        {
            return !ILifeSaver.TrySave(playerScript, hitVelocity: Vector3.up * 10f, enemy: __instance);
        }
        [HarmonyPatch(typeof(BushWolfEnemy), nameof(BushWolfEnemy.OnCollideWithPlayer))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> BushWolfEnemy_Save(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (!codes[i].Calls(onCollision))
                {
                    continue;
                }
                LocalBuilder playerLocal = generator.DeclareLocal(typeof(PlayerControllerB));
                List<CodeInstruction> newLocalDefineCode = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_S, 0),
                    new CodeInstruction(OpCodes.Ldarg_S, 1),
                    new CodeInstruction(OpCodes.Ldarg_S, 0),
                    new CodeInstruction(OpCodes.Ldfld, foxInKill),
                    new CodeInstruction(OpCodes.Ldc_I4_S, 0),
                    new CodeInstruction(OpCodes.Call, collision),
                    new CodeInstruction(OpCodes.Stloc_S, playerLocal)
                };
                codes.InsertRange(i + 1, newLocalDefineCode);
                i += newLocalDefineCode.Count + 1;
                for (int j = i; j < codes.Count; j++)
                {
                    if (!codes[j].Calls(collision))
                    {
                        continue;
                    }
                    codes[j] = new CodeInstruction(OpCodes.Ldloc_S, playerLocal);
                    for (int k = j - 1; k >= 0; k--)
                    {
                        if (codes[k].labels.Count == 0)
                        {
                            continue;
                        }
                        codes[k].MoveLabelsTo(codes[j]);
                        int removeAmount = j - k;
                        codes.RemoveRange(k, removeAmount);
                        if (j < i)
                        {
                            i -= removeAmount;
                        }
                        break;
                    }
                    break;
                }
                for (int j = i; j < codes.Count; j++)
                {
                    if (!codes[j].Calls(killPlayer))
                    {
                        continue;
                    }
                    int oldSkipIndex = -1;
                    Label? oldSkip = null;
                    for (int k = j; k >= 0; k--)
                    {
                        if (!codes[k].Branches(out oldSkip))
                        {
                            continue;
                        }
                        oldSkipIndex = k;
                        break;
                    }
                    if (!oldSkip.HasValue)
                    {
                        WildCardMod.Instance.Log.LogWarning($"Unable to apply transpiler \"{MethodBase.GetCurrentMethod().Name}\" to \"{original.Name}\"!");
                        return instructions;
                    }
                    Label newSkip = generator.DefineLabel();
                    for (int k = j; k < codes.Count; k++)
                    {
                        if (!codes[k].labels.Contains(oldSkip.Value))
                        {
                            continue;
                        }
                        LocalBuilder vectorLocal = generator.DeclareLocal(typeof(Vector3));
                        Label notSavedLabel = generator.DefineLabel();
                        List<CodeInstruction> newCode = new List<CodeInstruction>
                        {
                            new CodeInstruction(OpCodes.Ldloc_S, playerLocal),
                            new CodeInstruction(OpCodes.Ldc_I4_S, (int)CauseOfDeath.Mauling),
                            new CodeInstruction(OpCodes.Ldloca_S, vectorLocal),
                            new CodeInstruction(OpCodes.Initobj, typeof(Vector3)),
                            new CodeInstruction(OpCodes.Ldloc_S, vectorLocal),
                            new CodeInstruction(OpCodes.Ldarg_S, 0),
                            new CodeInstruction(OpCodes.Call, savePlayer),
                            new CodeInstruction(OpCodes.Brfalse_S, notSavedLabel),
                            new CodeInstruction(OpCodes.Ldarg_S, 0),
                            new CodeInstruction(OpCodes.Call, foxCancelReel),
                            new CodeInstruction(OpCodes.Ldarg_S, 0),
                            new CodeInstruction(OpCodes.Ldc_I4_S, 0),
                            new CodeInstruction(OpCodes.Call, switchBehaviourLocal),
                            new CodeInstruction(OpCodes.Br_S, newSkip)
                        };
                        codes[k].labels.Add(newSkip);
                        codes[oldSkipIndex + 1].labels.Add(notSavedLabel);
                        codes.InsertRange(oldSkipIndex + 1, newCode);
                        break;
                    }
                    break;
                }
                break;
            }
            if (WildCardMod.ModConfig.Debug)
            {
                MethodBase.GetCurrentMethod().LogIL(original, codes);
            }
            return codes;
        }
        [HarmonyPatch(typeof(CadaverGrowthAI), nameof(CadaverGrowthAI.BurstFromPlayer))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> CadaverGrowthAI_Save(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (!codes[i].Calls(bloomBurst))
                {
                    continue;
                }
                for (int j = i; j >= 0; j--)
                {
                    if (!codes[j].Branches(out _))
                    {
                        continue;
                    }
                    LocalBuilder vectorLocal = generator.DeclareLocal(typeof(Vector3));
                    Label resumeBurstLabel = generator.DefineLabel();
                    Label skipBurstLabel = generator.DefineLabel();
                    List<CodeInstruction> newCode = new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldarg_S, 1),
                        new CodeInstruction(OpCodes.Ldc_I4_S, (int)CauseOfDeath.Suffocation),
                        new CodeInstruction(OpCodes.Ldloca_S, vectorLocal),
                        new CodeInstruction(OpCodes.Initobj, typeof(Vector3)),
                        new CodeInstruction(OpCodes.Ldloc_S, vectorLocal),
                        new CodeInstruction(OpCodes.Ldnull),
                        new CodeInstruction(OpCodes.Call, savePlayer),
                        new CodeInstruction(OpCodes.Brfalse_S, resumeBurstLabel),
                        new CodeInstruction(OpCodes.Ldarg_S, 0),
                        new CodeInstruction(OpCodes.Ldarg_S, 1),
                        new CodeInstruction(OpCodes.Ldfld, playerClientId),
                        new CodeInstruction(OpCodes.Conv_I4),
                        new CodeInstruction(OpCodes.Call, cadaverCure),
                        new CodeInstruction(OpCodes.Ldarg_S, 0),
                        new CodeInstruction(OpCodes.Ldarg_S, 1),
                        new CodeInstruction(OpCodes.Ldfld, playerClientId),
                        new CodeInstruction(OpCodes.Conv_I4),
                        new CodeInstruction(OpCodes.Call, cadaverCureRPC),
                        new CodeInstruction(OpCodes.Br_S, skipBurstLabel)
                    };
                    codes[j + 1].labels.Add(resumeBurstLabel);
                    codes[i + 1].labels.Add(skipBurstLabel);
                    codes.InsertRange(j + 1, newCode);
                    break;
                }
                break;
            }
            if (WildCardMod.ModConfig.Debug)
            {
                MethodBase.GetCurrentMethod().LogIL(original, codes);
            }
            return codes;
        }
        [HarmonyPatch(typeof(CaveDwellerAI), nameof(CaveDwellerAI.OnCollideWithPlayer))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> CaveDwellerAI_TriggerSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = codes.Count - 1; i >= 0; i--)
            {
                if (!codes[i].Calls(dwellerKill))
                {
                    continue;
                }
                Label? newLabel = null;
                for (int j = i; j < codes.Count; j++)
                {
                    if (codes[j].labels.Count == 0)
                    {
                        continue;
                    }
                    newLabel = generator.DefineLabel();
                    codes[j].labels.Add(newLabel.Value);
                    break;
                }
                for (int j = i; j >= 0; j--)
                {
                    if (!codes[j].Branches(out _))
                    {
                        continue;
                    }
                    LocalBuilder vectorLocal = generator.DeclareLocal(typeof(Vector3));
                    List<CodeInstruction> newCode = new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldloc_S, 0),
                        new CodeInstruction(OpCodes.Ldc_I4_S, (int)CauseOfDeath.Mauling),
                        new CodeInstruction(OpCodes.Ldloca_S, vectorLocal),
                        new CodeInstruction(OpCodes.Initobj, typeof(Vector3)),
                        new CodeInstruction(OpCodes.Ldloc_S, vectorLocal),
                        new CodeInstruction(OpCodes.Ldarg_S, 0),
                        new CodeInstruction(OpCodes.Call, savePlayerTrigger),
                        new CodeInstruction(OpCodes.Brtrue_S, newLabel.Value)
                    };
                    codes.InsertRange(j + 1, newCode);
                    break;
                }
                break;
            }
            if (WildCardMod.ModConfig.Debug)
            {
                MethodBase.GetCurrentMethod().LogIL(original, codes);
            }
            return codes;
        }
        [HarmonyPatch(typeof(CentipedeAI), nameof(CentipedeAI.DamagePlayerOnIntervals))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> CentipedeAI_GraceSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = codes.Count - 1; i >= 0; i--)
            {
                if (!codes[i].Calls(damagePlayer))
                {
                    continue;
                }
                for (int j = i; j >= 0; j--)
                {
                    if (codes[j].labels.Count == 0)
                    {
                        continue;
                    }
                    Label destination = generator.DefineLabel();
                    CodeInstruction first = new CodeInstruction(OpCodes.Ldarg_S, 0);
                    codes[j].MoveLabelsTo(first);
                    LocalBuilder vectorLocal = generator.DeclareLocal(typeof(Vector3));
                    List<CodeInstruction> newCode = new List<CodeInstruction>
                    {
                        first,
                        new CodeInstruction(OpCodes.Ldfld, clingPlayer),
                        new CodeInstruction(OpCodes.Ldc_I4_S, (int)CauseOfDeath.Suffocation),
                        new CodeInstruction(OpCodes.Ldloca_S, vectorLocal),
                        new CodeInstruction(OpCodes.Initobj, typeof(Vector3)),
                        new CodeInstruction(OpCodes.Ldloc_S, vectorLocal),
                        new CodeInstruction(OpCodes.Ldarg_S, 0),
                        new CodeInstruction(OpCodes.Call, savePlayerGrace),
                        new CodeInstruction(OpCodes.Brtrue_S, destination)
                    };
                    codes[i + 1].labels.Add(destination);
                    codes.InsertRange(j, newCode);
                    break;
                }
                break;
            }
            if (WildCardMod.ModConfig.Debug)
            {
                MethodBase.GetCurrentMethod().LogIL(original, codes);
            }
            return codes;
        }
        [HarmonyPatch(typeof(DressGirlAI), nameof(DressGirlAI.OnCollideWithPlayer))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> DressGirlAI_Save(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = codes.Count - 1; i >= 0; i--)
            {
                if (!codes[i].Calls(killPlayer))
                {
                    continue;
                }
                Label? newLabel = null;
                for (int j = i; j >= 0; j--)
                {
                    if (!codes[j].Branches(out _))
                    {
                        continue;
                    }
                    LocalBuilder vectorLocal = generator.DeclareLocal(typeof(Vector3));
                    newLabel = generator.DefineLabel();
                    List<CodeInstruction> preCode = new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldloc_S, 0),
                        new CodeInstruction(OpCodes.Ldc_I4_S, (int)CauseOfDeath.Unknown),
                        new CodeInstruction(OpCodes.Ldloca_S, vectorLocal),
                        new CodeInstruction(OpCodes.Initobj, typeof(Vector3)),
                        new CodeInstruction(OpCodes.Ldloc_S, vectorLocal),
                        new CodeInstruction(OpCodes.Ldarg_S, 0),
                        new CodeInstruction(OpCodes.Call, savePlayer),
                        new CodeInstruction(OpCodes.Brtrue_S, newLabel.Value)
                    };
                    codes.InsertRange(j + 1, preCode);
                    i += preCode.Count;
                    break;
                }
                if (!newLabel.HasValue)
                {
                    WildCardMod.Instance.Log.LogWarning($"Unable to apply transpiler \"{MethodBase.GetCurrentMethod().Name}\" to \"{original.Name}\"!");
                    return instructions;
                }
                List<CodeInstruction> postCode = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_S, 0).WithLabels(newLabel.Value),
                    new CodeInstruction(OpCodes.Call, ghostStopChase),
                    new CodeInstruction(OpCodes.Ldarg_S, 0),
                    new CodeInstruction(OpCodes.Ldc_I4_S, 0),
                    new CodeInstruction(OpCodes.Stfld, timesStared),
                    new CodeInstruction(OpCodes.Ldarg_S, 0),
                    new CodeInstruction(OpCodes.Ldc_I4_S, 0),
                    new CodeInstruction(OpCodes.Stfld, timesSeenByPlayer),
                    new CodeInstruction(OpCodes.Ldarg_S, 0),
                    new CodeInstruction(OpCodes.Ldc_I4_S, 0),
                    new CodeInstruction(OpCodes.Stfld, couldNotStare)
                };
                codes.InsertRange(i + 1, postCode);
                break;
            }
            if (WildCardMod.ModConfig.Debug)
            {
                MethodBase.GetCurrentMethod().LogIL(original, codes);
            }
            return codes;
        }
        [HarmonyPatch(typeof(DressGirlAI), nameof(DressGirlAI.Update))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> DressGirlAI_Switch(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (!codes[i].Branches(out Label? serverLabel))
                {
                    continue;
                }
                for (int j = i; j < codes.Count; j++)
                {
                    if (!codes[j].labels.Contains(serverLabel.Value))
                    {
                        continue;
                    }
                    Label newLabel = generator.DefineLabel();
                    codes[j].labels.Add(newLabel);
                    for (int k = i + 1; k < codes.Count; k++)
                    {
                        if (!codes[k].Branches(out Label? controlledLabel))
                        {
                            continue;
                        }
                        codes[k].opcode = OpCodes.Brfalse_S;
                        for (int l = k; l < codes.Count; l++)
                        {
                            if (!codes[l].labels.Remove(controlledLabel.Value))
                            {
                                continue;
                            }
                            break;
                        }
                        codes[k + 1].labels.Add(controlledLabel.Value);
                        LocalBuilder vectorLocal = generator.DeclareLocal(typeof(Vector3));
                        List<CodeInstruction> newCode = new List<CodeInstruction>
                        {
                            new CodeInstruction(OpCodes.Ldarg_S, 0),
                            new CodeInstruction(OpCodes.Ldfld, hauntingPlayer),
                            new CodeInstruction(OpCodes.Ldc_I4_S, (int)CauseOfDeath.Unknown),
                            new CodeInstruction(OpCodes.Ldloca_S, vectorLocal),
                            new CodeInstruction(OpCodes.Initobj, typeof(Vector3)),
                            new CodeInstruction(OpCodes.Ldloc_S, vectorLocal),
                            new CodeInstruction(OpCodes.Ldarg_S, 0),
                            new CodeInstruction(OpCodes.Call, savePlayerGrace),
                            new CodeInstruction(OpCodes.Brfalse_S, newLabel)
                        };
                        codes.InsertRange(k + 1, newCode);
                        break;
                    }
                    break;
                }
                break;
            }
            if (WildCardMod.ModConfig.Debug)
            {
                MethodBase.GetCurrentMethod().LogIL(original, codes);
            }
            return codes;
        }
        [HarmonyPatch(typeof(FlowermanAI), nameof(FlowermanAI.killAnimation), MethodType.Enumerator)]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> FlowermanAI_TriggerSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = codes.Count - 1; i >= 0; i--)
            {
                if (!codes[i].Calls(killPlayer))
                {
                    continue;
                }
                for (int j = i; j >= 0; j--)
                {
                    if (!codes[j].Branches(out Label? inequalBranch))
                    {
                        continue;
                    }
                    Label newLabel = generator.DefineLabel();
                    for (int k = j; k < codes.Count; k++)
                    {
                        if (!codes[k].labels.Contains(inequalBranch.Value))
                        {
                            continue;
                        }
                        codes[k].labels.Add(newLabel);
                        break;
                    }
                    LocalBuilder vectorLocal = generator.DeclareLocal(typeof(Vector3));
                    List<CodeInstruction> newCode = new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldloc_S, 1),
                        new CodeInstruction(OpCodes.Ldfld, specialAnim),
                        new CodeInstruction(OpCodes.Ldc_I4_S, (int)CauseOfDeath.Strangulation),
                        new CodeInstruction(OpCodes.Ldloca_S, vectorLocal),
                        new CodeInstruction(OpCodes.Initobj, typeof(Vector3)),
                        new CodeInstruction(OpCodes.Ldloc_S, vectorLocal),
                        new CodeInstruction(OpCodes.Ldloc_S, 1),
                        new CodeInstruction(OpCodes.Call, savePlayerTrigger),
                        new CodeInstruction(OpCodes.Brtrue_S, newLabel)
                    };
                    codes.InsertRange(j + 1, newCode);
                    break;
                }
                break;
            }
            if (WildCardMod.ModConfig.Debug)
            {
                MethodBase.GetCurrentMethod().LogIL(original, codes);
            }
            return codes;
        }
        [HarmonyPatch(typeof(ForestGiantAI), nameof(ForestGiantAI.EatPlayerAnimation), MethodType.Enumerator)]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> ForestGiantAI_TriggerSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            FieldInfo playerEaten = original.DeclaringType.GetField("playerBeingEaten");
            for (int i = 0; i < codes.Count; i++)
            {
                if (!codes[i].Calls(killPlayer))
                {
                    continue;
                }
                for (int j = i; j >= 0; j--)
                {
                    if (!codes[j].Branches(out _))
                    {
                        continue;
                    }
                    LocalBuilder vectorLocal = generator.DeclareLocal(typeof(Vector3));
                    Label newLabel = generator.DefineLabel();
                    List<CodeInstruction> newCodes = new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldarg_S, 0),
                        new CodeInstruction(OpCodes.Ldfld, playerEaten),
                        new CodeInstruction(OpCodes.Ldc_I4_S, (int)CauseOfDeath.Crushing),
                        new CodeInstruction(OpCodes.Ldloca_S, vectorLocal),
                        new CodeInstruction(OpCodes.Initobj, typeof(Vector3)),
                        new CodeInstruction(OpCodes.Ldloc_S, vectorLocal),
                        new CodeInstruction(OpCodes.Ldloc_S, 1),
                        new CodeInstruction(OpCodes.Call, savePlayerTrigger),
                        new CodeInstruction(OpCodes.Brfalse_S, newLabel),
                        new CodeInstruction(OpCodes.Ldloc_S, 1),
                        new CodeInstruction(OpCodes.Call, giantStopKill),
                        new CodeInstruction(OpCodes.Ldloc_S, 1),
                        new CodeInstruction(OpCodes.Ldfld, giantPlayerStealth),
                        new CodeInstruction(OpCodes.Ldarg_S, 0),
                        new CodeInstruction(OpCodes.Ldfld, playerEaten),
                        new CodeInstruction(OpCodes.Ldfld, playerClientId),
                        new CodeInstruction(OpCodes.Conv_I4),
                        new CodeInstruction(OpCodes.Ldc_R4, 0f),
                        new CodeInstruction(OpCodes.Stelem_R4),
                        new CodeInstruction(OpCodes.Ldloc_S, 1),
                        new CodeInstruction(OpCodes.Ldc_I4_S, 0),
                        new CodeInstruction(OpCodes.Call, switchBehaviour),
                        new CodeInstruction(OpCodes.Ldc_I4_S, 0),
                        new CodeInstruction(OpCodes.Ret)
                    };
                    codes[j + 1].labels.Add(newLabel);
                    codes.InsertRange(j + 1, newCodes);
                    break;
                }
                break;
            }
            if (WildCardMod.ModConfig.Debug)
            {
                MethodBase.GetCurrentMethod().LogIL(original, codes);
            }
            return codes;
        }
        [HarmonyPatch(typeof(HauntedMaskItem), nameof(HauntedMaskItem.FinishAttaching))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> HauntedMaskItem_Save(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (!codes[i].Calls(allowDeath))
                {
                    continue;
                }
                for (int j = i; j < codes.Count; j++)
                {
                    if (!codes[j].Branches(out Label? oldSkipLabel))
                    {
                        continue;
                    }
                    Label? newLabel = null;
                    for (int k = j; k < codes.Count; k++)
                    {
                        if (!codes[k].labels.Contains(oldSkipLabel.Value))
                        {
                            continue;
                        }
                        newLabel = generator.DefineLabel();
                        codes[j].opcode = OpCodes.Brfalse_S;
                        codes[k].MoveLabelsTo(codes[j + 1]);
                        codes[k].labels.Add(newLabel.Value);
                        break;
                    }
                    if (!newLabel.HasValue)
                    {
                        WildCardMod.Instance.Log.LogWarning($"Unable to apply transpiler \"{MethodBase.GetCurrentMethod().Name}\" to \"{original.Name}\"!");
                        return instructions;
                    }
                    LocalBuilder vectorLocal = generator.DeclareLocal(typeof(Vector3));
                    List<CodeInstruction> newCodes = new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldarg_S, 0),
                        new CodeInstruction(OpCodes.Ldfld, maskHeldBy),
                        new CodeInstruction(OpCodes.Ldc_I4_S, (int)CauseOfDeath.Suffocation),
                        new CodeInstruction(OpCodes.Ldloca_S, vectorLocal),
                        new CodeInstruction(OpCodes.Initobj, typeof(Vector3)),
                        new CodeInstruction(OpCodes.Ldloc_S, vectorLocal),
                        new CodeInstruction(OpCodes.Ldnull),
                        new CodeInstruction(OpCodes.Call, savePlayer),
                        new CodeInstruction(OpCodes.Brfalse_S, newLabel.Value)
                    };
                    codes.InsertRange(j + 1, newCodes);
                    break;
                }
                break;
            }
            if (WildCardMod.ModConfig.Debug)
            {
                MethodBase.GetCurrentMethod().LogIL(original, codes);
            }
            return codes;
        }
        [HarmonyPatch(typeof(JesterAI), nameof(JesterAI.OnCollideWithPlayer))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> JesterAI_TriggerSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (!codes[i].Calls(jesterKill))
                {
                    continue;
                }
                for (int j = i; j >= 0; j--)
                {
                    if (!codes[j].opcode.Equals(OpCodes.Ret))
                    {
                        continue;
                    }
                    LocalBuilder vectorLocal = generator.DeclareLocal(typeof(Vector3));
                    Label newLabel = generator.DefineLabel();
                    List<CodeInstruction> newCode = new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldloc_S, 0),
                        new CodeInstruction(OpCodes.Ldc_I4_S, (int)CauseOfDeath.Mauling),
                        new CodeInstruction(OpCodes.Ldloca_S, vectorLocal),
                        new CodeInstruction(OpCodes.Initobj, typeof(Vector3)),
                        new CodeInstruction(OpCodes.Ldloc_S, vectorLocal),
                        new CodeInstruction(OpCodes.Ldarg_S, 0),
                        new CodeInstruction(OpCodes.Call, savePlayerTrigger),
                        new CodeInstruction(OpCodes.Brfalse_S, newLabel),
                        new CodeInstruction(OpCodes.Ldarg_S, 0),
                        new CodeInstruction(OpCodes.Ldc_I4_S, 0),
                        new CodeInstruction(OpCodes.Call, switchBehaviour),
                        new CodeInstruction(OpCodes.Ret)
                    };
                    codes[j + 1].MoveLabelsTo(newCode[0]);
                    codes[j + 1].labels.Add(newLabel);
                    codes.InsertRange(j + 1, newCode);
                    break;
                }
                break;
            }
            if (WildCardMod.ModConfig.Debug)
            {
                MethodBase.GetCurrentMethod().LogIL(original, codes);
            }
            return codes;
        }
        [HarmonyPatch(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.killAnimation), MethodType.Enumerator)]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> MaskedPlayerEnemy_TriggerSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (!codes[i].opcode.Equals(OpCodes.Switch))
                {
                    continue;
                }
                if (!(codes[i].operand is Label[] switchLabels && switchLabels.Length >= 5))
                {
                    continue;
                }
                for (int j = i; j < codes.Count; j++)
                {
                    if (!codes[j].labels.Contains(switchLabels[4]))
                    {
                        continue;
                    }
                    for (int k = j; k < codes.Count; k++)
                    {
                        if (!codes[k].opcode.Equals(OpCodes.Stfld))
                        {
                            continue;
                        }
                        Label newLabel = generator.DefineLabel();
                        LocalBuilder vectorLocal = generator.DeclareLocal(typeof(Vector3));
                        List<CodeInstruction> newCode = new List<CodeInstruction>
                        {
                            new CodeInstruction(OpCodes.Ldloc_S, 1),
                            new CodeInstruction(OpCodes.Ldfld, specialAnim),
                            new CodeInstruction(OpCodes.Ldc_I4_S, (int)CauseOfDeath.Strangulation),
                            new CodeInstruction(OpCodes.Ldloca_S, vectorLocal),
                            new CodeInstruction(OpCodes.Initobj, typeof(Vector3)),
                            new CodeInstruction(OpCodes.Ldloc_S, vectorLocal),
                            new CodeInstruction(OpCodes.Ldloc_S, 1),
                            new CodeInstruction(OpCodes.Call, savePlayerTrigger),
                            new CodeInstruction(OpCodes.Brfalse_S, newLabel),
                            new CodeInstruction(OpCodes.Ldloc_S, 1),
                            new CodeInstruction(OpCodes.Callvirt, cancelSpecialAnim),
                            new CodeInstruction(OpCodes.Ldc_I4_0),
                            new CodeInstruction(OpCodes.Ret)
                        };
                        codes[k + 1].labels.Add(newLabel);
                        codes.InsertRange(k + 1, newCode);
                        break;
                    }
                    break;
                }
                break;
            }
            if (WildCardMod.ModConfig.Debug)
            {
                MethodBase.GetCurrentMethod().LogIL(original, codes);
            }
            return codes;
        }
        [HarmonyPatch(typeof(MouthDogAI), nameof(MouthDogAI.OnCollideWithPlayer))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> MouthDogAI_TriggerSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (!codes[i].Calls(dogKill))
                {
                    continue;
                }
                Label? oldSkipKill = null;
                for (int j = i; j >= 0; j--)
                {
                    if (codes[j].Branches(out oldSkipKill))
                    {
                        break;
                    }
                }
                if (!oldSkipKill.HasValue)
                {
                    WildCardMod.Instance.Log.LogWarning($"Unable to apply transpiler \"{MethodBase.GetCurrentMethod().Name}\" to \"{original.Name}\"!");
                    return instructions;
                }
                LocalBuilder vectorLocal = generator.DeclareLocal(typeof(Vector3));
                Label newLabel = generator.DefineLabel();
                List<CodeInstruction> newCodes = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldloc_S, 0),
                    new CodeInstruction(OpCodes.Ldc_I4_S, (int)CauseOfDeath.Mauling),
                    new CodeInstruction(OpCodes.Ldloca_S, vectorLocal),
                    new CodeInstruction(OpCodes.Initobj, typeof(Vector3)),
                    new CodeInstruction(OpCodes.Ldloc_S, vectorLocal),
                    new CodeInstruction(OpCodes.Ldarg_S, 0),
                    new CodeInstruction(OpCodes.Call, savePlayerTrigger),
                    new CodeInstruction(OpCodes.Brtrue_S, newLabel)
                };
                for (int j = i; j < codes.Count; j++)
                {
                    if (codes[j].labels.Contains(oldSkipKill.Value))
                    {
                        codes[^1].labels.Add(newLabel);
                        codes.InsertRange(j, newCodes);
                        break;
                    }
                }
                break;
            }
            if (WildCardMod.ModConfig.Debug)
            {
                MethodBase.GetCurrentMethod().LogIL(original, codes);
            }
            return codes;
        }
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DamagePlayer))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> PlayerControllerB_TriggerSaveDamage(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            LocalBuilder overkill = null;
            LocalBuilder healthOverriden = null;
            Label? oldLabel1 = null;
            Label? oldLabel2 = null;
            Label? oldLabel3 = null;
            for (int i = 0; i < codes.Count; i++)
            {
                if (i - 3 < 0 || i + 6 >= codes.Count)
                {
                    continue;
                }
                if (codes[i].Calls(allowDeath))
                {
                    for (int j = i; j < codes.Count; j++)
                    {
                        if (!codes[j - 1].Branches(out _) || !codes[j].opcode.Equals(OpCodes.Ret))
                        {
                            continue;
                        }
                        for (int k = j; k < codes.Count; k++)
                        {
                            if (!codes[k].Branches(out oldLabel1))
                            {
                                continue;
                            }
                            overkill = generator.DeclareLocal(typeof(bool));
                            healthOverriden = generator.DeclareLocal(typeof(bool));
                            codes[k] = new CodeInstruction(OpCodes.Cgt);
                            List<CodeInstruction> setNewLocals = new List<CodeInstruction>
                            {
                                new CodeInstruction(OpCodes.Ldc_I4_S, 0),
                                new CodeInstruction(OpCodes.Ceq),
                                new CodeInstruction(OpCodes.Stloc_S, overkill),
                                new CodeInstruction(OpCodes.Ldc_I4_S, 0),
                                new CodeInstruction(OpCodes.Stloc_S, healthOverriden)
                            };
                            codes.InsertRange(k + 1, setNewLocals);
                            i = k + setNewLocals.Count + 1;
                            break;
                        }
                        if (overkill == null || !oldLabel1.HasValue)
                        {
                            WildCardMod.Instance.Log.LogWarning($"Unable to apply transpiler \"{MethodBase.GetCurrentMethod().Name}\" to \"{original.Name}\"!");
                            return instructions;
                        }
                        break;
                    }
                }
                if (overkill == null || !oldLabel1.HasValue)
                {
                    continue;
                }
                if (!oldLabel2.HasValue)
                {
                    codes[i].Branches(out oldLabel2);
                }
                else if (!oldLabel3.HasValue && codes[i].Branches(out oldLabel3))
                {
                    List<CodeInstruction> newBranch = new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldloc_S, overkill),
                        new CodeInstruction(OpCodes.Brfalse_S, oldLabel1.Value),
                        new CodeInstruction(OpCodes.Ldc_I4_S, 1),
                        new CodeInstruction(OpCodes.Stloc_S, healthOverriden)
                    };
                    codes.InsertRange(i + 1, newBranch);
                    i += newBranch.Count + 1;
                    Label newLabel = generator.DefineLabel();
                    Label noSaveJump = generator.DefineLabel();
                    Label overridenJump = generator.DefineLabel();
                    CodeInstruction elseIfCode = new CodeInstruction(OpCodes.Ldloc_S, overkill);
                    for (int j = i; j < codes.Count; j++)
                    {
                        if (codes[j].Branches(out _))
                        {
                            i = j;
                        }
                        if (codes[j].labels.Count < 3)
                        {
                            continue;
                        }
                        codes[j].MoveLabelsTo(elseIfCode);
                        break;
                    }
                    LocalBuilder vectorLocal = generator.DeclareLocal(typeof(Vector3));
                    List<CodeInstruction> newCode = new List<CodeInstruction>
                    {
                        elseIfCode,
                        new CodeInstruction(OpCodes.Brfalse_S, newLabel),
                        new CodeInstruction(OpCodes.Ldarg_S, 0),
                        new CodeInstruction(OpCodes.Ldarg_S, 4),
                        new CodeInstruction(OpCodes.Ldloca_S, vectorLocal),
                        new CodeInstruction(OpCodes.Initobj, typeof(Vector3)),
                        new CodeInstruction(OpCodes.Ldloc_S, vectorLocal),
                        new CodeInstruction(OpCodes.Ldnull),
                        new CodeInstruction(OpCodes.Call, savePlayerTrigger),
                        new CodeInstruction(OpCodes.Ldc_I4_S, 0),
                        new CodeInstruction(OpCodes.Ceq),
                        new CodeInstruction(OpCodes.Stloc_S, overkill),
                        new CodeInstruction(OpCodes.Ldloc_S, overkill),
                        new CodeInstruction(OpCodes.Brtrue_S, noSaveJump),
                        new CodeInstruction(OpCodes.Ldc_I4_S, 1),
                        new CodeInstruction(OpCodes.Stloc_S, healthOverriden),
                        new CodeInstruction(OpCodes.Ldloc_S, healthOverriden).WithLabels(newLabel, noSaveJump),
                        new CodeInstruction(OpCodes.Brtrue_S, overridenJump)
                    };
                    for (int j = i; j < codes.Count; j++)
                    {
                        if (!codes[j].Calls(mathfClamp3Int))
                        {
                            continue;
                        }
                        for (int k = j; k < codes.Count; k++)
                        {
                            if (!codes[k].StoresField(playerHealth))
                            {
                                continue;
                            }
                            codes[k + 1].labels.Add(overridenJump);
                            break;
                        }
                        break;
                    }
                    codes.InsertRange(i + 1, newCode);
                    i += newCode.Count + 1;
                    for (int j = i; j < codes.Count; j++)
                    {
                        if (!codes[j].Calls(updateHealth))
                        {
                            continue;
                        }
                        for (int l = j; l > 0; l--)
                        {
                            if (!codes[l].LoadsField(playerHealth))
                            {
                                continue;
                            }
                            codes.RemoveRange(l + 1, j - (l + 1));
                            List<CodeInstruction> uiBool = new List<CodeInstruction>
                            {
                                new CodeInstruction(OpCodes.Ldarg_S, 0),
                                new CodeInstruction(OpCodes.Ldfld, playerHealth),
                                new CodeInstruction(OpCodes.Ldc_I4_S, 100),
                                new CodeInstruction(OpCodes.Ceq),
                                new CodeInstruction(OpCodes.Ldc_I4_S, 0),
                                new CodeInstruction(OpCodes.Ceq)
                            };
                            codes.InsertRange(l + 1, uiBool);
                            break;
                        }
                        break;
                    }
                }
                else if (codes[i].Calls(killPlayer))
                {
                    for (int j = i; j < codes.Count; j++)
                    {
                        if (!codes[j].Branches(out _))
                        {
                            continue;
                        }
                        Label overkillJump = generator.DefineLabel();
                        List<CodeInstruction> newCode = new List<CodeInstruction>
                        {
                            new CodeInstruction(OpCodes.Ldloc_S, overkill),
                            new CodeInstruction(OpCodes.Brfalse_S, overkillJump)
                        };
                        codes[j + 1].MoveLabelsTo(newCode[0]);
                        for (int k = j; k < codes.Count; k++)
                        {
                            if (codes[k].Calls(makeInjured))
                            {
                                for (int l = k; l < codes.Count; l++)
                                {
                                    if (codes[l].Branches(out _))
                                    {
                                        codes[l + 1].labels.Add(overkillJump);
                                        break;
                                    }
                                }
                                break;
                            }
                        }
                        codes.InsertRange(j + 1, newCode);
                        break;
                    }
                    break;
                }
            }
            if (WildCardMod.ModConfig.Debug)
            {
                MethodBase.GetCurrentMethod().LogIL(original, codes);
            }
            return codes;
        }
        [HarmonyPatch(typeof(RedLocustBees), nameof(RedLocustBees.OnCollideWithPlayer))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> RedLocustBees_Save(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            Label? skipKill = null;
            for (int i = 0; i < codes.Count; i++)
            {
                if (!codes[i].Calls(beeKill))
                {
                    continue;
                }
                for (int j = i; j >= 0; j--)
                {
                    if (!codes[j].Branches(out _))
                    {
                        continue;
                    }
                    skipKill = generator.DefineLabel();
                    LocalBuilder vectorLocal = generator.DeclareLocal(typeof(Vector3));
                    List<CodeInstruction> newCode = new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldloc_S, 0),
                        new CodeInstruction(OpCodes.Ldc_I4_S, (int)CauseOfDeath.Electrocution),
                        new CodeInstruction(OpCodes.Ldloca_S, vectorLocal),
                        new CodeInstruction(OpCodes.Initobj, typeof(Vector3)),
                        new CodeInstruction(OpCodes.Ldloc_S, vectorLocal),
                        new CodeInstruction(OpCodes.Ldarg_S, 0),
                        new CodeInstruction(OpCodes.Call, savePlayer),
                        new CodeInstruction(OpCodes.Brtrue_S, skipKill.Value)
                    };
                    codes[j + 1].MoveLabelsTo(newCode[0]);
                    codes.InsertRange(j + 1, newCode);
                    break;
                }
                if (!skipKill.HasValue)
                {
                    WildCardMod.Instance.Log.LogWarning($"Unable to apply transpiler \"{MethodBase.GetCurrentMethod().Name}\" to \"{original.Name}\"!");
                    return instructions;
                }
                for (int j = i; j < codes.Count; j++)
                {
                    if (!codes[j].Branches(out Label? labelCheck) || labelCheck.Value == skipKill.Value)
                    {
                        continue;
                    }
                    codes[j].labels.Add(skipKill.Value);
                    break;
                }
                break;
            }
            if (WildCardMod.ModConfig.Debug)
            {
                MethodBase.GetCurrentMethod().LogIL(original, codes);
            }
            return codes;
        }
        [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.DestroyCar))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> VehicleController_Save(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (!codes[i].Calls(killPlayer))
                {
                    continue;
                }
                int preIndex = -1;
                List<CodeInstruction> preCode = new List<CodeInstruction>();
                Label? skipKill = null;
                for (int j = i; j >= 0; j--)
                {
                    if (!codes[j].Branches(out _))
                    {
                        continue;
                    }
                    preIndex = j + 1;
                    skipKill = generator.DefineLabel();
                    LocalBuilder vectorLocal = generator.DeclareLocal(typeof(Vector3));
                    preCode.Add(new CodeInstruction(OpCodes.Ldloc_S, 0));
                    preCode.Add(new CodeInstruction(OpCodes.Ldc_I4_S, (int)CauseOfDeath.Blast));
                    preCode.Add(new CodeInstruction(OpCodes.Ldloca_S, vectorLocal));
                    preCode.Add(new CodeInstruction(OpCodes.Initobj, typeof(Vector3)));
                    preCode.Add(new CodeInstruction(OpCodes.Ldloc_S, vectorLocal));
                    preCode.Add(new CodeInstruction(OpCodes.Ldnull));
                    preCode.Add(new CodeInstruction(OpCodes.Call, savePlayer));
                    preCode.Add(new CodeInstruction(OpCodes.Brtrue_S, skipKill.Value));
                    break;
                }
                if (!skipKill.HasValue)
                {
                    WildCardMod.Instance.Log.LogWarning($"Unable to apply transpiler \"{MethodBase.GetCurrentMethod().Name}\" to \"{original.Name}\"!");
                    return instructions;
                }
                int postIndex = -1;
                Label? skipNew = null;
                List<CodeInstruction> postCode = new List<CodeInstruction>();
                for (int j = i; j < codes.Count; j++)
                {
                    if (codes[j].labels.Count == 0)
                    {
                        continue;
                    }
                    postIndex = j + 1;
                    skipNew = generator.DefineLabel();
                    postCode.Add(new CodeInstruction(OpCodes.Br_S, skipNew));
                    postCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0).WithLabels(skipKill.Value));
                    postCode.Add(new CodeInstruction(OpCodes.Call, exitDriver));
                    postCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                    postCode.Add(new CodeInstruction(OpCodes.Call, exitPassenger));
                    break;
                }
                if (!skipNew.HasValue)
                {
                    WildCardMod.Instance.Log.LogWarning($"Unable to apply transpiler \"{MethodBase.GetCurrentMethod().Name}\" to \"{original.Name}\"!");
                    return instructions;
                }
                codes[postIndex].labels.Add(skipNew.Value);
                codes.InsertRange(postIndex, postCode);
                codes.InsertRange(preIndex, preCode);
                break;
            }
            if (WildCardMod.ModConfig.Debug)
            {
                MethodBase.GetCurrentMethod().LogIL(original, codes);
            }
            return codes;
        }
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.PlayerIsTargetable))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        internal static void EnemyAI_Untargetable(EnemyAI __instance, ref PlayerControllerB playerScript, ref bool __result)
        {
            if (!__result)
            {
                return;
            }
            ILifeSaver lifeSaver = ILifeSaver.HasLifeSaver(playerScript);
            if (lifeSaver == null)
            {
                return;
            }
            __result = !lifeSaver.UntargetableWhen(playerScript);
        }
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.CheckConditionsForSinkingInQuicksand))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        internal static void PlayerControllerB_GraceSaveQuicksand(PlayerControllerB __instance, ref bool __result)
        {
            if (!__result || __instance.isUnderwater)
            {
                return;
            }
            __result = !(ILifeSaver.TrySaveGraceOnly(__instance, CauseOfDeath.Suffocation) || (__instance.sinkingValue >= 1f && ILifeSaver.TrySaveTriggerOnly(__instance, CauseOfDeath.Suffocation)));
        }
    }
}