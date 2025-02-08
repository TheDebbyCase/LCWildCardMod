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
using System.Threading.Tasks;
using LethalLib.Modules;
using System.Collections;
using System.Reflection;
namespace LCWildCardMod.Items.SmithNote
{
    public class SmithNote : NoisemakerProp
    {
        public Texture2D[] debugTextures;
        public AudioSource spawnMusic;
        public AudioClip laughAudio;
        public AudioClip writingSound;
        public AudioClip countdownSound;
        public AudioClip[] selectSounds;
        public NetworkAnimator itemAnimator;
        public List<PlayerControllerB> playersList = new List<PlayerControllerB>();
        public List<SmithNoteInfo> infoComponents = new List<SmithNoteInfo>();
        public int pageIndex;
        public GameObject Name1;
        public GameObject Pfp1;
        public GameObject Name2;
        public GameObject Pfp2;
        public GameObject coverText;
        public GameObject infoPrefab;
        public int isCollected = 0;
        public float playerSelectCooldown = 0;
        public bool flippable = false;
        public PlayerControllerB killingPlayer;
        public List<Component> canvasComponents;
        internal Coroutine killCoroutine;
        internal bool isKillRunning = false;
        internal static HashSet<int> validParameters = new HashSet<int>();
        private System.Random random;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            random = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            canvasComponents = new List<Component>();
            canvasComponents.Add(Name1.GetComponentInChildren<TextMeshProUGUI>());
            canvasComponents.Add(Name2.GetComponentInChildren<TextMeshProUGUI>());
            canvasComponents.Add(Pfp1.GetComponentInChildren<RawImage>());
            canvasComponents.Add(Pfp2.GetComponentInChildren<RawImage>());
            coverText.SetActive(true);
            WildCardMod.wildcardKeyBinds.ExtraButton.performed += SelectPage;
            foreach (AnimatorControllerParameter parameter in itemAnimator.Animator.parameters)
            {
                validParameters.Add(Animator.StringToHash(parameter.name));
            }
            if (base.IsOwner)
            {
                BeginMusicServerRpc();
            }
        }
        public void BeginMusic()
        {
            if (isCollected == 0)
            {
                spawnMusic.Play();
            }
        }
        public override void EquipItem()
        {
            base.EquipItem();
            coverText.SetActive(true);
            if (isCollected == 0)
            {
                isCollected = 1;
                spawnMusic.Stop();
            }
            if (playersList.Contains(playerHeldBy))
            {
                StartOpening();
            }
            else if (GameNetworkManager.Instance.localPlayerController == playerHeldBy)
            {
                NewPage(playerHeldBy.playerClientId, true);
            }
        }
        public override void PocketItem()
        {
            StartClosing();
            Name1.SetActive(false);
            Pfp1.SetActive(false);
            coverText.SetActive(false);
            base.PocketItem();
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            StartClosing();
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
        public void SelectPage(InputAction.CallbackContext throwContext)
        {
            if (playerHeldBy != null && playerHeldBy.ItemSlots.Contains<GrabbableObject>(this) && playerSelectCooldown == 0)
            {
                playerSelectCooldown = 5;
                if (GameNetworkManager.Instance.localPlayerController == playerHeldBy)
                {
                    HUDManager.Instance.UIAudio.PlayOneShot(countdownSound, 3f);
                    if (isPocketed)
                    {
                        RaycastHit[] hits = Physics.RaycastAll(new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward), 5f, StartOfRound.Instance.playersMask, QueryTriggerInteraction.Ignore);
                        if (hits.Length > 0)
                        {
                            RaycastHit currentHit = hits[0];
                            float hitDistance = 5f;
                            foreach (RaycastHit hit in hits)
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
                                NewPage(currentHit.transform.GetComponent<PlayerControllerB>().playerClientId, false);
                            }
                            else
                            {
                                WildCardMod.Log.LogDebug($"{currentHit.transform.GetComponent<PlayerControllerB>().playerUsername} was already in the players list!");
                            }
                        }
                    }
                    else
                    {
                        KillServerRpc(playersList[pageIndex].playerClientId);
                    }
                }
            }
        }
        public void NewPage(ulong id, bool open)
        {
            HUDManager.Instance.UIAudio.PlayOneShot(selectSounds[random.Next(0, selectSounds.Length)], 3f);
            NewPageServerRpc(id, open);
        }
        public void SetPages()
        {
            if (infoComponents.Count == 1)
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
            WildCardMod.Log.LogDebug($"index: {pageIndex}");
        }
        public void StartOpening()
        {
            pageIndex = 0;
            Name1.SetActive(true);
            Pfp1.SetActive(true);
            Name1.GetComponentInChildren<TextMeshProUGUI>().text = infoComponents[pageIndex].username;
            Pfp1.GetComponentInChildren<RawImage>().texture = infoComponents[pageIndex].texture;
            Pfp1.GetComponentInChildren<RawImage>().color = infoComponents[pageIndex].colour;
            itemAnimator.SetTrigger("OpenBook");
        }
        public void StartClosing()
        {
            Name2.SetActive(false);
            Pfp2.SetActive(false);
            flippable = false;
            itemAnimator.SetTrigger("CloseBook");
        }
        public void StartFlipping()
        {
            CheckDead();
            flippable = false;
            Name2.SetActive(true);
            Pfp2.SetActive(true);
            Pfp1.SetActive(false);
            Pfp2.GetComponentInChildren<RawImage>().texture = infoComponents[pageIndex].texture;
            Pfp2.GetComponentInChildren<RawImage>().color = infoComponents[pageIndex].colour;
            SetPages();
            Name2.GetComponentInChildren<TextMeshProUGUI>().text = infoComponents[pageIndex].username;
            itemAnimator.SetTrigger("Activate");
        }
        public void FinishOpening()
        {
            flippable = true;
        }
        public void FinishClosing()
        {
            if (!isHeld)
            {
                Name1.SetActive(false);
                Pfp1.SetActive(false);
            }
        }
        public void FinishFlipping()
        {
            Name1.GetComponentInChildren<TextMeshProUGUI>().text = infoComponents[pageIndex].username;
            Name2.SetActive(false);
            Pfp2.SetActive(false);
            flippable = true;
        }
        public void PfpFrameUpdate()
        {
            Pfp1.SetActive(true);
            Pfp1.GetComponentInChildren<RawImage>().texture = infoComponents[pageIndex].texture;
            Pfp1.GetComponentInChildren<RawImage>().color = infoComponents[pageIndex].colour;
        }
        public void CheckDead()
        {
            foreach (PlayerControllerB player in playersList)
            {
                SmithNoteInfo info = infoComponents[playersList.IndexOf(player)];
                if (player.isPlayerDead || killingPlayer == player)
                {
                    if (!info.isDead)
                    {
                        string tempUser = info.username;
                        info.username = $"<s>{tempUser}";
                        info.colour = new Color(1f, 0.5f, 0.5f);
                        info.isDead = true;
                        foreach (Component component in canvasComponents)
                        {
                            if (component.TryGetComponent<TextMeshProUGUI>(out TextMeshProUGUI tempTextMesh) && tempTextMesh.text == tempUser)
                            {
                                tempTextMesh.text = info.username;
                            }
                            else if (component.TryGetComponent<RawImage>(out RawImage tempRawImage) && tempRawImage.texture == info.texture)
                            {
                                tempRawImage.color = info.colour;
                            }
                        }
                    }
                }
            }
        }
        public override void Update()
        {
            base.Update();
            if (killingPlayer != null && killingPlayer.isPlayerDead && isKillRunning == true)
            {
                StopCoroutine(killCoroutine);
                killCoroutine = null;
                isKillRunning = false;
                killingPlayer = null;
            }
            if (playerSelectCooldown > 0)
            {
                playerSelectCooldown -= Mathf.Abs(Time.deltaTime);
            }
            else if (playerSelectCooldown < 0)
            {
                playerSelectCooldown = 0;
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
        [ServerRpc(RequireOwnership = false)]
        public void NewPageServerRpc(ulong id, bool open)
        {
            WildCardMod.Log.LogDebug($"NewPageServerRpc, id: {id}");
            NewPageClientRpc(id, open);
        }
        [ClientRpc]
        public void NewPageClientRpc(ulong id, bool open)
        {
            WildCardMod.Log.LogDebug($"NewPageClientRpc, id: {id}");
            WildCardMod.Log.LogDebug($"Adding player {StartOfRound.Instance.allPlayerScripts[id].playerUsername} to players list!");
            playersList.Add(StartOfRound.Instance.allPlayerScripts[id]);
            infoComponents.Add(Instantiate<SmithNoteInfo>(infoPrefab.GetComponent<SmithNoteInfo>(), base.transform));
            infoComponents[^1].Spawn(this, StartOfRound.Instance.allPlayerScripts[id]);
            if (open)
            {
                StartOpening();
            }
        }
        [ServerRpc(RequireOwnership = false)]
        public void KillServerRpc(ulong id)
        {
            KillClientRpc(id);
        }
        [ClientRpc]
        public void KillClientRpc(ulong id)
        {
            killingPlayer = StartOfRound.Instance.allPlayerScripts[id];
            if (!killingPlayer.isPlayerDead)
            {
                WildCardMod.Log.LogDebug($"Running Kill with id: {id}");
                CheckDead();
                if (killingPlayer == GameNetworkManager.Instance.localPlayerController)
                {
                    HUDManager.Instance.UIAudio.PlayOneShot(laughAudio, 2f);
                    WildCardMod.Log.LogDebug($"Smith Note Killing This Player!");
                    killingPlayer.JumpToFearLevel(1f);
                    killCoroutine = StartCoroutine(KillCoroutine());
                }
                if (playerHeldBy == GameNetworkManager.Instance.localPlayerController)
                {
                    WildCardMod.Log.LogDebug($"Smith Note Killing {killingPlayer}!");
                    HUDManager.Instance.UIAudio.PlayOneShot(writingSound, 3f);
                }
            }
        }
        public IEnumerator KillCoroutine()
        {
            isKillRunning = true;
            yield return new WaitForSeconds(4.5f);
            GameNetworkManager.Instance.localPlayerController.KillPlayer(Vector3.up, true, CauseOfDeath.Unknown, 1);
            isKillRunning = false;
        }
    }
}