using GameNetcodeStuff;
using HarmonyLib;
using LCWildCardMod.Items;
using System;
namespace LCWildCardMod.Patches
{
    public static class CojiroPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.PlayerHitGroundEffects))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static bool PlayerControllerB_PreventFallDamage(PlayerControllerB __instance)
        {
            float startingFallValue = __instance.fallValue;
            float startingFallValueUncapped = __instance.fallValueUncapped;
            try
            {
                Cojiro cojiroRef = null;
                if (__instance.isHoldingObject && __instance.currentlyHeldObjectServer.TryGetComponent(out cojiroRef) && cojiroRef.isFloating && __instance.fallValue >= -38f)
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
                        if (!__instance.ItemSlots[i].TryGetComponent(out cojiroRef))
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
                __instance.fallValue = startingFallValue;
                __instance.fallValueUncapped = startingFallValueUncapped;
                Log.LogError(exception);
            }
            return true;
        }
    }
}