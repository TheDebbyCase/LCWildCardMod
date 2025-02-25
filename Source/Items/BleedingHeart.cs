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
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
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
            if (base.IsServer && playerHeldBy == null && currentUseCooldown <= 0f && startingValue > 0)
            {
                playerMovementMag = 0f;
                if (scrapValue > 0)
                {
                    ScrapValueServerRpc(scrapValue - 1);
                    intensityValue = ((float)startingValue - (float)scrapValue) / ((float)startingValue);
                    SetIntensityServerRpc(intensityValue);
                }
                currentUseCooldown = 3f;
            }
            else if (playerHeldBy != null)
            {
                if (playerHeldBy == GameNetworkManager.Instance.localPlayerController)
                {
                    playerMovementMag = Mathf.Clamp01(GameNetworkManager.Instance.localPlayerController.moveInputVector.magnitude);
                    SetMagnitudeServerRpc(playerMovementMag);
                    SetSensitivityServerRpc(IngamePlayerSettings.Instance.settings.lookSensitivity);
                }
                log.LogDebug($"Slosh Value: {(playerMovementMag + 0.5f) * (Mathf.Max(0.1f, playerHeldBy.playerActions.Movement.Look.ReadValue<Vector2>().magnitude / 360f)) * Mathf.Max(1f, playerSensitivity)}");
                itemAnimator.Animator.SetLayerWeight(itemAnimator.Animator.GetLayerIndex("SloshingLayer"), Mathf.Max(0.1f, Mathf.Min(1f, (playerMovementMag + 0.5f) * (Mathf.Max(0.1f, playerHeldBy.playerActions.Movement.Look.ReadValue<Vector2>().magnitude / 360f)) * Mathf.Max(1f, playerSensitivity))));
            }
            if (itemAnimator.Animator.GetLayerWeight(itemAnimator.Animator.GetLayerIndex("LiquidLevelLayer")) < targetLiquidLevel - 0.01f || itemAnimator.Animator.GetLayerWeight(itemAnimator.Animator.GetLayerIndex("LiquidLevelLayer")) > targetLiquidLevel + 0.01f)
            {
                if (startWeight == -1f)
                {
                    startWeight = itemAnimator.Animator.GetLayerWeight(itemAnimator.Animator.GetLayerIndex("LiquidLevelLayer"));
                }
                weightOverTime += Time.deltaTime;
                float lerpTarget = Mathf.Lerp(startWeight, targetLiquidLevel, weightCurve.Evaluate(weightOverTime));
                itemAnimator.Animator.SetLayerWeight(itemAnimator.Animator.GetLayerIndex("LiquidLevelLayer"), lerpTarget);
                if (itemAnimator.Animator.GetLayerWeight(itemAnimator.Animator.GetLayerIndex("LiquidLevelLayer")) > targetLiquidLevel - 0.01f && itemAnimator.Animator.GetLayerWeight(itemAnimator.Animator.GetLayerIndex("LiquidLevelLayer")) < targetLiquidLevel + 0.01f)
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
            itemAnimator.Animator.SetLayerWeight(itemAnimator.Animator.GetLayerIndex("SloshingLayer"), 0.1f);
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
                RoundManager.Instance.PlayAudibleNoise(base.transform.position, 25f, 0.25f, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
                WalkieTalkie.TransmitOneShotAudio(beatAudio, beatAudio.clip);
            }
        }
        public IEnumerator ExplodeCoroutine()
        {
            EnableItemMeshes(false);
            beatAudio.PlayOneShot(smashClip);
            Landmine.SpawnExplosion(this.transform.position + Vector3.up, true, 5f, 10f, 25, 10f);
            yield return new WaitForSeconds(0.5f);
            this.NetworkObject.Despawn();
        }
        public override int GetItemDataToSave()
        {
            return startingValue;
        }
        public override void LoadItemSaveData(int saveData)
        {
            startingValue = saveData;
        }
        [ServerRpc(RequireOwnership = false)]
        public void ScrapValueServerRpc(int newValue)
        {
            ScrapValueClientRpc(newValue);
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
        [ServerRpc(RequireOwnership = false)]
        public void SetIntensityServerRpc(float intensity)
        {
            SetIntensityClientRpc(intensity);
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