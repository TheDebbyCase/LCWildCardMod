using HarmonyLib;
using LCWildCardMod.Utils;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(DepositItemsDesk))]
    public static class DepositItemsDeskPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(nameof(DepositItemsDesk.CollisionDetect))]
        [HarmonyPrefix]
        public static bool HaloSave()
        {
            return !GameNetworkManager.Instance.localPlayerController.SaveIfHalo();
        }
    }
}