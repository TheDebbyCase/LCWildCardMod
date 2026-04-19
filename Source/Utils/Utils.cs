using BepInEx.Configuration;
using GameNetcodeStuff;
using HarmonyLib;
using LCWildCardMod.Items.Fyrus;
using LCWildCardMod.Items;
using LethalCompanyInputUtils.Api;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
namespace LCWildCardMod.Utils
{
    internal static class HarmonyHelper
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
        internal static MethodInfo haloSave = AccessTools.Method(typeof(SaveHelper), nameof(SaveHelper.SaveIfHalo), new Type[] { typeof(PlayerControllerB) });
        internal static MethodInfo anySave = AccessTools.Method(typeof(SaveHelper), nameof(SaveHelper.SaveIfAny), new Type[] { typeof(PlayerControllerB), typeof(EnemyAI) });
        internal static MethodInfo wasFyrusOrHaloGraceSaved = AccessTools.Method(typeof(SaveHelper), nameof(SaveHelper.SaveIfFyrusOrHaloExhausting), new Type[] { typeof(PlayerControllerB), typeof(EnemyAI) });
        internal static MethodInfo killPlayer = AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer), new Type[] { typeof(Vector3), typeof(bool), typeof(CauseOfDeath), typeof(int), typeof(Vector3), typeof(bool) });
        internal static MethodInfo damagePlayer = AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.DamagePlayer), new Type[] { typeof(int), typeof(bool), typeof(bool), typeof(CauseOfDeath), typeof(int), typeof(bool), typeof(Vector3) });
        internal static MethodInfo makeInjured = AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.MakeCriticallyInjured), new Type[] { typeof(bool) });
        internal static MethodInfo updateHealth = AccessTools.Method(typeof(HUDManager), nameof(HUDManager.UpdateHealthUI), new Type[] { typeof(int), typeof(bool) });
        internal static MethodInfo switchBehaviour = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.SwitchToBehaviourState), new Type[] { typeof(int) });
        internal static MethodInfo switchBehaviourLocal = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.SwitchToBehaviourStateOnLocalClient), new Type[] { typeof(int) });
        internal static MethodInfo setSpeed = AccessTools.Method(typeof(NavMeshAgent), "set_speed", new Type[] { typeof(float) });
        internal static MethodInfo allowDeath = AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.AllowPlayerDeath));
        internal static MethodInfo cancelSpecialAnim = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.CancelSpecialAnimationWithPlayer));
        internal static MethodInfo beeKill = AccessTools.Method(typeof(RedLocustBees), nameof(RedLocustBees.BeeKillPlayerOnLocalClient), new Type[] { typeof(int) });
        internal static MethodInfo dogKill = AccessTools.Method(typeof(MouthDogAI), nameof(MouthDogAI.KillPlayerServerRpc), new Type[] { typeof(int) });
        internal static MethodInfo maskKill = AccessTools.Method(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.FinishKillAnimation), new Type[] { typeof(bool) });
        internal static MethodInfo jesterKill = AccessTools.Method(typeof(JesterAI), nameof(JesterAI.KillPlayerServerRpc), new Type[] { typeof(int) });
        internal static MethodInfo brackenKill = AccessTools.Method(typeof(FlowermanAI), nameof(FlowermanAI.KillPlayerAnimationServerRpc), new Type[] { typeof(int) });
        internal static MethodInfo dwellerKill = AccessTools.Method(typeof(CaveDwellerAI), nameof(CaveDwellerAI.KillPlayerAnimationServerRpc), new Type[] { typeof(int) });
        internal static MethodInfo bloomBurst = AccessTools.Method(typeof(CadaverBloomAI), nameof(CadaverBloomAI.BurstForth), new Type[] { typeof(PlayerControllerB), typeof(bool), typeof(Vector3), typeof(Vector3) });
        internal static MethodInfo gameNetworkInstance = AccessTools.Method(typeof(GameNetworkManager), "get_Instance");
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
        internal static FieldInfo foxDragging = AccessTools.Field(typeof(BushWolfEnemy), nameof(BushWolfEnemy.dragging));
        internal static FieldInfo hasBurst = AccessTools.Field(typeof(CadaverBloomAI), nameof(CadaverBloomAI.hasBurst));
        internal static FieldInfo clingPlayer = AccessTools.Field(typeof(CentipedeAI), nameof(CentipedeAI.clingingToPlayer));
        internal static FieldInfo timesSeenByPlayer = AccessTools.Field(typeof(DressGirlAI), nameof(DressGirlAI.timesSeenByPlayer));
        internal static FieldInfo hauntingPlayer = AccessTools.Field(typeof(DressGirlAI), nameof(DressGirlAI.hauntingPlayer));
        internal static FieldInfo enemyState = AccessTools.Field(typeof(EnemyAI), nameof(EnemyAI.currentBehaviourStateIndex));
        internal static FieldInfo enemyAgent = AccessTools.Field(typeof(EnemyAI), nameof(EnemyAI.agent));
        internal static FieldInfo brackenEvade = AccessTools.Field(typeof(FlowermanAI), nameof(FlowermanAI.evadeStealthTimer));
        internal static FieldInfo maskHeldBy = AccessTools.Field(typeof(HauntedMaskItem), nameof(HauntedMaskItem.previousPlayerHeldBy));
        internal static FieldInfo specialAnim = AccessTools.Field(typeof(EnemyAI), nameof(EnemyAI.inSpecialAnimationWithPlayer));
        internal static FieldInfo maskedLastPlayer = AccessTools.Field(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.lastPlayerKilled));
        internal static FieldInfo playerSinking = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.sinkingValue));
        internal static FieldInfo playerCrouching = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.isCrouching));
        internal static FieldInfo playerHealth = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.health));
        internal static FieldInfo playerInjured = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.criticallyInjured));
        internal static FieldInfo playerID = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.playerClientId));
        internal static FieldInfo beeZapMode = AccessTools.Field(typeof(RedLocustBees), nameof(RedLocustBees.beesZappingMode));
        internal static FieldInfo giantPlayerStealth = AccessTools.Field(typeof(ForestGiantAI), nameof(ForestGiantAI.playerStealthMeters));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogString(string toLog)
        {
            WildCardMod.Instance.Log.LogDebug(toLog);
        }
        internal static List<CodeInstruction> DebugString(string toLog, params Label[] labelsStart)
        {
            return new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldstr, toLog).WithLabels(labelsStart),
                new CodeInstruction(OpCodes.Call, logString)
            };
        }
        internal static List<CodeInstruction> DebugLoad<T>(string toLog, OpCode opcode, object operand = null, params Label[] labelsStart)
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
        internal static List<CodeInstruction> DebugLoad<T>(string toLog, IEnumerable<CodeInstruction> loadInstructions, params Label[] labelsStart)
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
        internal static List<CodeInstruction> DebugLoadFromThis<T>(string toLog, IEnumerable<CodeInstruction> loadInstructions, params Label[] labelsStart)
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
        internal static List<CodeInstruction> DebugLoadFromThis<T>(string toLog, OpCode opcode, object operand = null, params Label[] labelsStart)
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
        internal static List<CodeInstruction> DebugThisEnemyName()
        {
            return DebugLoadFromThis<string>("Enemy", new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldflda, enemyType),
                new CodeInstruction(OpCodes.Ldflda, enemyName)
            });
        }
        internal static List<CodeInstruction> DebugEnemyName(OpCode loadEnemyOpcode, object loadEnemyOperand = null)
        {
            return DebugLoad<string>("Enemy", new List<CodeInstruction>()
            {
                new CodeInstruction(loadEnemyOpcode, loadEnemyOperand),
                new CodeInstruction(OpCodes.Ldflda, enemyType),
                new CodeInstruction(OpCodes.Ldflda, enemyName)
            });
        }
        internal static List<CodeInstruction> DebugThisPlayerName()
        {
            return DebugLoadFromThis<string>("Player", new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldflda, playerName)
            });
        }
        internal static List<CodeInstruction> DebugPlayerName(OpCode loadPlayerOpcode, object loadPlayerOperand = null)
        {
            return DebugLoad<string>("Player", new List<CodeInstruction>()
            {
                new CodeInstruction(loadPlayerOpcode, loadPlayerOperand),
                new CodeInstruction(OpCodes.Ldflda, playerName)
            });
        }
        internal static CodeInstruction StoreToLoad(CodeInstruction store)
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
        internal static CodeInstruction LoadToStore(CodeInstruction load)
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
        internal static bool AreLoadEqual(CodeInstruction load1, CodeInstruction load2)
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
    }
    internal static class SaveHelper
    {
        internal static bool IsSaveable(PlayerControllerB player, out bool starSave, out SmithHalo halo, EnemyAI enemy = null)
        {
            halo = null;
            starSave = SaveIfFyrus(player, enemy);
            return starSave || (player.isHoldingObject && player.currentlyHeldObjectServer.TryGetComponent(out halo) && ((halo.savedPlayer == player && halo.exhausting) || halo.isExhausted == 0)) || HasSavedHalo(player, out halo);
        }
        internal static bool SaveIfFyrus(PlayerControllerB player, EnemyAI enemy = null)
        {
            if (player == null)
            {
                return false;
            }
            FyrusStar star = null;
            for (int i = 0; i < FyrusStar.allSpawnedStars.Count; i++)
            {
                FyrusStar checkingStar = FyrusStar.allSpawnedStars[i];
                if (checkingStar.affectingPlayer != player)
                {
                    continue;
                }
                star = checkingStar;
                break;
            }
            if (star == null)
            {
                return false;
            }
            WildCardMod.Instance.Log.LogDebug($"Fyrus star saved {player.playerUsername}!");
            if (enemy == null)
            {
                return true;
            }
            EnemyAICollisionDetect collision = enemy.GetComponentInChildren<EnemyAICollisionDetect>();
            if (collision == null)
            {
                return true;
            }
            if (star.hitCooldown <= 0f)
            {
                (collision as IHittable).Hit(1, (enemy.transform.position - player.transform.position).normalized * 2.5f, player, true);
                star.hitCooldown = star.hitCooldownMax;
            }
            return true;
        }
        internal static bool SaveIfAny(PlayerControllerB player, EnemyAI enemy = null)
        {
            if (IsSaveable(player, out bool starSave, out SmithHalo halo, enemy))
            {
                if (!starSave)
                {
                    halo.ExhaustLocal(player);
                }
                return true;
            }
            return false;
        }
        internal static bool SaveIfHalo(PlayerControllerB player)
        {
            if (IsSaveable(player, out bool starSave, out SmithHalo halo) && !starSave && halo.savedPlayer != player)
            {
                halo.ExhaustLocal(player);
                return true;
            }
            return false;
        }
        internal static bool WasHaloSaved(PlayerControllerB player, out SmithHalo halo)
        {
            return IsSaveable(player, out bool starSave, out halo) && !starSave && halo.savedPlayer == player;
        }
        internal static bool SaveIfFyrusOrHaloExhausting(PlayerControllerB player, EnemyAI enemy = null)
        {
            return SaveIfFyrus(player, enemy) || WasHaloSaved(player, out _);
        }
        internal static bool HasSavedHalo(PlayerControllerB player, out SmithHalo halo)
        {
            halo = null;
            if (player == null)
            {
                return false;
            }
            for (int i = 0; i < player.ItemSlots.Length; i++)
            {
                GrabbableObject item = player.ItemSlots[i];
                if (item == null)
                {
                    continue;
                }
                if (!item.TryGetComponent(out halo))
                {
                    continue;
                }
                if (halo.savedPlayer != player || !halo.exhausting)
                {
                    continue;
                }
                return true;
            }
            return HasSavedHaloAnywhere(player, out halo);
        }
        internal static bool HasSavedHaloAnywhere(PlayerControllerB player, out SmithHalo halo)
        {
            halo = null;
            if (player == null)
            {
                return false;
            }
            for (int i = 0; i < SmithHalo.allSpawnedHalos.Count; i++)
            {
                SmithHalo haloCheck = SmithHalo.allSpawnedHalos[i];
                if (haloCheck.savedPlayer != player || !haloCheck.exhausting)
                {
                    continue;
                }
                halo = haloCheck;
                return true;
            }
            return false;
        }
    }
    internal static class EventsClass
    {
        internal static bool roundStarted = false;
        internal delegate void RoundStart();
        internal delegate void RoundEnd();
        internal static event RoundStart OnRoundStart;
        internal static event RoundEnd OnRoundEnd;
        internal static void RoundStarted()
        {
            if (roundStarted)
            {
                return;
            }
            roundStarted = true;
            OnRoundStart?.Invoke();
        }
        internal static void RoundEnded()
        {
            if (!roundStarted)
            {
                return;
            }
            roundStarted = false;
            OnRoundEnd?.Invoke();
        }
    }
    internal class KeyBinds : LcInputActions
    {
        [InputAction("<Keyboard>/r", Name = "WildCardUse")]
        internal InputAction WildCardButton { get; set; }
    }
    public class AdditionalInfo : MonoBehaviour
    {
        public bool defaultEnabled;
        public string defaultRarities;
        public bool isBonus;
    }
    internal static class SkinsClass
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        internal static void SetSkin(EnemyAI enemy)
        {
            Skin skinToApply = GetRandomSkin(enemy.enemyType.enemyName, SkinType.Enemy);
            if (skinToApply == null)
            {
                return;
            }
            switch ((skinToApply.target as EnemyType).enemyName)
            {
                case "Earth Leviathan":
                    {
                        Log.LogDebug($"Skin \"{skinToApply.skinName}\" is being applied!");
                        Transform meshContainerTransform = enemy.transform.Find("MeshContainer");
                        SkinnedMeshRenderer meshRenderer = meshContainerTransform.Find("Renderer").GetComponent<SkinnedMeshRenderer>();
                        meshRenderer.sharedMesh = skinToApply.newMesh;
                        meshRenderer.sharedMaterial = skinToApply.newMaterial;
                        meshContainerTransform.Find("Armature").Find("Bone").Find("Bone.001").Find("Bone.003").Find("Bone.002").Find("ScanNode").GetComponent<ScanNodeProperties>().headerText = skinToApply.skinName;
                        SandWormAI sandWorm = enemy.transform.GetComponent<SandWormAI>();
                        sandWorm.ambientRumbleSFX[0] = skinToApply.newAudioClips[0];
                        sandWorm.ambientRumbleSFX[1] = skinToApply.newAudioClips[0];
                        sandWorm.ambientRumbleSFX[2] = skinToApply.newAudioClips[0];
                        sandWorm.creatureSFX.volume *= 1.5f;
                        sandWorm.roarSFX[0] = skinToApply.newAudioClips[1];
                        sandWorm.roarSFX[1] = skinToApply.newAudioClips[2];
                        break;
                    }
                default:
                    {
                        Log.LogError($"\"{skinToApply.skinName}\" did not match any known enemy type!");
                        break;
                    }
            }
        }
        internal static void SetSkin(GrabbableObject item)
        {
            Skin skinToApply = GetRandomSkin(item.itemProperties.itemName, SkinType.Item);
            if (skinToApply == null)
            {
                return;
            }
            switch ((skinToApply.target as Item).itemName)
            {
                case "Clown horn":
                    {
                        Log.LogDebug($"Skin \"{skinToApply.skinName}\" is being applied!");
                        Item newProperties = UnityEngine.Object.Instantiate(item.itemProperties);
                        newProperties.itemName = skinToApply.skinName;
                        newProperties.isConductiveMetal = false;
                        newProperties.grabSFX = skinToApply.newAudioClips[0];
                        newProperties.dropSFX = skinToApply.newAudioClips[1];
                        newProperties.toolTips[0] = "Squeeze : [LMB]";
                        newProperties.positionOffset = new Vector3(0.05f, 0.15f, -0.05f);
                        item.useCooldown = 0.5f;
                        MeshFilter prefabMeshFilter = newProperties.spawnPrefab.GetComponent<MeshFilter>();
                        prefabMeshFilter.mesh = skinToApply.newMesh;
                        prefabMeshFilter.sharedMesh = skinToApply.newMesh;
                        MeshFilter itemMeshFilter = item.transform.GetComponent<MeshFilter>();
                        itemMeshFilter.mesh = skinToApply.newMesh;
                        itemMeshFilter.sharedMesh = skinToApply.newMesh;
                        MeshRenderer itemMeshRenderer = item.transform.GetComponent<MeshRenderer>();
                        itemMeshRenderer.material = skinToApply.newMaterial;
                        itemMeshRenderer.sharedMaterial = skinToApply.newMaterial;
                        item.transform.Find("ScanNode").GetComponent<ScanNodeProperties>().headerText = skinToApply.skinName;
                        Animator anim = item.gameObject.AddComponent<Animator>();
                        anim.runtimeAnimatorController = skinToApply.newAnimationController;
                        item.transform.GetComponent<NoisemakerProp>().triggerAnimator = anim;
                        item.itemProperties = newProperties;
                        break;
                    }
                default:
                    {
                        Log.LogError($"\"{skinToApply.skinName}\" did not match any known item!");
                        break;
                    }
            }
        }
        static Skin GetRandomSkin(string target, SkinType type)
        {
            System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            int nothingWeight = 0;
            int skinsWeight = 0;
            List<Skin> potentialSkins = new List<Skin>();
            for (int i = 0; i < WildCardMod.Instance.skinList.Count; i++)
            {
                Skin skin = WildCardMod.Instance.skinList[i];
                switch (type)
                {
                    case SkinType.Enemy:
                        {
                            if (skin.target is EnemyType enemy && enemy.enemyName == target)
                            {
                                potentialSkins.Add(skin);
                            }
                            break;
                        }
                    case SkinType.Item:
                        {
                            if (skin.target is Item item && item.itemName == target)
                            {
                                potentialSkins.Add(skin);
                            }
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            if (potentialSkins.Count == 0)
            {
                return null;
            }
            for (int i = 0; i < potentialSkins.Count; i++)
            {
                Skin skin = potentialSkins[i];
                string skinName = skin.skinName;
                if (WildCardMod.Instance.ModConfig.skinApplyChance[skinName].Value <= 0)
                {
                    Log.LogDebug($"Skin \"{skinName}\" was disabled!");
                    potentialSkins.RemoveAt(i);
                    i--;
                    continue;
                }
                Log.LogDebug($"Adding skin \"{skinName}\"'s chance weight!");
                skinsWeight += WildCardMod.Instance.ModConfig.skinApplyChance[skinName].Value;
                nothingWeight += 100 - WildCardMod.Instance.ModConfig.skinApplyChance[skinName].Value;
            }
            float applyChance = (float)random.NextDouble();
            Log.LogDebug($"Rolling to see if a skin will be applied!");
            if (((float)nothingWeight / (float)(nothingWeight + skinsWeight)) >= applyChance)
            {
                return null;
            }
            for (int i = potentialSkins.Count - 1; i > 1; i--)
            {
                int j = random.Next(i + 1);
                (potentialSkins[i], potentialSkins[j]) = (potentialSkins[j], potentialSkins[i]);
            }
            for (int i = 0; i < potentialSkins.Count; i++)
            {
                Skin skin = potentialSkins[i];
                string skinName = skin.skinName;
                Log.LogDebug($"Rolling to see if \"{skinName}\" is selected!");
                if (WildCardMod.Instance.ModConfig.skinApplyChance[skinName].Value / skinsWeight >= applyChance)
                {
                    Log.LogDebug($"Skin \"{skinName}\" was selected!");
                    return skin;
                }
            }
            return null;
        }
    }
    [CreateAssetMenu(menuName = "WildCard/Skin", order = 1)]
    public class Skin : ScriptableObject
    {
        public string skinName;
        public bool skinEnabled;
        public int skinChance;
        public ScriptableObject target;
        public Mesh newMesh;
        public Material newMaterial;
        public AudioClip[] newAudioClips;
        public RuntimeAnimatorController newAnimationController;
    }
    [CreateAssetMenu(menuName = "WildCard/MapObject", order = 1)]
    public class MapObject : ScriptableObject
    {
        public string mapObjectName;
        public SpawnableMapObject spawnableMapObject;
        public List<LevelCurve> levelCurves;
        public bool autoHandle;
        internal Func<SelectableLevel, AnimationCurve> GetCurveFunc()
        {
            AnimationCurve curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0, 0));
            if (levelCurves != null && (autoHandle || WildCardMod.Instance.ModConfig.useDefaultMapObjectCurve[mapObjectName].Value))
            {
                return (x) =>
                {
                    for (int i = 0; i < levelCurves.Count; i++)
                    {
                        LevelCurve levelCurve = levelCurves[i];
                        string checkLevelName = levelCurve.level;
                        if (checkLevelName != x.name)
                        {
                            continue;
                        }
                        curve = levelCurve.curve;
                        break;
                    }
                    return curve;
                };
            }
            else if (WildCardMod.Instance.ModConfig.mapObjectMinMax.TryGetValue(mapObjectName, out (ConfigEntry<int>, ConfigEntry<int>) minMax))
            {
                curve.keys[0].value = minMax.Item1.Value;
                curve.keys[1].value = minMax.Item2.Value;
            }
            return (x) => curve;
        }
    }
    [Serializable]
    public class LevelCurve
    {
        public string level;
        public AnimationCurve curve;
    }
    internal enum SkinType
    {
        Item,
        Enemy
    }
    [Serializable]
    public struct NameImagePair
    {
        public string name;
        public Texture2D image;
        internal static Dictionary<string, Texture2D> ConvertToDict(List<NameImagePair> pairs)
        {
            Dictionary<string, Texture2D> dict = new Dictionary<string, Texture2D>();
            for (int i = 0; i < pairs.Count; i++)
            {
                NameImagePair pair = pairs[i];
                if (!dict.TryAdd(pair.name, pair.image))
                {
                    WildCardMod.Instance.Log.LogDebug($"Name image pair of name \"{pair.name}\"");
                }
            }
            return dict;
        }
    }
}