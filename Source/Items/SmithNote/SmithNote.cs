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
using LCWildCardMod.Utils;
namespace LCWildCardMod.Items.SmithNote
{
    public class SmithNote : NoisemakerProp
    {
        BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        public Texture2D[] debugTextures;
        public List<NameImagePair> enemyImagesEditor;
        internal Dictionary<string, Texture2D> enemyImages;
        public GameObject hudTextures;
        public AudioSource spawnMusic;
        public AudioClip laughAudio;
        public AudioClip writingSound;
        public AudioClip countdownSound;
        public AudioClip[] selectSounds;
        public NetworkAnimator itemAnimator;
        public List<TextMeshProUGUI> textMeshList;
        public List<RawImage> rawImageList;
        public GameObject infoPrefab;
        internal Dictionary<int, SmithNoteInfo> playerNotes = new Dictionary<int, SmithNoteInfo>();
        internal int currentElement;
        internal float playerSelectCooldown = 0;
        internal bool isFlippable = false;
        internal PlayerControllerB killingPlayer;
        internal bool isKillRunning = false;
        internal int lastFrameLivingPlayers;
        internal static Animator localHudAnim;
        internal static int noteCount = 0;
        internal Coroutine killCoroutine;
        System.Random random;
        void Awake()
        {
            enemyImages = NameImagePair.ConvertToDict(enemyImagesEditor);
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            noteCount++;
            EventsClass.OnRoundStart += SetHUDAnimator;
            random = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            textMeshList[2].rectTransform.parent.gameObject.SetActive(true);
            lastFrameLivingPlayers = StartOfRound.Instance.livingPlayers;
            WildCardMod.Instance.KeyBinds.WildCardButton.performed += SelectPage;
            if (base.IsOwner)
            {
                BeginMusicServerRpc();
            }
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            noteCount--;
            EventsClass.OnRoundStart -= SetHUDAnimator;
            if (noteCount <= 0 && localHudAnim == null)
            {
                Destroy(localHudAnim.gameObject);
                localHudAnim = null;
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
            if (playerNotes.ContainsKey((int)playerHeldBy.playerClientId))
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
            if (!isFlippable)
            {
                return;
            }
            int noiseIndex = random.Next(0, noiseSFX.Length);
            float volume = (float)random.Next((int)(minLoudness * 100f), (int)(maxLoudness * 100f)) / 100f;
            float pitch = (float)random.Next((int)(minPitch * 100f), (int)(maxPitch * 100f)) / 100f;
            noiseAudio.pitch = pitch;
            StartFlipping();
            noiseAudio.PlayOneShot(noiseSFX[noiseIndex], volume);
            WalkieTalkie.TransmitOneShotAudio(noiseAudio, noiseSFX[noiseIndex], volume);
            playerHeldBy.timeSinceMakingLoudNoise = 0f;
            RoundManager.Instance.PlayAudibleNoise(base.transform.position, noiseRange, volume, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
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
        internal void BeginMusic()
        {
            if (hasBeenHeld)
            {
                return;
            }
            spawnMusic.Play();
        }
        internal void SelectPage(InputAction.CallbackContext throwContext)
        {
            if (playerHeldBy == null)
            {
                return;
            }
            if (!playerHeldBy.ItemSlots.Contains(this) || playerSelectCooldown > 0)
            {
                return;
            }
            if (GameNetworkManager.Instance.localPlayerController != playerHeldBy)
            {
                return;
            }
            playerSelectCooldown = 5;
            HUDManager.Instance.UIAudio.PlayOneShot(countdownSound, 3f);
            if (!isPocketed)
            {
                KillServerRpc((ulong)currentElement);
                return;
            }
            RaycastHit[] hits = Physics.RaycastAll(new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward), 5f, StartOfRound.Instance.playersMask, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
            {
                return;
            }
            float hitDistance = 5f;
            PlayerControllerB target = null;
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit newHit = hits[i];
                PlayerControllerB newPlayer = newHit.transform.GetComponent<PlayerControllerB>();
                if (newPlayer == null || newPlayer == playerHeldBy || newHit.distance > hitDistance)
                {
                    continue;
                }
                hitDistance = newHit.distance;
                target = newPlayer;
            }
            if (target == null)
            {
                return;
            }
            if (playerNotes.ContainsKey((int)target.playerClientId))
            {
                Log.LogDebug($"{target.playerUsername} was already in the players list!");
                return;
            }
            NewPageServerRpc(target.playerClientId);
        }
        internal void SetPages()
        {
            int index = playerNotes.Keys.ToList().IndexOf(currentElement);
            if (playerNotes.Count <= 1 || index >= playerNotes.Count - 1)
            {
                currentElement = playerNotes.ElementAt(0).Key;
                return;
            }
            index++;
            currentElement = playerNotes.ElementAt(index).Key;
            Log.LogDebug($"playerNotes index: {index}");
        }
        internal void StartOpening()
        {
            currentElement = playerNotes.ElementAt(0).Key;
            textMeshList[0].rectTransform.parent.gameObject.SetActive(true);
            rawImageList[0].rectTransform.parent.gameObject.SetActive(true);
            SmithNoteInfo info = playerNotes[currentElement];
            textMeshList[0].text = info.targetName;
            rawImageList[0].texture = info.texture;
            rawImageList[0].color = info.colour;
            if (!base.IsServer)
            {
                return;
            }
            itemAnimator.SetTrigger("OpenBook");
        }
        internal void StartClosing()
        {
            textMeshList[1].rectTransform.parent.gameObject.SetActive(false);
            rawImageList[1].rectTransform.parent.gameObject.SetActive(false);
            isFlippable = false;
            if (!base.IsServer)
            {
                return;
            }
            itemAnimator.SetTrigger("CloseBook");
        }
        internal void StartFlipping()
        {
            isFlippable = false;
            textMeshList[1].rectTransform.parent.gameObject.SetActive(true);
            rawImageList[1].rectTransform.parent.gameObject.SetActive(true);
            rawImageList[0].rectTransform.parent.gameObject.SetActive(false);
            SmithNoteInfo info = playerNotes[currentElement];
            rawImageList[1].texture = info.texture;
            rawImageList[1].color = info.colour;
            SetPages();
            textMeshList[1].text = info.targetName;
            if (!base.IsServer)
            {
                return;
            }
            itemAnimator.SetTrigger("Activate");
        }
        public void FinishOpening()
        {
            isFlippable = true;
        }
        public void FinishClosing()
        {
            if (isHeld)
            {
                return;
            }
            textMeshList[0].rectTransform.parent.gameObject.SetActive(false);
            rawImageList[0].rectTransform.parent.gameObject.SetActive(false);
        }
        public void FinishFlipping()
        {
            textMeshList[0].text = playerNotes[currentElement].targetName;
            textMeshList[1].rectTransform.parent.gameObject.SetActive(false);
            rawImageList[1].rectTransform.parent.gameObject.SetActive(false);
            isFlippable = true;
        }
        public void PfpFrameUpdate()
        {
            rawImageList[0].rectTransform.parent.gameObject.SetActive(true);
            SmithNoteInfo info = playerNotes[currentElement];
            rawImageList[0].texture = info.texture;
            rawImageList[0].color = info.colour;
        }
        internal void CheckDead()
        {
            List<int> ids = playerNotes.Keys.ToList();
            for (int i = 0; i < ids.Count; i++)
            {
                int id = ids[i];
                PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[id];
                SmithNoteInfo info = playerNotes[id];
                string user;
                if ((player.isPlayerDead || killingPlayer == player) && !info.isDead)
                {
                    user = info.targetName;
                    info.targetName = $"<s>{user}";
                    info.colour = new Color(1f, 0.5f, 0.5f);
                    info.isDead = true;
                    for (int j = 0; j < 2; j++)
                    {
                        TextMeshProUGUI text = textMeshList[j];
                        if (text.text != user)
                        {
                            continue;
                        }
                        text.text = info.targetName;
                    }
                    for (int j = 0; j < 2; j++)
                    {
                        RawImage image = rawImageList[j];
                        if (image.texture != info.texture)
                        {
                            continue;
                        }
                        image.color = info.colour;
                    }
                    return;
                }
                if (player.isPlayerDead || !info.isDead)
                {
                    return;
                }
                user = info.targetName;
                info.targetName = $"{player.playerUsername}";
                info.colour = Color.white;
                info.isDead = false;
                for (int j = 0; j < 2; j++)
                {
                    TextMeshProUGUI text = textMeshList[j];
                    if (text.text != user)
                    {
                        continue;
                    }
                    text.text = info.targetName;
                }
                for (int j = 0; j < 2; j++)
                {
                    RawImage image = rawImageList[j];
                    if (image.texture != info.texture)
                    {
                        continue;
                    }
                    image.color = info.colour;
                }
            }
        }
        internal IEnumerator KillCoroutine()
        {
            isKillRunning = true;
            yield return new WaitForSeconds(4.5f);
            PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
            if (!SaveHelper.SaveIfHalo(localPlayer))
            {
                localPlayer.KillPlayer(Vector3.up, true, CauseOfDeath.Unknown, 1);
            }
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
            Log.LogDebug($"Adding player {selectingPlayer.playerUsername} to players list!");
            SmithNoteInfo info = Instantiate(infoPrefab.GetComponent<SmithNoteInfo>(), base.transform);
            playerNotes.Add((int)id, info);
            info.Spawn(this, selectingPlayer);
            if (!isPocketed)
            {
                StartOpening();
            }
            if (playerHeldBy != GameNetworkManager.Instance.localPlayerController)
            {
                return;
            }
            HUDManager.Instance.UIAudio.PlayOneShot(selectSounds[random.Next(0, selectSounds.Length)], 3f);
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
            if (killingPlayer.isPlayerDead)
            {
                return;
            }
            Log.LogDebug($"Running Kill with id: {id}");
            CheckDead();
            if (killingPlayer == GameNetworkManager.Instance.localPlayerController)
            {
                HUDManager.Instance.UIAudio.PlayOneShot(laughAudio, 2f);
                Log.LogDebug($"Smith Note Killing This Player!");
                killingPlayer.JumpToFearLevel(1f);
                localHudAnim?.SetTrigger("Kill");
                killCoroutine = StartCoroutine(KillCoroutine());
            }
            if (playerHeldBy != GameNetworkManager.Instance.localPlayerController)
            {
                return;
            }
            Log.LogDebug($"Smith Note Killing {killingPlayer}!");
            HUDManager.Instance.UIAudio.PlayOneShot(writingSound, 3f);
        }
        internal void SetHUDAnimator()
        {
            localHudAnim ??= Instantiate(hudTextures, GameNetworkManager.Instance.localPlayerController.playerHudUIContainer).GetComponent<Animator>();
        }
    }
}