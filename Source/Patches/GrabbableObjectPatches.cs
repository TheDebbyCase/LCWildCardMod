using HarmonyLib;
using LCWildCardMod.Utils;
using System;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(GrabbableObject))]
    public static class GrabbableObjectPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(nameof(GrabbableObject.Start))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        public static void ChangeAssets(GrabbableObject __instance)
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