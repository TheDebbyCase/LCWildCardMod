using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Components;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using GameNetcodeStuff;
using TMPro;
using System;
using System.Linq;
namespace LCWildCardMod.Items.SmithNote
{
    public class SmithNote : NoisemakerProp
    {
        public AudioSource spawnMusic;
        public NetworkAnimator itemAnimator;
        public List<PlayerControllerB> playersList = new List<PlayerControllerB>();
        public List<SmithNotePage> pagesList = new List<SmithNotePage>();
        public List<MeshRenderer> materialComponents = new List<MeshRenderer>();
        public Transform parentTransform;
        public GameObject pagePrefab;
        public int isCollected = 0;
        internal static HashSet<int> validParameters = new HashSet<int>();
        private System.Random random;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            random = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            WildCardMod.wildcardKeyBinds.ExtraButton.performed += SelectPage;
            foreach (AnimatorControllerParameter parameter in itemAnimator.Animator.parameters)
            {
                validParameters.Add(Animator.StringToHash(parameter.name));
            }
            BeginMusicServerRpc();
        }
        public void BeginMusic()
        {
            if (isCollected == 0)
            {
                spawnMusic.Play();
            }
        }
        public void FlipPage()
        {
            if (itemAnimator.Animator.GetInteger("CurrentPage") == 2)
            {
                itemAnimator.Animator.SetInteger("CurrentPage", 0);
            }
            else
            {
                itemAnimator.Animator.SetInteger("CurrentPage", itemAnimator.Animator.GetInteger("CurrentPage") + 1);
            }
            itemAnimator.SetTrigger("Activate");
        }
        public void SelectPage(InputAction.CallbackContext throwContext)
        {
            if (playerHeldBy != null && playerHeldBy.ItemSlots.Contains<GrabbableObject>(this))
            {
                if (isHeld)
                {

                }
                else
                {
                    PlayerControllerB selectingPlayer = StartOfRound.Instance.allPlayerScripts[random.Next(StartOfRound.Instance.allPlayerScripts.Length)];
                    if (!playersList.Contains(selectingPlayer))
                    {
                        playersList.Add(selectingPlayer);
                        pagesList.Add(CreatePage(selectingPlayer));
                    }
                }
            }
        }
        public SmithNotePage CreatePage(PlayerControllerB player)
        {
            SmithNotePage page = Instantiate<SmithNotePage>(pagePrefab.GetComponent<SmithNotePage>(), parentTransform);
            pagesList.Add(page);
            page.Spawn(this, player);
            return page;
        }
        public override void EquipItem()
        {
            base.EquipItem();
            itemAnimator.SetTrigger("OpenBook");
            itemAnimator.Animator.SetInteger("CurrentPage", 0);
        }
        public override void PocketItem()
        {
            base.PocketItem();
            itemAnimator.SetTrigger("CloseBook");
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            itemAnimator.SetTrigger("CloseBook");
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            int noiseIndex = random.Next(0, noiseSFX.Length);
            float volume = (float)random.Next((int)(minLoudness * 100f), (int)(maxLoudness * 100f)) / 100f;
            float pitch = (float)random.Next((int)(minPitch * 100f), (int)(maxPitch * 100f)) / 100f;
            noiseAudio.pitch = pitch;
            noiseAudio.PlayOneShot(noiseSFX[noiseIndex], volume);
            FlipPage();
            WalkieTalkie.TransmitOneShotAudio(noiseAudio, noiseSFX[noiseIndex], volume);
            RoundManager.Instance.PlayAudibleNoise(base.transform.position, noiseRange, volume, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
            playerHeldBy.timeSinceMakingLoudNoise = 0f;
        }
        public override int GetItemDataToSave()
        {
            return isCollected;
        }
        public override void LoadItemSaveData(int saveData)
        {
            isCollected = saveData;
        }
        [ServerRpc(RequireOwnership = false)]
        public void BeginMusicServerRpc()
        {
            BeginMusicClientRpc(isCollected);
        }
        [ClientRpc]
        public void BeginMusicClientRpc(int id)
        {
            isCollected = id;
            BeginMusic();
        }
    }
}