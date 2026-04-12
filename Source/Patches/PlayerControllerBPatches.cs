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
using UnityEngine.InputSystem;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    public static class PlayerControllerBPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        static MethodInfo haloSaveMethod = AccessTools.Method(typeof(Extensions), nameof(Extensions.SaveIfHalo), new Type[] { typeof(PlayerControllerB) });
        static MethodInfo killPlayerMethod = AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer), new Type[] { typeof(Vector3), typeof(bool), typeof(CauseOfDeath), typeof(int), typeof(Vector3), typeof(bool) });
        static FieldInfo sinkingField = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.sinkingValue));
        static FieldInfo crouchingField = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.isCrouching));
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
                if (__instance.isHoldingObject && __instance.currentlyHeldObjectServer.TryGetComponent<Cojiro>(out cojiroRef) && cojiroRef.isFloating && __instance.fallValue >= -38f)
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
                        if (!__instance.ItemSlots[i].TryGetComponent<Cojiro>(out cojiroRef))
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