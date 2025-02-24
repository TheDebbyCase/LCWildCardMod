using HarmonyLib;
using LCWildCardMod.Utils;
using System.Collections.Generic;

namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(GrabbableObject))]
    public static class GrabbableObjectPatch
    {
        static readonly BepInEx.Logging.ManualLogSource log = WildCardMod.Log;
        [HarmonyPatch(nameof(GrabbableObject.Start))]
        [HarmonyPostfix]
        private static void ChangeAssets(GrabbableObject __instance)
        {
            List<Skin> skins = new List<Skin>();
            List<Skin> skinList = WildCardMod.skinList;
            for (int i = 0; i < skinList.Count; i++)
            {
                if (skinList[i].targetItem != null && __instance.itemProperties.itemName == skinList[i].targetItem.itemName)
                {
                    skins.Add(skinList[i]);
                }
            }
            if (skins.Count > 0)
            {
                log.LogDebug($"A \"{__instance.itemProperties.itemName}\" has spawned with potential skins!");
                WildCardMod.skinsClass.SetSkin(skins, null, __instance);
            }
        }
    }
}