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
            starSave = player.SaveIfFyrus();
            return starSave || (player.isHoldingObject && player.currentlyHeldObjectServer.TryGetComponent(out haloRef) && haloRef.isExhausted == 0);
        }
        internal static bool SaveIfFyrus(this PlayerControllerB player, EnemyAI enemy = null)
        {
            if (!(FyrusStar.playersEffect.TryGetValue(player.playerSteamId, out bool effect) && effect))
            {
                return false;
            }
            WildCardMod.Instance.Log.LogDebug($"Fyrus star saved {player.playerUsername}!");
            if (enemy == null)
            {
                return true;
            }
            EnemyAICollisionDetect collision = enemy.GetComponentInChildren<EnemyAICollisionDetect>();
            if (collision == null)
            {
                return true;
            }
            (collision as IHittable).Hit(1, (enemy.transform.position - player.transform.position).normalized * 2.5f, player, true);
            return true;
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