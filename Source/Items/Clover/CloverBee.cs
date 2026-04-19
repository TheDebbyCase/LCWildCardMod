using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
namespace LCWildCardMod.Items.Clover
{
    public class CloverBee : PhysicsProp
    {
        BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        public NetworkAnimator itemAnimator;
        public AudioSource buzzSource;
        public AnimationCurve buzzCurve;
        public AudioSource shootSource;
        public AudioClip shootClip;
        public Transform stinger;
        public ParticleSystem stingerFlash;
        public ParticleSystem stingerHitSpark;
        internal Vector3 targetPosition;
        internal bool isShooting;
        internal float stingerTime;
        internal Vector3 startingPosition;
        internal Vector3 localPosition;
        internal Item activatedProperties;
        internal Item deactivatedProperties;
        internal PlayerControllerB lastPlayer;
        internal List<IHittable> hitList = new List<IHittable>();
        System.Random random;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            CloverNecklace.beeList.Add(this);
            random = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            buzzSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, buzzCurve);
            activatedProperties = itemProperties;
            deactivatedProperties = Instantiate(itemProperties);
            deactivatedProperties.toolTips = new string[2] { $"<s>[LMB] : Fire Stinger", "It doesn't seem to recognise you... Find the pendant..." };
            localPosition = stinger.localPosition;
            insertedBattery.charge = 1f;
            if (!IsServer)
            {
                return;
            }
            StartCoroutine(BuzzLoop());
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            CloverNecklace.beeList.Remove(this);
        }
        public override void GrabItem()
        {
            base.GrabItem();
            lastPlayer = playerHeldBy;
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!IsOwner)
            {
                return;
            }
            if (CloverNecklace.oneNecklace == null || CloverNecklace.oneNecklace.playerHeldBy != playerHeldBy)
            {
                ShootServerRpc(Vector3.zero, (float)random.Next(80, 121) / 100f);
                playerHeldBy.DamagePlayer(25, false, true, CauseOfDeath.Stabbing);
                return;
            }
            Ray ray = new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward);
            targetPosition = ray.GetPoint(20f);
            if (Physics.Raycast(ray, out RaycastHit hit, 20f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                targetPosition = ray.GetPoint(hit.distance - 0.05f);
            }
            hitList.Clear();
            ShootServerRpc(targetPosition, (float)random.Next(80, 121) / 100f);
        }
        public override void Update()
        {
            base.Update();
            if (!isShooting)
            {
                return;
            }
            stinger.position = Vector3.Lerp(startingPosition, targetPosition, stingerTime);
            stingerTime += Mathf.Abs(Time.deltaTime * 8f);
            if (stingerTime >= 1)
            {
                stingerHitSpark.Play();
                isShooting = false;
                StartCoroutine(WaitForParticles());
                return;
            }
            if (!IsOwner)
            {
                return;
            }
            RaycastHit[] objectsHit = Physics.SphereCastAll(stinger.position, 0.25f, lastPlayer.gameplayCamera.transform.forward, 0f, 1084754248, QueryTriggerInteraction.Collide);
            for (int i = 0; i < objectsHit.Length; i++)
            {
                RaycastHit hit = objectsHit[i];
                if (hit.transform.TryGetComponent(out IHittable hitComponent) && !hitList.Contains(hitComponent) && lastPlayer.transform != hit.transform)
                {
                    hitList.Add(hitComponent);
                    hitComponent.Hit(1, lastPlayer.gameplayCamera.transform.forward, lastPlayer, true, 1);
                }
            }
        }
        internal IEnumerator WaitForParticles()
        {
            yield return new WaitUntil(() => !stingerHitSpark.IsAlive());
            stinger.localPosition = localPosition;
            stingerTime = 0;
            Log.LogDebug("Clover Bee Resetting Projectile");
            if (!IsServer)
            {
                yield break;
            }
            itemAnimator.SetTrigger("New Stinger");
        }
        internal void ToggleHeld(bool held)
        {
            if (held)
            {
                itemProperties = activatedProperties;
                return;
            }
            itemProperties = deactivatedProperties;
        }
        internal IEnumerator BuzzLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds((float)random.Next(5, 51) / 10f);
                PlayBuzzClientRpc((float)random.Next(80, 121) / 100f);
            }
        }
        [ClientRpc]
        public void PlayBuzzClientRpc(float pitch)
        {
            buzzSource.pitch = pitch;
            buzzSource.Play();
            WalkieTalkie.TransmitOneShotAudio(buzzSource, buzzSource.clip);
        }
        [ServerRpc(RequireOwnership = false)]
        public void ShootServerRpc(Vector3 target, float pitch)
        {
            ShootClientRpc(target, pitch);
        }
        [ClientRpc]
        public void ShootClientRpc(Vector3 target, float pitch)
        {
            if (target != Vector3.zero)
            {
                Log.LogDebug("Clover Bee Shooting");
                targetPosition = target;
                startingPosition = stinger.position;
                isShooting = true;
                shootSource.clip = shootClip;
                shootSource.volume = 0.5f;
                stingerFlash.Play();
            }
            else
            {
                shootSource.clip = itemProperties.dropSFX;
                shootSource.volume = 1.5f;
                Log.LogDebug("Clover Bee Revolting");
            }
            shootSource.pitch = pitch;
            shootSource.Play();
            WalkieTalkie.TransmitOneShotAudio(shootSource, shootSource.clip);
            insertedBattery.empty = insertedBattery.charge <= 0;
        }
    }
}