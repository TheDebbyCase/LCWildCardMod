using GameNetcodeStuff;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
namespace LCWildCardMod.MapObjects
{
    public enum WormState
    {
        Peeping,
        Hungry,
        Consuming,
        Sleeping
    }
    public class WormPit : NetworkBehaviour/*, IHittable*/
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        public AudioSource pitSource;
        public AudioSource miscSource;
        public AudioClip[] squishClips;
        public AudioClip biteClip;
        public AudioClip growlClip;
        internal WormState State = WormState.Peeping;
        public Transform irisTransform;
        public NetworkAnimator netAnim;
        public float peepCooldown;
        public float peepWaitBetween = 5f;
        public float peepWaitHold;
        PlayerControllerB playerLookingAt;
        readonly List<PlayerControllerB> playersOverlapping = new List<PlayerControllerB>();
        bool peeping;
        float lookLerp;
        Quaternion initialRot;
        Quaternion targetRot;
        public float hunger;
        public float patience = 5f;
        public float sleepiness = 0;
        bool consuming;
        System.Random random;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Log.LogDebug("Worm Pit Spawned");
            random = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
        }
        void Update()
        {
            if (!base.IsServer)
            {
                return;
            }
            for (int i = 0; i < playersOverlapping.Count; i++)
            {
                playerLookingAt = SelectNewPlayer(playersOverlapping[i]);
            }
            if (playersOverlapping.Count == 0)
            {
                playerLookingAt = SelectNewPlayer();
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
                            netAnim.Animator.SetBool("HoldPeep", false);
                            float pitch = (float)random.Next(9, 11) / 10f;
                            PlayGrowlClientRpc(pitch);
                            SetStateClientRpc(WormState.Hungry);
                            break;
                        }
                        if (playerLookingAt != null && peeping)
                        {
                            hunger += Time.deltaTime * 1.5f;
                            if (Vector3.Distance(this.transform.position + (Vector3.up / 2f), playerLookingAt.transform.position) <= 3f)
                            {
                                hunger += Time.deltaTime * 2f;
                            }
                        }
                        peepCooldown += Time.deltaTime;
                        break;
                    }
                case WormState.Hungry:
                    {
                        if ((playerLookingAt != null && Vector3.Distance(this.transform.position + (Vector3.up / 2f), playerLookingAt.transform.position) <= 2.5f) || patience <= 0f)
                        {
                            SetStateClientRpc(WormState.Consuming);
                        }
                        else if (playerLookingAt != null && Vector3.Distance(this.transform.position + (Vector3.up / 2f), playerLookingAt.transform.position) <= 10f)
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
                                PlayerControllerB player = playersOverlapping[i];
                                PlayerFearIncreaseClientRpc(true, 2f, 1f, player.actualClientId);
                                PlayerExternalForcesClientRpc(player.actualClientId);
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
                                PitMusicClientRpc(false);
                                SetStateClientRpc(WormState.Peeping);
                                break;
                            }
                        }
                        else
                        {
                            consuming = true;
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
                            break;
                        }
                        SetStateClientRpc(WormState.Peeping);
                        break;
                    }
            }
        }
        public void PitChomp()
        {
            if (!base.IsServer || !consuming)
            {
                return;
            }
            RaycastHit[] objectsHit = Physics.SphereCastAll(this.transform.position, 2f, this.transform.up, 0f, 1074266120, QueryTriggerInteraction.Collide);
            for (int i = 0; i < objectsHit.Length; i++)
            {
                RaycastHit hit = objectsHit[i];
                if (hit.transform.TryGetComponent(out PlayerControllerB player))
                {
                    DamagePlayerCheckClientRpc(player.playerClientId);
                }
                else if (hit.transform.TryGetComponent(out EnemyAICollisionDetect enemyCol))
                {
                    enemyCol.mainScript.HitEnemyClientRpc(2, -1, true);
                }
            }
        }
        [ClientRpc]
        public void DamagePlayerCheckClientRpc(ulong id)
        {
            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            if (player != StartOfRound.Instance.allPlayerScripts[(int)id])
            {
                return;
            }
            player.DamagePlayer(15, true, true, CauseOfDeath.Mauling, 0, false, this.transform.up * -10f);
        }
        public void PitAudioAnim()
        {
            if (State != WormState.Consuming)
            {
                return;
            }
            miscSource.pitch = (float)random.Next(9, 11) / 10f;
            miscSource.PlayOneShot(biteClip);
        }
        PlayerControllerB SelectNewPlayer(PlayerControllerB player = null)
        {
            if (player == null || player.isPlayerDead || (Physics.Linecast(this.transform.position + (Vector3.up / 2f), player.playerGlobalHead.position, 1107298560, QueryTriggerInteraction.Ignore) && Vector3.Distance(this.transform.position, player.transform.position) >= 7.5f))
            {
                return null;
            }
            else if (player != playerLookingAt && (playerLookingAt == null || (playerLookingAt != null && Vector3.Distance(player.transform.position, this.transform.position + (Vector3.up / 2f)) < Vector3.Distance(playerLookingAt.transform.position, this.transform.position + (Vector3.up / 2f)))))
            {
                Log.LogDebug($"WormPit Selected Player set to {player.playerUsername}!");
                return player;
            }
            return playerLookingAt;
        }
        //PlayerControllerB GetClosestPlayer()
        //{
        //    PlayerControllerB closest = null;
        //    float distance = 20f;
        //    for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
        //    {
        //        PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[i];
        //        float newDistance = Vector3.Distance(player.transform.position, this.transform.position);
        //        if (newDistance <= 20f)
        //        {
        //            playerIDInRange.Add((int)player.actualClientId);
        //        }
        //        else
        //        {
        //            playerIDInRange.Remove((int)player.actualClientId);
        //        }
        //        if (newDistance > distance)
        //        {
        //            continue;
        //        }
        //        closest = player;
        //        distance = newDistance;
        //    }
        //    return TryNewPlayer(closest);
        //}
        void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.TryGetComponent(out PlayerControllerB player))
            {
                return;
            }
            if (!player.isPlayerDead)
            {
                playersOverlapping.Add(player);
            }
            else
            {
                playersOverlapping.Remove(player);
                if (playersOverlapping.Count > 0)
                {
                    return;
                }
                playerLookingAt = SelectNewPlayer();
            }
        }
        void OnTriggerExit(Collider other)
        {
            if (other.gameObject.TryGetComponent(out PlayerControllerB player))
            {
                playersOverlapping.Remove(player);
            }
            if (playersOverlapping.Count > 0)
            {
                return;
            }
            playerLookingAt = SelectNewPlayer();
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
            if (Quaternion.Dot(initialRot, irisTransform.rotation) < 0.95f)
            {
                lookLerp = 0f;
                return;
            }
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
        [ClientRpc]
        public void SetStateClientRpc(WormState newState)
        {
            if (base.IsServer)
            {
                peeping = false;
                consuming = false;
                peepCooldown = 0f;
                hunger = 0f;
                sleepiness = 0f;
                patience = 5f;
            }
            State = newState;
        }
        [ClientRpc]
        public void PlayerExternalForcesClientRpc(ulong id)
        {
            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            if (player.actualClientId != id)
            {
                return;
            }
            Vector3 pitPlayerVector = this.transform.position - player.transform.position;
            if (pitPlayerVector.magnitude >= 5f && Physics.Linecast(this.transform.position + (Vector3.up / 2f), player.cameraContainerTransform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                return;
            }
            player.externalForces += pitPlayerVector.normalized * 3.5f * Mathf.Lerp(5.5f, 3f, pitPlayerVector.magnitude) * Mathf.Lerp(0.1f, 0.5f, pitPlayerVector.magnitude);
        }
        [ClientRpc]
        public void PlayerFearIncreaseClientRpc(bool overTime, float amount, float cap, ulong id)
        {
            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            if (player.actualClientId != id)
            {
                return;
            }
            if (overTime)
            {
                player.IncreaseFearLevelOverTime(amount, cap);
                return;
            }
            player.JumpToFearLevel(amount);
        }
        [ClientRpc]
        public void PlaySquishClientRpc(int id, float pitch)
        {
            miscSource.pitch = pitch;
            miscSource.PlayOneShot(squishClips[id], 0.75f);
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
                return;
            }
            pitSource.Stop();
        }
        //public bool Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
        //{
        //    if (State == WormState.Consuming)
        //    {
        //        return false;
        //    }
        //    HitServerRpc(playerWhoHit.actualClientId);
        //    return true;
        //}
        //[ServerRpc]
        //public void HitServerRpc(ulong id)
        //{
        //    playerLookingAt = SelectNewPlayer(StartOfRound.Instance.allPlayerScripts[(int)id]);
        //    netAnim.Animator.SetBool("HoldPeep", false);
        //    SetStateClientRpc(WormState.Consuming);
        //}
    }
}