//using GameNetcodeStuff;
//using HarmonyLib;
//using LCWildCardMod.Items;
//using System;
//using System.Collections.Generic;
//using System.Text;
//namespace LCWildCardMod.Patches
//{
//    [HarmonyPatch(typeof(PlayerControllerB))]
//    public static class PlayerControllerBPatch
//    {
//        [HarmonyPatch(nameof(PlayerControllerB.DamagePlayer))]
//        [HarmonyPrefix]
//        public static bool SavePlayer(PlayerControllerB __instance, ref CauseOfDeath causeOfDeath, ref int damageNumber)
//        {
//            WildCardMod.Log.LogDebug($"{causeOfDeath}");
//            if (__instance.isHoldingObject && __instance.currentlyHeldObjectServer.TryGetComponent<SmithHalo>(out _) && !(causeOfDeath == CauseOfDeath.Unknown || causeOfDeath == CauseOfDeath.Drowning || causeOfDeath == CauseOfDeath.Abandoned || causeOfDeath == CauseOfDeath.Inertia || causeOfDeath == CauseOfDeath.Suffocation))
//            {
//                WildCardMod.Log.LogDebug("Saving Player");
//                if (damageNumber >= __instance.health)
//                {
//                    __instance.health = damageNumber + 1;
//                }
//            }
//            return true;
//        }
//    }
//}