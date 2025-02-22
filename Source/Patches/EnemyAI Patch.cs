using HarmonyLib;
using LCWildCardMod.Utils;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(EnemyAI))]
    public static class EnemyAIPatch
    {
        [HarmonyPatch(nameof(EnemyAI.Start))]
        [HarmonyAfter(new string[] {"AudioKnight.StarlancerAIFix", "antlershed.lethalcompany.enemyskinregistry"})]
        [HarmonyPostfix]
        private static void ChangeAssets(EnemyAI __instance)
        {
            foreach (Skin skin in WildCardMod.skinList)
            {
                if (__instance.enemyType.enemyName == skin.targetEnemy.enemyName)
                {
                    WildCardMod.skinsClass.SetSkin(__instance, skin);
                    break;
                }
            }
        }
    }
}