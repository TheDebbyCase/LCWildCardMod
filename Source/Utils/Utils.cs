using GameNetcodeStuff;
using HarmonyLib;
using LCWildCardMod.Items;
using LethalCompanyInputUtils.Api;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Text;
using System.Collections;
using Unity.Netcode;
using System.Linq;
using Unity.Netcode.Components;
using UnityEngine.Audio;
using LethalCompanyInputUtils.BindingPathEnums;
using BepInEx.Logging;
using System.Collections.ObjectModel;
using LethalLib.Modules;
using static LethalLib.Modules.MapObjects;
namespace LCWildCardMod.Utils
{
    public static class HarmonyHelper
    {
        internal static MethodInfo toString = AccessTools.Method(typeof(object), nameof(ToString));
        internal static MethodInfo stringConcat3 = AccessTools.Method(typeof(string), nameof(string.Concat), new Type[] { typeof(string), typeof(string), typeof(string) });
        internal static MethodInfo logString = AccessTools.Method(typeof(HarmonyHelper), nameof(LogString), new Type[] { typeof(string) });
        internal static MethodInfo inequality = AccessTools.Method(typeof(UnityEngine.Object), "op_Inequality", new Type[] { typeof(UnityEngine.Object), typeof(UnityEngine.Object) });
        internal static MethodInfo mathfClamp3Int = AccessTools.Method(typeof(Mathf), nameof(Mathf.Clamp), new Type[] { typeof(int), typeof(int), typeof(int) });
        internal static MethodInfo collision = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.MeetsStandardPlayerCollisionConditions), new Type[] { typeof(Collider), typeof(bool), typeof(bool) });
        internal static MethodInfo onCollision = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.OnCollideWithPlayer), new Type[] { typeof(Collider) });
        internal static MethodInfo exitDriver = AccessTools.Method(typeof(VehicleController), nameof(VehicleController.ExitDriverSideSeat));
        internal static MethodInfo exitPassenger = AccessTools.Method(typeof(VehicleController), nameof(VehicleController.ExitPassengerSideSeat));
        internal static MethodInfo savePlayer = AccessTools.Method(typeof(ILifeSaver), nameof(ILifeSaver.TrySave), new Type[] { typeof(PlayerControllerB), typeof(CauseOfDeath), typeof(Vector3), typeof(EnemyAI) });
        internal static MethodInfo savePlayerGrace = AccessTools.Method(typeof(ILifeSaver), nameof(ILifeSaver.TrySaveGraceOnly), new Type[] { typeof(PlayerControllerB), typeof(CauseOfDeath), typeof(Vector3), typeof(EnemyAI) });
        internal static MethodInfo savePlayerTrigger = AccessTools.Method(typeof(ILifeSaver), nameof(ILifeSaver.TrySaveTriggerOnly), new Type[] { typeof(PlayerControllerB), typeof(CauseOfDeath), typeof(Vector3), typeof(EnemyAI) });
        internal static MethodInfo killPlayer = AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer), new Type[] { typeof(Vector3), typeof(bool), typeof(CauseOfDeath), typeof(int), typeof(Vector3), typeof(bool) });
        internal static MethodInfo damagePlayer = AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.DamagePlayer), new Type[] { typeof(int), typeof(bool), typeof(bool), typeof(CauseOfDeath), typeof(int), typeof(bool), typeof(Vector3) });
        internal static MethodInfo makeInjured = AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.MakeCriticallyInjured), new Type[] { typeof(bool) });
        internal static MethodInfo updateHealth = AccessTools.Method(typeof(HUDManager), nameof(HUDManager.UpdateHealthUI), new Type[] { typeof(int), typeof(bool) });
        internal static MethodInfo switchBehaviour = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.SwitchToBehaviourState), new Type[] { typeof(int) });
        internal static MethodInfo switchBehaviourLocal = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.SwitchToBehaviourStateOnLocalClient), new Type[] { typeof(int) });
        internal static MethodInfo allowDeath = AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.AllowPlayerDeath));
        internal static MethodInfo cancelSpecialAnim = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.CancelSpecialAnimationWithPlayer));
        internal static MethodInfo beeKill = AccessTools.Method(typeof(RedLocustBees), nameof(RedLocustBees.BeeKillPlayerOnLocalClient), new Type[] { typeof(int) });
        internal static MethodInfo dogKill = AccessTools.Method(typeof(MouthDogAI), nameof(MouthDogAI.KillPlayerServerRpc), new Type[] { typeof(int) });
        internal static MethodInfo jesterKill = AccessTools.Method(typeof(JesterAI), nameof(JesterAI.KillPlayerServerRpc), new Type[] { typeof(int) });
        internal static MethodInfo dwellerKill = AccessTools.Method(typeof(CaveDwellerAI), nameof(CaveDwellerAI.KillPlayerAnimationServerRpc), new Type[] { typeof(int) });
        internal static MethodInfo bloomBurst = AccessTools.Method(typeof(CadaverBloomAI), nameof(CadaverBloomAI.BurstForth), new Type[] { typeof(PlayerControllerB), typeof(bool), typeof(Vector3), typeof(Vector3) });
        internal static MethodInfo foxCancelReel = AccessTools.Method(typeof(BushWolfEnemy), nameof(BushWolfEnemy.CancelReelingPlayerIn));
        internal static MethodInfo cadaverCure = AccessTools.Method(typeof(CadaverGrowthAI), nameof(CadaverGrowthAI.CurePlayer), new Type[] { typeof(int) });
        internal static MethodInfo cadaverCureRPC = AccessTools.Method(typeof(CadaverGrowthAI), nameof(CadaverGrowthAI.CurePlayerRpc), new Type[] { typeof(int) });
        internal static MethodInfo giantStopKill = AccessTools.Method(typeof(ForestGiantAI), nameof(ForestGiantAI.StopKillAnimation));
        internal static MethodInfo ghostStopChase = AccessTools.Method(typeof(DressGirlAI), nameof(DressGirlAI.StopChasing));
        internal static FieldInfo playerName = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.playerUsername));
        internal static FieldInfo playerClientId = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.playerClientId));
        internal static FieldInfo enemyType = AccessTools.Field(typeof(EnemyAI), nameof(EnemyAI.enemyType));
        internal static FieldInfo enemyName = AccessTools.Field(typeof(EnemyType), nameof(EnemyType.enemyName));
        internal static FieldInfo foxInKill = AccessTools.Field(typeof(BushWolfEnemy), nameof(BushWolfEnemy.inKillAnimation));
        internal static FieldInfo clingPlayer = AccessTools.Field(typeof(CentipedeAI), nameof(CentipedeAI.clingingToPlayer));
        internal static FieldInfo timesSeenByPlayer = AccessTools.Field(typeof(DressGirlAI), nameof(DressGirlAI.timesSeenByPlayer));
        internal static FieldInfo timesStared = AccessTools.Field(typeof(DressGirlAI), nameof(DressGirlAI.timesStared));
        internal static FieldInfo couldNotStare = AccessTools.Field(typeof(DressGirlAI), nameof(DressGirlAI.couldNotStareLastAttempt));
        internal static FieldInfo hauntingPlayer = AccessTools.Field(typeof(DressGirlAI), nameof(DressGirlAI.hauntingPlayer));
        internal static FieldInfo maskHeldBy = AccessTools.Field(typeof(HauntedMaskItem), nameof(HauntedMaskItem.previousPlayerHeldBy));
        internal static FieldInfo specialAnim = AccessTools.Field(typeof(EnemyAI), nameof(EnemyAI.inSpecialAnimationWithPlayer));
        internal static FieldInfo playerSinking = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.sinkingValue));
        internal static FieldInfo playerHealth = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.health));
        internal static FieldInfo giantPlayerStealth = AccessTools.Field(typeof(ForestGiantAI), nameof(ForestGiantAI.playerStealthMeters));
        private static readonly ReadOnlyDictionary<OpCode, int> stackChange = new ReadOnlyDictionary<OpCode, int>(new Dictionary<OpCode, int>()
        {
            [OpCodes.Ldloc] = 1,
            [OpCodes.Ldloc_0] = 1,
            [OpCodes.Ldloc_1] = 1,
            [OpCodes.Ldloc_2] = 1,
            [OpCodes.Ldloc_3] = 1,
            [OpCodes.Ldloc_S] = 1,
            [OpCodes.Ldloca] = 1,
            [OpCodes.Ldloca_S] = 1,
            [OpCodes.Ldarg] = 1,
            [OpCodes.Ldarg_0] = 1,
            [OpCodes.Ldarg_1] = 1,
            [OpCodes.Ldarg_2] = 1,
            [OpCodes.Ldarg_3] = 1,
            [OpCodes.Ldarg_S] = 1,
            [OpCodes.Ldarga] = 1,
            [OpCodes.Ldarga_S] = 1,
            [OpCodes.Ldnull] = 1,
            [OpCodes.Ldobj] = 0,
            [OpCodes.Ldfld] = 0,
            [OpCodes.Ldsfld] = 1,
            [OpCodes.Ldflda] = 0,
            [OpCodes.Ldsflda] = 1,
            [OpCodes.Ldstr] = 1,
            [OpCodes.Ldtoken] = 1,
            [OpCodes.Ldftn] = 1,
            [OpCodes.Ldvirtftn] = 1,
            [OpCodes.Ldc_I4] = 1,
            [OpCodes.Ldc_I4_0] = 1,
            [OpCodes.Ldc_I4_1] = 1,
            [OpCodes.Ldc_I4_2] = 1,
            [OpCodes.Ldc_I4_3] = 1,
            [OpCodes.Ldc_I4_4] = 1,
            [OpCodes.Ldc_I4_5] = 1,
            [OpCodes.Ldc_I4_6] = 1,
            [OpCodes.Ldc_I4_7] = 1,
            [OpCodes.Ldc_I4_8] = 1,
            [OpCodes.Ldc_I4_M1] = 1,
            [OpCodes.Ldc_I4_S] = 1,
            [OpCodes.Ldc_I8] = 1,
            [OpCodes.Ldc_R4] = 1,
            [OpCodes.Ldc_R8] = 1,
            [OpCodes.Ldelem] = -1,
            [OpCodes.Ldelem_I] = -1,
            [OpCodes.Ldelem_I1] = -1,
            [OpCodes.Ldelem_I2] = -1,
            [OpCodes.Ldelem_I4] = -1,
            [OpCodes.Ldelem_I8] = -1,
            [OpCodes.Ldelem_R4] = -1,
            [OpCodes.Ldelem_R8] = -1,
            [OpCodes.Ldelem_U1] = -1,
            [OpCodes.Ldelem_U2] = -1,
            [OpCodes.Ldelem_U4] = -1,
            [OpCodes.Ldelem_Ref] = -1,
            [OpCodes.Ldelema] = -1,
            [OpCodes.Ldlen] = 0,
            [OpCodes.Ldind_I] = 1,
            [OpCodes.Ldind_I1] = 1,
            [OpCodes.Ldind_I2] = 1,
            [OpCodes.Ldind_I4] = 1,
            [OpCodes.Ldind_I8] = 1,
            [OpCodes.Ldind_R4] = 1,
            [OpCodes.Ldind_R8] = 1,
            [OpCodes.Ldind_Ref] = 1,
            [OpCodes.Ldind_U1] = 1,
            [OpCodes.Ldind_U2] = 1,
            [OpCodes.Ldind_U4] = 1,
            [OpCodes.Localloc] = 1,
            [OpCodes.Stloc] = -1,
            [OpCodes.Stloc_0] = -1,
            [OpCodes.Stloc_1] = -1,
            [OpCodes.Stloc_2] = -1,
            [OpCodes.Stloc_3] = -1,
            [OpCodes.Stloc_S] = -1,
            [OpCodes.Starg] = -1,
            [OpCodes.Starg_S] = -1,
            [OpCodes.Stfld] = -2,
            [OpCodes.Stsfld] = -1,
            [OpCodes.Stobj] = -2,
            [OpCodes.Stelem] = -3,
            [OpCodes.Stelem_I] = -3,
            [OpCodes.Stelem_I1] = -3,
            [OpCodes.Stelem_I2] = -3,
            [OpCodes.Stelem_I4] = -3,
            [OpCodes.Stelem_I8] = -3,
            [OpCodes.Stelem_R4] = -3,
            [OpCodes.Stelem_R8] = -3,
            [OpCodes.Stelem_Ref] = -2,
            [OpCodes.Stind_I] = -2,
            [OpCodes.Stind_I1] = -2,
            [OpCodes.Stind_I2] = -2,
            [OpCodes.Stind_I4] = -2,
            [OpCodes.Stind_I8] = -2,
            [OpCodes.Stind_R4] = -2,
            [OpCodes.Stind_Ref] = -2,
            [OpCodes.Initobj] = -1,
            [OpCodes.Newobj] = 1,
            [OpCodes.Initblk] = -1,
            [OpCodes.Newarr] = 1,
            [OpCodes.Add] = -1,
            [OpCodes.Add_Ovf] = -1,
            [OpCodes.Add_Ovf_Un] = -1,
            [OpCodes.Sub] = -1,
            [OpCodes.Sub_Ovf] = -1,
            [OpCodes.Sub_Ovf_Un] = -1,
            [OpCodes.Mul] = -1,
            [OpCodes.Mul_Ovf] = -1,
            [OpCodes.Mul_Ovf_Un] = -1,
            [OpCodes.Div] = -1,
            [OpCodes.Div_Un] = -1,
            [OpCodes.Rem] = -1,
            [OpCodes.Rem_Un] = -1,
            [OpCodes.Neg] = 0,
            [OpCodes.And] = -1,
            [OpCodes.Or] = 0,
            [OpCodes.Not] = 0,
            [OpCodes.Xor] = -1,
            [OpCodes.Ceq] = -1,
            [OpCodes.Clt] = -1,
            [OpCodes.Clt_Un] = -1,
            [OpCodes.Cgt] = -1,
            [OpCodes.Cgt_Un] = -1,
            [OpCodes.Br] = 0,
            [OpCodes.Br_S] = 0,
            [OpCodes.Brtrue] = -1,
            [OpCodes.Brtrue_S] = -1,
            [OpCodes.Brfalse] = -1,
            [OpCodes.Brfalse_S] = -1,
            [OpCodes.Beq] = -2,
            [OpCodes.Beq_S] = -2,
            [OpCodes.Blt] = -2,
            [OpCodes.Blt_S] = -2,
            [OpCodes.Ble] = -2,
            [OpCodes.Ble_S] = -2,
            [OpCodes.Bge] = -2,
            [OpCodes.Bge_S] = -2,
            [OpCodes.Bgt] = -2,
            [OpCodes.Bgt_S] = -2,
            [OpCodes.Bne_Un] = -2,
            [OpCodes.Bne_Un_S] = -2,
            [OpCodes.Blt_Un] = -2,
            [OpCodes.Blt_Un_S] = -2,
            [OpCodes.Ble_Un] = -2,
            [OpCodes.Ble_Un_S] = -2,
            [OpCodes.Bge_Un] = -2,
            [OpCodes.Bge_Un_S] = -2,
            [OpCodes.Bgt_Un] = -2,
            [OpCodes.Bgt_Un_S] = -2,
            [OpCodes.Switch] = -1,
            [OpCodes.Jmp] = 0,
            [OpCodes.Ret] = 0,
            [OpCodes.Leave] = 0,
            [OpCodes.Leave_S] = 0,
            [OpCodes.Refanytype] = 0,
            [OpCodes.Refanyval] = 0,
            [OpCodes.Mkrefany] = 0,
            [OpCodes.Castclass] = 0,
            [OpCodes.Box] = 0,
            [OpCodes.Unbox] = 0,
            [OpCodes.Unbox_Any] = 0,
            [OpCodes.Conv_I] = 0,
            [OpCodes.Conv_I1] = 0,
            [OpCodes.Conv_I2] = 0,
            [OpCodes.Conv_I4] = 0,
            [OpCodes.Conv_I8] = 0,
            [OpCodes.Conv_Ovf_I] = 0,
            [OpCodes.Conv_Ovf_I1] = 0,
            [OpCodes.Conv_Ovf_I2] = 0,
            [OpCodes.Conv_Ovf_I4] = 0,
            [OpCodes.Conv_Ovf_I8] = 0,
            [OpCodes.Conv_Ovf_I_Un] = 0,
            [OpCodes.Conv_Ovf_I1_Un] = 0,
            [OpCodes.Conv_Ovf_I2_Un] = 0,
            [OpCodes.Conv_Ovf_I4_Un] = 0,
            [OpCodes.Conv_Ovf_I8_Un] = 0,
            [OpCodes.Conv_U] = 0,
            [OpCodes.Conv_U1] = 0,
            [OpCodes.Conv_U2] = 0,
            [OpCodes.Conv_U4] = 0,
            [OpCodes.Conv_U8] = 0,
            [OpCodes.Conv_Ovf_U] = 0,
            [OpCodes.Conv_Ovf_U1] = 0,
            [OpCodes.Conv_Ovf_U2] = 0,
            [OpCodes.Conv_Ovf_U4] = 0,
            [OpCodes.Conv_Ovf_U8] = 0,
            [OpCodes.Conv_Ovf_U_Un] = 0,
            [OpCodes.Conv_Ovf_U1_Un] = 0,
            [OpCodes.Conv_Ovf_U2_Un] = 0,
            [OpCodes.Conv_Ovf_U4_Un] = 0,
            [OpCodes.Conv_Ovf_U8_Un] = 0,
            [OpCodes.Conv_R4] = 0,
            [OpCodes.Conv_R8] = 0,
            [OpCodes.Conv_R_Un] = 0,
            [OpCodes.Cpblk] = -2,
            [OpCodes.Cpobj] = -2,
            [OpCodes.Dup] = 1,
            [OpCodes.Nop] = 0,
            [OpCodes.Pop] = -1,
            [OpCodes.Shl] = -1,
            [OpCodes.Shr] = -1,
            [OpCodes.Shr_Un] = -1,
            [OpCodes.Sizeof] = 1,
            [OpCodes.Isinst] = 0,
            [OpCodes.Break] = 0,
            [OpCodes.Arglist] = 1,
            [OpCodes.Throw] = -1,
            [OpCodes.Rethrow] = 0,
            [OpCodes.Ckfinite] = -1,
            [OpCodes.Endfinally] = 0,
            [OpCodes.Endfilter] = 0
        });
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogString(string toLog)
        {
            WildCardMod.Instance.Log.LogDebug(toLog);
        }
        public static void LogIL(this MethodBase patch, MethodBase patching, List<CodeInstruction> codes)
        {
            if (patch.GetCustomAttribute<HarmonyTranspiler>() == null)
            {
                return;
            }
            Dictionary<int, string> args = new Dictionary<int, string>()
            {
                [0] = $"({patching.DeclaringType.FullName})"
            };
            ParameterInfo[] parameters = patching.GetParameters();
            List<string> argStrings = new List<string>();
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                argStrings.Add($"{parameter.ParameterType.FullName} {parameter.Name}");
                args.Add(i + 1, $"({argStrings[^1]})");
            }
            StringBuilder builder = new StringBuilder("IL after patching ");
            builder.Append(patching.DeclaringType.FullName).Append('.').Append(patching.Name).Append('(').AppendJoin(", ", argStrings).Append(')').Append(" with ").Append(patch.DeclaringType.FullName).Append('.').Append(patch.Name).AppendLine("\n");
            Dictionary<int, string> locs = new Dictionary<int, string>();
            LocalVariableInfo[] baseLocals = patching.GetMethodBody().LocalVariables.ToArray();
            for (int i = 0; i < baseLocals.Length; i++)
            {
                LocalVariableInfo newLocal = baseLocals[i];
                locs.Add(newLocal.LocalIndex, $"({newLocal.LocalType.FullName})");
            }
            Dictionary<Label, Vector2Int> labelPairs = new Dictionary<Label, Vector2Int>();
            Dictionary<int, Label[]> branchOuts = new Dictionary<int, Label[]>();
            Dictionary<int, Label[]> branchIns = new Dictionary<int, Label[]>();
            List<int> io = new List<int>();
            int step = 0;
            bool breakOut = false;
            int codeCount = codes.Count;
            for (int i = 0; i < codeCount; i++)
            {
                bool lastCode = i == codeCount - 1;
                CodeInstruction code = codes[i];
                OpCode opcode = code.opcode;
                object operand = code.operand;
                Label[] labels = code.labels.ToArray();
                switch (step)
                {
                    case 0:
                        {
                            int value = 0;
                            if (opcode == OpCodes.Call || opcode == OpCodes.Callvirt)
                            {
                                MethodInfo method = code.operand as MethodInfo;
                                value = -method.GetParameters().Length;
                                if (!method.IsStatic)
                                {
                                    value--;
                                }
                                if (method.ReturnType != typeof(void))
                                {
                                    value++;
                                }
                            }
                            else if (stackChange.ContainsKey(opcode))
                            {
                                value = stackChange[opcode];
                            }
                            else
                            {
                                builder.Append("Error: Tried to add stack change value for OpCode: ").Append(opcode).Append(", but there was no saved value, setting change to 0!").AppendLine("\n");
                            }
                            io.Add(value);
                            Label[] newLabels = null;
                            if (code.Branches(out Label? newLabel))
                            {
                                newLabels = new Label[] { newLabel.Value };
                            }
                            else if (opcode == OpCodes.Switch)
                            {
                                newLabels = code.operand as Label[];
                            }
                            if (newLabels != null)
                            {
                                branchOuts.Add(i, newLabels);
                                for (int j = 0; j < newLabels.Length; j++)
                                {
                                    newLabel = newLabels[j];
                                    if (labelPairs.TryAdd(newLabel.Value, new Vector2Int(i, -1)))
                                    {
                                        continue;
                                    }
                                    labelPairs[newLabel.Value] = new Vector2Int(i, labelPairs[newLabel.Value].y);
                                }
                            }
                            int labelCount = code.labels.Count;
                            if (labelCount <= 0)
                            {
                                break;
                            }
                            branchIns.Add(i, labels);
                            for (int j = 0; j < labelCount; j++)
                            {
                                Label label = labels[j];
                                if (labelPairs.TryAdd(label, new Vector2Int(-1, i)))
                                {
                                    continue;
                                }
                                labelPairs[label] = new Vector2Int(labelPairs[label].x, i);
                            }
                            break;
                        }
                    case 1:
                        {
                            StringBuilder lineBuilder = new StringBuilder();
                            bool hasEmptyBefore = builder[^1] == '\n' && builder[^2] == '\r' && builder[^3] == '\n';
                            lineBuilder.Append(i).Append(" (").Append(io[i]).Append(')').Append(": ").Append(code.ToString()).Replace("::", ".").Replace(" NULL", string.Empty);
                            if (code.IsLdarg() || code.IsLdarga() || code.IsStarg())
                            {
                                if (!(operand is int argument))
                                {
                                    argument = -1;
                                    if (opcode == OpCodes.Ldarg_0)
                                    {
                                        argument = 0;
                                    }
                                    else if (opcode == OpCodes.Ldarg_1)
                                    {
                                        argument = 1;
                                    }
                                    else if (opcode == OpCodes.Ldarg_2)
                                    {
                                        argument = 2;
                                    }
                                    else if (opcode == OpCodes.Ldarg_3)
                                    {
                                        argument = 3;
                                    }
                                }
                                if (argument >= 0)
                                {
                                    lineBuilder.Append(' ').Append(args[argument]);
                                }
                            }
                            else if (code.IsLdloc() || code.IsStloc())
                            {
                                if (!(operand is int local))
                                {
                                    local = -1;
                                    if (code.IsLdloc())
                                    {
                                        if (opcode == OpCodes.Ldloc_0)
                                        {
                                            local = 0;
                                        }
                                        else if (opcode == OpCodes.Ldloc_1)
                                        {
                                            local = 1;
                                        }
                                        else if (opcode == OpCodes.Ldloc_2)
                                        {
                                            local = 2;
                                        }
                                        else if (opcode == OpCodes.Ldloc_3)
                                        {
                                            local = 3;
                                        }
                                    }
                                    else if (code.IsStloc())
                                    {
                                        if (opcode == OpCodes.Stloc_0)
                                        {
                                            local = 0;
                                        }
                                        else if (opcode == OpCodes.Stloc_1)
                                        {
                                            local = 1;
                                        }
                                        else if (opcode == OpCodes.Stloc_2)
                                        {
                                            local = 2;
                                        }
                                        else if (opcode == OpCodes.Stloc_3)
                                        {
                                            local = 3;
                                        }
                                    }
                                }
                                if (local >= 0)
                                {
                                    lineBuilder.Append(' ').Append(locs[local]);
                                }
                            }
                            else if (code.Branches(out Label? checking))
                            {
                                lineBuilder.Append(" (Jumps to: ");
                                if (labelPairs.TryGetValue(checking.Value, out Vector2Int pairVector))
                                {
                                    lineBuilder.Append(pairVector.y);
                                }
                                else
                                {
                                    lineBuilder.Append("NOT FOUND");
                                }
                                lineBuilder.AppendLine(")");
                            }
                            else if (opcode == OpCodes.Switch && operand is Label[] gotLabels && labels.Length > 0)
                            {
                                lineBuilder.Append(" (Jumps to: ");
                                List<object> dests = new List<object>();
                                for (int j = 0; j < gotLabels.Length; j++)
                                {
                                    if (!labelPairs.TryGetValue(gotLabels[j], out Vector2Int pairVector))
                                    {
                                        dests.Add("NOT FOUND");
                                        continue;
                                    }
                                    dests.Add(pairVector.y);
                                }
                                lineBuilder.AppendJoin(", ", dests).AppendLine(")");
                            }
                            else if (!lastCode && opcode.Leaves())
                            {
                                lineBuilder.Append('\n');
                            }
                            else if (opcode == OpCodes.Break)
                            {
                                if (!hasEmptyBefore)
                                {
                                    lineBuilder.Insert(0, '\n');
                                    hasEmptyBefore = true;
                                }
                                lineBuilder.Append('\n');
                            }
                            int labelCount = code.labels.Count;
                            if (labelCount > 0)
                            {
                                if (!hasEmptyBefore)
                                {
                                    lineBuilder.Insert(0, '\n');
                                }
                                lineBuilder.Append(" (Receives jump");
                                if (labelCount > 1)
                                {
                                    lineBuilder.Append('s');
                                }
                                lineBuilder.Append(" from: ");
                                List<object> startLocs = new List<object>();
                                for (int j = 0; j < labelCount; j++)
                                {
                                    if (!labelPairs.TryGetValue(code.labels[j], out Vector2Int pairVector))
                                    {
                                        startLocs.Add("NOT FOUND");
                                        continue;
                                    }
                                    startLocs.Add(pairVector.x);
                                }
                                lineBuilder.AppendJoin(", ", startLocs).Append(')');
                            }
                            builder.AppendLine(lineBuilder.ToString());
                            break;
                        }
                    default:
                        {
                            breakOut = true;
                            break;
                        }
                }
                if (breakOut)
                {
                    break;
                }
                if (!lastCode)
                {
                    continue;
                }
                i = -1;
                step++;
            }
            builder.Append('\n').AppendLine("----------------------------------------------------------------");
            LogString(builder.ToString());
        }
        public static List<CodeInstruction> DebugString(string toLog, params Label[] labelsStart)
        {
            return new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldstr, toLog).WithLabels(labelsStart),
                new CodeInstruction(OpCodes.Call, logString)
            };
        }
        public static List<CodeInstruction> DebugLoad<T>(string toLog, OpCode opcode, object operand = null, params Label[] labelsStart)
        {
            return new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldstr, toLog).WithLabels(labelsStart),
                new CodeInstruction(OpCodes.Ldstr, ": "),
                new CodeInstruction(opcode, operand),
                new CodeInstruction(OpCodes.Box, typeof(T)),
                new CodeInstruction(OpCodes.Callvirt, toString),
                new CodeInstruction(OpCodes.Call, stringConcat3),
                new CodeInstruction(OpCodes.Call, logString)
            };
        }
        public static List<CodeInstruction> DebugLoad<T>(string toLog, IEnumerable<CodeInstruction> loadInstructions, params Label[] labelsStart)
        {
            List<CodeInstruction> instructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldstr, toLog).WithLabels(labelsStart),
                new CodeInstruction(OpCodes.Ldstr, ": "),
                new CodeInstruction(OpCodes.Box, typeof(T)),
                new CodeInstruction(OpCodes.Callvirt, toString),
                new CodeInstruction(OpCodes.Call, stringConcat3),
                new CodeInstruction(OpCodes.Call, logString)
            };
            instructions.InsertRange(2, loadInstructions);
            return instructions;
        }
        public static List<CodeInstruction> DebugLoadFromThis<T>(string toLog, IEnumerable<CodeInstruction> loadInstructions, params Label[] labelsStart)
        {
            List<CodeInstruction> instructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldstr, toLog).WithLabels(labelsStart),
                new CodeInstruction(OpCodes.Ldstr, ": "),
                new CodeInstruction(OpCodes.Ldarg, 0),
                new CodeInstruction(OpCodes.Box, typeof(T)),
                new CodeInstruction(OpCodes.Callvirt, toString),
                new CodeInstruction(OpCodes.Call, stringConcat3),
                new CodeInstruction(OpCodes.Call, logString)
            };
            instructions.InsertRange(3, loadInstructions);
            return instructions;
        }
        public static List<CodeInstruction> DebugLoadFromThis<T>(string toLog, OpCode opcode, object operand = null, params Label[] labelsStart)
        {
            return new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldstr, toLog).WithLabels(labelsStart),
                new CodeInstruction(OpCodes.Ldstr, ": "),
                new CodeInstruction(OpCodes.Ldarg_S, 0),
                new CodeInstruction(opcode, operand),
                new CodeInstruction(OpCodes.Box, typeof(T)),
                new CodeInstruction(OpCodes.Callvirt, toString),
                new CodeInstruction(OpCodes.Call, stringConcat3),
                new CodeInstruction(OpCodes.Call, logString)
            };
        }
        public static List<CodeInstruction> DebugThisEnemyName(params Label[] labelsStart)
        {
            return DebugEnemyName(OpCodes.Ldarg_S, 0, labelsStart);
        }
        public static List<CodeInstruction> DebugEnemyName(OpCode loadEnemyOpcode, object loadEnemyOperand = null, params Label[] labelsStart)
        {
            return DebugLoad<string>("Enemy", new List<CodeInstruction>() { new CodeInstruction(loadEnemyOpcode, loadEnemyOperand), new CodeInstruction(OpCodes.Ldfld, enemyType), new CodeInstruction(OpCodes.Ldfld, enemyName) }, labelsStart);
        }
        public static List<CodeInstruction> DebugEnemyName(IEnumerable<CodeInstruction> loadEnemyInstructions, params Label[] labelsStart)
        {
            return DebugLoad<string>("Enemy", new List<CodeInstruction>(loadEnemyInstructions) { new CodeInstruction(OpCodes.Ldfld, enemyType), new CodeInstruction(OpCodes.Ldfld, enemyName) }, labelsStart);
        }
        public static List<CodeInstruction> DebugThisPlayerName(params Label[] labelsStart)
        {
            return DebugPlayerName(OpCodes.Ldarg_S, 0, labelsStart);
        }
        public static List<CodeInstruction> DebugPlayerName(OpCode loadPlayerOpcode, object loadPlayerOperand = null, params Label[] labelsStart)
        {
            return DebugLoad<string>("Player", new List<CodeInstruction>() { new CodeInstruction(loadPlayerOpcode, loadPlayerOperand), new CodeInstruction(OpCodes.Ldfld, playerName) }, labelsStart);
        }
        public static List<CodeInstruction> DebugPlayerName(IEnumerable<CodeInstruction> loadPlayerInstructions, params Label[] labelsStart)
        {
            return DebugLoad<string>("Player", new List<CodeInstruction>(loadPlayerInstructions) { new CodeInstruction(OpCodes.Ldfld, playerName) }, labelsStart);
        }
        public static CodeInstruction StoreToLoad(CodeInstruction store)
        {
            object operand = store.operand;
            CodeInstruction load = new CodeInstruction(OpCodes.Nop);
            if (store.opcode == OpCodes.Stloc_0)
            {
                load = new CodeInstruction(OpCodes.Ldloc_S, 0);
            }
            else if (store.opcode == OpCodes.Stloc_1)
            {
                load = new CodeInstruction(OpCodes.Ldloc_S, 1);
            }
            else if (store.opcode == OpCodes.Stloc_2)
            {
                load = new CodeInstruction(OpCodes.Ldloc_S, 2);
            }
            else if (store.opcode == OpCodes.Stloc_3)
            {
                load = new CodeInstruction(OpCodes.Ldloc_S, 3);
            }
            else if (store.opcode == OpCodes.Stloc_S || store.opcode == OpCodes.Ldloc)
            {
                load = new CodeInstruction(OpCodes.Ldloc_S, operand);
            }
            else if (store.opcode == OpCodes.Starg_S || store.opcode == OpCodes.Starg)
            {
                load = new CodeInstruction(OpCodes.Ldarg_S, operand);
            }
            else if (store.opcode == OpCodes.Stfld || store.opcode == OpCodes.Stsfld)
            {
                load = new CodeInstruction(OpCodes.Ldfld, operand);
            }
            else if (store.opcode == OpCodes.Stobj)
            {
                load = new CodeInstruction(OpCodes.Ldobj, operand);
            }
            return load;
        }
        public static CodeInstruction LoadToStore(CodeInstruction load)
        {
            object operand = load.operand;
            CodeInstruction store = new CodeInstruction(OpCodes.Nop);
            if (load.opcode == OpCodes.Ldloc_0)
            {
                store = new CodeInstruction(OpCodes.Stloc_S, 0);
            }
            else if (load.opcode == OpCodes.Ldloc_1)
            {
                store = new CodeInstruction(OpCodes.Stloc_S, 1);
            }
            else if (load.opcode == OpCodes.Ldloc_2)
            {
                store = new CodeInstruction(OpCodes.Stloc_S, 2);
            }
            else if (load.opcode == OpCodes.Ldloc_3)
            {
                store = new CodeInstruction(OpCodes.Stloc_S, 3);
            }
            else if (load.opcode == OpCodes.Ldloc_S || load.opcode == OpCodes.Ldloc)
            {
                store = new CodeInstruction(OpCodes.Stloc_S, operand);
            }
            else if (load.opcode == OpCodes.Ldarg_0)
            {
                store = new CodeInstruction(OpCodes.Starg_S, 0);
            }
            else if (load.opcode == OpCodes.Ldarg_1)
            {
                store = new CodeInstruction(OpCodes.Starg_S, 1);
            }
            else if (load.opcode == OpCodes.Ldarg_2)
            {
                store = new CodeInstruction(OpCodes.Starg_S, 2);
            }
            else if (load.opcode == OpCodes.Ldarg_3)
            {
                store = new CodeInstruction(OpCodes.Starg_S, 3);
            }
            else if (load.opcode == OpCodes.Ldarg_S || load.opcode == OpCodes.Ldarg)
            {
                store = new CodeInstruction(OpCodes.Starg_S, operand);
            }
            else if (load.opcode == OpCodes.Ldfld)
            {
                FieldInfo field = operand as FieldInfo;
                if (field != null)
                {
                    if (field.IsStatic)
                    {
                        store = new CodeInstruction(OpCodes.Stsfld, operand);
                    }
                    else
                    {
                        store = new CodeInstruction(OpCodes.Stfld, operand);
                    }
                }
            }
            else if (load.opcode == OpCodes.Ldobj)
            {
                store = new CodeInstruction(OpCodes.Stobj, operand);
            }
            return store;
        }
        public static bool AreLoadEqual(CodeInstruction load1, CodeInstruction load2)
        {
            int? load1Operand = load1.operand as int?;
            int? load2Operand = load2.operand as int?;
            OpCode opCode1 = load1.opcode;
            OpCode opCode2 = load2.opcode;
            if (load1.IsLdloc() && load2.IsLdloc())
            {
                if (opCode1 == opCode2 && load1.OperandIs(load2.operand))
                {
                    return true;
                }
                else if (opCode1 == OpCodes.Ldloc || opCode1 == OpCodes.Ldloc_S)
                {
                    return load1.OperandIs(load2.operand);
                }
                else if (opCode1 == OpCodes.Ldloc_0)
                {
                    return opCode2 == OpCodes.Ldloc_0 || (load2Operand.HasValue && load2Operand.Value == 0);
                }
                else if (opCode1 == OpCodes.Ldloc_1)
                {
                    return opCode2 == OpCodes.Ldloc_1 || (load2Operand.HasValue && load2Operand.Value == 1);
                }
                else if (opCode1 == OpCodes.Ldloc_2)
                {
                    return opCode2 == OpCodes.Ldloc_2 || (load2Operand.HasValue && load2Operand.Value == 2);
                }
                else if (opCode1 == OpCodes.Ldloc_3)
                {
                    return opCode2 == OpCodes.Ldloc_3 || (load2Operand.HasValue && load2Operand.Value == 3);
                }
                else if ((opCode1 == OpCodes.Ldloca || opCode1 == OpCodes.Ldloca_S) && (opCode2 == OpCodes.Ldloca || opCode2 == OpCodes.Ldloca_S))
                {
                    return load1.OperandIs(load2.operand);
                }
            }
            else if (load1.IsLdarg() && load2.IsLdarg())
            {
                return (load2Operand.HasValue && load1.IsLdarg(load2Operand.Value)) || (load1Operand.HasValue && load2.IsLdarg(load1Operand.Value));
            }
            else if (load1.IsLdarga() && load2.IsLdarga())
            {
                return load1.OperandIs(load2.operand);
            }
            return opCode1 == opCode2 && load1.OperandIs(load2.operand);
        }
        public static bool Leaves(this CodeInstruction code)
        {
            return code.opcode.Leaves();
        }
        public static bool Leaves(this OpCode opcode)
        {
            return opcode == OpCodes.Ret || opcode == OpCodes.Jmp || opcode == OpCodes.Leave || opcode == OpCodes.Leave_S || opcode == OpCodes.Throw || opcode == OpCodes.Rethrow || opcode == OpCodes.Ckfinite || opcode == OpCodes.Endfinally || opcode == OpCodes.Endfilter;
        }
        internal static (Harmony, Type[]) GetHarmony(string id, params Type[] toPatch)
        {
            Dictionary<string, (Harmony, Type[])> harmonies = WildCardMod.Harmonies;
            if (!WildCardMod.Harmonies.TryGetValue(id, out (Harmony, Type[]) newHarmony))
            {
                newHarmony = (new Harmony(id), toPatch);
                harmonies.Add(id, newHarmony);
            }
            return newHarmony;
        }
        internal static void TogglePatches(string harmonyID, bool patchIf, params Type[] types)
        {
            (Harmony, Type[]) harmonyTuple = GetHarmony(harmonyID, types);
            Harmony harmony = harmonyTuple.Item1;
            types = harmonyTuple.Item2;
            bool hasPatches = Harmony.HasAnyPatches(harmonyID);
            if (patchIf)
            {
                if (hasPatches || types == null)
                {
                    return;
                }
                for (int i = 0; i < types.Length; i++)
                {
                    Type type = types[i];
                    if (WildCardMod.ModConfig.Debug)
                    {
                        WildCardMod.Instance.Log.LogDebug($"Patching methods of class \"{type}\" with Harmony ID \"{harmonyID}\"");
                    }
                    harmony.PatchAll(type);
                }
                return;
            }
            if (!hasPatches)
            {
                return;
            }
            if (WildCardMod.ModConfig.Debug)
            {
                WildCardMod.Instance.Log.LogDebug($"Unpatching methods with Harmony ID \"{harmonyID}\"");
            }
            harmony.UnpatchSelf();
        }
        internal static void ILCodeCheck()
        {
            
        }
    }
    public interface IWildCardBase
    {
        internal static int totalBases = 0;
        public static void Awake(IWildCardBase instance)
        {
            totalBases++;
            if (instance.Animator != null)
            {
                instance.Animator.Base = instance;
            }
            for (int i = 0; i < instance.Audio.Count; i++)
            {
                SelectAudioClips audio = instance.Audio[i];
                audio.SetBase(instance);
                if (audio.HUDAudio)
                {
                    audio.HUDOverride();
                    continue;
                }
                audio.SetMixer(WildUtils.DiageticMasterGroup);
            }
            for (int i = 0; i < instance.Animations.Count; i++)
            {
                SelectAnimationParameters anim = instance.Animations[i];
                anim.SetBase(instance);
                anim.SetAnimator(instance.Animator);
                anim.SetRandom(instance.Random);
                if (!anim.playOnSpawn)
                {
                    continue;
                }
                anim.PlayAll(true);
            }
            for (int i = 0; i < instance.Particles.Count; i++)
            {
                SelectParticles particle = instance.Particles[i];
                particle.SetBase(instance);
                if (particle.playOnAwake)
                {
                    particle.PlayAll();
                }
                if (!particle.applyOnAwake)
                {
                    continue;
                }
                particle.AllApplyAll();
            }
            for (int i = 0; i < instance.MeshRenderers.Count; i++)
            {
                SelectRenderers render = instance.MeshRenderers[i];
                render.SetBase(instance);
                if (!render.applyOnAwake)
                {
                    continue;
                }
                render.AllApplyAll();
            }
            for (int i = 0; i < instance.ModelVariants.Count; i++)
            {
                SelectModelVariants variant = instance.ModelVariants[i];
                variant.SetBase(instance);
            }
            for (int i = 0; i < instance.Lights.Count; i++)
            {
                SelectLights light = instance.Lights[i];
                light.SetBase(instance);
            }
        }
        public static void OnNetworkPostSpawn(IWildCardBase instance, bool skipAudio = false)
        {
            instance.Animator?.SetNetworkEnabled(instance.NetworkAnimations);
            for (int i = 0; i < instance.ModelVariants.Count; i++)
            {
                SelectModelVariants variants = instance.ModelVariants[i];
                if (!variants.randomOnSpawn)
                {
                    continue;
                }
                variants.SwitchRandom();
            }
            for (int i = 0; i < instance.Lights.Count; i++)
            {
                SelectLights lights = instance.Lights[i];
                if (!lights.enableOnSpawn)
                {
                    continue;
                }
                lights.EnableAll();
            }
            if (skipAudio)
            {
                return;
            }
            for (int i = 0; i < instance.Audio.Count; i++)
            {
                SelectAudioClips audioClips = instance.Audio[i];
                if (!audioClips.playOnSpawn)
                {
                    continue;
                }
                audioClips.PlayRandomClip();
            }
        }
        public static void Update(IWildCardBase instance)
        {
            for (int i = 0; i < instance.Audio.Count; i++)
            {
                SelectAudioClips clips = instance.Audio[i];
                if (!clips.Loops)
                {
                    continue;
                }
                clips.LoopTick();
            }
            for (int i = 0; i < instance.Animations.Count; i++)
            {
                SelectAnimationParameters anim = instance.Animations[i];
                if (!anim.clientsTick && !instance.IsServer)
                {
                    continue;
                }
                anim.TickAll();
            }
        }
        public static void Initialize(IWildCardBase instance, ref List<SelectablePair<SelectAudioClips>> audioClips, ref List<SelectablePair<SelectAnimationParameters>> animations, ref List<SelectablePair<SelectParticles>> particles, ref List<SelectablePair<SelectRenderers>> meshRenderers, ref List<SelectablePair<SelectModelVariants>> modelVariants, ref List<SelectablePair<SelectLights>> lights, out ListDict<string, SelectAudioClips> audioDict, out ListDict<string, SelectAnimationParameters> animDict, out ListDict<string, SelectParticles> particleDict, out ListDict<string, SelectRenderers> renderDict, out ListDict<string, SelectModelVariants> variantDict, out ListDict<string, SelectLights> lightDict)
        {
            instance.Animator = AnimationHandler.Create(instance.Transform.GetComponentInChildren<NetworkAnimator>());
            audioDict = new ListDict<string, SelectAudioClips>();
            for (int i = 0; i < audioClips.Count; i++)
            {
                SelectablePair<SelectAudioClips> select = audioClips[i];
                select.selectable.Id = i;
                select.selectable.SetBase(instance);
                audioDict.Add(select.id, select.selectable);
            }
            audioClips = null;
            animDict = new ListDict<string, SelectAnimationParameters>();
            for (int i = 0; i < animations.Count; i++)
            {
                SelectablePair<SelectAnimationParameters> select = animations[i];
                select.selectable.Id = i;
                select.selectable.SetBase(instance);
                animDict.Add(select.id, select.selectable);
            }
            animations = null;
            particleDict = new ListDict<string, SelectParticles>();
            for (int i = 0; i < particles.Count; i++)
            {
                SelectablePair<SelectParticles> select = particles[i];
                select.selectable.Id = i;
                select.selectable.SetBase(instance);
                particleDict.Add(select.id, select.selectable);
            }
            particles = null;
            renderDict = new ListDict<string, SelectRenderers>();
            for (int i = 0; i < meshRenderers.Count; i++)
            {
                SelectablePair<SelectRenderers> select = meshRenderers[i];
                select.selectable.Id = i;
                select.selectable.SetBase(instance);
                renderDict.Add(select.id, select.selectable);
            }
            meshRenderers = null;
            variantDict = new ListDict<string, SelectModelVariants>();
            for (int i = 0; i < modelVariants.Count; i++)
            {
                SelectablePair<SelectModelVariants> select = modelVariants[i];
                select.selectable.Id = i;
                select.selectable.SetBase(instance);
                variantDict.Add(select.id, select.selectable);
            }
            modelVariants = null;
            lightDict = new ListDict<string, SelectLights>();
            for (int i = 0; i < lights.Count; i++)
            {
                SelectablePair<SelectLights> select = lights[i];
                select.selectable.Id = i;
                select.selectable.SetBase(instance);
                lightDict.Add(select.id, select.selectable);
            }
            lights = null;
        }
        public static bool HitOrDamage(IWildCardBase instance, IHittable hittable, int playerDamage, int hitForce, Vector3 hitDirection, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1, CauseOfDeath playerDeathCause = CauseOfDeath.Unknown, float playerForceMultiplier = 1f)
        {
            PlayerControllerB hittingPlayer = hittable as PlayerControllerB;
            if (hittingPlayer != null)
            {
                instance.DamagePlayer(hittingPlayer, playerDamage, playerDeathCause, force: hitDirection * playerForceMultiplier);
                return true;
            }
            return hittable.Hit(hitForce, hitDirection, playerWhoHit, playHitSFX, hitID);
        }
        public string Name { get; set; }
        public bool IsServer => false;
        public bool IsOwner => false;
        public Transform Transform => null;
        public ListDict<string, SelectAudioClips> Audio => null;
        public ListDict<string, SelectAnimationParameters> Animations => null;
        public ListDict<string, SelectParticles> Particles => null;
        public ListDict<string, SelectRenderers> MeshRenderers => null;
        public ListDict<string, SelectModelVariants> ModelVariants => null;
        public ListDict<string, SelectLights> Lights => null;
        public int State { get; set; }
        public AnimationHandler Animator { get; set; }
        public bool NetworkAnimations { get; set; }
        public System.Random Random => null;
        public void Initialize();
        public void PlayClipNetworked(int id, int index, bool oneShot, float volume, float pitch);
        public void RepeatClipNetworked(int id, bool oneShot);
        public void StopAudioNetworked(int id);
        public void PauseAudioNetworked(int id, bool pause);
        public void MuteAudioNetworked(int id, bool oneShot);
        public void DogNoiseNetworked(int id, float volume, float pitch);
        public void PlayParticlesNetworked(int id, bool restart, int index = -1);
        public void StopParticlesNetworked(int id, bool clear, int index = -1);
        public void PauseParticlesNetworked(int id, int index = -1);
        public void ClearParticlesNetworked(int id, int index = -1);
        public void EmitParticlesNetworked(int id, int count, int index = -1);
        public void SetStateNetworked(int newState);
        public void SetNetworkAnimationsNetworked(bool value);
        public void SetParameterNetworked(int hash, float value = 0f);
        public void SetVariantNetworked(int id, int variantIndex);
        public void SetLightEnabledNetworked(int id, bool enable, int index = -1);
        public void DamagePlayer(PlayerControllerB player, int damage, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown, int animation = 0, Vector3 force = default);
        public bool HitOrDamage(IHittable hittable, int playerDamage, int hitForce, Vector3 hitDirection, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1, CauseOfDeath playerDeathCause = CauseOfDeath.Unknown, float playerForceMultiplier = 1f);
    }
    internal interface ILifeSaver
    {
        private static readonly Dictionary<string, List<ILifeSaver>> lifeSavers = new Dictionary<string, List<ILifeSaver>>();
        internal static Dictionary<string, List<ILifeSaver>> AllLifeSavers
        {
            get
            {
                return lifeSavers;
            }
        }
        internal static bool AnyEnabled => AllLifeSavers.Count > 0;
        internal static bool TrySave(PlayerControllerB player, CauseOfDeath cause = CauseOfDeath.Unknown, Vector3 hitVelocity = default, EnemyAI enemy = null)
        {
            ILifeSaver lifeSaver = HasLifeSaver(player);
            if (lifeSaver == null)
            {
                return false;
            }
            if (lifeSaver.CanSave(player, cause))
            {
                if (lifeSaver.TriggerWhen(player, cause))
                {
                    lifeSaver.Save(player, cause, hitVelocity, enemy);
                }
                return true;
            }
            return false;
        }
        internal static bool TrySaveGraceOnly(PlayerControllerB player, CauseOfDeath cause = CauseOfDeath.Unknown, Vector3 hitVelocity = default, EnemyAI enemy = null)
        {
            ILifeSaver lifeSaver = HasLifeSaver(player);
            if (lifeSaver == null)
            {
                return false;
            }
            if (lifeSaver.CanSave(player, cause) && lifeSaver.GraceWhen(player, cause))
            {
                if (lifeSaver.TriggerWhen(player, cause))
                {
                    lifeSaver.Save(player, cause, hitVelocity, enemy);
                }
                return true;
            }
            return false;
        }
        internal static bool TrySaveTriggerOnly(PlayerControllerB player, CauseOfDeath cause = CauseOfDeath.Unknown, Vector3 hitVelocity = default, EnemyAI enemy = null)
        {
            ILifeSaver lifeSaver = HasLifeSaver(player);
            if (lifeSaver == null)
            {
                return false;
            }
            if (lifeSaver.CanSave(player, cause) && lifeSaver.TriggerWhen(player, cause))
            {
                lifeSaver.Save(player, cause, hitVelocity, enemy);
                return true;
            }
            return false;
        }
        internal static ILifeSaver HasLifeSaver(PlayerControllerB player)
        {
            if (player == null)
            {
                return null;
            }
            List<ILifeSaver> potentialLifeSavers = new List<ILifeSaver>();
            string[] availableLifeSavers = AllLifeSavers.Keys.ToArray();
            for (int i = 0; i < availableLifeSavers.Length; i++)
            {
                List<ILifeSaver> lifeSaversList = AllLifeSavers[availableLifeSavers[i]];
                for (int j = 0; j < lifeSaversList.Count; j++)
                {
                    ILifeSaver newLifeSaver = lifeSaversList[j];
                    if (!newLifeSaver.CanSave(player))
                    {
                        continue;
                    }
                    potentialLifeSavers.Add(newLifeSaver);
                }
            }
            potentialLifeSavers.Sort((x, y) => x.Priority.CompareTo(y.Priority));
            return potentialLifeSavers.FirstOrDefault();
        }
        internal static bool IsLifeSaver(GameObject gameObject, out ILifeSaver lifeSaver)
        {
            return IsLifeSaver(gameObject.GetComponentInChildren<WildCardProp>(), out lifeSaver);
        }
        internal static bool IsLifeSaver(object instance, out ILifeSaver lifeSaver)
        {
            if (instance == null)
            {
                lifeSaver = null;
                return false;
            }
            lifeSaver = instance as ILifeSaver;
            return lifeSaver != null;
        }
        internal static bool Register(object instance)
        {
            if (!IsLifeSaver(instance, out ILifeSaver newLifeSaver))
            {
                return false;
            }
            return Register(newLifeSaver);
        }
        internal static bool Register(ILifeSaver newLifeSaver)
        {
            string typeName = newLifeSaver.GetType().Name;
            if (!AllLifeSavers.TryGetValue(typeName, out List<ILifeSaver> lifeSavers))
            {
                return false;
            }
            lifeSavers.Add(newLifeSaver);
            return true;
        }
        internal static void Unregister(object instance)
        {
            if (!IsLifeSaver(instance, out ILifeSaver oldLifeSaver))
            {
                return;
            }
            Unregister(oldLifeSaver);
        }
        internal static void Unregister(ILifeSaver oldLifeSaver)
        {
            string typeName = oldLifeSaver.GetType().Name;
            if (!AllLifeSavers.TryGetValue(typeName, out List<ILifeSaver> lifeSavers))
            {
                return;
            }
            lifeSavers.Remove(oldLifeSaver);
        }
        internal int Priority => 0;
        internal bool UntargetableWhen(PlayerControllerB player);
        internal bool GraceWhen(PlayerControllerB player, CauseOfDeath cause = CauseOfDeath.Unknown);
        internal bool TriggerWhen(PlayerControllerB player, CauseOfDeath cause = CauseOfDeath.Unknown);
        internal bool CanSave(PlayerControllerB player, CauseOfDeath cause = CauseOfDeath.Unknown);
        internal void Save(PlayerControllerB player, CauseOfDeath cause = CauseOfDeath.Unknown, Vector3 hitVelocity = default, EnemyAI enemy = null);
    }
    public static class EventsClass
    {
        public delegate void RoundStart();
        public static event RoundStart OnRoundStart;
        public delegate void RoundEnd();
        public static event RoundEnd OnRoundEnd;
        public static bool RoundStarted { get; private set; }
        internal static void RoundStartInvoke()
        {
            if (RoundStarted)
            {
                return;
            }
            RoundStarted = true;
            OnRoundStart?.Invoke();
        }
        internal static void RoundEndInvoke()
        {
            if (!RoundStarted)
            {
                return;
            }
            RoundStarted = false;
            OnRoundEnd?.Invoke();
        }
    }
    public static class WildUtils
    {
        private static AudioMixerGroup diageticMaster = default;
        public static readonly Dictionary<int, float> playerSpeedMultipliers = new Dictionary<int, float>();
        public static ListDict<string, EnemyType> AllEnemies { get; internal set; }
        public static ReadOnlyDictionary<string, string> TrueEnemyNames { get; internal set; }
        public static AudioMixerGroup DiageticMasterGroup
        {
            get
            {
                if (diageticMaster == null && SoundManager.Instance != null)
                {
                    diageticMaster = SoundManager.Instance.diageticMixer.FindMatchingGroups("Master")[0];
                }
                return diageticMaster;
            }
        }
        public static bool IsSubclassOfRawGeneric(this Type toCheck, Type generic)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                Type current = toCheck;
                if (toCheck.IsGenericType)
                {
                    current = toCheck.GetGenericTypeDefinition();
                }
                if (generic == current)
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }
        public static bool IsLocal(this PlayerControllerB player)
        {
            if (player == null)
            {
                return false;
            }
            return player.IsOwner && (!player.IsOwnedByServer || player.isHostPlayerObject);
        }
        public static StringBuilder GetAllFieldValues(this object instance, StringBuilder builder = null, int nestCount = 0)
        {
            nestCount++;
            builder ??= new StringBuilder();
            builder.AppendLine("\n");
            for (int i = 0; i < nestCount - 1; i++)
            {
                builder.Append('\t');
            }
            builder.Append($"{instance} values ");
            for (int i = 0; i < nestCount + 1; i++)
            {
                builder.Append('-');
            }
            builder.Append('\n');
            Type instanceType = instance.GetType();
            List<FieldInfo> fields = AccessTools.GetDeclaredFields(instanceType);
            Type checkingType = instanceType.BaseType;
            while (checkingType != null && (typeof(GrabbableObject).IsAssignableFrom(checkingType) || checkingType.IsSubclassOfRawGeneric(typeof(Selectable<>))))
            {
                fields.AddRange(AccessTools.GetDeclaredFields(checkingType));
                checkingType = checkingType.BaseType;
            }
            for (int i = 0; i < fields.Count; i++)
            {
                FieldInfo field = fields[i];
                object value = field.GetValue(instance);
                Type fieldType = field.FieldType;
                string fieldTypeName = fieldType.ToString();
                for (int j = 0; j < nestCount; j++)
                {
                    builder.Append('\t');
                }
                builder.Append($"{field.Name} ({fieldTypeName[(fieldTypeName.LastIndexOf('.') + 1)..]}):");
                if ((fieldType.IsArray && fieldType != typeof(string)) || (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>)))
                {
                    builder = (value as IEnumerable).GetEnumerableValues(builder, nestCount);
                    continue;
                }
                else if (instance is Item && field.Name == "spawnPrefab")
                {
                    builder = (value as GameObject).GetComponent<GrabbableObject>().GetAllFieldValues(builder, nestCount);
                    continue;
                }
                else if (instance is WildCardMapObject && field.Name == "spawnableMapObject")
                {
                    builder = (value as SpawnableMapObject).prefabToSpawn.GetComponent<NetworkBehaviour>().GetAllFieldValues(builder, nestCount);
                    continue;
                }
                else if (fieldType == typeof(RepeatingAnimation) || fieldType == typeof(BurstIntermediary) || fieldType.IsSubclassOfRawGeneric(typeof(Selectable<>)))
                {
                    builder = value.GetAllFieldValues(builder, nestCount);
                    continue;
                }
                string valueString = "null";
                if (value != null && !(value is string valueAsString && valueAsString.Length == 0))
                {
                    valueString = value.ToString();
                }
                builder.AppendLine($" {valueString}\n");
            }
            return builder;
        }
        public static StringBuilder GetEnumerableValues(this IEnumerable data, StringBuilder builder = null, int nestCount = 0)
        {
            nestCount++;
            builder ??= new StringBuilder();
            if (data != null)
            {
                foreach (object value in data)
                {
                    builder.Append('\n');
                    Type type = value.GetType();
                    if ((type.IsArray && type != typeof(string)) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)))
                    {
                        builder = (value as IEnumerable).GetEnumerableValues(builder, nestCount);
                        continue;
                    }
                    else if (type.IsAssignableFrom(typeof(UnityEngine.Object)))
                    {
                        builder.AppendLine(value.GetAllFieldValues(builder, nestCount).ToString());
                        continue;
                    }
                    else if (type == typeof(RepeatingAnimation) || type == typeof(BurstIntermediary))
                    {
                        builder.AppendLine(value.GetAllFieldValues(builder, nestCount).ToString());
                        continue;
                    }
                    string valueString = "null";
                    if (value != null && !(value is string valueAsString && valueAsString.Length == 0))
                    {
                        valueString = value.ToString();
                    }
                    for (int i = 0; i < nestCount; i++)
                    {
                        builder.Append('\t');
                    }
                    builder.AppendLine(valueString);
                }
            }
            if (builder.ToString().EndsWith(':'))
            {
                builder.AppendLine(" null");
            }
            return builder.AppendLine();
        }
        public static RpcParams GetRPCTarget(this PlayerControllerB player)
        {
            return new RpcSendParams() { Target = player.RpcTarget.Single(player.actualClientId, RpcTargetUse.Temp) };
        }
        public static float MultiplyPlayerSpeed(this PlayerControllerB player, float multiplier)
        {
            if (player == null)
            {
                return 0f;
            }
            player.movementSpeed *= multiplier;
            int id = (int)player.playerClientId;
            if (!playerSpeedMultipliers.TryAdd(id, multiplier))
            {
                playerSpeedMultipliers[id] *= multiplier;
            }
            return player.movementSpeed;
        }
        public static float MultiplyPlayerSpeed(int id, float multiplier)
        {
            return StartOfRound.Instance.allPlayerScripts[id].MultiplyPlayerSpeed(multiplier);
        }
        public static float ResetPlayerSpeed(this PlayerControllerB player)
        {
            if (player == null)
            {
                return 0f;
            }
            int id = (int)player.playerClientId;
            if (!playerSpeedMultipliers.TryGetValue(id, out float multiplier))
            {
                return player.movementSpeed;
            }
            playerSpeedMultipliers[id] = 1f;
            player.movementSpeed /= multiplier;
            return player.movementSpeed;
        }
        public static float ResetPlayerSpeed(int id)
        {
            return StartOfRound.Instance.allPlayerScripts[id].ResetPlayerSpeed();
        }
        public static int GCD(this int[] values)
        {
            if (values == null || values.Length == 0)
            {
                return 0;
            }
            int factor = values[0];
            if (values.Length == 1)
            {
                return factor;
            }
            for (int i = 1; i < values.Length; i++)
            {
                int currentValue = values[i];
                if (currentValue <= 0 || factor == 0)
                {
                    factor = currentValue;
                    continue;
                }
                int factorValue = factor;
                int checkingValue = currentValue;
                while (factorValue > 0 && checkingValue > 0)
                {
                    if (factorValue > checkingValue)
                    {
                        factorValue %= checkingValue;
                        continue;
                    }
                    checkingValue %= factorValue;
                }
                factor = factorValue | checkingValue;
            }
            return factor;
        }
        public static ParticleSystem.Burst CreateBurst(BurstIntermediary intermediary)
        {
            return new ParticleSystem.Burst(intermediary.time, (short)intermediary.minCount, (short)intermediary.maxCount, intermediary.cycles, intermediary.interval)
            {
                probability = intermediary.probability
            };
        }
        public static Particles CreateParticleSystem(Transform transformTarget)
        {
            ParticleSystem system = transformTarget.gameObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule mainModule = system.main;
            mainModule.playOnAwake = false;
            return Particles.Create(system);
        }
        public static void RegisterMapObject(IndoorMapHazard mapObject, Levels.LevelTypes levels, Func<SelectableLevel, AnimationCurve> spawnRateFunction = null)
        {
            RegisterMapObject(mapObject.hazardType, levels, spawnRateFunction);
        }
        public static void RegisterMapObject(IndoorMapHazard mapObject, Levels.LevelTypes levels = Levels.LevelTypes.None, string[] levelOverrides = null, Func<SelectableLevel, AnimationCurve> spawnRateFunction = null)
        {
            RegisterMapObject(mapObject.hazardType, levels, levelOverrides, spawnRateFunction);
        }
        public static void RegisterMapObject(IndoorMapHazardType mapObject, Levels.LevelTypes levels, Func<SelectableLevel, AnimationCurve> spawnRateFunction = null)
        {
            RegisterMapObject(mapObject, levels, null, spawnRateFunction);
        }
        public static void RegisterMapObject(IndoorMapHazardType mapObject, Levels.LevelTypes levels = Levels.LevelTypes.None, string[] levelOverrides = null, Func<SelectableLevel, AnimationCurve> spawnRateFunction = null)
        {
            mapObjects.Add(new RegisteredMapObject
            {
                indoorMapHazardType = mapObject,
                levels = levels,
                spawnRateFunction = spawnRateFunction,
                spawnLevelOverrides = levelOverrides
            });
        }
        public static void RemoveMapObject(IndoorMapHazard mapObject, Levels.LevelTypes levelFlags, string[] levelOverrides = null)
        {
            RemoveMapObject(mapObject.hazardType, levelFlags, levelOverrides);
        }
        public static void RemoveMapObject(IndoorMapHazardType mapObject, Levels.LevelTypes levelFlags, string[] levelOverrides = null)
        {
            if (StartOfRound.Instance == null)
            {
                return;
            }
            SelectableLevel[] levels = StartOfRound.Instance.levels;
            for (int i = 0; i < levels.Length; i++)
            {
                SelectableLevel level = levels[i];
                string name = level.name;
                bool alwaysValid = (levelFlags.HasFlag(Levels.LevelTypes.All) || (levelOverrides?.Any((string item) => item.ToLowerInvariant() == name.ToLowerInvariant()) ?? false) || (levelFlags.HasFlag(Levels.LevelTypes.Modded) && !Enum.IsDefined(typeof(Levels.LevelTypes), name)));
                if (!(Enum.IsDefined(typeof(Levels.LevelTypes), name) || alwaysValid))
                {
                    continue;
                }
                Levels.LevelTypes levelEnum = (alwaysValid ? Levels.LevelTypes.All : ((Levels.LevelTypes)Enum.Parse(typeof(Levels.LevelTypes), name)));
                if (!alwaysValid && !levelFlags.HasFlag(levelEnum))
                {
                    continue;
                }
                level.indoorMapHazards = level.indoorMapHazards.Where((IndoorMapHazard x) => x.hazardType.prefabToSpawn != mapObject.prefabToSpawn).ToArray();
            }
        }
        public static string[] LayersFromMask(int mask)
        {
            string maskBinary = Convert.ToString(mask, 2);
            List<string> layers = new List<string>();
            for (int i = maskBinary.Length - 1; i >= 0; i--)
            {
                if (maskBinary[i] == '0')
                {
                    continue;
                }
                layers.Add(LayerMask.LayerToName(i));
            }
            return layers.ToArray();
        }
    }
    [Serializable]
    public class KeyBinds : LcInputActions
    {
        [InputAction(KeyboardControl.R, Name = "WildCardUse")]
        public InputAction WildCardButton { get; private set; }
    }
    [Serializable]
    public class BurstIntermediary
    {
        public BurstIntermediary(float time = 0f, int minCount = 30, int maxCount = 30, int cycles = 0, float interval = 0.01f, float probability = 1f)
        {
            this.time = Mathf.Max(0f, time);
            this.minCount = Mathf.Max(0, minCount);
            this.maxCount = Mathf.Max(0, maxCount);
            this.cycles = Mathf.Max(0, cycles);
            this.interval = Mathf.Clamp01(interval);
            this.probability = Mathf.Clamp01(probability);
        }
        public BurstIntermediary(float time = 0f, int count = 30, int cycles = 0, float interval = 0.01f, float probability = 1f) : this(time, count, count, cycles, interval, probability) { }
        [Min(0f)]
        [SerializeField]
        public float time = default;
        [Min(0f)]
        [SerializeField]
        public int minCount = default;
        [Min(0f)]
        [SerializeField]
        public int maxCount = default;
        [Min(0f)]
        [SerializeField]
        public int cycles = default;
        [Range(0f, 1f)]
        [SerializeField]
        public float interval = default;
        [Range(0f, 1f)]
        [SerializeField]
        public float probability = default;
    }
    [Serializable]
    public class AnimationHandler
    {
        private AnimationHandler(NetworkAnimator animator)
        {
            networkAnimator = animator;
            this.animator = networkAnimator.Animator;
        }
        ManualLogSource Log => WildCardMod.Instance.Log;
        [HideInInspector]
        [SerializeReference]
        private NetworkAnimator networkAnimator = default;
        [HideInInspector]
        [SerializeReference]
        private Animator animator = default;
        private Dictionary<string, (int, AnimatorControllerParameterType)> nameHashTypesDict;
        private Traverse updaterTraverse;
        private IWildCardBase wildCardBase;
        public bool IsNetworked
        {
            get
            {
                if (networkAnimator == null)
                {
                    return false;
                }
                return networkAnimator.enabled;
            }
        }
        public Animator Original
        {
            get
            {
                return animator;
            }
        }
        public NetworkAnimator OriginalNetworked
        {
            get
            {
                return networkAnimator;
            }
        }
        public Dictionary<string, (int, AnimatorControllerParameterType)> Parameters
        {
            get
            {
                if (nameHashTypesDict == null || nameHashTypesDict.Count == 0)
                {
                    ResetDictionary();
                }
                return nameHashTypesDict;
            }
        }
        public IWildCardBase Base
        {
            get
            {
                wildCardBase ??= networkAnimator.GetComponentInParent<IWildCardBase>();
                return wildCardBase;
            }
            set
            {
                if (wildCardBase != null)
                {
                    return;
                }
                wildCardBase = value;
            }
        }
        public static AnimationHandler Create(NetworkAnimator animator)
        {
            if (animator == null)
            {
                return null;
            }
            if (animator.Animator == null)
            {
                return null;
            }
            return new AnimationHandler(animator);
        }
        public bool SetParameter(string parameter, float value)
        {
            try
            {
                switch (GetParameterType(parameter))
                {
                    case AnimatorControllerParameterType.Float:
                        {
                            animator.SetFloat(parameter, value);
                            break;
                        }
                    case AnimatorControllerParameterType.Int:
                        {
                            animator.SetInteger(parameter, Mathf.RoundToInt(value));
                            break;
                        }
                    case AnimatorControllerParameterType.Bool:
                        {
                            animator.SetBool(parameter, Convert.ToBoolean(Mathf.RoundToInt(value)));
                            break;
                        }
                    default:
                        {
                            if (IsNetworked)
                            {
                                networkAnimator.SetTrigger(parameter);
                                break;
                            }
                            animator.SetTrigger(parameter);
                            break;
                        }
                }
            }
            catch (Exception exception)
            {
                Log.LogWarning($"{animator?.name} animator tried to set parameter \"{parameter}\" but something went wrong!");
                Log.LogWarning(exception);
                return false;
            }
            return true;
        }
        public bool SetParameter(int hash, float value)
        {
            return SetParameter(GetParameter(hash), value);
        }
        public bool Trigger(string parameter)
        {
            if (!Parameters.TryGetValue(parameter, out (int, AnimatorControllerParameterType) pair))
            {
                if (WildCardMod.ModConfig.Debug)
                {
                    Log.LogDebug($"{animator?.name} animator could not find a parameter of name \"{parameter}\"!");
                }
                return false;
            }
            if (pair.Item2 != AnimatorControllerParameterType.Trigger)
            {
                Log.LogWarning($"{animator?.name} animator parameter \"{parameter}\" was not a trigger!");
                return false;
            }
            try
            {
                if (IsNetworked)
                {
                    networkAnimator.SetTrigger(pair.Item1);
                    return true;
                }
                animator.SetTrigger(pair.Item1);
            }
            catch (Exception exception)
            {
                Log.LogWarning($"{animator?.name} animator tried to trigger \"{parameter}\" but something went wrong!");
                Log.LogWarning(exception);
                return false;
            }
            return true;
        }
        public bool ResetTrigger(string parameter)
        {
            if (!Parameters.TryGetValue(parameter, out (int, AnimatorControllerParameterType) pair))
            {
                if (WildCardMod.ModConfig.Debug)
                {
                    Log.LogDebug($"{animator?.name} animator could not find a parameter of name \"{parameter}\"!");
                }
                return false;
            }
            if (pair.Item2 != AnimatorControllerParameterType.Trigger)
            {
                Log.LogWarning($"{animator?.name} animator parameter \"{parameter}\" was not a trigger!");
                return false;
            }
            try
            {
                if (IsNetworked)
                {
                    networkAnimator.ResetTrigger(pair.Item1);
                    return true;
                }
                animator.ResetTrigger(pair.Item1);
            }
            catch (Exception exception)
            {
                Log.LogWarning($"{animator?.name} animator tried to trigger \"{parameter}\" but something went wrong!");
                Log.LogWarning(exception);
                return false;
            }
            return true;
        }
        public bool SetFloat(string parameter, float value)
        {
            if (!Parameters.TryGetValue(parameter, out (int, AnimatorControllerParameterType) pair))
            {
                if (WildCardMod.ModConfig.Debug)
                {
                    Log.LogDebug($"{animator?.name} animator could not find a parameter of name \"{parameter}\"!");
                }
                return false;
            }
            if (pair.Item2 != AnimatorControllerParameterType.Float)
            {
                Log.LogWarning($"{animator?.name} animator parameter \"{parameter}\" was not a float!");
                return false;
            }
            try
            {
                animator.SetFloat(pair.Item1, value);
            }
            catch (Exception exception)
            {
                Log.LogWarning($"{animator?.name} animator tried to set \"{parameter}\" to float \"{value}\" but something went wrong!");
                Log.LogWarning(exception);
                return false;
            }
            return true;
        }
        public bool SetBool(string parameter, bool value)
        {
            if (!Parameters.TryGetValue(parameter, out (int, AnimatorControllerParameterType) pair))
            {
                if (WildCardMod.ModConfig.Debug)
                {
                    Log.LogDebug($"{animator?.name} animator could not find a parameter of name \"{parameter}\"!");
                }
                return false;
            }
            if (pair.Item2 != AnimatorControllerParameterType.Bool)
            {
                Log.LogWarning($"{animator?.name} animator parameter \"{parameter}\" was not a bool!");
                return false;
            }
            try
            {
                animator.SetBool(pair.Item1, value);
            }
            catch (Exception exception)
            {
                Log.LogWarning($"{animator?.name} animator tried to set \"{parameter}\" to bool \"{value}\" but something went wrong!");
                Log.LogWarning(exception);
                return false;
            }
            return true;
        }
        public bool SetInt(string parameter, int value)
        {
            if (!Parameters.TryGetValue(parameter, out (int, AnimatorControllerParameterType) pair))
            {
                if (WildCardMod.ModConfig.Debug)
                {
                    Log.LogDebug($"{animator?.name} animator could not find a parameter of name \"{parameter}\"!");
                }
                return false;
            }
            if (pair.Item2 != AnimatorControllerParameterType.Int)
            {
                Log.LogWarning($"{animator?.name} animator parameter \"{parameter}\" was not an int!");
                return false;
            }
            try
            {
                animator.SetInteger(pair.Item1, value);
            }
            catch (Exception exception)
            {
                Log.LogWarning($"{animator?.name} animator tried to set \"{parameter}\" to int \"{value}\" but something went wrong!");
                Log.LogWarning(exception);
                return false;
            }
            return true;
        }
        public float GetFloat(string parameter)
        {
            float value = 0f;
            if (!Parameters.TryGetValue(parameter, out (int, AnimatorControllerParameterType) pair))
            {
                if (WildCardMod.ModConfig.Debug)
                {
                    Log.LogDebug($"{animator?.name} animator could not find a parameter of name \"{parameter}\"!");
                }
                return value;
            }
            if (pair.Item2 != AnimatorControllerParameterType.Float)
            {
                Log.LogWarning($"{animator?.name} animator parameter \"{parameter}\" was not a float!");
                return value;
            }
            try
            {
                value = animator.GetFloat(pair.Item1);
            }
            catch (Exception exception)
            {
                Log.LogWarning($"{animator?.name} animator tried to get float from \"{parameter}\" but something went wrong!");
                Log.LogWarning(exception);
                return value;
            }
            return value;
        }
        public bool GetBool(string parameter)
        {
            bool value = false;
            if (!Parameters.TryGetValue(parameter, out (int, AnimatorControllerParameterType) pair))
            {
                if (WildCardMod.ModConfig.Debug)
                {
                    Log.LogDebug($"{animator?.name} animator could not find a parameter of name \"{parameter}\"!");
                }
                return value;
            }
            if (pair.Item2 != AnimatorControllerParameterType.Bool)
            {
                Log.LogWarning($"{animator?.name} animator parameter \"{parameter}\" was not a bool!");
                return value;
            }
            try
            {
                value = animator.GetBool(pair.Item1);
            }
            catch (Exception exception)
            {
                Log.LogWarning($"{animator?.name} animator tried to get bool from \"{parameter}\" but something went wrong!");
                Log.LogWarning(exception);
                return value;
            }
            return value;
        }
        public int GetInt(string parameter)
        {
            int value = 0;
            if (!Parameters.TryGetValue(parameter, out (int, AnimatorControllerParameterType) pair))
            {
                if (WildCardMod.ModConfig.Debug)
                {
                    Log.LogDebug($"{animator?.name} animator could not find a parameter of name \"{parameter}\"!");
                }
                return value;
            }
            if (pair.Item2 != AnimatorControllerParameterType.Int)
            {
                Log.LogWarning($"{animator?.name} animator parameter \"{parameter}\" was not an int!");
                return value;
            }
            try
            {
                value = animator.GetInteger(pair.Item1);
            }
            catch (Exception exception)
            {
                Log.LogWarning($"{animator?.name} animator tried to get int from \"{parameter}\" but something went wrong!");
                Log.LogWarning(exception);
                return value;
            }
            return value;
        }
        public float GetNormalizedTime(int layerIndex = 0)
        {
            return animator.GetCurrentAnimatorStateInfo(layerIndex).normalizedTime;
        }
        public void SetNetworkEnabled(bool enable)
        {
            if (networkAnimator == null || enable == networkAnimator.enabled)
            {
                return;
            }
            updaterTraverse ??= Traverse.Create(networkAnimator).Field("m_NetworkAnimatorStateChangeHandler");
            INetworkUpdateSystem networkAnimatorUpdater = (INetworkUpdateSystem)updaterTraverse.GetValue();
            networkAnimator.enabled = enable;
            Base.SetNetworkAnimationsNetworked(enable);
            if (enable)
            {
                networkAnimatorUpdater.RegisterNetworkUpdate(NetworkUpdateStage.PreUpdate);
                return;
            }
            networkAnimatorUpdater.UnregisterNetworkUpdate(NetworkUpdateStage.PreUpdate);
        }
        public string[] GetParameters()
        {
            return Parameters.Keys.ToArray();
        }
        public int[] GetHashes()
        {
            return Parameters.Values.Select((x) => x.Item1).ToArray();
        }
        public int GetHash(string parameter)
        {
            if (!Parameters.TryGetValue(parameter, out (int, AnimatorControllerParameterType) pair))
            {
                return 0;
            }
            return pair.Item1;
        }
        public string GetParameter(int hash)
        {
            string[] parameters = GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                string parameter = parameters[i];
                if (GetHash(parameter) != hash)
                {
                    continue;
                }
                return parameter;
            }
            return string.Empty;
        }
        public AnimatorControllerParameterType? GetParameterType(string parameter)
        {
            if (!Parameters.TryGetValue(parameter, out (int, AnimatorControllerParameterType) pair))
            {
                return null;
            }
            return pair.Item2;
        }
        private void ResetDictionary()
        {
            AnimatorControllerParameter[] parameters = animator.parameters;
            nameHashTypesDict ??= new Dictionary<string, (int, AnimatorControllerParameterType)>();
            nameHashTypesDict.Clear();
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                nameHashTypesDict.TryAdd(parameter.name, (parameter.nameHash, parameter.type));
            }
        }
    }
    [Serializable]
    public class Particles
    {
        public Particles(ParticleSystem particleSystem)
        {
            system = particleSystem;
            renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        }
        [SerializeField]
        private ParticleSystem system = default;
        private ParticleSystemRenderer renderer = default;
        public ParticleSystem System => system;
        public ParticleSystemRenderer Renderer
        {
            get
            {
                if (renderer == null && system != null)
                {
                    renderer = system.GetComponent<ParticleSystemRenderer>();
                }
                return renderer;
            }
        }
        public static Particles Create(ParticleSystem particleSystem)
        {
            if (particleSystem == null)
            {
                return null;
            }
            return new Particles(particleSystem);
        }
    }
    [Serializable]
    public class RepeatingTimer : Repeater
    {
        public RepeatingTimer()
        {
            manualReset = true;
        }
        public RepeatingTimer(float min, float max) : base(min, max)
        {
            manualReset = true;
        }
        public RepeatingTimer(float noRandom) : this(noRandom, noRandom) { }
        public bool Complete => CurrentTime <= 0f;
        public void Restart()
        {
            ResetTimer();
            Resume();
        }
        public override bool TimerOverride()
        {
            return false;
        }
        public override void TimerTrigger(bool overriden = false)
        {
            
        }
    }
    [Serializable]
    public class RepeatingAction : Repeater
    {
        public RepeatingAction(Action action)
        {
            SetAction(action);
        }
        private Action action;
        public override bool TimerOverride()
        {
            return false;
        }
        public override void TimerTrigger(bool overriden = false)
        {
            action.Invoke();
        }
        public void SetAction(Action action)
        {
            this.action = action;
        }
    }
    [Serializable]
    public class RepeatingAnimation : Repeater
    {
        public enum RandomAnimationType
        {
            None,
            FlipFlop,
            Between,
            From
        }
        public RepeatingAnimation(string parameter)
        {
            this.parameter = parameter;
        }
        public RepeatingAnimation(string parameter, AnimationHandler animationHandler) : this(parameter)
        {
            this.parameter = parameter;
            SetAnimator(animationHandler);
        }
        [SerializeField]
        private string parameter = string.Empty;
        [SerializeField]
        private RandomAnimationType randomType = RandomAnimationType.None;
        [SerializeField]
        private float[] randomValues = null;
        [SerializeField]
        private bool manualNetwork = false;
        [SerializeField]
        private string syncToAudio = string.Empty;
        private AnimationHandler animator = null;
        private bool flipFlop = false;
        public string ParameterName
        {
            get
            {
                return parameter;
            }
        }
        public RandomAnimationType RandomType
        {
            get
            {
                return randomType;
            }
            private set
            {
                randomType = value;
            }
        }
        public float[] Values
        {
            get
            {
                return randomValues;
            }
            private set
            {
                randomValues = value;
            }
        }
        public override void TimerTrigger(bool overriden = false)
        {
            if (overriden)
            {
                SelectAudioClips audio = animator.Base.Audio[syncToAudio];
                if (audio != null && audio.IsPlaying)
                {
                    DoAnimation(audio.ClipTime);
                    return;
                }
                DoAnimation(0f);
                return;
            }
            DoAnimation();
        }
        public override bool TimerOverride()
        {
            return syncToAudio != null && syncToAudio.Length > 0;
        }
        public void DoAnimation(float? overrideValue = null)
        {
            if (overrideValue.HasValue)
            {
                SetParameter(overrideValue.Value);
                return;
            }
            float min = 0f;
            float max = 0f;
            if (Values != null && Values.Length > 0)
            {
                min = Mathf.Min(Values);
                max = Mathf.Max(Values);
            }
            if (Mathf.Approximately(min, max))
            {
                SetParameter(min);
                return;
            }
            switch (RandomType)
            {
                case RandomAnimationType.Between:
                    {
                        SetParameter(animator.Base.Random.Next(Mathf.RoundToInt(min * 100f), Mathf.RoundToInt(max * 100f) + 1) * 0.01f);
                        break;
                    }
                case RandomAnimationType.FlipFlop:
                    {
                        flipFlop = !flipFlop;
                        if (flipFlop)
                        {
                            SetParameter(max);
                            break;
                        }
                        SetParameter(min);
                        break;
                    }
                case RandomAnimationType.From:
                    {
                        SetParameter(randomValues[animator.Base.Random.Next(0, randomValues.Length)]);
                        break;
                    }
                default:
                    {
                        SetParameter(0f);
                        break;
                    }
            }
        }
        public void SetValues(RandomAnimationType? type = null, params float?[] toChange)
        {
            if (type.HasValue)
            {
                RandomType = type.Value;
            }
            if (toChange == null)
            {
                Values = new float[0];
                return;
            }
            float[] newValues = new float[toChange.Length];
            for (int i = 0; i < toChange.Length; i++)
            {
                if (toChange[i].HasValue)
                {
                    newValues[i] = toChange[i].Value;
                    continue;
                }
                if (i >= Values.Length)
                {
                    newValues[i] = 0f;
                    continue;
                }
                newValues[i] = Values[i];
            }
            Values = newValues;
        }
        public void UpdateValues(params float?[] toChange)
        {
            float?[] newValues = new float?[Values.Length];
            for (int i = 0; i < newValues.Length; i++)
            {
                if (i < toChange.Length && toChange[i].HasValue)
                {
                    newValues[i] = toChange[i];
                    continue;
                }
                newValues[i] = Values[i];
            }
            SetValues(toChange: newValues);
        }
        public void SetAnimator(AnimationHandler animationHandler)
        {
            AnimatorControllerParameterType? type = animationHandler.GetParameterType(ParameterName);
            if (!type.HasValue)
            {
                throw new ArgumentOutOfRangeException($"{animator?.Original?.name} animator could not find a parameter of name \"{ParameterName}\"");
            }
            animator = animationHandler;
        }
        private void SetParameter(float value)
        {
            animator.SetParameter(ParameterName, value);
            if (animator.IsNetworked || !manualNetwork)
            {
                return;
            }
            animator.Base.SetParameterNetworked(animator.GetHash(ParameterName), value);
        }
    }
    [Serializable]
    public abstract class Repeater
    {
        public Repeater() { }
        public Repeater(float min, float max)
        {
            timerMin = min;
            timerMax = max;
        }
        public Repeater(float noRandom) : this(noRandom, noRandom) { }
        [Min(0f)]
        [SerializeField]
        private float timerMin = 0.5f;
        [Min(0f)]
        [SerializeField]
        private float timerMax = 5f;
        [SerializeField]
        internal bool manualReset = false;
        private Vector2Int timerMinMaxRound;
        private Func<bool> waitFor = () => true;
        private float timer = 0f;
        private bool pause = false;
        private Vector2? oldTimerValues = null;
        private System.Random random;
        public float CurrentTime => timer;
        public Vector2 TimerValues
        {
            get
            {
                return new Vector2(timerMin, timerMax);
            }
            private set
            {
                timerMin = value.x;
                timerMax = value.y;
                timerMinMaxRound = new Vector2Int(Mathf.RoundToInt(timerMin * 100f), Mathf.RoundToInt(timerMax * 100f) + 1);
            }
        }
        public Func<bool> LoopWaitsFor
        {
            get
            {
                waitFor ??= () => true;
                return waitFor;
            }
            set
            {
                waitFor = value;
            }
        }
        public bool IsPlaying
        {
            get
            {
                return !pause;
            }
            private set
            {
                pause = !value;
            }
        }
        public System.Random Random
        {
            get
            {
                return random;
            }
            private set
            {
                random = value;
            }
        }
        public void Tick()
        {
            if (pause)
            {
                return;
            }
            bool overriden = TimerOverride();
            if (overriden)
            {
                TimerTrigger(overriden);
                return;
            }
            if (timer > 0f)
            {
                timer -= Time.deltaTime;
                return;
            }
            if (!LoopWaitsFor.Invoke())
            {
                return;
            }
            if (manualReset)
            {
                Pause();
            }
            else
            {
                ResetTimer();
            }
            TimerTrigger();
        }
        public abstract void TimerTrigger(bool overriden = false);
        public abstract bool TimerOverride();
        public void ResetTimer(bool undoOneTime = true)
        {
            Vector2 currentValues = TimerValues;
            if (undoOneTime && oldTimerValues.HasValue)
            {
                TimerValues = oldTimerValues.Value;
                oldTimerValues = null;
            }
            if (Mathf.Approximately(currentValues.x, currentValues.y))
            {
                timer = currentValues.x;
                return;
            }
            timer = Random.Next(timerMinMaxRound.x, timerMinMaxRound.y) * 0.01f;
        }
        public void Pause()
        {
            IsPlaying = false;
        }
        public void Resume()
        {
            IsPlaying = true;
        }
        public void SetTimer(float? newMin = null, float? newMax = null, bool oneTime = false)
        {
            Vector2 oldValues = TimerValues;
            if (oneTime)
            {
                oldTimerValues = oldValues;
            }
            if (newMin.HasValue)
            {
                oldValues.x = newMin.Value;
            }
            if (newMax.HasValue)
            {
                oldValues.y = newMax.Value;
            }
            TimerValues = oldValues;
        }
        public void SetRandom(System.Random random)
        {
            Random = random;
            SetTimer();
            ResetTimer();
        }
    }
    [Serializable]
    public class SelectablePair<T>
    {
        [SerializeField]
        public string id = default;
        [SerializeField]
        public T selectable = default;
    }
    [Serializable]
    public class ListDict<TKey, TValue>
    {
        public ListDict()
        {

        }
        public ListDict(IEnumerable<TKey> keys, IEnumerable<TValue> values)
        {
            this.keys = keys.ToList();
            this.values = values.ToList();
        }
        [HideInInspector]
        [SerializeField]
        private List<TKey> keys = new List<TKey>();
        [HideInInspector]
        [SerializeField]
        private List<TValue> values = new List<TValue>();
        public bool Add(TKey key, TValue value)
        {
            if (keys.Contains(key))
            {
                return false;
            }
            keys.Add(key);
            values.Add(value);
            return true;
        }
        public TValue NewGet(TKey key)
        {
            if (Add(key, default))
            {
                TValue newValue = default;
                try
                {
                    newValue = Activator.CreateInstance<TValue>();
                }
                finally
                {
                    this[key] = newValue;
                }
                return newValue;
            }
            return this[key];
        }
        public bool Insert(int index, TKey key, TValue value)
        {
            if (!InRange(index) || keys.Contains(key))
            {
                return false;
            }
            keys.Insert(index, key);
            values.Insert(index, value);
            return true;
        }
        public bool Remove(TKey key)
        {
            return RemoveAt(keys.IndexOf(key));
        }
        public bool RemoveAt(int index)
        {
            if (!InRange(index))
            {
                return false;
            }
            keys.RemoveAt(index);
            values.RemoveAt(index);
            return true;
        }
        public void Clear()
        {
            keys.Clear();
            values.Clear();
        }
        public bool ContainsKey(TKey key)
        {
            return keys.Contains(key);
        }
        public bool ContainsValue(TValue value)
        {
            return values.Contains(value);
        }
        public bool ContainsPair(TKey key, TValue value)
        {
            if (!InRange(keys.IndexOf(key)))
            {
                return false;
            }
            return this[key].Equals(value);
        }
        public int IndexOf(TKey key)
        {
            return keys.IndexOf(key);
        }
        public TKey KeyOf(int index)
        {
            if (!InRange(index))
            {
                return default;
            }
            return keys[index];
        }
        public bool InRange(int index)
        {
            return index >= 0 && index < keys.Count;
        }
        public bool TryGetValue(TKey key, out TValue value)
        {
            value = default;
            if (!ContainsKey(key))
            {
                return false;
            }
            value = this[key];
            return true;
        }
        public ListDict<TKey, TValue> Combined(ListDict<TKey, TValue> other)
        {
            return new ListDict<TKey, TValue>(keys.Concat(other.keys), values.Concat(other.values));
        }
        public TValue this[TKey target]
        {
            get
            {
                int index = IndexOf(target);
                if (!InRange(index))
                {
                    return default;
                }
                return values[index];
            }
            set
            {
                int index = IndexOf(target);
                if (!InRange(index))
                {
                    return;
                }
                values[index] = value;
            }
        }
        public TValue this[int index]
        {
            get
            {
                if (!InRange(index))
                {
                    return default;
                }
                return values[index];
            }
            set
            {
                if (!InRange(index))
                {
                    return;
                }
                values[index] = value;
            }
        }
        public List<TKey> Keys
        {
            get
            {
                return new List<TKey>(keys);
            }
        }
        public List<TValue> Values
        {
            get
            {
                return new List<TValue>(values);
            }
        }
        public int Count => keys.Count;
    }
    [Serializable]
    public class WeightedOption<T>
    {
        [SerializeField]
        public string name;
        [Min(0f)]
        [SerializeField]
        public int weight = 0;
        [SerializeField]
        public T option = default;
        public WeightedOption(T option, int weight = 0) : this(option.ToString(), option, weight) { }
        public WeightedOption(string name, T option, int weight = 0)
        {
            this.name = name;
            this.option = option;
            this.weight = weight;
        }
    }
    [Serializable]
    public enum SkinType
    {
        Item,
        Enemy
    }
    [Serializable]
    [Flags]
    public enum AssetType
    {
        None = 0,
        Scrap = 1,
        Skin = 2,
        MapObject = 4,
        All = 7
    }
}