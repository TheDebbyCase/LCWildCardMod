//using Unity.Netcode;
//using UnityEngine;
//using Unity.Netcode.Components;
//using System.Collections.Generic;
//using UnityEngine.InputSystem;
//using GameNetcodeStuff;
//using TMPro;
//using System;
//namespace LCWildCardMod.Items.SmithNote
//{
//    public class SmithNote : NoisemakerProp
//    {
//        public AudioSource spawnMusic;
//        public NetworkAnimator itemAnimator;
//        public PlayerControllerB[] availablePlayers;
//        public SmithNotePage[] pagesList;
//        public int isCollected = 0;
//        internal static HashSet<int> validParameters = new HashSet<int>();
//        private System.Random random;
//        public override void OnNetworkSpawn()
//        {
//            base.OnNetworkSpawn();
//            random = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
//            WildCardMod.wildcardKeyBinds.ExtraButton.performed += SelectPage;
//            BeginMusicServerRpc();
//        }
//        public void BeginMusic()
//        {
//            if (isCollected == 0)
//            {
//                spawnMusic.Play();
//            }
//        }
//        public void FlipPage()
//        {

//        }
//        public void SelectPage(InputAction.CallbackContext throwContext)
//        {

//        }
//        public override void ItemActivate(bool used, bool buttonDown = true)
//        {
//            if (GameNetworkManager.Instance.localPlayerController != null)
//            {
//                FlipPage();
//                if (noiseSFX.Length > 0)
//                {
//                    int num = random.Next(0, noiseSFX.Length);
//                    float num2 = (float)random.Next((int)(minLoudness * 100f), (int)(maxLoudness * 100f)) / 100f;
//                    float pitch = (float)random.Next((int)(minPitch * 100f), (int)(maxPitch * 100f)) / 100f;
//                    noiseAudio.pitch = pitch;
//                    noiseAudio.PlayOneShot(noiseSFX[num], num2);
//                    if (noiseAudioFar != null)
//                    {
//                        noiseAudioFar.pitch = pitch;
//                        noiseAudioFar.PlayOneShot(noiseSFXFar[num], num2);
//                    }
//                    if (itemAnimator != null && validParameters.Contains(Animator.StringToHash("Activate")))
//                    {
//                        itemAnimator.SetTrigger("Activate");
//                    }

//                    WalkieTalkie.TransmitOneShotAudio(noiseAudio, noiseSFX[num], num2);
//                    RoundManager.Instance.PlayAudibleNoise(base.transform.position, noiseRange, num2, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
//                    if (minLoudness >= 0.6f && playerHeldBy != null)
//                    {
//                        playerHeldBy.timeSinceMakingLoudNoise = 0f;
//                    }
//                }
//            }
//        }
//        public override int GetItemDataToSave()
//        {
//            return isCollected;
//        }
//        public override void LoadItemSaveData(int saveData)
//        {
//            isCollected = saveData;
//        }
//        [ServerRpc(RequireOwnership = false)]
//        public void BeginMusicServerRpc()
//        {
//            BeginMusicClientRpc(isCollected);
//        }
//        [ClientRpc]
//        public void BeginMusicClientRpc(int id)
//        {
//            isCollected = id;
//            BeginMusic();
//        }
//    }
//}