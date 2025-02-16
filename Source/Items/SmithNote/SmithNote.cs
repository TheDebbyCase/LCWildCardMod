using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Components;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using GameNetcodeStuff;
using TMPro;
using System.Linq;
using UnityEngine.UI;
using System.Collections;
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
        public Dictionary<PlayerControllerB, SmithNoteInfo> playerNotes = new Dictionary<PlayerControllerB, SmithNoteInfo>();
        public KeyValuePair<PlayerControllerB, SmithNoteInfo> currentElement;
        public List<TextMeshProUGUI> textMeshList;
        public List<RawImage> rawImageList;
        public GameObject infoPrefab;
        public float playerSelectCooldown = 0;
        public bool isFlippable = false;
        public PlayerControllerB killingPlayer;
        public bool isKillRunning = false;
        public int lastFrameLivingPlayers;
        internal Coroutine killCoroutine;
        private System.Random random;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            random = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            textMeshList[2].rectTransform.parent.gameObject.SetActive(true);
            lastFrameLivingPlayers = StartOfRound.Instance.livingPlayers;
            WildCardMod.wildcardKeyBinds.WildCardButton.performed += SelectPage;
            if (base.IsOwner)
            {
                BeginMusicServerRpc();
            }
        }
        public override void EquipItem()
        {
            if (!hasBeenHeld)
            {
                spawnMusic.Stop();
            }
            base.EquipItem();
            textMeshList[2].rectTransform.parent.gameObject.SetActive(true);
            if (playerNotes.ContainsKey(playerHeldBy))
            {
                StartOpening();
            }
            else if (GameNetworkManager.Instance.localPlayerController == playerHeldBy)
            {
                NewPageServerRpc(playerHeldBy.playerClientId);
            }
        }
        public override void PocketItem()
        {
            base.PocketItem();
            StartClosing();
            textMeshList[0].rectTransform.parent.gameObject.SetActive(false);
            rawImageList[0].rectTransform.parent.gameObject.SetActive(false);
            textMeshList[2].rectTransform.parent.gameObject.SetActive(false);
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            StartClosing();
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            if (isFlippable)
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
            if (lastFrameLivingPlayers != StartOfRound.Instance.livingPlayers)
            {
                CheckDead();
                lastFrameLivingPlayers = StartOfRound.Instance.livingPlayers;
            }
        }
        public override void LoadItemSaveData(int saveData)
        {
            hasBeenHeld = true;
        }
        public void BeginMusic()
        {
            if (!hasBeenHeld)
            {
                spawnMusic.Play();
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
                            if (!playerNotes.ContainsKey(currentHit.transform.GetComponent<PlayerControllerB>()))
                            {
                                NewPageServerRpc(currentHit.transform.GetComponent<PlayerControllerB>().playerClientId);
                            }
                            else
                            {
                                WildCardMod.Log.LogDebug($"{currentHit.transform.GetComponent<PlayerControllerB>().playerUsername} was already in the players list!");
                            }
                        }
                    }
                    else
                    {
                        KillServerRpc(currentElement.Key.playerClientId);
                    }
                }
            }
        }
        public void SetPages()
        {
            int index = playerNotes.Keys.ToList().IndexOf(currentElement.Key);
            if (playerNotes.Count > 1 && index < playerNotes.Count - 1)
            {
                index++;
                currentElement = playerNotes.ElementAt(index);
                WildCardMod.Log.LogDebug($"playerNotes index: {index}");
                return;
            }
            currentElement = playerNotes.ElementAt(0);
        }
        public void StartOpening()
        {
            currentElement = playerNotes.ElementAt(0);
            textMeshList[0].rectTransform.parent.gameObject.SetActive(true);
            rawImageList[0].rectTransform.parent.gameObject.SetActive(true);
            textMeshList[0].text = currentElement.Value.username;
            rawImageList[0].texture = currentElement.Value.texture;
            rawImageList[0].color = currentElement.Value.colour;
            if (base.IsServer)
            {
                itemAnimator.SetTrigger("OpenBook");
            }
        }
        public void StartClosing()
        {
            textMeshList[1].rectTransform.parent.gameObject.SetActive(false);
            rawImageList[1].rectTransform.parent.gameObject.SetActive(false);
            isFlippable = false;
            if (base.IsServer)
            {
                itemAnimator.SetTrigger("CloseBook");
            }
        }
        public void StartFlipping()
        {
            isFlippable = false;
            textMeshList[1].rectTransform.parent.gameObject.SetActive(true);
            rawImageList[1].rectTransform.parent.gameObject.SetActive(true);
            rawImageList[0].rectTransform.parent.gameObject.SetActive(false);
            rawImageList[1].texture = currentElement.Value.texture;
            rawImageList[1].color = currentElement.Value.colour;
            SetPages();
            textMeshList[1].text = currentElement.Value.username;
            if (base.IsServer)
            {
                itemAnimator.SetTrigger("Activate");
            }
        }
        public void FinishOpening()
        {
            isFlippable = true;
        }
        public void FinishClosing()
        {
            if (!isHeld)
            {
                textMeshList[0].rectTransform.parent.gameObject.SetActive(false);
                rawImageList[0].rectTransform.parent.gameObject.SetActive(false);
            }
        }
        public void FinishFlipping()
        {
            textMeshList[0].text = currentElement.Value.username;
            textMeshList[1].rectTransform.parent.gameObject.SetActive(false);
            rawImageList[1].rectTransform.parent.gameObject.SetActive(false);
            isFlippable = true;
        }
        public void PfpFrameUpdate()
        {
            rawImageList[0].rectTransform.parent.gameObject.SetActive(true);
            rawImageList[0].texture = currentElement.Value.texture;
            rawImageList[0].color = currentElement.Value.colour;
        }
        public void CheckDead()
        {
            foreach (KeyValuePair<PlayerControllerB, SmithNoteInfo> pair in playerNotes)
            {
                PlayerControllerB player = pair.Key;
                SmithNoteInfo info = pair.Value;
                if ((player.isPlayerDead || killingPlayer == player) && !info.isDead)
                {
                    string user = info.username;
                    info.username = $"<s>{user}";
                    info.colour = new Color(1f, 0.5f, 0.5f);
                    info.isDead = true;
                    for (int i = 0; i < 2; i++)
                    {
                        TextMeshProUGUI text = textMeshList[i];
                        if (text.text == user)
                        {
                            text.text = info.username;
                        }
                    }
                    for (int i = 0; i < 2; i++)
                    {
                        RawImage image = rawImageList[i];
                        if (image.texture == info.texture)
                        {
                            image.color = info.colour;
                        }
                    }
                }
                else if (!player.isPlayerDead && info.isDead)
                {
                    string user = info.username;
                    info.username = $"{player.playerUsername}";
                    info.colour = Color.white;
                    info.isDead = false;
                    for (int i = 0; i < 2; i++)
                    {
                        TextMeshProUGUI text = textMeshList[i];
                        if (text.text == user)
                        {
                            text.text = info.username;
                        }
                    }
                    for (int i = 0; i < 2; i++)
                    {
                        RawImage image = rawImageList[i];
                        if (image.texture == info.texture)
                        {
                            image.color = info.colour;
                        }
                    }
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
        [ServerRpc(RequireOwnership = false)]
        public void BeginMusicServerRpc()
        {
            BeginMusicClientRpc(hasBeenHeld);
        }
        [ClientRpc]
        public void BeginMusicClientRpc(bool held)
        {
            hasBeenHeld = held;
            BeginMusic();
        }
        [ServerRpc(RequireOwnership = false)]
        public void NewPageServerRpc(ulong id)
        {
            NewPageClientRpc(id);
        }
        [ClientRpc]
        public void NewPageClientRpc(ulong id)
        {
            PlayerControllerB selectingPlayer = StartOfRound.Instance.allPlayerScripts[id];
            WildCardMod.Log.LogDebug($"Adding player {selectingPlayer.playerUsername} to players list!");
            playerNotes.Add(selectingPlayer, Instantiate<SmithNoteInfo>(infoPrefab.GetComponent<SmithNoteInfo>(), base.transform));
            playerNotes.GetValueOrDefault(selectingPlayer).Spawn(this, selectingPlayer);
            if (!isPocketed)
            {
                StartOpening();
            }
            if (playerHeldBy == GameNetworkManager.Instance.localPlayerController)
            {
                HUDManager.Instance.UIAudio.PlayOneShot(selectSounds[random.Next(0, selectSounds.Length)], 3f);
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
    }
}