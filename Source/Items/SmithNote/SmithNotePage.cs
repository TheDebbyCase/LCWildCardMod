using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Components;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using GameNetcodeStuff;
using TMPro;
using System;
using Steamworks;
using UnityEngine.UI;
namespace LCWildCardMod.Items.SmithNote
{
    public class SmithNoteInfo : MonoBehaviour
    {
        public PlayerControllerB selectedPlayer;
        public Texture2D texture;
        public string username;
        public SmithNote noteReference;
        public void Spawn(SmithNote reference, PlayerControllerB player)
        {
            noteReference = reference;
            selectedPlayer = player;
            username = selectedPlayer.playerUsername;
            if (!GameNetworkManager.Instance.disableSteam)
            {
                GetProfilePicture();
            }
        }
        public async void GetProfilePicture()
        {
            texture = HUDManager.GetTextureFromImage(await SteamFriends.GetLargeAvatarAsync(selectedPlayer.playerSteamId));
        }
    }
}