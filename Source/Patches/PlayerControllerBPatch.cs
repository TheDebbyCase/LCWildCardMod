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
            if (__instance.isHoldingObject && __instance.currentlyHeldObjectServer.TryGetComponent<Halo>(out Halo haloRef) && haloRef.isExhausted == 0 && !(causeOfDeath == CauseOfDeath.Unknown || causeOfDeath == CauseOfDeath.Drowning || causeOfDeath == CauseOfDeath.Abandoned || causeOfDeath == CauseOfDeath.Inertia || causeOfDeath == CauseOfDeath.Suffocation) && damageNumber >= __instance.health)
            {
                WildCardMod.Log.LogDebug($"Saving Player from {causeOfDeath}");
                __instance.health = damageNumber + 1;
                haloRef.ExhaustHaloServerRpc();
            }
            return true;
        }
        [HarmonyPatch("Discard_performed")]
        [HarmonyPrefix]
        public static bool PreventDropping(PlayerControllerB __instance)
        {
            if (__instance.isHoldingObject && __instance.currentlyHeldObjectServer.TryGetComponent<Halo>(out Halo haloRef) && haloRef.isThrowing)
            {
                WildCardMod.Log.LogDebug($"Preventing Drop");
                return false;
            }
            else
            {
                return true;
            }
        }
        [HarmonyPatch("ScrollMouse_performed")]
        [HarmonyPrefix]
        public static bool PreventSwitching(PlayerControllerB __instance)
        {
            if (__instance.isHoldingObject && __instance.currentlyHeldObjectServer.TryGetComponent<Halo>(out Halo haloRef) && haloRef.isThrowing)
            {
                WildCardMod.Log.LogDebug($"Preventing Switch");
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}