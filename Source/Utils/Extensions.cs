using GameNetcodeStuff;
using LCWildCardMod.Items.Fyrus;
using LCWildCardMod.Items;
namespace LCWildCardMod.Utils
{
    internal static class Extensions
    {
        internal static bool IsSaveable(this PlayerControllerB player, out bool starSave, out SmithHalo haloRef)
        {
            haloRef = null;
            starSave = player.IsFyrusSaveable();
            return starSave || (player.isHoldingObject && player.currentlyHeldObjectServer.TryGetComponent(out haloRef) && haloRef.isExhausted == 0);
        }
        internal static bool IsFyrusSaveable(this PlayerControllerB player)
        {
            return FyrusStar.playersEffect.TryGetValue(player.playerSteamId, out bool effect) && effect;
        }
        internal static bool SaveIfAny(this PlayerControllerB player)
        {
            if (player.IsSaveable(out bool starSave, out SmithHalo haloRef))
            {
                if (!starSave)
                {
                    haloRef.ExhaustLocal(player);
                }
                return true;
            }
            return false;
        }
        internal static bool SaveIfHalo(this PlayerControllerB player)
        {
            if (player.IsSaveable(out bool starSave, out SmithHalo haloRef) && !starSave)
            {
                haloRef.ExhaustLocal(player);
                return true;
            }
            return false;
        }
    }
}