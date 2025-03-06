using GameNetcodeStuff;
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
        public float peepWaitBetween = 10f;
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
                switch (State)
                {
                    case WormState.Peeping:
                        {
                            if (peeping)
                            {
                                if (playerLookingAt != null)
                                {
                                    playerLookingAt.IncreaseFearLevelOverTime(1f, 0.5f);
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
                                    peepWaitBetween = ((float)random.Next(50, 101) / 10f);
                                    peepWaitHold = ((float)random.Next(30, 81) / 10f);
                                    peeping = true;
                                    peepCooldown = 0f;
                                }
                            }
                            if (hunger >= 25f)
                            {
                                peeping = false;
                                peepCooldown = 0f;
                                netAnim.Animator.SetBool("HoldPeep", false);
                                hunger = 0f;
                                float pitch = (float)random.Next(9, 11) / 10f;
                                PlayGrowlClientRpc(pitch);
                                State = WormState.Hungry;
                                break;
                            }
                            if (playerLookingAt != null && peeping)
                            {
                                hunger += Time.deltaTime * 1.5f;
                                if (Vector3.Distance(this.transform.position, playerLookingAt.transform.position) <= 7.5f)
                                {
                                    hunger += Time.deltaTime * 2f;
                                }
                            }
                            peepCooldown += Time.deltaTime;
                            break;
                        }
                    case WormState.Hungry:
                        {
                            if (Vector3.Distance(this.transform.position, playerLookingAt.transform.position) <= 5f || patience <= 0f)
                            {
                                patience = 10f;
                                State = WormState.Consuming;
                                break;
                            }
                            else if (Vector3.Distance(this.transform.position, playerLookingAt.transform.position) <= 15f)
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
                                    playersOverlapping[i].IncreaseFearLevelOverTime(5f, 5f);
                                    playersOverlapping[i].externalForces += (this.transform.position - playersOverlapping[i].transform.position).normalized * 3.5f;
                                }
                                if (sleepiness >= 12.5f)
                                {
                                    consuming = false;
                                    netAnim.SetTrigger("Emerge");
                                    sleepiness = ((float)random.Next(75, 151) / 10f);
                                    float pitch = (float)random.Next(9, 11) / 10f;
                                    PlayGrowlClientRpc(pitch);
                                    PitMusicClientRpc(false);
                                    State = WormState.Sleeping;
                                    break;
                                }
                                else if ((playersOverlapping.Count == 0 && sleepiness >= 7.5f))
                                {
                                    consuming = false;
                                    netAnim.SetTrigger("Emerge");
                                    sleepiness = 0f;
                                    PitMusicClientRpc(false);
                                    State = WormState.Peeping;
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
                                    playersOverlapping[i].JumpToFearLevel(0.5f, true);
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
                                State = WormState.Peeping;
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
                        player.DamagePlayer(15, true, true, CauseOfDeath.Mauling, 0, false, this.transform.up * -10f);
                    }
                    else if (objectsHit[i].transform.GetComponent<EnemyAICollisionDetect>() && objectsHit[i].transform.TryGetComponent(out IHittable hitComponent))
                    {
                        hitComponent.Hit(2, this.transform.up * -1f, null, true);
                    }
                }
            }
        }
        public void PitAudioAnim()
        {
            miscSource.pitch = (float)random.Next(9, 11) / 10f;
            miscSource.PlayOneShot(biteClip);
        }
        public PlayerControllerB SelectNewPlayer(PlayerControllerB player)
        {
            if ((player == null || player.isPlayerDead) || (Physics.Linecast(this.transform.position, player.playerGlobalHead.position, 1107298560, QueryTriggerInteraction.Ignore) && Vector3.Distance(this.transform.position, playerLookingAt.transform.position) >= 7.5f))
            {
                if (playerLookingAt != null)
                {
                    log.LogDebug($"WormPit Selected Player changed to null!");
                }
                return null;
            }
            else if (player != playerLookingAt && (playerLookingAt == null || (Vector3.Distance(player.transform.position, this.transform.position) < Vector3.Distance(playerLookingAt.transform.position, this.transform.position))))
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