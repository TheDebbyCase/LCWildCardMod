using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
namespace LCWildCardMod.Items.Clover
{
    public class CloverBee : PhysicsProp
    {
        readonly BepInEx.Logging.ManualLogSource log = WildCardMod.Log;
        public NetworkAnimator itemAnimator;
        public AudioSource buzzSource;
        public AudioSource shootSource;
        public Vector3 targetPosition;
        public bool isShooting;
        public float stingerTime;
        public Vector3 startingPosition;
        public Vector3 localPosition;
        public Transform stinger;
        public PlayerControllerB lastPlayer;
        public List<IHittable> hitList = new List<IHittable>();
        public CloverNecklace necklaceRef;
        public readonly string necklaceString = "It doesn't seem to recognise you... Find the pendant...";
        public readonly string fireString = "[LMB] : Fire Stinger";
        private System.Random random;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            random = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            localPosition = stinger.localPosition;
            insertedBattery.charge = 1f;
            if (base.IsServer)
            {
                StartCoroutine(BuzzLoop());
                StartCoroutine(NecklaceCheck());
            }
        }
        public override void GrabItem()
        {
            base.GrabItem();
            lastPlayer = playerHeldBy;
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (base.IsOwner && necklaceRef != null && necklaceRef.playerHeldBy == playerHeldBy)
            {
                Ray ray = new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, 20f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                {
                    targetPosition = ray.GetPoint(hit.distance - 0.05f);
                }
                else
                {
                    targetPosition = ray.GetPoint(20f);
                }
                if (hitList.Count > 0)
                {
                    hitList.Clear();
                }
                ShootServerRpc(targetPosition, (float)random.Next(80, 121) / 100f, true);
            }
            else if (base.IsOwner)
            {
                ShootServerRpc(Vector3.zero, (float)random.Next(80, 121) / 100f, false);
                playerHeldBy.DamagePlayer(25, false, true, CauseOfDeath.Stabbing);
            }
        }
        public override void Update()
        {
            base.Update();
            if (isShooting)
            {
                stinger.position = Vector3.Lerp(startingPosition, targetPosition, stingerTime);
                stingerTime += Mathf.Abs(Time.deltaTime * 8f);
                if (stingerTime >= 1)
                {
                    if (base.IsServer)
                    {
                        log.LogDebug("Clover Bee Resetting Projectile");
                        itemAnimator.SetTrigger("New Stinger");
                    }
                    isShooting = false;
                    stinger.localPosition = localPosition;
                    stingerTime = 0;
                }
                else if (base.IsOwner)
                {
                    RaycastHit[] objectsHit = Physics.SphereCastAll(stinger.position, 0.25f, lastPlayer.gameplayCamera.transform.forward, 0f, 1084754248, QueryTriggerInteraction.Collide);
                    for (int i = 0; i < objectsHit.Length; i++)
                    {
                        if (objectsHit[i].transform.TryGetComponent<IHittable>(out var hitComponent) && !hitList.Contains(hitComponent) && lastPlayer.transform != objectsHit[i].transform && (objectsHit[i].transform.GetComponent<PlayerControllerB>() || objectsHit[i].transform.GetComponent<EnemyAICollisionDetect>()))
                        {
                            hitList.Add(hitComponent);
                            hitComponent.Hit(1, lastPlayer.gameplayCamera.transform.forward, lastPlayer, true, 1);
                        }
                    }
                }
            }

        }
        public void SetNecklaceController(CloverNecklace necklace)
        {
            log.LogDebug($"Located {necklace.itemProperties.itemName}");
            necklaceRef = necklace;
            ToggleHeld(false);
        }
        public void RemoveNecklaceController(CloverNecklace necklace)
        {
            if (necklace == necklaceRef)
            {
                necklaceRef = null;
            }
        }
        public void ToggleHeld(bool held)
        {
            Item newProperties = Instantiate(itemProperties);
            if (held)
            {
                newProperties.toolTips = new string[1] {fireString};
                itemProperties = newProperties;
            }
            else
            {
                newProperties.toolTips = new string[2] {$"<s>{fireString}", necklaceString};
                itemProperties = newProperties;
            }
        }
        public IEnumerator NecklaceCheck()
        {
            yield return new WaitUntil(() => RoundManager.Instance.dungeonFinishedGeneratingForAllPlayers || StartOfRound.Instance.inShipPhase);
            CloverNecklace necklace = FindObjectOfType<CloverNecklace>();
            if (necklace != null && necklaceRef != necklace)
            {
                necklaceRef = necklace;
                necklaceRef.beeList.Add(this);
            }
        }
        public IEnumerator BuzzLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds((float)random.Next(5, 51) / 10f);
                PlayBuzzServerRpc((float)random.Next(80, 121) / 100f);
                RoundManager.Instance.PlayAudibleNoise(base.transform.position, 25f, 0.75f, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
            }
        }
        [ServerRpc(RequireOwnership = false)]
        public void PlayBuzzServerRpc(float pitch)
        {
            PlayBuzzClientRpc(pitch);
        }
        [ClientRpc]
        public void PlayBuzzClientRpc(float pitch)
        {
            buzzSource.pitch = pitch;
            buzzSource.Play();
            WalkieTalkie.TransmitOneShotAudio(buzzSource, buzzSource.clip);
        }
        [ServerRpc(RequireOwnership = false)]
        public void ShootServerRpc(Vector3 target, float pitch, bool hasNecklace)
        {
            ShootClientRpc(target, pitch, hasNecklace);
        }
        [ClientRpc]
        public void ShootClientRpc(Vector3 target, float pitch, bool hasNecklace)
        {
            if (hasNecklace)
            {
                log.LogDebug("Clover Bee Shooting");
                targetPosition = target;
                startingPosition = stinger.position;
                isShooting = true;
            }
            else
            {
                log.LogDebug("Clover Bee Revolting");
            }
            shootSource.pitch = pitch;
            shootSource.Play();
            WalkieTalkie.TransmitOneShotAudio(shootSource, shootSource.clip);
            if (insertedBattery.charge <= 0 && !insertedBattery.empty)
            {
                insertedBattery.empty = true;
            }
        }
    }
}