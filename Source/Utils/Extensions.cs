using GameNetcodeStuff;
using LCWildCardMod.Items.Fyrus;
using LCWildCardMod.Items;
using UnityEngine;
namespace LCWildCardMod.Utils
{
    internal static class Extensions
    {
        internal static bool IsSaveable(this PlayerControllerB player, out bool starSave, out SmithHalo halo, EnemyAI enemy = null)
        {
            halo = null;
            starSave = player.SaveIfFyrus(enemy);
            if (player.isHoldingObject)
            {
                WildCardMod.Instance.Log.LogDebug("Player is holding something");
                GrabbableObject item = player.currentlyHeldObjectServer;
                if (item.TryGetComponent(out SmithHalo testHalo))
                {
                    WildCardMod.Instance.Log.LogDebug("Found holding halo");
                    if ((testHalo.savedPlayer == player && testHalo.exhausting) || testHalo.isExhausted == 0)
                    {
                        WildCardMod.Instance.Log.LogDebug("Halo can save");
                    }
                }
            }
            else if (player.HasSavedHalo(out SmithHalo testHalo2))
            {
                WildCardMod.Instance.Log.LogDebug("A halo has already saved this player");
            }
            return starSave || (player.isHoldingObject && player.currentlyHeldObjectServer.TryGetComponent(out halo) && ((halo.savedPlayer == player && halo.exhausting) || halo.isExhausted == 0)) || player.HasSavedHalo(out halo);
        }
        internal static bool SaveIfFyrus(this PlayerControllerB player, EnemyAI enemy = null)
        {
            if (player == null)
            {
                return false;
            }
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
        internal static bool SaveIfAny(this PlayerControllerB player, EnemyAI enemy = null)
        {
            if (player.IsSaveable(out bool starSave, out SmithHalo halo, enemy))
            {
                if (!starSave)
                {
                    halo.ExhaustLocal(player);
                }
                return true;
            }
            return false;
        }
        internal static bool SaveIfHalo(this PlayerControllerB player)
        {
            if (player.IsSaveable(out bool starSave, out SmithHalo halo) && !starSave && halo.savedPlayer != player)
            {
                halo.ExhaustLocal(player);
                return true;
            }
            return false;
        }
        internal static bool WasHaloSaved(this PlayerControllerB player, out SmithHalo halo)
        {
            return player.IsSaveable(out bool starSave, out halo) && !starSave && halo.savedPlayer == player;
        }
        internal static bool SaveIfFyrusOrHaloExhausting(this PlayerControllerB player, EnemyAI enemy = null)
        {
            return SaveIfFyrus(player, enemy) || WasHaloSaved(player, out _);
        }
        internal static bool HasSavedHalo(this PlayerControllerB player, out SmithHalo halo)
        {
            halo = null;
            if (player == null)
            {
                return false;
            }
            for (int i = 0; i < player.ItemSlots.Length; i++)
            {
                GrabbableObject item = player.ItemSlots[i];
                if (item == null)
                {
                    continue;
                }
                if (!item.TryGetComponent(out halo))
                {
                    continue;
                }
                if (halo.savedPlayer != player || !halo.exhausting)
                {
                    continue;
                }
                return true;
            }
            return player.HasSavedHaloAnywhere(out halo);
        }
        internal static bool HasSavedHaloAnywhere(this PlayerControllerB player, out SmithHalo halo)
        {
            halo = null;
            if (player == null)
            {
                return false;
            }
            SmithHalo[] halos = Object.FindObjectsOfType<SmithHalo>();
            for (int i = 0; i < halos.Length; i++)
            {
                SmithHalo haloCheck = halos[i];
                if (haloCheck.savedPlayer != player || !haloCheck.exhausting)
                {
                    continue;
                }
                halo = haloCheck;
                return true;
            }
            return false;
        }
    }
}