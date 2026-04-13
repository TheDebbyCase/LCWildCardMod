using UnityEngine;
using GameNetcodeStuff;
using Steamworks;
using System;
namespace LCWildCardMod.Items.SmithNote
{
    internal class SmithNoteInfo : MonoBehaviour
    {
        BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        internal PlayerControllerB selectedPlayer = null;
        internal EnemyAI selectedEnemy = null;
        internal Texture2D texture;
        internal Color colour = Color.white;
        internal string targetName;
        internal bool isDead = false;
        internal SmithNote noteReference;
        internal void Spawn(SmithNote reference, PlayerControllerB player)
        {
            noteReference = reference;
            selectedPlayer = player;
            targetName = selectedPlayer.playerUsername;
            Log.LogDebug($"Spawning Smith Note page for player \"{targetName}\"!");
            if (!GameNetworkManager.Instance.disableSteam)
            {
                try
                {
                    GetProfilePicture();
                }
                catch (Exception exception)
                {
                    Log.LogError(exception);
                }
                return;
            }
            if (int.TryParse(targetName[^1].ToString(), out int index) && index >= 0 && index < reference.debugTextures.Length)
            {
                texture = reference.debugTextures[index];
            }
        }
        internal void Spawn(SmithNote reference, EnemyAI enemy)
        {
            noteReference = reference;
            selectedEnemy = enemy;
            targetName = selectedEnemy.enemyType.enemyName;
            Log.LogDebug($"Spawning Smith Note page for enemy \"{targetName}\"!");
            if (!reference.enemyImages.TryGetValue(enemy.enemyType.enemyName, out texture))
            {
                Log.LogWarning($"No image was found to display in the Smith Note for the enemy \"{enemy.enemyType.enemyName}\"");
            }
        }
        internal async void GetProfilePicture()
        {
            texture = HUDManager.GetTextureFromImage(await SteamFriends.GetLargeAvatarAsync(selectedPlayer.playerSteamId));
            Log.LogDebug($"Found profile picture!");
        }
    }
}