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
        public Color colour = Color.white;
        public string username;
        public bool isDead = false;
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
            else if (username == "Player #0")
            {
                texture = reference.debugTextures[0];
            }
            else if (username == "Player #1")
            {
                texture = reference.debugTextures[1];
            }
            else if (username == "Player #2")
            {
                texture = reference.debugTextures[2];
            }
            else if (username == "Player #3")
            {
                texture = reference.debugTextures[3];
            }
        }
        public async void GetProfilePicture()
        {
            texture = HUDManager.GetTextureFromImage(await SteamFriends.GetLargeAvatarAsync(selectedPlayer.playerSteamId));
        }
    }
}