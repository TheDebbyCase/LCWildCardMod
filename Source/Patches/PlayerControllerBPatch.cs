using GameNetcodeStuff;
using HarmonyLib;
using LCWildCardMod.Items;
using LCWildCardMod.Items.Fyrus;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    public static class PlayerControllerBPatch
    {
        [HarmonyPatch(nameof(PlayerControllerB.DamagePlayer))]
        [HarmonyPrefix]
        public static bool SavePlayerDamage(PlayerControllerB __instance, ref CauseOfDeath causeOfDeath, ref int damageNumber)
        {
            if (causeOfDeath != CauseOfDeath.Gunshots)
            {
                if (__instance.GetComponentInChildren<FyrusAttach>() != null)
                {
                    WildCardMod.Log.LogDebug($"Saving Player from {causeOfDeath}");
                    damageNumber = 0;
                }
                else if (__instance.isHoldingObject && __instance.currentlyHeldObjectServer.TryGetComponent<SmithHalo>(out SmithHalo haloRef) && haloRef.isExhausted == 0 && damageNumber >= __instance.health)
                {
                    WildCardMod.Log.LogDebug($"Saving Player from {causeOfDeath}");
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
                    WildCardMod.Log.LogDebug($"Saving Player from {causeOfDeath}");
                    return false;
                }
                else if (__instance.isHoldingObject && __instance.currentlyHeldObjectServer.TryGetComponent<SmithHalo>(out SmithHalo haloRef) && haloRef.isExhausted == 0)
                {
                    WildCardMod.Log.LogDebug($"Saving Player from {causeOfDeath}");
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
                WildCardMod.Log.LogDebug($"Preventing Fall Damage");
                __instance.fallValue = -10f;
                __instance.fallValueUncapped = -10f;
                cojiroRef.itemAnimator.Animator.SetBool("Floating", false);
            }
            else if (cojiroRef == null && __instance.fallValue >= -38f)
            {
                Cojiro[] cojiroList = UnityEngine.Object.FindObjectsOfType<Cojiro>();
                foreach (Cojiro cojiro in cojiroList)
                {
                    if (cojiro.previousPlayer == __instance && cojiro.currentUseCooldown > 0)
                    {
                        WildCardMod.Log.LogDebug($"Preventing Fall Damage");
                        __instance.fallValue = -10f;
                        __instance.fallValueUncapped = -10f;
                        cojiro.itemAnimator.Animator.SetBool("Floating", false);
                    }
                }
            }
            return true;
        }
    }
}