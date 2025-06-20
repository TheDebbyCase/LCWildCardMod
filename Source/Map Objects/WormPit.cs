﻿using GameNetcodeStuff;
using LCWildCardMod.Utils;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
namespace LCWildCardMod.MapObjects
{
    public enum WormState {Peeping, Hungry, Consuming, Sleeping}
    public class WormPit : NetworkBehaviour
    {
        readonly BepInEx.Logging.ManualLogSource log = WildCardMod.Log;
        public MapObject mapObject;
        public AudioSource pitSource;
        public AudioSource miscSource;
        public AudioClip[] squishClips;
        public AudioClip biteClip;
        public AudioClip growlClip;
        public WormState State = WormState.Peeping;
        public Transform irisTransform;
        public NetworkAnimator netAnim;
        public float peepCooldown;
        public float peepWaitBetween = 5f;
        public float peepWaitHold;
        public PlayerControllerB playerLookingAt;
        public List<PlayerControllerB> playersOverlapping = new List<PlayerControllerB>();
        public bool peeping;
        public float lookLerp;
        public Quaternion initialRot;
        public Quaternion targetRot;
        public float hunger;
        public float patience = 5f;
        public float sleepiness = 0;
        public bool consuming;
        private System.Random random;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            log.LogDebug("Worm Pit Spawned");
            random = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
        }
        public void Update()
        {
            if (base.IsServer)
            {
                for (int i = 0; i < playersOverlapping.Count; i++)
                {
                    playerLookingAt = SelectNewPlayer(playersOverlapping[i]);
                }
                if (playersOverlapping.Count == 0)
                {
                    playerLookingAt = SelectNewPlayer(null);
                }
                switch (State)
                {
                    case WormState.Peeping:
                        {
                            if (peeping)
                            {
                                if (playerLookingAt != null)
                                {
                                    PlayerFearIncreaseClientRpc(true, 1f, 0.5f, playerLookingAt.actualClientId);
                                    IrisLookClientRpc(false, playerLookingAt.playerClientId);
                                }
                                if (peepCooldown >= peepWaitHold)
                                {
                                    int id = random.Next(0, squishClips.Length);
                                    float pitch = (float)random.Next(9, 11) / 10f;
                                    PlaySquishClientRpc(id, pitch);
                                    netAnim.Animator.SetBool("HoldPeep", false);
                                    peeping = false;
                                    peepCooldown = 0f;
                                }
                            }
                            else
                            {
                                if (irisTransform.localRotation != Quaternion.identity)
                                {
                                    lookLerp = 0f;
                                    IrisLookClientRpc(true);
                                }
                                if (peepCooldown >= peepWaitBetween)
                                {
                                    int id = random.Next(0, squishClips.Length);
                                    float pitch = (float)random.Next(9, 11) / 10f;
                                    PlaySquishClientRpc(id, pitch);
                                    netAnim.SetTrigger("Peep");
                                    netAnim.Animator.SetBool("HoldPeep", true);
                                    peepWaitBetween = ((float)random.Next(25, 76) / 10f);
                                    peepWaitHold = ((float)random.Next(40, 71) / 10f);
                                    peeping = true;
                                    peepCooldown = 0f;
                                }
                            }
                            if (hunger >= 5f)
                            {
                                peeping = false;
                                peepCooldown = 0f;
                                netAnim.Animator.SetBool("HoldPeep", false);
                                hunger = 0f;
                                float pitch = (float)random.Next(9, 11) / 10f;
                                PlayGrowlClientRpc(pitch);
                                SetStateClientRpc(WormState.Hungry);
                                break;
                            }
                            if (playerLookingAt != null && peeping)
                            {
                                hunger += Time.deltaTime * 1.5f;
                                if (Vector3.Distance(this.transform.position + (Vector3.up / 2f), playerLookingAt.transform.position) <= 7.5f)
                                {
                                    hunger += Time.deltaTime * 2f;
                                }
                            }
                            peepCooldown += Time.deltaTime;
                            break;
                        }
                    case WormState.Hungry:
                        {
                            if ((playerLookingAt != null && Vector3.Distance(this.transform.position + (Vector3.up / 2f), playerLookingAt.transform.position) <= 5f) || patience <= 0f)
                            {
                                patience = 5f;
                                SetStateClientRpc(WormState.Consuming);
                                break;
                            }
                            else if (playerLookingAt != null && Vector3.Distance(this.transform.position + (Vector3.up / 2f), playerLookingAt.transform.position) <= 15f)
                            {
                                patience -= Time.deltaTime;
                            }
                            break;
                        }
                    case WormState.Consuming:
                        {
                            if (consuming)
                            {
                                for (int i = 0; i < playersOverlapping.Count; i++)
                                {
                                    PlayerFearIncreaseClientRpc(true, 2f, 1f, playersOverlapping[i].actualClientId);
                                    PlayerExternalForcesClientRpc(playersOverlapping[i].actualClientId);
                                }
                                if (sleepiness >= 12.5f)
                                {
                                    consuming = false;
                                    netAnim.SetTrigger("Emerge");
                                    sleepiness = ((float)random.Next(50, 126) / 10f);
                                    float pitch = (float)random.Next(9, 11) / 10f;
                                    PlayGrowlClientRpc(pitch);
                                    PitMusicClientRpc(false);
                                    SetStateClientRpc(WormState.Sleeping);
                                    break;
                                }
                                else if (playersOverlapping.Count == 0 && sleepiness >= 7.5f)
                                {
                                    consuming = false;
                                    netAnim.SetTrigger("Emerge");
                                    sleepiness = 0f;
                                    PitMusicClientRpc(false);
                                    SetStateClientRpc(WormState.Peeping);
                                    break;
                                }
                            }
                            else
                            {
                                consuming = true;
                                sleepiness = 0f;
                                netAnim.SetTrigger("Emerge");
                                PitMusicClientRpc(true);
                                for (int i = 0; i < playersOverlapping.Count; i++)
                                {
                                    PlayerFearIncreaseClientRpc(false, 0.25f, 0, playersOverlapping[i].actualClientId);
                                }
                            }
                            sleepiness += Time.deltaTime;
                            break;
                        }
                    case WormState.Sleeping:
                        {
                            if (sleepiness > 0f)
                            {
                                sleepiness -= Time.deltaTime;
                            }
                            else
                            {
                                SetStateClientRpc(WormState.Peeping);
                                break;
                            }
                            break;
                        }
                }
            }
        }
        public void PitChomp()
        {
            if (base.IsServer && consuming)
            {
                RaycastHit[] objectsHit = Physics.SphereCastAll(this.transform.position, 2f, this.transform.up, 0f, 1074266120, QueryTriggerInteraction.Collide);
                for (int i = 0; i < objectsHit.Length; i++)
                {
                    if (objectsHit[i].transform.TryGetComponent(out PlayerControllerB player))
                    {
                        DamagePlayerCheckClientRpc(player.playerClientId);
                    }
                    else if (objectsHit[i].transform.TryGetComponent<EnemyAICollisionDetect>(out EnemyAICollisionDetect enemyCol))
                    {
                        enemyCol.mainScript.HitEnemyClientRpc(2, -1, true);
                    }
                }
            }
        }
        [ClientRpc]
        public void DamagePlayerCheckClientRpc(ulong id)
        {
            if (GameNetworkManager.Instance.localPlayerController == StartOfRound.Instance.allPlayerScripts[(int)id])
            {
                GameNetworkManager.Instance.localPlayerController.DamagePlayer(15, true, true, CauseOfDeath.Mauling, 0, false, this.transform.up * -10f);
            }
        }
        public void PitAudioAnim()
        {
            if (State == WormState.Consuming)
            {
                miscSource.pitch = (float)random.Next(9, 11) / 10f;
                miscSource.PlayOneShot(biteClip);
            }
        }
        public PlayerControllerB SelectNewPlayer(PlayerControllerB player)
        {
            if (player == null || player.isPlayerDead || (Physics.Linecast(this.transform.position + (Vector3.up / 2f), player.playerGlobalHead.position, 1107298560, QueryTriggerInteraction.Ignore) && Vector3.Distance(this.transform.position, player.transform.position) >= 7.5f))
            {
                if (playerLookingAt != null)
                {
                    log.LogDebug($"WormPit Selected Player changed to null!");
                }
                return null;
            }
            else if (player != playerLookingAt && (playerLookingAt == null || (playerLookingAt != null && Vector3.Distance(player.transform.position, this.transform.position + (Vector3.up / 2f)) < Vector3.Distance(playerLookingAt.transform.position, this.transform.position + (Vector3.up / 2f)))))
            {
                if (playerLookingAt != player)
                {
                    log.LogDebug($"WormPit Selected Player changed to {player.playerUsername}!");
                }
                return player;
            }
            return playerLookingAt;
        }
        public void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent(out PlayerControllerB player))
            {
                if (!player.isPlayerDead)
                {
                    playersOverlapping.Add(player);
                }
                else
                {
                    playersOverlapping.Remove(player);
                    if (playersOverlapping.Count == 0)
                    {
                        playerLookingAt = SelectNewPlayer(null);
                    }
                }
            }
        }
        public void OnTriggerExit(Collider other)
        {
            if (other.gameObject.TryGetComponent(out PlayerControllerB player))
            {
                playersOverlapping.Remove(player);
            }
            if (playersOverlapping.Count == 0)
            {
                playerLookingAt = SelectNewPlayer(null);
            }
        }
        [ClientRpc]
        public void IrisLookClientRpc(bool nullify, ulong id = 0)
        {
            if (lookLerp == 0f)
            {
                initialRot = irisTransform.rotation;
            }
            if (lookLerp < 360f)
            {
                lookLerp += Time.deltaTime * 270f;
            }
            else if (lookLerp > 360f)
            {
                lookLerp = 360f;
            }
            if (Quaternion.Dot(initialRot, irisTransform.rotation) >= 0.95f)
            {
                Quaternion newRot;
                if (nullify)
                {
                    newRot = Quaternion.LookRotation(this.transform.up);
                }
                else if (GameNetworkManager.Instance.localPlayerController == StartOfRound.Instance.allPlayerScripts[id])
                {
                    newRot = Quaternion.LookRotation(StartOfRound.Instance.allPlayerScripts[id].gameplayCamera.transform.position - this.transform.position);
                }
                else
                {
                    newRot = Quaternion.LookRotation(StartOfRound.Instance.allPlayerScripts[id].playerGlobalHead.transform.position - this.transform.position);
                }
                if (newRot.eulerAngles.x < 225f)
                {
                    newRot = Quaternion.Euler(225f, newRot.eulerAngles.y, newRot.eulerAngles.z);
                }
                else if (newRot.eulerAngles.x > 315f)
                {
                    newRot = Quaternion.Euler(315f, newRot.eulerAngles.y, newRot.eulerAngles.z);
                }
                targetRot = newRot;
                irisTransform.rotation = Quaternion.RotateTowards(initialRot, targetRot, lookLerp);
            }
            else
            {
                lookLerp = 0f;
            }
        }
        [ClientRpc]
        public void SetStateClientRpc(WormState newState)
        {
            State = newState;
        }
        [ClientRpc]
        public void PlayerExternalForcesClientRpc(ulong id)
        {
            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            if (player.actualClientId == id)
            {
                Vector3 pitPlayerVector = this.transform.position - player.transform.position;
                if (pitPlayerVector.magnitude < 5f || !Physics.Linecast(this.transform.position + (Vector3.up / 2f), player.cameraContainerTransform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                {
                    player.externalForces += (pitPlayerVector).normalized * (3.5f / pitPlayerVector.magnitude);
                }
            }
        }
        [ClientRpc]
        public void PlayerFearIncreaseClientRpc(bool overTime, float amount, float cap, ulong id)
        {
            if (GameNetworkManager.Instance.localPlayerController.actualClientId == id)
            {
                if (overTime)
                {
                    GameNetworkManager.Instance.localPlayerController.IncreaseFearLevelOverTime(amount, cap);
                }
                else
                {
                    GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(amount);
                }
            }
        }
        [ClientRpc]
        public void PlaySquishClientRpc(int id, float pitch)
        {
            miscSource.pitch = pitch;
            miscSource.PlayOneShot(squishClips[id], 1.5f);
        }
        [ClientRpc]
        public void PlayGrowlClientRpc(float pitch)
        {
            miscSource.pitch = pitch;
            miscSource.PlayOneShot(growlClip, 1.5f);
        }
        [ClientRpc]
        public void PitMusicClientRpc(bool play)
        {
            if (play)
            {
                pitSource.Play();
            }
            else
            {
                pitSource.Stop();
            }
        }
    }
}