﻿using GameNetcodeStuff;
using HarmonyLib;
using LCWildCardMod.Items;
using LCWildCardMod.Items.Fyrus;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    public static class PlayerControllerBPatch
    {
        static readonly BepInEx.Logging.ManualLogSource log = WildCardMod.Log;
        [HarmonyPatch(nameof(PlayerControllerB.DamagePlayer))]
        [HarmonyPrefix]
        public static bool SavePlayerDamage(PlayerControllerB __instance, ref CauseOfDeath causeOfDeath, ref int damageNumber)
        {
            if (causeOfDeath != CauseOfDeath.Gunshots)
            {
                if (__instance.GetComponentInChildren<FyrusAttach>() != null)
                {
                    log.LogDebug($"FyrusStar Saving Player from {causeOfDeath}");
                    damageNumber = 0;
                }
                else if (__instance.isHoldingObject && __instance.currentlyHeldObjectServer.TryGetComponent<SmithHalo>(out SmithHalo haloRef) && haloRef.isExhausted == 0 && damageNumber >= __instance.health)
                {
                    log.LogDebug($"Halo Saving Player from {causeOfDeath}");
                    __instance.health = damageNumber + 1;
                    if (haloRef.exhaustCoroutine == null)
                    {
                        haloRef.ExhaustHaloServerRpc();
                    }
                }
            }
            return true;
        }
        [HarmonyPatch(nameof(PlayerControllerB.KillPlayer))]
        [HarmonyPrefix]
        public static bool SavePlayerKill(PlayerControllerB __instance, ref CauseOfDeath causeOfDeath)
        {
            if (causeOfDeath == CauseOfDeath.Gunshots)
            {
                if (__instance.GetComponentInChildren<FyrusAttach>() != null)
                {
                    log.LogDebug($"FyrusStar Saving Player from {causeOfDeath}");
                    return false;
                }
                else if (__instance.isHoldingObject && __instance.currentlyHeldObjectServer.TryGetComponent<SmithHalo>(out SmithHalo haloRef) && haloRef.isExhausted == 0)
                {
                    log.LogDebug($"Halo Saving Player from {causeOfDeath}");
                    __instance.health = 1;
                    if (haloRef.exhaustCoroutine == null)
                    {
                        haloRef.ExhaustHaloServerRpc();
                    }
                    return false;
                }
            }
            return true;
        }
        [HarmonyPatch(nameof(PlayerControllerB.PlayerHitGroundEffects))]
        [HarmonyPrefix]
        public static bool PreventFallDamage(PlayerControllerB __instance)
        {
            Cojiro cojiroRef = null;
            if (__instance.isHoldingObject && __instance.currentlyHeldObjectServer.TryGetComponent<Cojiro>(out cojiroRef) && cojiroRef.isFloating && __instance.fallValue >= -38f)
            {
                log.LogDebug($"Cojiro Preventing Fall Damage");
                __instance.fallValue = -10f;
                __instance.fallValueUncapped = -10f;
                cojiroRef.itemAnimator.Animator.SetBool("Floating", false);
            }
            else if (cojiroRef == null && __instance.fallValue >= -38f)
            {
                Cojiro[] cojiroList = UnityEngine.Object.FindObjectsOfType<Cojiro>();
                for (int i = 0; i < cojiroList.Length; i++)
                {
                    if (cojiroList[i].previousPlayer == __instance && cojiroList[i].currentUseCooldown > 0)
                    {
                        log.LogDebug($"Cojiro Preventing Fall Damage");
                        __instance.fallValue = -10f;
                        __instance.fallValueUncapped = -10f;
                        cojiroList[i].itemAnimator.Animator.SetBool("Floating", false);
                    }
                }
            }
            return true;
        }
    }
}