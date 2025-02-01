using GameNetcodeStuff;
using HarmonyLib;
using LCWildCardMod.Items;
using System;
using System.Collections.Generic;
using System.Text;
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
                haloRef.ExhaustHalo();
            }
            return true;
        }
    }
}