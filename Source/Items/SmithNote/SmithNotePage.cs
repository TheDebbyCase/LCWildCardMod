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
    public class SmithNotePage : MonoBehaviour
    {
        public int id;
        public PlayerControllerB selectedPlayer;
        public RawImage profilePic;
        public TextMeshPro textMesh;
        public SmithNote noteReference;
        public Camera camera;
        public RenderTexture renderTexture;
        public void Spawn(SmithNote noteRef, PlayerControllerB player)
        {
            noteReference = noteRef;
            id = noteReference.pagesList.Count - 1;
            selectedPlayer = player;
            camera = this.GetComponent<Camera>();
            if (!GameNetworkManager.Instance.disableSteam)
            {
                HUDManager.FillImageWithSteamProfile(profilePic, selectedPlayer.playerSteamId);
            }
            renderTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
            camera.targetTexture = renderTexture;
        }
    }
}