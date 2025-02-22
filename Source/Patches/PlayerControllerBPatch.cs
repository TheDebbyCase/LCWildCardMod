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
        public static bool SavePlayer(PlayerControllerB __instance, ref CauseOfDeath causeOfDeath, ref int damageNumber)
        {
            if (!(causeOfDeath == CauseOfDeath.Unknown || causeOfDeath == CauseOfDeath.Drowning || causeOfDeath == CauseOfDeath.Abandoned || causeOfDeath == CauseOfDeath.Inertia || causeOfDeath == CauseOfDeath.Suffocation))
            {
                if (__instance.isHoldingObject && __instance.currentlyHeldObjectServer.TryGetComponent<SmithHalo>(out SmithHalo haloRef) && haloRef.isExhausted == 0 && damageNumber >= __instance.health)
                {
                    WildCardMod.Log.LogDebug($"Saving Player from {causeOfDeath}");
                    __instance.health = damageNumber + 1;
                    haloRef.ExhaustHaloServerRpc();
                }
                if (__instance.GetComponentInChildren<FyrusAttach>() != null)
                {
                    damageNumber = 0;
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
                __instance.GetCurrentMaterialStandingOn();
                __instance.movementAudio.PlayOneShot(StartOfRound.Instance.playerHitGroundSoft, 1f);
                __instance.LandFromJumpServerRpc(false);
                cojiroRef.itemAnimator.Animator.SetBool("Floating", false);
                return false;
            }
            else if (cojiroRef == null && __instance.fallValue >= -38f)
            {
                Cojiro[] cojiroList = UnityEngine.Object.FindObjectsOfType<Cojiro>();
                foreach (Cojiro cojiro in cojiroList)
                {
                    if (cojiro.previousPlayer == __instance && cojiro.currentUseCooldown > 0)
                    {
                        WildCardMod.Log.LogDebug($"Preventing Fall Damage");
                        __instance.GetCurrentMaterialStandingOn();
                        __instance.movementAudio.PlayOneShot(StartOfRound.Instance.playerHitGroundSoft, 1f);
                        __instance.LandFromJumpServerRpc(false);
                        cojiro.itemAnimator.Animator.SetBool("Floating", false);
                        return false;
                    }
                }
            }
            return true;
        }
    }
}