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
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    public static class PlayerControllerBPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        static MethodInfo haloSaveMethod = AccessTools.Method(typeof(Extensions), nameof(Extensions.SaveIfHalo), new Type[] { typeof(PlayerControllerB) });
        static MethodInfo killPlayerMethod = AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer), new Type[] { typeof(Vector3), typeof(bool), typeof(CauseOfDeath), typeof(int), typeof(Vector3), typeof(bool) });
        static MethodInfo getHudMethod = AccessTools.Method(typeof(HUDManager), "get_Instance");
        static MethodInfo setCracksMethod = AccessTools.Method(typeof(HUDManager), nameof(HUDManager.SetCracksOnVisor), new Type[] { typeof(float) });
        static FieldInfo sinkingField = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.sinkingValue));
        static FieldInfo crouchingField = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.isCrouching));
        static FieldInfo healthField = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.health));
        static FieldInfo injuredField = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.criticallyInjured));
        [HarmonyPatch(nameof(PlayerControllerB.DamagePlayer))]
        [HarmonyPrefix]
        public static bool SavePlayerDamage(PlayerControllerB __instance, ref CauseOfDeath causeOfDeath, ref int damageNumber)
        {
            try
            {
                return !__instance.IsFyrusSaveable();
            }
            catch (Exception exception)
            {
                Log.LogError(exception);
            }
            return true;
        }
        [HarmonyPatch(nameof(PlayerControllerB.DamagePlayer))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SavePlayerDamage(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            LocalBuilder overkill = null;
            LocalBuilder healthOverride = null;
            Label? oldLabel1 = null;
            Label? oldLabel2 = null;
            Label? oldLabel3 = null;
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (i - 3 < 0 || i + 6 >= codes.Count)
                {
                    continue;
                }
                if (codes[i + 2].LoadsField(healthField) && codes[i].opcode.Equals(OpCodes.Ret) && codes[i + 3].IsLdarg(1) && codes[i + 4].opcode.Equals(OpCodes.Sub) && codes[i + 5].opcode.Equals(OpCodes.Ldc_I4_0) && codes[i - 1].Branches(out Label? oldLabel0) && codes[i + 6].Branches(out oldLabel1))
                {
                    List<CodeInstruction> newCode = new List<CodeInstruction>();
                    overkill = generator.DeclareLocal(typeof(bool));
                    healthOverride = generator.DeclareLocal(typeof(bool));
                    CodeInstruction jumpDest = new CodeInstruction(OpCodes.Ldarg_0);
                    jumpDest.labels.Add(oldLabel0.Value);
                    newCode.Add(jumpDest);
                    newCode.Add(new CodeInstruction(OpCodes.Ldfld, healthField));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_1));
                    newCode.Add(new CodeInstruction(OpCodes.Sub));
                    newCode.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
                    newCode.Add(new CodeInstruction(OpCodes.Cgt));
                    newCode.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
                    newCode.Add(new CodeInstruction(OpCodes.Ceq));
                    newCode.Add(new CodeInstruction(OpCodes.Stloc_S, (byte)overkill.LocalIndex));
                    newCode.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
                    newCode.Add(new CodeInstruction(OpCodes.Stloc_S, (byte)healthOverride.LocalIndex));
                    codes.RemoveRange(i + 1, 6);
                    codes.InsertRange(i + 1, newCode);
                    i += newCode.Count;
                }
                if (overkill != null && oldLabel1.HasValue && codes[i - 1].opcode.Equals(OpCodes.Ldc_I4_S) && codes[i - 1].OperandIs((byte)50) && codes[i].Branches(out oldLabel3) && codes[i - 3].Branches(out oldLabel2)) //
                {
                    List<CodeInstruction> newCode = new List<CodeInstruction>();
                    Label newLabel = generator.DefineLabel();
                    Label noSaveJump = generator.DefineLabel();
                    Label overridenJump = generator.DefineLabel();
                    newCode.Add(new CodeInstruction(OpCodes.Ldloc_S, (byte)overkill.LocalIndex));
                    newCode.Add(new CodeInstruction(OpCodes.Brfalse_S, oldLabel1.Value));
                    newCode.Add(new CodeInstruction(OpCodes.Ldc_I4_1));
                    newCode.Add(new CodeInstruction(OpCodes.Stloc_S, (byte)healthOverride.LocalIndex));
                    codes.InsertRange(i + 1, newCode);
                    i += newCode.Count;
                    newCode.Clear();
                    CodeInstruction elseIfCode = new CodeInstruction(OpCodes.Ldloc_S, (byte)overkill.LocalIndex);
                    elseIfCode.labels.Add(oldLabel1.Value);
                    elseIfCode.labels.Add(oldLabel2.Value);
                    elseIfCode.labels.Add(oldLabel3.Value);
                    newCode.Add(elseIfCode);
                    newCode.Add(new CodeInstruction(OpCodes.Brfalse_S, newLabel));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    newCode.Add(new CodeInstruction(OpCodes.Call, haloSaveMethod));
                    newCode.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
                    newCode.Add(new CodeInstruction(OpCodes.Ceq));
                    newCode.Add(new CodeInstruction(OpCodes.Stloc_S, (byte)overkill.LocalIndex));
                    newCode.Add(new CodeInstruction(OpCodes.Ldloc_S, (byte)overkill.LocalIndex));
                    newCode.Add(new CodeInstruction(OpCodes.Brtrue_S, noSaveJump));
                    newCode.Add(new CodeInstruction(OpCodes.Ldc_I4_1));
                    newCode.Add(new CodeInstruction(OpCodes.Stloc_S, (byte)healthOverride.LocalIndex));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    newCode.Add(new CodeInstruction(OpCodes.Ldc_I4_1));
                    newCode.Add(new CodeInstruction(OpCodes.Stfld, healthField));
                    CodeInstruction newIf = new CodeInstruction(OpCodes.Ldloca_S, (byte)healthOverride.LocalIndex);
                    newIf.labels.Add(newLabel);
                    newIf.labels.Add(noSaveJump);
                    newCode.Add(newIf);
                    newCode.Add(new CodeInstruction(OpCodes.Brfalse_S, overridenJump));
                    codes[i + 5].labels.Clear();
                    codes[i + 14].labels.Add(overridenJump);
                    codes.InsertRange(i + 1, newCode);
                    i += newCode.Count;
                }
                if (overkill != null && oldLabel1.HasValue && oldLabel2.HasValue && oldLabel3.HasValue && codes[i].Calls(killPlayerMethod))
                {
                    List<CodeInstruction> newCode = new List<CodeInstruction>();
                    Label overkillJump = generator.DefineLabel();
                    newCode.Add(new CodeInstruction(OpCodes.Ldloc_S, (byte)overkill.LocalIndex));
                    newCode.Add(new CodeInstruction(OpCodes.Brfalse_S, overkillJump));
                    codes[i + 16].labels.Add(overkillJump);
                    codes.InsertRange(i + 2, newCode);
                    break;
                }
            }
            return codes.AsEnumerable();
        }
        [HarmonyPatch(nameof(PlayerControllerB.KillPlayer))]
        [HarmonyPrefix]
        public static bool SavePlayerKill(PlayerControllerB __instance, ref Vector3 bodyVelocity, ref bool spawnBody, ref CauseOfDeath causeOfDeath)
        {
            try
            {
                if (!__instance.IsSaveable(out bool starSave, out SmithHalo haloRef))
                {
                    return true;
                }
                if (causeOfDeath != CauseOfDeath.Unknown && spawnBody)
                {
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
            }
            catch (Exception exception)
            {
                Log.LogError(exception);
            }
            return true;
        }
        [HarmonyPatch(nameof(PlayerControllerB.PlayerHitGroundEffects))]
        [HarmonyPrefix]
        public static bool PreventFallDamage(PlayerControllerB __instance)
        {
            try
            {
                Cojiro cojiroRef = null;
                if (__instance.isHoldingObject && __instance.currentlyHeldObjectServer.TryGetComponent(out cojiroRef) && cojiroRef.isFloating && __instance.fallValue >= -38f)
                {
                    Log.LogDebug($"Cojiro Preventing Fall Damage");
                    __instance.fallValue = -10f;
                    __instance.fallValueUncapped = -10f;
                    cojiroRef.itemAnimator.Animator.SetBool("Floating", false);
                }
                else if (cojiroRef == null && __instance.fallValue >= -38f)
                {
                    for (int i = 0; i < __instance.ItemSlots.Length; i++)
                    {
                        if (__instance.ItemSlots[i] == null)
                        {
                            continue;
                        }
                        if (!__instance.ItemSlots[i].TryGetComponent(out cojiroRef))
                        {
                            continue;
                        }
                        if (cojiroRef.previousPlayer == __instance && cojiroRef.currentUseCooldown > 0)
                        {
                            Log.LogDebug($"Cojiro Preventing Fall Damage");
                            __instance.fallValue = -10f;
                            __instance.fallValueUncapped = -10f;
                            cojiroRef.itemAnimator.Animator.SetBool("Floating", false);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Log.LogError(exception);
            }
            return true;
        }
        [HarmonyPatch(nameof(PlayerControllerB.CheckConditionsForSinkingInQuicksand))]
        [HarmonyPostfix]
        public static void AntiQuicksandStar(PlayerControllerB __instance, ref bool __result)
        {
            try
            {
                if (!__result)
                {
                    return;
                }
                __result = !__instance.IsFyrusSaveable();
            }
            catch (Exception exception)
            {
                Log.LogError(exception);
            }
        }
        [HarmonyPatch(nameof(PlayerControllerB.Update))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> HaloSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(killPlayerMethod) && codes[i + 10].LoadsField(crouchingField) && codes[i - 12].LoadsField(sinkingField))
                {
                    List<CodeInstruction> newCode = new List<CodeInstruction>();
                    Label killLabel = generator.DefineLabel();
                    Label skipLabel = generator.DefineLabel();
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    newCode.Add(new CodeInstruction(OpCodes.Call, haloSaveMethod));
                    newCode.Add(new CodeInstruction(OpCodes.Brfalse_S, killLabel));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    newCode.Add(new CodeInstruction(OpCodes.Ldc_R4, 0f));
                    newCode.Add(new CodeInstruction(OpCodes.Stfld, sinkingField));
                    newCode.Add(new CodeInstruction(OpCodes.Br_S, skipLabel));
                    codes[i - 9].labels.Add(killLabel);
                    codes[i + 9].labels.Add(skipLabel);
                    codes.InsertRange(i - 9, newCode);
                    break;
                }
            }
            return codes.AsEnumerable();
        }
    }
}