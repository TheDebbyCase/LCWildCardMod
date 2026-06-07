using GameNetcodeStuff;
using HarmonyLib;
using LCWildCardMod.Items;
using LCWildCardMod.Utils;
using System;
namespace LCWildCardMod.Patches
{
    internal static class CojiroPatches
    {
        private static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.PlayerHitGroundEffects))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        internal static bool PlayerControllerB_PreventFallDamage(PlayerControllerB __instance)
        {
            try
            {
                if (!__instance.takingFallDamage)
                {
                    return true;
                }
                if (__instance.isHoldingObject && __instance.currentlyHeldObjectServer.TryGetComponent(out Cojiro cojiroRef) && cojiroRef.isFloating)
                {
                    Log.LogDebug($"Cojiro Preventing Fall Damage");
                    __instance.takingFallDamage = false;
                    return true;
                }
                for (int i = 0; i < __instance.ItemSlots.Length; i++)
                {
                    if (__instance.ItemSlots[i] == null || !__instance.ItemSlots[i].TryGetComponent(out cojiroRef) || !cojiroRef.isPocketed || !cojiroRef.LastPlayerHeldBy.IsLocal() || cojiroRef.currentUseCooldown <= 0)
                    {
                        continue;
                    }
                    Log.LogDebug($"Cojiro Preventing Fall Damage");
                    __instance.takingFallDamage = false;
                    return true;
                }
            }
            catch (Exception exception)
            {
                Log.LogError(exception);
            }
            return true;
        }
    }
}