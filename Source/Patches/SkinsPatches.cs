using HarmonyLib;
using LCWildCardMod.Utils;
using System;
namespace LCWildCardMod.Patches
{
    internal static class SkinsPatches
    {
        private static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        internal static void GrabbableObject_ChangeAssets(GrabbableObject __instance)
        {
            try
            {
                WildCardSkin.SetSkin(__instance);
            }
            catch (Exception exception)
            {
                Log.LogError(exception);
            }
        }
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.Start))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        internal static void EnemyAI_ChangeAssets(EnemyAI __instance)
        {
            try
            {
                WildCardSkin.SetSkin(__instance);
            }
            catch (Exception exception)
            {
                Log.LogError(exception);
            }
        }
    }
}