using GameNetcodeStuff;
using HarmonyLib;
using LCWildCardMod.Items;
using System;
using System.Collections.Generic;
using System.Text;
//namespace LCWildCardMod.Patches
//{
//    [HarmonyPatch(typeof(PlayerControllerB))]
//    public static class PlayerControllerBPatch
//    {
//        [HarmonyPatch(nameof(PlayerControllerB.KillPlayer))]
//        [HarmonyPrefix]
//        public static void SavePlayer(PlayerControllerB __instance)
//        {
//            if (__instance.isHoldingObject && __instance.currentlyHeldObject.name == "WormItem")
//            {
//                return;
//            }
//        }
//    }
//}