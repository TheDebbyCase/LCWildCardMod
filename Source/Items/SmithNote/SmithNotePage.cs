using UnityEngine;
using GameNetcodeStuff;
using Steamworks;
using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
namespace LCWildCardMod.Items.SmithNote
{
    internal class SmithNoteInfo : MonoBehaviour
    {
        BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        internal PlayerControllerB selectedPlayer;
        internal Texture2D texture;
        internal Color colour = Color.white;
        internal string username;
        internal bool isDead = false;
        internal SmithNote noteReference;
        internal void Spawn(SmithNote reference, PlayerControllerB player)
        {
            noteReference = reference;
            selectedPlayer = player;
            username = selectedPlayer.playerUsername;
            Log.LogDebug($"Spawning Smith Note page for player \"{username}\"!");
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
            if (int.TryParse(username[^1].ToString(), out int index) && index >= 0 && index < reference.debugTextures.Length)
            {
                texture = reference.debugTextures[index];
            }
        }
        internal async void GetProfilePicture()
        {
            texture = HUDManager.GetTextureFromImage(await SteamFriends.GetLargeAvatarAsync(selectedPlayer.playerSteamId));
            Log.LogDebug($"Found profile picture!");
        }
    }
}