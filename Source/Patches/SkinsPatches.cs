using HarmonyLib;
using LCWildCardMod.Utils;
using System;
namespace LCWildCardMod.Patches
{
    public static class SkinsPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        public static void GrabbableObject_ChangeAssets(GrabbableObject __instance)
        {
            try
            {
                SkinsClass.SetSkin(__instance);
            }
            catch (Exception exception)
            {
                Log.LogError(exception);
            }
        }
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.Start))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        public static void EnemyAI_ChangeAssets(EnemyAI __instance)
        {
            try
            {
                SkinsClass.SetSkin(__instance);
            }
            catch (Exception exception)
            {
                Log.LogError(exception);
            }
        }
    }
}