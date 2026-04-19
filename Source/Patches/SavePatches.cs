using GameNetcodeStuff;
using HarmonyLib;
using LCWildCardMod.Items;
using LCWildCardMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static LCWildCardMod.Utils.HarmonyHelper;
namespace LCWildCardMod.Patches
{
    public static class EnemyAIFyrusOrHaloGraceSavePatch
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> GetEachEnemyPlayerCollision()
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
        public static IEnumerable<CodeInstruction> FyrusOrHaloGraceSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            bool foundDamage = false;
            int collisionIndex = -1;
            int damageParams = damagePlayer.GetParameters().Length;
            CodeInstruction loadPlayerLocal = null;
            Log.LogDebug($"Patching {original.DeclaringType}.{original.Name}");
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
                List<CodeInstruction> newCode = new List<CodeInstruction>
                {
                    newLoadPlayerLocal,
                    new CodeInstruction(OpCodes.Ldarg_S, 0),
                    new CodeInstruction(OpCodes.Call, wasFyrusOrHaloGraceSaved),
                    new CodeInstruction(OpCodes.Brtrue_S, destination)
                };
                codes[i + 1].labels.Add(destination);
                codes.InsertRange(loadPlayer, newCode);
                break;
            }
            if (collisionIndex == -1)
            {
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
                Label newLabel = generator.DefineLabel();
                finalCode.Add(loadPlayerLocal);
                finalCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                finalCode.Add(new CodeInstruction(OpCodes.Call, wasFyrusOrHaloGraceSaved));
                finalCode.Add(new CodeInstruction(OpCodes.Brfalse_S, newLabel));
                finalCode.Add(new CodeInstruction(OpCodes.Ret));
                if (inverseNullCheckIndex != -1 && finalCode.Count > 0)
                {
                    insertAt = inverseNullCheckIndex;
                    codes[insertAt].MoveLabelsTo(finalCode[0]);
                }
                codes[insertAt].labels.Add(newLabel);
            }
            codes.InsertRange(insertAt, finalCode);
            return codes;
        }
    }
    public static class SavePatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(typeof(CadaverGrowthAI), nameof(CadaverGrowthAI.CurePlayer))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static bool CadaverGrowthAI_CureMore(CadaverGrowthAI __instance)
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
        public static bool DepositItemsDesk_Save()
        {
            return !SaveHelper.SaveIfAny(GameNetworkManager.Instance.localPlayerController);
        }
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DamagePlayer))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static bool PlayerControllerB_FyrusSaveDamage(PlayerControllerB __instance, ref Vector3 force)
        {
            bool saved = SaveHelper.SaveIfFyrus(__instance);
            if (saved)
            {
                __instance.externalForceAutoFade += force;
            }
            return !saved;
        }
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static bool PlayerControllerB_SaveKill(PlayerControllerB __instance, ref Vector3 bodyVelocity, ref bool spawnBody, ref CauseOfDeath causeOfDeath)
        {
            if (!SaveHelper.IsSaveable(__instance, out bool starSave, out SmithHalo haloRef) || causeOfDeath == CauseOfDeath.Unknown || !spawnBody)
            {
                return true;
            }
            if (starSave)
            {
                __instance.externalForceAutoFade += bodyVelocity;
            }
            else
            {
                Log.LogDebug("Running Halo Exhaust from Kill");
                haloRef.ExhaustLocal(__instance, bodyVelocity);
            }
            return false;
        }
        [HarmonyPatch(typeof(SandWormAI), nameof(SandWormAI.EatPlayer))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static bool SandWormAI_Save(SandWormAI __instance, ref PlayerControllerB playerScript)
        {
            try
            {
                return !SaveHelper.SaveIfAny(playerScript, __instance);
            }
            catch (Exception exception)
            {
                Log.LogError(exception);
            }
            return true;
        }
        [HarmonyPatch(typeof(BushWolfEnemy), nameof(BushWolfEnemy.OnCollideWithPlayer))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> BushWolfEnemy_Save(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
                    new CodeInstruction(OpCodes.Stloc_S, playerLocal.LocalIndex)
                };
                codes.InsertRange(i + 1, newLocalDefineCode);
                i += newLocalDefineCode.Count + 1;
                for (int j = i; j < codes.Count; j++)
                {
                    if (!codes[j].Calls(collision))
                    {
                        continue;
                    }
                    codes[j] = new CodeInstruction(OpCodes.Ldloc_S, playerLocal.LocalIndex);
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
                        return codes;
                    }
                    Label newSkip = generator.DefineLabel();
                    for (int k = j; k < codes.Count; k++)
                    {
                        if (!codes[k].labels.Contains(oldSkip.Value))
                        {
                            continue;
                        }
                        Label notSavedLabel = generator.DefineLabel();
                        List<CodeInstruction> newCode = new List<CodeInstruction>
                        {
                            new CodeInstruction(OpCodes.Ldloc_S, playerLocal.LocalIndex),
                            new CodeInstruction(OpCodes.Ldarg_S, 0),
                            new CodeInstruction(OpCodes.Call, anySave),
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
            return codes;
        }
        [HarmonyPatch(typeof(CadaverGrowthAI), nameof(CadaverGrowthAI.BurstFromPlayer))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> CadaverGrowthAI_Save(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
                    Label resumeBurstLabel = generator.DefineLabel();
                    Label skipBurstLabel = generator.DefineLabel();
                    List<CodeInstruction> newCode = new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldarg_S, 1),
                        new CodeInstruction(OpCodes.Ldnull),
                        new CodeInstruction(OpCodes.Call, anySave),
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
            return codes;
        }
        [HarmonyPatch(typeof(CaveDwellerAI), nameof(CaveDwellerAI.OnCollideWithPlayer))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> CaveDwellerAI_HaloSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
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
                    List<CodeInstruction> newCode = new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldloc_S, 0),
                        new CodeInstruction(OpCodes.Call, haloSave),
                        new CodeInstruction(OpCodes.Brtrue_S, newLabel.Value)
                    };
                    codes.InsertRange(i + 1, newCode);
                    break;
                }
                break;
            }
            return codes;
        }
        [HarmonyPatch(typeof(CentipedeAI), nameof(CentipedeAI.DamagePlayerOnIntervals))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> CentipedeAI_FyrusSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
                    List<CodeInstruction> newCode = new List<CodeInstruction>
                    {
                        first,
                        new CodeInstruction(OpCodes.Ldfld, clingPlayer),
                        new CodeInstruction(OpCodes.Ldarg_S, 0),
                        new CodeInstruction(OpCodes.Call, wasFyrusOrHaloGraceSaved),
                        new CodeInstruction(OpCodes.Brtrue_S, destination)
                    };
                    codes[i + 1].labels.Add(destination);
                    codes.InsertRange(j, newCode);
                    break;
                }
                break;
            }
            return codes;
        }
        [HarmonyPatch(typeof(DressGirlAI), nameof(DressGirlAI.OnCollideWithPlayer))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DressGirlAI_Save(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
                    newLabel = generator.DefineLabel();
                    List<CodeInstruction> preCode = new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldloc_S, 0),
                        new CodeInstruction(OpCodes.Ldarg_S, 0),
                        new CodeInstruction(OpCodes.Call, anySave),
                        new CodeInstruction(OpCodes.Brtrue_S, newLabel.Value)
                    };
                    codes.InsertRange(j + 1, preCode);
                    i += preCode.Count;
                    break;
                }
                if (!newLabel.HasValue)
                {
                    return codes;
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
            return codes;
        }
        [HarmonyPatch(typeof(DressGirlAI), nameof(DressGirlAI.Update))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DressGirlAI_Switch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
                        List<CodeInstruction> newCode = new List<CodeInstruction>
                        {
                            new CodeInstruction(OpCodes.Ldarg_S, 0),
                            new CodeInstruction(OpCodes.Ldfld, hauntingPlayer),
                            new CodeInstruction(OpCodes.Ldarg_S, 0),
                            new CodeInstruction(OpCodes.Call, wasFyrusOrHaloGraceSaved),
                            new CodeInstruction(OpCodes.Brfalse_S, newLabel)
                        };
                        codes.InsertRange(k + 1, newCode);
                        break;
                    }
                    break;
                }
                break;
            }
            return codes;
        }
        [HarmonyPatch(typeof(FlowermanAI), nameof(FlowermanAI.OnCollideWithPlayer))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> FlowermanAI_HaloSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = codes.Count - 1; i >= 0; i--)
            {
                if (!codes[i].Calls(brackenKill))
                {
                    continue;
                }
                Label skipKill = generator.DefineLabel();
                for (int j = i; j < codes.Count; j++)
                {
                    if (codes[j].labels.Count == 0)
                    {
                        continue;
                    }
                    codes[j].labels.Add(skipKill);
                    break;
                }
                for (int j = i; j >= 0; j--)
                {
                    if (!codes[j].Branches(out _))
                    {
                        continue;
                    }
                    Label newLabel = generator.DefineLabel();
                    List<CodeInstruction> newCode = new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldloc_S, 0),
                        new CodeInstruction(OpCodes.Call, haloSave),
                        new CodeInstruction(OpCodes.Brfalse_S, newLabel),
                        new CodeInstruction(OpCodes.Ldarg_S, 0),
                        new CodeInstruction(OpCodes.Ldc_I4_S, 1),
                        new CodeInstruction(OpCodes.Call, switchBehaviour),
                        new CodeInstruction(OpCodes.Ldarg_S, 0),
                        new CodeInstruction(OpCodes.Ldfld, enemyAgent),
                        new CodeInstruction(OpCodes.Ldc_R4, 0f),
                        new CodeInstruction(OpCodes.Callvirt, setSpeed),
                        new CodeInstruction(OpCodes.Ldarg_S, 0),
                        new CodeInstruction(OpCodes.Ldc_R4, 0f),
                        new CodeInstruction(OpCodes.Stfld, brackenEvade),
                        new CodeInstruction(OpCodes.Br_S, skipKill)
                    };
                    codes[j + 1].labels.Add(newLabel);
                    codes.InsertRange(j + 1, newCode);
                    break;
                }
                break;
            }
            return codes;
        }
        [HarmonyPatch(typeof(ForestGiantAI), nameof(ForestGiantAI.EatPlayerAnimation), MethodType.Enumerator)]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ForestGiantAI_HaloSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
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
                    Label newLabel = generator.DefineLabel();
                    List<CodeInstruction> newCodes = new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldarg_S, 0),
                        new CodeInstruction(OpCodes.Ldfld, playerEaten),
                        new CodeInstruction(OpCodes.Call, haloSave),
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
            return codes;
        }
        [HarmonyPatch(typeof(HauntedMaskItem), nameof(HauntedMaskItem.FinishAttaching))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> HauntedMaskItem_Save(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
                        return codes;
                    }
                    List<CodeInstruction> newCodes = new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldarg_S, 0),
                        new CodeInstruction(OpCodes.Ldfld, maskHeldBy),
                        new CodeInstruction(OpCodes.Ldnull),
                        new CodeInstruction(OpCodes.Call, anySave),
                        new CodeInstruction(OpCodes.Brfalse_S, newLabel.Value)
                    };
                    codes.InsertRange(j + 1, newCodes);
                    break;
                }
                break;
            }
            return codes;
        }
        [HarmonyPatch(typeof(JesterAI), nameof(JesterAI.OnCollideWithPlayer))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> JesterAI_HaloSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
                    Label newLabel = generator.DefineLabel();
                    List<CodeInstruction> newCode = new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldloc_S, 0),
                        new CodeInstruction(OpCodes.Call, haloSave),
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
            return codes;
        }
        [HarmonyPatch(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.killAnimation), MethodType.Enumerator)]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> MaskedPlayerEnemy_HaloSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
                        List<CodeInstruction> newCode = new List<CodeInstruction>
                        {
                            new CodeInstruction(OpCodes.Ldloc_S, 1),
                            new CodeInstruction(OpCodes.Ldfld, specialAnim),
                            new CodeInstruction(OpCodes.Call, haloSave),
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
            return codes;
        }
        [HarmonyPatch(typeof(MouthDogAI), nameof(MouthDogAI.OnCollideWithPlayer))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> MouthDogAI_HaloSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
                    return codes;
                }
                Label newLabel = generator.DefineLabel();
                List<CodeInstruction> newCodes = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldloc_S, 0),
                    new CodeInstruction(OpCodes.Call, haloSave),
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
            return codes;
        }
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DamagePlayer))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> PlayerControllerB_HaloSaveDamage(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
                    Log.LogDebug("Found PlayerControllerB.AllowPlayerDeath, replacing next branch with setting new locals");
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
                            List<CodeInstruction> setNewLocals = new List<CodeInstruction>();
                            setNewLocals.Add(new CodeInstruction(OpCodes.Ldc_I4_S, 0));
                            setNewLocals.Add(new CodeInstruction(OpCodes.Ceq));
                            setNewLocals.Add(new CodeInstruction(OpCodes.Stloc_S, overkill.LocalIndex));
                            setNewLocals.AddRange(DebugLoadFromThis<int>("Starting health", OpCodes.Ldfld, playerHealth));
                            setNewLocals.AddRange(DebugLoad<int>("Damage", OpCodes.Ldarg_S, 1));
                            setNewLocals.AddRange(DebugLoad<bool>("Is Overkill?", OpCodes.Ldloc_S, overkill.LocalIndex));
                            setNewLocals.Add(new CodeInstruction(OpCodes.Ldc_I4_S, 0));
                            setNewLocals.Add(new CodeInstruction(OpCodes.Stloc_S, healthOverriden.LocalIndex));
                            codes.InsertRange(k + 1, setNewLocals);
                            i = k + setNewLocals.Count + 1;
                            break;
                        }
                        if (overkill == null || !oldLabel1.HasValue)
                        {
                            Log.LogWarning("Unable to apply transpiler to PlayerControllerB.DamagePlayer! Halo save will not work properly!");
                            return codes;
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
                    Log.LogDebug("Successfully set locals, adding overkill checks");
                    List<CodeInstruction> newBranch = new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldloc_S, overkill.LocalIndex),
                        new CodeInstruction(OpCodes.Brfalse_S, oldLabel1.Value),
                        new CodeInstruction(OpCodes.Ldc_I4_S, 1),
                        new CodeInstruction(OpCodes.Stloc_S, healthOverriden.LocalIndex)
                    };
                    newBranch.AddRange(DebugLoad<bool>("Vanilla critical injury, setting health override to", OpCodes.Ldloc_S, healthOverriden.LocalIndex));
                    codes.InsertRange(i + 1, newBranch);
                    i += newBranch.Count + 1;
                    Label newLabel = generator.DefineLabel();
                    Label noSaveJump = generator.DefineLabel();
                    Label overridenJump = generator.DefineLabel();
                    CodeInstruction elseIfCode = new CodeInstruction(OpCodes.Ldloc_S, overkill.LocalIndex);
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
                        Log.LogDebug("Moving first if statement's branch destinations to the new else if statement");
                        codes[j].MoveLabelsTo(elseIfCode);
                        break;
                    }
                    List<CodeInstruction> newCode = new List<CodeInstruction>();
                    newCode.Add(elseIfCode);
                    newCode.Add(new CodeInstruction(OpCodes.Brfalse_S, newLabel));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Call, haloSave));
                    newCode.Add(new CodeInstruction(OpCodes.Ldc_I4_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Ceq));
                    newCode.Add(new CodeInstruction(OpCodes.Stloc_S, overkill.LocalIndex));
                    newCode.AddRange(DebugLoad<bool>("Checking for halo, setting Overkill to", OpCodes.Ldloc_S, overkill.LocalIndex));
                    newCode.Add(new CodeInstruction(OpCodes.Ldloc_S, overkill.LocalIndex));
                    newCode.Add(new CodeInstruction(OpCodes.Brtrue_S, noSaveJump));
                    newCode.Add(new CodeInstruction(OpCodes.Ldc_I4_S, 1));
                    newCode.Add(new CodeInstruction(OpCodes.Stloc_S, healthOverriden.LocalIndex));
                    newCode.AddRange(DebugString("Saved player from death"));
                    newCode.Add(new CodeInstruction(OpCodes.Ldloc_S, healthOverriden.LocalIndex).WithLabels(newLabel, noSaveJump));
                    newCode.Add(new CodeInstruction(OpCodes.Brtrue_S, overridenJump));
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
                            Log.LogDebug("Found health clamp, adding overriden branch after");
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
                    Log.LogDebug("Found PlayerControllerB.KillPlayer, adding overkillJump jump after next branch");
                    for (int j = i; j < codes.Count; j++)
                    {
                        if (!codes[j].Branches(out _))
                        {
                            continue;
                        }
                        Label overkillJump = generator.DefineLabel();
                        List<CodeInstruction> newCode = new List<CodeInstruction>
                        {
                            new CodeInstruction(OpCodes.Ldloc_S, overkill.LocalIndex),
                            new CodeInstruction(OpCodes.Brfalse_S, overkillJump)
                        };
                        newCode.AddRange(DebugString("Critically injuring player"));
                        codes[j + 1].MoveLabelsTo(newCode[0]);
                        for (int k = j; k < codes.Count; k++)
                        {
                            if (codes[k].Calls(makeInjured))
                            {
                                Log.LogDebug("Found PlayerControllerB.MakeCriticallyInjured, adding overkillJump destination to next branch");
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
            return codes;
        }
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Update))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> PlayerControllerB_HaloSaveSink(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (!codes[i].Calls(killPlayer))
                {
                    continue;
                }
                int preKillIndex = -1;
                bool checkForPreSink = false;
                for (int j = i; j > 0; j--)
                {
                    if (!codes[j].LoadsField(playerSinking))
                    {
                        continue;
                    }
                    checkForPreSink = true;
                    for (int k = j; k < codes.Count; k++)
                    {
                        if (!codes[k].Branches(out _))
                        {
                            continue;
                        }
                        preKillIndex = k;
                        break;
                    }
                    break;
                }
                Label? skipLabel = null;
                Label? skipKillDestinationRef = null;
                bool checkForPostSink = false;
                for (int j = i; j < codes.Count; j++)
                {
                    if (!checkForPostSink && codes[j].LoadsField(playerSinking))
                    {
                        checkForPostSink = true;
                        continue;
                    }
                    if (!skipKillDestinationRef.HasValue)
                    {
                        codes[j].Branches(out skipKillDestinationRef);
                    }
                    else if (checkForPreSink && checkForPostSink && codes[j].labels.Contains(skipKillDestinationRef.Value))
                    {
                        skipLabel = generator.DefineLabel();
                        codes[j].labels.Add(skipLabel.Value);
                        break;
                    }
                }
                if (!checkForPreSink || !checkForPostSink)
                {
                    continue;
                }
                i = preKillIndex;
                Label killLabel = generator.DefineLabel();
                List<CodeInstruction> newCode = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_S, 0),
                    new CodeInstruction(OpCodes.Call, haloSave),
                    new CodeInstruction(OpCodes.Brfalse_S, killLabel),
                    new CodeInstruction(OpCodes.Ldarg_S, 0),
                    new CodeInstruction(OpCodes.Ldc_R4, 0f),
                    new CodeInstruction(OpCodes.Stfld, playerSinking),
                    new CodeInstruction(OpCodes.Br_S, skipLabel.Value)
                };
                codes[i + 1].labels.Add(killLabel);
                codes.InsertRange(i + 1, newCode);
                break;
            }
            return codes;
        }
        [HarmonyPatch(typeof(RedLocustBees), nameof(RedLocustBees.OnCollideWithPlayer))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> RedLocustBees_Save(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
                    for (int k = j; k >= 0; k--)
                    {
                        if (!codes[k].Branches(out _))
                        {
                            continue;
                        }
                        skipKill = generator.DefineLabel();
                        List<CodeInstruction> newCode = new List<CodeInstruction>
                        {
                            new CodeInstruction(OpCodes.Ldloc_S, 0),
                            new CodeInstruction(OpCodes.Ldarg_S, 0),
                            new CodeInstruction(OpCodes.Call, anySave),
                            new CodeInstruction(OpCodes.Brtrue_S, skipKill.Value)
                        };
                        codes[j + 1].MoveLabelsTo(newCode[0]);
                        codes.InsertRange(j + 1, newCode);
                        break;
                    }
                    break;
                }
                if (!skipKill.HasValue)
                {
                    return codes;
                }
                Label? oldLabel = null;
                for (int j = i; j < codes.Count; j++)
                {
                    if (!oldLabel.HasValue && !codes[j].Branches(out oldLabel))
                    {
                        continue;
                    }
                    if (codes[j].labels.Contains(oldLabel.Value))
                    {
                        codes[j].labels.Add(skipKill.Value);
                        break;
                    }
                }
                break;
            }
            return codes;
        }
        [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.DestroyCar))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> VehicleController_Save(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
                    preCode.Add(new CodeInstruction(OpCodes.Ldloc_S, 0));
                    preCode.Add(new CodeInstruction(OpCodes.Ldnull));
                    preCode.Add(new CodeInstruction(OpCodes.Call, anySave));
                    preCode.Add(new CodeInstruction(OpCodes.Brtrue_S, skipKill.Value));
                    break;
                }
                if (!skipKill.HasValue)
                {
                    return codes;
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
                    return codes;
                }
                codes[postIndex].labels.Add(skipNew.Value);
                codes.InsertRange(postIndex, postCode);
                codes.InsertRange(preIndex, preCode);
                break;
            }
            return codes;
        }
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.PlayerIsTargetable))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        public static void EnemyAI_HaloGraceUntargetable(ref PlayerControllerB playerScript, ref bool __result)
        {
            if (__result)
            {
                __result = !SaveHelper.WasHaloSaved(playerScript, out _);
            }
        }
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.CheckConditionsForSinkingInQuicksand))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        public static void PlayerControllerB_FyrusSaveQuicksand(PlayerControllerB __instance, ref bool __result)
        {
            if (!__result || __instance.isUnderwater)
            {
                return;
            }
            __result = !SaveHelper.SaveIfFyrus(__instance);
        }
    }
}