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
namespace LCWildCardMod.Items.SmithNote
{
    public class SmithNote : NoisemakerProp
    {
        public Texture2D[] debugTextures;
        public AudioSource spawnMusic;
        public AudioClip laughAudio;
        public AudioClip writingSound;
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
        internal static RaycastHit[] playerHits;
        internal static float hitDistance;
        internal static RaycastHit currentHit;
        internal Coroutine killCoroutine;
        internal static HashSet<int> validParameters = new HashSet<int>();
        private System.Random random;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            playerHits = new RaycastHit[StartOfRound.Instance.connectedPlayersAmount];
            random = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
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
        public void SelectPage(InputAction.CallbackContext throwContext)
        {
            if (playerHeldBy != null && playerHeldBy.ItemSlots.Contains<GrabbableObject>(this) && playerSelectCooldown == 0 && base.IsOwner)
            {
                playerSelectCooldown = 1;
                if (isPocketed)
                {
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
                            NewPage(currentHit.transform.GetComponent<PlayerControllerB>().playerClientId);
                        }
                        else
                        {
                            WildCardMod.Log.LogDebug($"{currentHit.transform.GetComponent<PlayerControllerB>().playerUsername} was already in the players list!");
                        }
                    }
                }
                else
                {
                    WildCardMod.Log.LogDebug($"Smith Note Killing {playersList[pageIndex].playerUsername}!");
                    KillServerRpc(playersList[pageIndex].playerClientId);
                }
            }
        }
        public void NewPage(ulong id)
        {
            WildCardMod.Log.LogDebug($"Adding player {StartOfRound.Instance.allPlayerScripts[id].playerUsername} to players list!");
            SelectSoundServerRpc();
            playersList.Add(StartOfRound.Instance.allPlayerScripts[id]);
            infoComponents.Add(Instantiate<SmithNoteInfo>(infoPrefab.GetComponent<SmithNoteInfo>(), base.transform));
            infoComponents[^1].Spawn(this, StartOfRound.Instance.allPlayerScripts[id]);
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
        public void StartOpening()
        {
            pageIndex = 0;
            Name1.SetActive(true);
            Pfp1.SetActive(true);
            Name1.GetComponentInChildren<TextMeshProUGUI>().text = infoComponents[pageIndex].username;
            Pfp1.GetComponentInChildren<RawImage>().texture = infoComponents[pageIndex].texture;
            itemAnimator.SetTrigger("OpenBook");
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
        public void StartClosing()
        {
            Name2.SetActive(false);
            Pfp2.SetActive(false);
            flippable = false;
            itemAnimator.SetTrigger("CloseBook");
        }
        public void StartFlipping()
        {
            flippable = false;
            Name2.SetActive(true);
            Pfp2.SetActive(true);
            Pfp1.SetActive(false);
            Pfp2.GetComponentInChildren<RawImage>().texture = infoComponents[pageIndex].texture;
            SetPages();
            Name2.GetComponentInChildren<TextMeshProUGUI>().text = infoComponents[pageIndex].username;
            itemAnimator.SetTrigger("Activate");
        }
        public void PfpFrameUpdate()
        {
            Pfp1.SetActive(true);
            Pfp1.GetComponentInChildren<RawImage>().texture = infoComponents[pageIndex].texture;
        }
        public override void Update()
        {
            base.Update();
            if (killingPlayer != null && killingPlayer.isPlayerDead && killCoroutine != null)
            {
                StopCoroutine(killCoroutine);
            }
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
            coverText.SetActive(true);
            if (isCollected == 0)
            {
                isCollected = 1;
                spawnMusic.Stop();
            }
            if (!playersList.Contains(playerHeldBy))
            {
                NewPage(playerHeldBy.playerClientId);
            }
            StartOpening();
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
        public IEnumerator KillCoroutine(ulong id)
        {
            yield return new WaitForSeconds(5);
            StartOfRound.Instance.allPlayerScripts[id].KillPlayer(Vector3.zero);
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
        public void SelectSoundServerRpc()
        {
            SelectSoundClientRpc();
        }
        [ClientRpc]
        public void SelectSoundClientRpc()
        {
            if (playerHeldBy == GameNetworkManager.Instance.localPlayerController)
            {
                HUDManager.Instance.UIAudio.PlayOneShot(selectSounds[random.Next(0, selectSounds.Length)], 1f);
            }
        }
        [ServerRpc(RequireOwnership =false)]
        public void KillServerRpc(ulong id)
        {
            KillClientRpc(id);
        }
        [ClientRpc]
        public void KillClientRpc(ulong id)
        {
            killingPlayer = StartOfRound.Instance.allPlayerScripts[id];
            WildCardMod.Log.LogDebug($"Running Kill with id: {id}");
            if (playerHeldBy == GameNetworkManager.Instance.localPlayerController)
            {
                HUDManager.Instance.UIAudio.PlayOneShot(writingSound, 1f);
            }
            if (killingPlayer == GameNetworkManager.Instance.localPlayerController && !killingPlayer.isPlayerDead)
            {
                HUDManager.Instance.UIAudio.PlayOneShot(laughAudio, 1f);
                WildCardMod.Log.LogDebug($"Smith Note Killing This Player!");
                GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(1f);
                killCoroutine = StartCoroutine(KillCoroutine(id));
            }
        }
    }
}