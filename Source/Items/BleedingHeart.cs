using GameNetcodeStuff;
using System;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class BleedingHeart : PhysicsProp
    {
        readonly BepInEx.Logging.ManualLogSource log = WildCardMod.Log;
        public ScanNodeProperties scanNode;
        public NetworkAnimator itemAnimator;
        public AudioSource beatAudio;
        public AnimationCurve weightCurve;
        public ParticleSystem dripParticles;
        public AudioClip smashClip;
        public int startingValue = 0;
        public float intensityValue;
        public float targetLiquidLevel;
        public float startWeight = -1;
        public float weightOverTime = 0f;
        public float playerMovementMag;
        public int playerSensitivity;
        public int levelLayerIndex;
        public int sloshLayerIndex;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            levelLayerIndex = itemAnimator.Animator.GetLayerIndex("LiquidLevelLayer");
            sloshLayerIndex = itemAnimator.Animator.GetLayerIndex("SloshingLayer");
            StartCoroutine(StartingValueCoroutine());
        }
        public IEnumerator StartingValueCoroutine()
        {
            yield return new WaitUntil(() => scrapValue > 0);
            if (startingValue == 0)
            {
                startingValue = scrapValue;
            }
        }
        public override void Update()
        {
            base.Update();
            if (base.IsServer && currentUseCooldown <= 0f && startingValue > 0)
            {
                playerMovementMag = 0f;
                if (scrapValue > 0)
                {
                    ScrapValueClientRpc(scrapValue - 1);
                    intensityValue = ((float)startingValue - (float)scrapValue) / ((float)startingValue);
                    SetIntensityClientRpc(intensityValue);
                }
                currentUseCooldown = 3f;
            }
            if (playerHeldBy != null)
            {
                if (playerHeldBy == GameNetworkManager.Instance.localPlayerController)
                {
                    SetMagnitudeServerRpc(Mathf.Clamp01(GameNetworkManager.Instance.localPlayerController.moveInputVector.magnitude));
                    SetSensitivityServerRpc(IngamePlayerSettings.Instance.settings.lookSensitivity);
                }
                log.LogDebug($"Slosh Intensity: {(playerMovementMag + 0.5f) * (Mathf.Max(0.1f, playerHeldBy.playerActions.Movement.Look.ReadValue<Vector2>().magnitude / 360f)) * Mathf.Max(1f, playerSensitivity)}");
                itemAnimator.Animator.SetLayerWeight(sloshLayerIndex, Mathf.Max(0.1f, Mathf.Min(1f, (playerMovementMag + 0.5f) * (Mathf.Max(0.1f, playerHeldBy.playerActions.Movement.Look.ReadValue<Vector2>().magnitude / 360f)) * Mathf.Max(1f, playerSensitivity))));
            }
            float currentLiquidWeight = itemAnimator.Animator.GetLayerWeight(levelLayerIndex);
            if (currentLiquidWeight < targetLiquidLevel - 0.01f || currentLiquidWeight > targetLiquidLevel + 0.01f)
            {
                if (startWeight == -1f)
                {
                    startWeight = currentLiquidWeight;
                }
                weightOverTime += Time.deltaTime;
                float lerpTarget = Mathf.Lerp(startWeight, targetLiquidLevel, weightCurve.Evaluate(weightOverTime));
                itemAnimator.Animator.SetLayerWeight(levelLayerIndex, lerpTarget);
                currentLiquidWeight = itemAnimator.Animator.GetLayerWeight(levelLayerIndex);
                if (currentLiquidWeight > targetLiquidLevel - 0.01f && currentLiquidWeight < targetLiquidLevel + 0.01f)
                {
                    startWeight = -1f;
                    weightOverTime = 0f;
                }
            }
            if (scrapValue <= 0 && hasBeenHeld)
            {
                StartCoroutine(ExplodeCoroutine());
            }
        }
        public override void EquipItem()
        {
            base.EquipItem();
            dripParticles.Play();
            dripParticles.Emit(2);
            if (base.IsServer)
            {
                itemAnimator.Animator.SetBool("isHeld", true);
            }
        }
        public override void PocketItem()
        {
            base.PocketItem();
            dripParticles.Stop();
            dripParticles.Clear();
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            dripParticles.Play();
            dripParticles.Emit(2);
            itemAnimator.Animator.SetLayerWeight(sloshLayerIndex, 0.1f);
            if (base.IsServer)
            {
                itemAnimator.Animator.SetBool("isHeld", false);
            }
        }
        public void HeartBeat()
        {
            if (scrapValue > 0)
            {
                beatAudio.Stop();
                beatAudio.Play();
                WalkieTalkie.TransmitOneShotAudio(beatAudio, beatAudio.clip);
                RoundManager.Instance.PlayAudibleNoise(base.transform.position, 25f, 0.25f, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
            }
            if (base.IsServer)
            {
                if (itemAnimator.Animator.GetBool("isHeld"))
                {
                    itemAnimator.Animator.SetBool("isHeld", false);
                }
            }
        }
        public IEnumerator ExplodeCoroutine()
        {
            if (playerHeldBy != null)
            {
                int playerOriginalSlot = playerHeldBy.currentItemSlot;
                PlayerControllerB player = playerHeldBy;
                if (player.currentlyHeldObjectServer != this)
                {
                    player.SwitchToItemSlot(Array.FindIndex(player.ItemSlots, x => x == this));
                }
                player.DiscardHeldObject();
                if (playerOriginalSlot != player.currentItemSlot)
                {
                    player.SwitchToItemSlot(playerOriginalSlot);
                }
            }
            yield return new WaitForSeconds(0.1f);
            EnableItemMeshes(false);
            beatAudio.PlayOneShot(smashClip);
            yield return new WaitForSeconds(0.1f);
            Landmine.SpawnExplosion(this.transform.position + Vector3.up, true, 5f, 10f, 25, 10f);
            yield return new WaitForSeconds(0.5f);
            if (base.IsServer)
            {
                this.NetworkObject.Despawn();
            }
        }
        public override int GetItemDataToSave()
        {
            return startingValue;
        }
        public override void LoadItemSaveData(int saveData)
        {
            startingValue = saveData;
        }
        [ClientRpc]
        public void ScrapValueClientRpc(int newValue)
        {
            SetScrapValue(newValue);
            if (hasBeenHeld)
            {
                log.LogDebug($"Bleeding Heart Value: {scrapValue}, Starting Value: {startingValue}");
            }
        }
        [ClientRpc]
        public void SetIntensityClientRpc(float intensity)
        {
            intensityValue = intensity;
            targetLiquidLevel = intensityValue;
            ParticleSystem.Burst burst = dripParticles.emission.GetBurst(0);
            burst.probability = Mathf.Max(0.2f, Mathf.Min(0.8f, targetLiquidLevel));
            dripParticles.emission.SetBurst(0, burst);
        }
        [ServerRpc(RequireOwnership = false)]
        public void SetMagnitudeServerRpc(float magnitude)
        {
            SetMagnitudeClientRpc(magnitude);
        }
        [ClientRpc]
        public void SetMagnitudeClientRpc(float magnitude)
        {
            playerMovementMag = magnitude;
        }
        [ServerRpc(RequireOwnership = false)]
        public void SetSensitivityServerRpc(int sensitivity)
        {
            SetSensitivityClientRpc(sensitivity);
        }
        [ClientRpc]
        public void SetSensitivityClientRpc(int sensitivity)
        {
            playerSensitivity = sensitivity;
        }
    }
}