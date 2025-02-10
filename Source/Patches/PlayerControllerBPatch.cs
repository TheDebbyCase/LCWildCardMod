using GameNetcodeStuff;
using HarmonyLib;
using LCWildCardMod.Items;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    public static class PlayerControllerBPatch
    {
        [HarmonyPatch(nameof(PlayerControllerB.DamagePlayer))]
        [HarmonyPrefix]
        public static bool SavePlayer(PlayerControllerB __instance, ref CauseOfDeath causeOfDeath, ref int damageNumber)
        {
            if (__instance.isHoldingObject && __instance.currentlyHeldObjectServer.TryGetComponent<SmithHalo>(out SmithHalo haloRef) && haloRef.isExhausted == 0 && !(causeOfDeath == CauseOfDeath.Unknown || causeOfDeath == CauseOfDeath.Drowning || causeOfDeath == CauseOfDeath.Abandoned || causeOfDeath == CauseOfDeath.Inertia || causeOfDeath == CauseOfDeath.Suffocation) && damageNumber >= __instance.health)
            {
                WildCardMod.Log.LogDebug($"Saving Player from {causeOfDeath}");
                __instance.health = damageNumber + 1;
                haloRef.ExhaustHaloServerRpc();
            }
            return true;
        }
        [HarmonyPatch(nameof(PlayerControllerB.Discard_performed))]
        [HarmonyPrefix]
        public static bool PreventDropping(PlayerControllerB __instance)
        {
            if (__instance.isHoldingObject && __instance.currentlyHeldObjectServer.TryGetComponent<SmithHalo>(out SmithHalo haloRef) && haloRef.isThrowing)
            {
                WildCardMod.Log.LogDebug($"Preventing Drop");
                return false;
            }
            else
            {
                return true;
            }
        }
        [HarmonyPatch(nameof(PlayerControllerB.ScrollMouse_performed))]
        [HarmonyPrefix]
        public static bool PreventSwitching(PlayerControllerB __instance)
        {
            if (__instance.isHoldingObject && __instance.currentlyHeldObjectServer.TryGetComponent<SmithHalo>(out SmithHalo haloRef) && haloRef.isThrowing)
            {
                WildCardMod.Log.LogDebug($"Preventing Switch");
                return false;
            }
            else
            {
                return true;
            }
        }
        [HarmonyPatch(nameof(PlayerControllerB.PlayerHitGroundEffects))]
        [HarmonyPrefix]
        public static bool PreventFallDamage(PlayerControllerB __instance)
        {
            if (__instance.isHoldingObject && __instance.currentlyHeldObjectServer.TryGetComponent<Cojiro>(out Cojiro cojiroRef) && cojiroRef.isFloating)
            {
                WildCardMod.Log.LogDebug($"Preventing Fall Damage");
                __instance.GetCurrentMaterialStandingOn();
                __instance.movementAudio.PlayOneShot(StartOfRound.Instance.playerHitGroundSoft, 1f);
                __instance.LandFromJumpServerRpc(false);
                return false;
            }
            return true;
        }
    }
}