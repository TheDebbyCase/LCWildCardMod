using HarmonyLib;
using LCWildCardMod.Utils;
using System.Collections.Generic;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(EnemyAI))]
    public static class EnemyAIPatch
    {
        static readonly BepInEx.Logging.ManualLogSource log = WildCardMod.Log;
        [HarmonyPatch(nameof(EnemyAI.Start))]
        [HarmonyAfter(new string[] {"AudioKnight.StarlancerAIFix", "antlershed.lethalcompany.enemyskinregistry"})]
        [HarmonyPostfix]
        private static void ChangeAssets(EnemyAI __instance)
        {
            List<Skin> skins = new List<Skin>();
            List<Skin> skinList = WildCardMod.skinList;
            for (int i = 0; i < skinList.Count; i++)
            {
                if (skinList[i].targetEnemy != null && __instance.enemyType.enemyName == skinList[i].targetEnemy.enemyName)
                {
                    skins.Add(skinList[i]);
                }
            }
            if (skins.Count > 0)
            {
                log.LogDebug($"A \"{__instance.enemyType.enemyName}\" has spawned with potential skins!");
                WildCardMod.skinsClass.SetSkin(skins, __instance);
            }
        }
    }
}