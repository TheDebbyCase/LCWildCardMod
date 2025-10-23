using HarmonyLib;
using LCWildCardMod.Utils;
using System.Collections.Generic;
using System.Linq;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(EnemyAI))]
    public static class EnemyAIPatch
    {
        static readonly BepInEx.Logging.ManualLogSource log = WildCardMod.Log;
        [HarmonyPatch(nameof(EnemyAI.Start))]
        [HarmonyAfter(new string[] { "AudioKnight.StarlancerAIFix", "antlershed.lethalcompany.enemyskinregistry" })]
        [HarmonyPostfix]
        public static void ChangeAssets(EnemyAI __instance)
        {
            IEnumerable<Skin> skins = WildCardMod.skinList.Where((x) => x.targetEnemy != null && x.targetEnemy.enemyName == __instance.enemyType.enemyName);
            if (skins.Count() > 0)
            {
                log.LogDebug($"A \"{__instance.enemyType.enemyName}\" has spawned with potential skins!");
                WildCardMod.skinsClass.SetSkin(skins.ToList(), __instance);
            }
        }
    }
}