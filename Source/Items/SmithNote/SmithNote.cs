using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Components;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using GameNetcodeStuff;
using TMPro;
using System;
using System.Linq;
using UnityEngine.UI;
using System.Numerics;
namespace LCWildCardMod.Items.SmithNote
{
    public class SmithNote : NoisemakerProp
    {
        public AudioSource spawnMusic;
        public NetworkAnimator itemAnimator;
        public List<PlayerControllerB> playersList = new List<PlayerControllerB>();
        public List<SmithNoteInfo> infoComponents = new List<SmithNoteInfo>();
        public int pageIndex;
        public int secondPageIndex;
        public Canvas canvasOne;
        public Canvas canvasTwo;
        public Canvas canvasThree;
        public Canvas canvasFour;
        public Canvas coverCanvas;
        public GameObject infoPrefab;
        public int isCollected = 0;
        public float playerSelectCooldown = 0;
        public bool flippable = false;
        internal static RaycastHit[] playerHits;
        internal static float hitDistance;
        internal static RaycastHit currentHit;
        internal static HashSet<int> validParameters = new HashSet<int>();
        private System.Random random;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            playerHits = new RaycastHit[StartOfRound.Instance.connectedPlayersAmount];
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
        public void SelectPage(InputAction.CallbackContext throwContext)
        {
            if (playerHeldBy != null && playerHeldBy.ItemSlots.Contains<GrabbableObject>(this))
            {
                if (isPocketed)
                {
                    if (playerSelectCooldown == 0)
                    {
                        playerSelectCooldown = 1;
                        Ray ray = new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward);
                        if (Physics.RaycastNonAlloc(ray, playerHits, 5f, StartOfRound.Instance.playersMask, QueryTriggerInteraction.Ignore) > 0)
                        {
                            hitDistance = 5f;
                            foreach (RaycastHit hit in playerHits)
                            {
                                if (hit.transform.GetComponent<PlayerControllerB>() == playerHeldBy)
                                {
                                    continue;
                                }
                                else if (hit.distance > hitDistance)
                                {
                                    continue;
                                }
                                else
                                {
                                    hitDistance = hit.distance;
                                    currentHit = hit;
                                }
                            }
                            if (!playersList.Contains(currentHit.transform.GetComponent<PlayerControllerB>()))
                            {
                                NewPage(currentHit.transform.GetComponent<PlayerControllerB>());
                            }
                            else
                            {
                                WildCardMod.Log.LogDebug($"{currentHit.transform.GetComponent<PlayerControllerB>().playerUsername} was already in the players list!");
                            }
                        }
                    }
                }
                else
                {
                    WildCardMod.Log.LogDebug($"Smith Note Killing {playersList[pageIndex].playerUsername}!");
                }
            }
        }
        public void NewPage(PlayerControllerB player)
        {
            WildCardMod.Log.LogDebug($"Adding player {player.playerUsername} to players list!");
            playersList.Add(player);
            infoComponents.Add(Instantiate<SmithNoteInfo>(infoPrefab.GetComponent<SmithNoteInfo>(), base.transform));
            infoComponents[^1].Spawn(this, player);
        }
        public void FinishOpening()
        {
            WildCardMod.Log.LogDebug($"Finish Opening");
            flippable = true;
        }
        public void FinishClosing()
        {
            WildCardMod.Log.LogDebug($"Finish Closing");
            canvasOne.enabled = false;
            canvasTwo.enabled = false;
            canvasThree.enabled = false;
            canvasFour.enabled = false;
            canvasOne.gameObject.SetActive(false);
            canvasTwo.gameObject.SetActive(false);
            canvasThree.gameObject.SetActive(false);
            canvasFour.gameObject.SetActive(false);
        }
        public void FinishFlipping()
        {
            WildCardMod.Log.LogDebug($"Finish Flipping");
            flippable = true;
            canvasOne.GetComponentInChildren<TextMeshProUGUI>().text = infoComponents[pageIndex].username;
            canvasTwo.GetComponentInChildren<RawImage>().texture = infoComponents[pageIndex].texture;
            
        }
        public void StartOpening()
        {
            WildCardMod.Log.LogDebug($"Start Opening");
            canvasOne.gameObject.SetActive(true);
            canvasTwo.gameObject.SetActive(true);
            canvasThree.gameObject.SetActive(true);
            canvasOne.enabled = true;
            canvasTwo.enabled = true;
            canvasThree.enabled = true;
            SetPages(true);
            canvasOne.GetComponentInChildren<TextMeshProUGUI>().text = infoComponents[pageIndex].username;
            canvasTwo.GetComponentInChildren<RawImage>().texture = infoComponents[pageIndex].texture;
            itemAnimator.SetTrigger("OpenBook");
        }
        public void SetPages(bool open)
        {
            if (open)
            {
                pageIndex = 0;
            }
            else
            {
                if (infoComponents.Count == 0)
                {
                    pageIndex = -1;
                }
                else if (infoComponents.Count == 1)
                {
                    pageIndex = 0;
                }
                else
                {
                    pageIndex++;
                    if (pageIndex >= playersList.Count)
                    {
                        pageIndex = 0;
                    }
                }
            }
            if (pageIndex == -1)
            {
                canvasOne.GetComponentInChildren<TextMeshProUGUI>().text = "";
                canvasTwo.GetComponentInChildren<RawImage>().texture = null;
                canvasThree.GetComponentInChildren<TextMeshProUGUI>().text = "";
                canvasFour.GetComponentInChildren<RawImage>().texture = null;
            }
            else
            {
                secondPageIndex = pageIndex + 1;
                if (secondPageIndex >= playersList.Count)
                {
                    secondPageIndex = 0;
                }
            }
            WildCardMod.Log.LogDebug($"1st index: {pageIndex}, 2nd index: {secondPageIndex}");
        }
        public void StartClosing()
        {
            WildCardMod.Log.LogDebug($"Start Closing");
            flippable = false;
            itemAnimator.SetTrigger("CloseBook");
        }
        public void StartFlipping()
        {
            WildCardMod.Log.LogDebug($"Start Flipping");
            flippable = false;
            canvasFour.gameObject.SetActive(true);
            canvasFour.enabled = true;
            SetPages(false);
            canvasThree.GetComponentInChildren<TextMeshProUGUI>().text = infoComponents[secondPageIndex].username;
            canvasFour.GetComponentInChildren<RawImage>().texture = infoComponents[secondPageIndex].texture;
            itemAnimator.SetTrigger("Activate");
        }
        public override void Update()
        {
            base.Update();
            if (playerSelectCooldown > 0)
            {
                playerSelectCooldown -= Mathf.Abs(Time.deltaTime * 1f);
            }
            else if (playerSelectCooldown < 0)
            {
                playerSelectCooldown = 0;
            }
        }
        public override void EquipItem()
        {
            base.EquipItem();
            if (isCollected == 0)
            {
                isCollected = 1;
                spawnMusic.Stop();
            }
            if (!playersList.Contains(playerHeldBy))
            {
                NewPage(playerHeldBy);
            }
            StartOpening();
            coverCanvas.gameObject.SetActive(true);
            coverCanvas.enabled = true;
        }
        public override void PocketItem()
        {
            base.PocketItem();
            StartClosing();
            canvasOne.enabled = false;
            canvasTwo.enabled = false;
            canvasThree.enabled = false;
            canvasFour.enabled = false;
            coverCanvas.enabled = false;
            canvasOne.gameObject.SetActive(false);
            canvasTwo.gameObject.SetActive(false);
            canvasThree.gameObject.SetActive(false);
            canvasFour.gameObject.SetActive(false);
            coverCanvas.gameObject.SetActive(false);
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            StartClosing();
            coverCanvas.gameObject.SetActive(true);
            coverCanvas.enabled = true;
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            if (flippable)
            {
                int noiseIndex = random.Next(0, noiseSFX.Length);
                float volume = (float)random.Next((int)(minLoudness * 100f), (int)(maxLoudness * 100f)) / 100f;
                float pitch = (float)random.Next((int)(minPitch * 100f), (int)(maxPitch * 100f)) / 100f;
                noiseAudio.pitch = pitch;
                StartFlipping();
                noiseAudio.PlayOneShot(noiseSFX[noiseIndex], volume);
                WalkieTalkie.TransmitOneShotAudio(noiseAudio, noiseSFX[noiseIndex], volume);
                RoundManager.Instance.PlayAudibleNoise(base.transform.position, noiseRange, volume, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
                playerHeldBy.timeSinceMakingLoudNoise = 0f;
            }
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