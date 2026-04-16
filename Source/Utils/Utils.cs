using GameNetcodeStuff;
using HarmonyLib;
using LCWildCardMod.Config;
using LCWildCardMod.Items.Fyrus;
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
    internal static class TranspilerHelper
    {
        internal static MethodInfo toString = AccessTools.Method(typeof(object), nameof(object.ToString));
        internal static MethodInfo stringConcat3 = AccessTools.Method(typeof(string), nameof(string.Concat), new Type[] { typeof(string), typeof(string), typeof(string) });
        internal static MethodInfo logString = AccessTools.Method(typeof(TranspilerHelper), nameof(LogString), new Type[] { typeof(string) });
        internal static MethodInfo inequality = AccessTools.Method(typeof(UnityEngine.Object), "op_Inequality", new Type[] { typeof(UnityEngine.Object), typeof(UnityEngine.Object) });
        internal static MethodInfo mathfClamp3Int = AccessTools.Method(typeof(Mathf), nameof(Mathf.Clamp), new Type[] { typeof(int), typeof(int), typeof(int) });
        internal static MethodInfo collision = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.MeetsStandardPlayerCollisionConditions), new Type[] { typeof(Collider), typeof(bool), typeof(bool) });
        internal static MethodInfo exitDriver = AccessTools.Method(typeof(VehicleController), nameof(VehicleController.ExitDriverSideSeat));
        internal static MethodInfo exitPassenger = AccessTools.Method(typeof(VehicleController), nameof(VehicleController.ExitPassengerSideSeat));
        internal static MethodInfo fyrusSave = AccessTools.Method(typeof(Extensions), nameof(Extensions.SaveIfFyrus), new Type[] { typeof(PlayerControllerB), typeof(EnemyAI) });
        internal static MethodInfo haloSave = AccessTools.Method(typeof(Extensions), nameof(Extensions.SaveIfHalo), new Type[] { typeof(PlayerControllerB) });
        internal static MethodInfo anySave = AccessTools.Method(typeof(Extensions), nameof(Extensions.SaveIfAny), new Type[] { typeof(PlayerControllerB) });
        internal static MethodInfo killPlayer = AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer), new Type[] { typeof(Vector3), typeof(bool), typeof(CauseOfDeath), typeof(int), typeof(Vector3), typeof(bool) });
        internal static MethodInfo damagePlayer = AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.DamagePlayer), new Type[] { typeof(int), typeof(bool), typeof(bool), typeof(CauseOfDeath), typeof(int), typeof(bool), typeof(Vector3) });
        internal static MethodInfo makeInjured = AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.MakeCriticallyInjured), new Type[] { typeof(bool) });
        internal static MethodInfo switchBehaviour = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.SwitchToBehaviourState), new Type[] { typeof(int) });
        internal static MethodInfo switchBehaviourLocal = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.SwitchToBehaviourStateOnLocalClient), new Type[] { typeof(int) });
        internal static MethodInfo setSpeed = AccessTools.Method(typeof(NavMeshAgent), "set_speed", new Type[] { typeof(float) });
        internal static MethodInfo allowDeath = AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.AllowPlayerDeath));
        internal static MethodInfo cancelSpecialAnim = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.CancelSpecialAnimationWithPlayer));
        internal static MethodInfo beeKill = AccessTools.Method(typeof(RedLocustBees), nameof(RedLocustBees.BeeKillPlayerOnLocalClient), new Type[] { typeof(int) });
        internal static MethodInfo gameNetworkInstance = AccessTools.Method(typeof(GameNetworkManager), "get_Instance");
        internal static MethodInfo foxCancelReel = AccessTools.Method(typeof(BushWolfEnemy), nameof(BushWolfEnemy.CancelReelingPlayerIn));
        internal static MethodInfo cadaverCure = AccessTools.Method(typeof(CadaverGrowthAI), nameof(CadaverGrowthAI.CurePlayer), new Type[] { typeof(int) });
        internal static MethodInfo cadaverCureRPC = AccessTools.Method(typeof(CadaverGrowthAI), nameof(CadaverGrowthAI.CurePlayerRpc), new Type[] { typeof(int) });
        internal static MethodInfo giantGrabRPC = AccessTools.Method(typeof(ForestGiantAI), nameof(ForestGiantAI.GrabPlayerServerRpc), new Type[] { typeof(int) });
        internal static FieldInfo playerName = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.playerUsername));
        internal static FieldInfo playerClientId = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.playerClientId));
        internal static FieldInfo enemyType = AccessTools.Field(typeof(EnemyAI), nameof(EnemyAI.enemyType));
        internal static FieldInfo enemyName = AccessTools.Field(typeof(EnemyType), nameof(EnemyType.enemyName));
        internal static FieldInfo foxInKill = AccessTools.Field(typeof(BushWolfEnemy), nameof(BushWolfEnemy.inKillAnimation));
        internal static FieldInfo foxDragging = AccessTools.Field(typeof(BushWolfEnemy), nameof(BushWolfEnemy.dragging));
        internal static FieldInfo hasBurst = AccessTools.Field(typeof(CadaverBloomAI), nameof(CadaverBloomAI.hasBurst));
        internal static FieldInfo clingPlayer = AccessTools.Field(typeof(CentipedeAI), nameof(CentipedeAI.clingingToPlayer));
        internal static FieldInfo timesSeenByPlayer = AccessTools.Field(typeof(DressGirlAI), nameof(DressGirlAI.timesSeenByPlayer));
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
        internal static FieldInfo beeZapMode = AccessTools.Field(typeof(RedLocustBees), nameof(RedLocustBees.beesZappingMode));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogString(string toLog)
        {
            WildCardMod.Instance.Log.LogDebug(toLog);
        }
        internal static List<CodeInstruction> DebugString(string toLog, params Label[] labelsStart)
        {
            List<CodeInstruction> instructions = new List<CodeInstruction>();
            instructions.Add(new CodeInstruction(OpCodes.Ldstr, toLog));
            if (labelsStart != null)
            {
                instructions[0].labels.AddRange(labelsStart);
            }
            instructions.Add(new CodeInstruction(OpCodes.Call, logString));
            return instructions;
        }
        internal static List<CodeInstruction> DebugLoad<T>(string toLog, OpCode opcode, object operand = null, params Label[] labelsStart)
        {
            List<CodeInstruction> instructions = new List<CodeInstruction>();
            instructions.Add(new CodeInstruction(OpCodes.Ldstr, toLog));
            if (labelsStart != null)
            {
                instructions[0].labels.AddRange(labelsStart);
            }
            instructions.Add(new CodeInstruction(OpCodes.Ldstr, ": "));
            instructions.Add(new CodeInstruction(opcode, operand));
            instructions.Add(new CodeInstruction(OpCodes.Box, typeof(T)));
            instructions.Add(new CodeInstruction(OpCodes.Callvirt, toString));
            instructions.Add(new CodeInstruction(OpCodes.Call, stringConcat3));
            instructions.Add(new CodeInstruction(OpCodes.Call, logString));
            return instructions;
        }
        internal static List<CodeInstruction> DebugLoad<T>(string toLog, IEnumerable<CodeInstruction> loadInstructions, params Label[] labelsStart)
        {
            List<CodeInstruction> instructions = new List<CodeInstruction>();
            instructions.Add(new CodeInstruction(OpCodes.Ldstr, toLog));
            if (labelsStart != null)
            {
                instructions[0].labels.AddRange(labelsStart);
            }
            instructions.Add(new CodeInstruction(OpCodes.Ldstr, ": "));
            instructions.AddRange(loadInstructions);
            instructions.Add(new CodeInstruction(OpCodes.Box, typeof(T)));
            instructions.Add(new CodeInstruction(OpCodes.Callvirt, toString));
            instructions.Add(new CodeInstruction(OpCodes.Call, stringConcat3));
            instructions.Add(new CodeInstruction(OpCodes.Call, logString));
            return instructions;
        }
        internal static List<CodeInstruction> DebugLoadFromThis<T>(string toLog, IEnumerable<CodeInstruction> loadInstructions, params Label[] labelsStart)
        {
            List<CodeInstruction> instructions = new List<CodeInstruction>();
            instructions.Add(new CodeInstruction(OpCodes.Ldstr, toLog));
            if (labelsStart != null)
            {
                instructions[0].labels.AddRange(labelsStart);
            }
            instructions.Add(new CodeInstruction(OpCodes.Ldstr, ": "));
            instructions.Add(new CodeInstruction(OpCodes.Ldarg, 0));
            instructions.AddRange(loadInstructions);
            instructions.Add(new CodeInstruction(OpCodes.Box, typeof(T)));
            instructions.Add(new CodeInstruction(OpCodes.Callvirt, toString));
            instructions.Add(new CodeInstruction(OpCodes.Call, stringConcat3));
            instructions.Add(new CodeInstruction(OpCodes.Call, logString));
            return instructions;
        }
        internal static List<CodeInstruction> DebugLoadFromThis<T>(string toLog, OpCode opcode, object operand = null, params Label[] labelsStart)
        {
            List<CodeInstruction> instructions = new List<CodeInstruction>();
            instructions.Add(new CodeInstruction(OpCodes.Ldstr, toLog));
            if (labelsStart != null)
            {
                instructions[0].labels.AddRange(labelsStart);
            }
            instructions.Add(new CodeInstruction(OpCodes.Ldstr, ": "));
            instructions.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
            instructions.Add(new CodeInstruction(opcode, operand));
            instructions.Add(new CodeInstruction(OpCodes.Box, typeof(T)));
            instructions.Add(new CodeInstruction(OpCodes.Callvirt, toString));
            instructions.Add(new CodeInstruction(OpCodes.Call, stringConcat3));
            instructions.Add(new CodeInstruction(OpCodes.Call, logString));
            return instructions;
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
            if (store.opcode.Equals(OpCodes.Stloc_0))
            {
                load = new CodeInstruction(OpCodes.Ldloc_S, 0);
            }
            else if (store.opcode.Equals(OpCodes.Stloc_1))
            {
                load = new CodeInstruction(OpCodes.Ldloc_S, 1);
            }
            else if (store.opcode.Equals(OpCodes.Stloc_2))
            {
                load = new CodeInstruction(OpCodes.Ldloc_S, 2);
            }
            else if (store.opcode.Equals(OpCodes.Stloc_3))
            {
                load = new CodeInstruction(OpCodes.Ldloc_S, 3);
            }
            else if (store.opcode.Equals(OpCodes.Stloc_S) || store.opcode.Equals(OpCodes.Ldloc))
            {
                load = new CodeInstruction(OpCodes.Ldloc_S, operand);
            }
            else if (store.opcode.Equals(OpCodes.Starg_S) || store.opcode.Equals(OpCodes.Starg))
            {
                load = new CodeInstruction(OpCodes.Ldarg_S, operand);
            }
            else if (store.opcode.Equals(OpCodes.Stfld) || store.opcode.Equals(OpCodes.Stsfld))
            {
                load = new CodeInstruction(OpCodes.Ldfld, operand);
            }
            else if (store.opcode.Equals(OpCodes.Stobj))
            {
                load = new CodeInstruction(OpCodes.Ldobj, operand);
            }
            return load;
        }
        internal static CodeInstruction LoadToStore(CodeInstruction load)
        {
            object operand = load.operand;
            CodeInstruction store = new CodeInstruction(OpCodes.Nop);
            if (load.opcode.Equals(OpCodes.Ldloc_0))
            {
                store = new CodeInstruction(OpCodes.Stloc_S, 0);
            }
            else if (load.opcode.Equals(OpCodes.Ldloc_1))
            {
                store = new CodeInstruction(OpCodes.Stloc_S, 1);
            }
            else if (load.opcode.Equals(OpCodes.Ldloc_2))
            {
                store = new CodeInstruction(OpCodes.Stloc_S, 2);
            }
            else if (load.opcode.Equals(OpCodes.Ldloc_3))
            {
                store = new CodeInstruction(OpCodes.Stloc_S, 3);
            }
            else if (load.opcode.Equals(OpCodes.Ldloc_S) || load.opcode.Equals(OpCodes.Ldloc))
            {
                store = new CodeInstruction(OpCodes.Stloc_S, operand);
            }
            else if (load.opcode.Equals(OpCodes.Ldarg_0))
            {
                store = new CodeInstruction(OpCodes.Starg_S, 0);
            }
            else if (load.opcode.Equals(OpCodes.Ldarg_1))
            {
                store = new CodeInstruction(OpCodes.Starg_S, 1);
            }
            else if (load.opcode.Equals(OpCodes.Ldarg_2))
            {
                store = new CodeInstruction(OpCodes.Starg_S, 2);
            }
            else if (load.opcode.Equals(OpCodes.Ldarg_3))
            {
                store = new CodeInstruction(OpCodes.Starg_S, 3);
            }
            else if (load.opcode.Equals(OpCodes.Ldarg_S) || load.opcode.Equals(OpCodes.Ldarg))
            {
                store = new CodeInstruction(OpCodes.Starg_S, operand);
            }
            else if (load.opcode.Equals(OpCodes.Ldfld))
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
            else if (load.opcode.Equals(OpCodes.Ldobj))
            {
                store = new CodeInstruction(OpCodes.Stobj, operand);
            }
            return store;
        }
        internal static bool AreLoadEqual(CodeInstruction load1, CodeInstruction load2)
        {
            int? load1Operand = load1.operand as int?;
            int? load2Operand = load2.operand as int?;
            if (load1.IsLdloc() && load2.IsLdloc())
            {
                if (load1.opcode.Equals(load2.opcode) && load1.OperandIs(load2.operand))
                {
                    return true;
                }
                else if (load1.opcode.Equals(OpCodes.Ldloc) || load1.opcode.Equals(OpCodes.Ldloc_S))
                {
                    return load1.OperandIs(load2.operand);
                }
                else if (load1.opcode.Equals(OpCodes.Ldloc_0))
                {
                    return load2.opcode.Equals(OpCodes.Ldloc_0) || (load2Operand.HasValue && load2Operand.Value == 0);
                }
                else if (load1.opcode.Equals(OpCodes.Ldloc_1))
                {
                    return load2.opcode.Equals(OpCodes.Ldloc_1) || (load2Operand.HasValue && load2Operand.Value == 1);
                }
                else if (load1.opcode.Equals(OpCodes.Ldloc_2))
                {
                    return load2.opcode.Equals(OpCodes.Ldloc_2) || (load2Operand.HasValue && load2Operand.Value == 2);
                }
                else if (load1.opcode.Equals(OpCodes.Ldloc_3))
                {
                    return load2.opcode.Equals(OpCodes.Ldloc_3) || (load2Operand.HasValue && load2Operand.Value == 3);
                }
            }
            else if ((load1.opcode.Equals(OpCodes.Ldloca) || load1.opcode.Equals(OpCodes.Ldloca_S)) && (load2.opcode.Equals(OpCodes.Ldloca) || load2.opcode.Equals(OpCodes.Ldloca_S)))
            {
                return load1.OperandIs(load2.operand);
            }
            else if (load1.IsLdarg() && load2.IsLdarg())
            {
                return (load2Operand.HasValue && load1.IsLdarg(load2Operand.Value)) || (load1Operand.HasValue && load2.IsLdarg(load1Operand.Value));
            }
            else if (load1.IsLdarga() && load2.IsLdarga())
            {
                return load1.OperandIs(load2.operand);
            }
            return load1.opcode.Equals(load2.opcode) && load1.OperandIs(load2.operand);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void TestMethod()
        {
            
        }
    }
    //[HarmonyPatch(typeof(TranspilerHelper))]
    //public static class TestPatches
    //{
    //    [HarmonyPatch(nameof(TranspilerHelper.TestMethod))]
    //    [HarmonyTranspiler]
    //    public static IEnumerable<CodeInstruction> TestTranspiler(IEnumerable<CodeInstruction> instructions)
    //    {
    //        List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
    //        for (int i = 0; i < codes.Count; i++)
    //        {
    //            object operand = codes[i].operand;
    //            if (operand != null)
    //            {
    //                MethodInfo method = operand as MethodInfo;
    //                FieldInfo field = null;
    //                if (method == null)
    //                {
    //                    field = operand as FieldInfo;
    //                }
    //                WildCardMod.Instance.Log.LogDebug($"{codes[i]} OPERAND ({operand.GetType()}): {operand}");
    //                if (method != null)
    //                {
    //                    WildCardMod.Instance.Log.LogDebug($"Returns: {method.ReturnParameter.ParameterType}, Generic?: {method.IsGenericMethod}, Virtual?: {method.IsVirtual}, Declared by: {method.DeclaringType}");
    //                }
    //                else if (field != null)
    //                {
    //                    WildCardMod.Instance.Log.LogDebug($"Declared by: {field.DeclaringType}, Type: {field.FieldType}");
    //                }
    //            }
    //            else
    //            {
    //                WildCardMod.Instance.Log.LogDebug($"{codes[i]}");
    //            }
    //        }
    //        return codes;
    //    }
    //}
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
            OnRoundStart.Invoke();
        }
        internal static void RoundEnded()
        {
            if (!roundStarted)
            {
                return;
            }
            OnRoundEnd.Invoke();
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
        static List<BepInEx.Configuration.ConfigEntry<int>> ConfigChances => WildCardMod.Instance.ModConfig.skinApplyChance;
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
            Skin skinToApply = null;
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
                            if (!(skin.target is EnemyType))
                            {
                                continue;
                            }
                            if ((skin.target as EnemyType).enemyName == target)
                            {
                                potentialSkins.Add(skin);
                            }
                            break;
                        }
                    case SkinType.Item:
                        {
                            if (!(skin.target is Item))
                            {
                                continue;
                            }
                            if ((skin.target as Item).itemName == target)
                            {
                                potentialSkins.Add(skin);
                            }
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
                int index = WildCardMod.Instance.skinList.IndexOf(potentialSkins[i]);
                if (ConfigChances[index].Value <= 0)
                {
                    Log.LogDebug($"Skin \"{potentialSkins[i].skinName}\" was disabled!");
                    potentialSkins.Remove(potentialSkins[i]);
                    i--;
                    continue;
                }
                Log.LogDebug($"Adding skin \"{potentialSkins[i].skinName}\"'s chance weight!");
                skinsWeight += ConfigChances[index].Value;
                nothingWeight += 100 - ConfigChances[index].Value;
            }
            float applyChance = (float)random.NextDouble();
            Log.LogDebug($"Rolling to see if a skin will be applied!");
            if (((float)nothingWeight / (float)(nothingWeight + skinsWeight)) < applyChance)
            {
                for (int i = 0; i < potentialSkins.Count; i++)
                {
                    Log.LogDebug($"Rolling to see if \"{potentialSkins[i].skinName}\" is selected!");
                    if (ConfigChances[WildCardMod.Instance.skinList.IndexOf(potentialSkins[i])].Value / skinsWeight >= applyChance)
                    {
                        Log.LogDebug($"Skin \"{potentialSkins[i].skinName}\" was selected!");
                        skinToApply = potentialSkins[i];
                        break;
                    }
                }
            }
            return skinToApply;
        }
    }
    [CreateAssetMenu(menuName = "WCScriptableObjects/Skin", order = 1)]
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
    [CreateAssetMenu(menuName = "WCScriptableObjects/MapObject", order = 1)]
    public class MapObject : ScriptableObject
    {
        public string mapObjectName;
        public SpawnableMapObject spawnableMapObject;
        public Func<SelectableLevel, AnimationCurve> curveFunc;
        public List<LevelCurve> levelCurves;
        public bool autoHandle;
    }
    [Serializable]
    public class LevelCurve
    {
        public string level;
        public AnimationCurve curve;
    }
    internal static class MapObjectHelper
    {
        static WildCardConfig ModConfig => WildCardMod.Instance.ModConfig;
        static List<MapObject> MapObjects => WildCardMod.Instance.mapObjectsList;
        static List<MapObject> AutoMapObjects => WildCardMod.Instance.autoMapObjectsList;
        static int mapIndex = 0;
        internal static AnimationCurve MapObjectFunc(SelectableLevel level)
        {
            AnimationCurve curve;
            List<MapObject> maps = MapObjects;
            maps.AddRange(AutoMapObjects);
            if (maps[mapIndex].autoHandle || (ModConfig.useDefaultMapObjectCurve.Count > mapIndex && ModConfig.useDefaultMapObjectCurve[mapIndex].Value))
            {
                List<string> levelsList = new List<string>();
                for (int i = 0; i < maps[mapIndex].levelCurves.Count; i++)
                {
                    levelsList.Add(maps[mapIndex].levelCurves[i].level);
                }
                for (int i = 0; i < maps[mapIndex].levelCurves.Count; i++)
                {
                    LevelCurve levelCurve = maps[mapIndex].levelCurves[i];
                    if (!levelsList.Contains(levelCurve.level))
                    {
                        continue;
                    }
                    else if (levelCurve.level == level.name)
                    {
                        curve = levelCurve.curve;
                        mapIndex++;
                        return curve;
                    }
                }
                curve = maps[mapIndex].spawnableMapObject.numberToSpawn;
                mapIndex++;
                return curve;
            }
            else if (ModConfig.mapObjectMinNo.Count > mapIndex && ModConfig.mapObjectMaxNo.Count > mapIndex)
            {
                curve = new AnimationCurve(new Keyframe(0, ModConfig.mapObjectMinNo[mapIndex].Value), new Keyframe(1, ModConfig.mapObjectMaxNo[mapIndex].Value));
                mapIndex++;
                return curve;
            }
            else
            {
                curve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0f, 0f));
                mapIndex++;
                return curve;
            }
        }
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