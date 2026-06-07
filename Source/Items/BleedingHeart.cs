using GameNetcodeStuff;
using LCWildCardMod.Utils;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class BleedingHeart : WildCardProp
    {
        [Space(3f)]
        [Header("BleedingHeart")]
        [Space(3f)]
        [SerializeField]
        private AnimationCurve weightCurve = default;
        [SerializeField]
        private int valueLoss = 5;
        [SerializeField]
        private float minSlosh = 0.01f;
        [SerializeField]
        private Vector2 burstMinMax = new Vector2(0.2f, 0.8f);
        [SerializeField]
        private int burstIndex = 0;
        [SerializeField]
        private float killRange = 5f;
        [SerializeField]
        private float damageRange = 10f;
        [SerializeField]
        private int playerDamage = 25;
        [SerializeField]
        private float forceMultiplier = 10f;
        private int startingValue = 0;
        private float inverseStartingValue = 0f;
        private float intensityValue = default;
        private float targetLiquidLevel = default;
        private float startWeight = -1;
        private float weightOverTime = 0f;
        private float movementMag = default;
        private int sensitivity = default;
        private int levelLayerIndex = default;
        private int sloshLayerIndex = default;
        private bool exploding = default;
        public override void Start()
        {
            base.Start();
            levelLayerIndex = Animator.Original.GetLayerIndex("LiquidLevelLayer");
            sloshLayerIndex = Animator.Original.GetLayerIndex("SloshingLayer");
            SetStartingValue();
        }
        internal override void OnEnable()
        {
            EventsClass.OnRoundStart += SetStartingValue;
        }
        internal override void OnDisable()
        {
            EventsClass.OnRoundStart -= SetStartingValue;
        }
        private void SetStartingValue()
        {
            if (startingValue == 0)
            {
                startingValue = ScrapValue;
            }
            inverseStartingValue = 1f / (float)Mathf.Max(1, startingValue);
        }
        public override void Update()
        {
            base.Update();
            if (IsServer && currentUseCooldown <= 0f && !isHeld && !isHeldByEnemy && startingValue > 0 && !StartOfRound.Instance.inShipPhase)
            {
                movementMag = 0f;
                if (scrapValue > 0)
                {
                    ScrapValue -= valueLoss;
                    SetIntensity(((float)startingValue - (float)ScrapValue) * inverseStartingValue);
                }
                currentUseCooldown = useCooldown;
            }
            if (isHeld)
            {
                if (LastPlayerHeldBy.IsLocal())
                {
                    SetSlosh(Mathf.Clamp01(LastPlayerHeldBy.moveInputVector.magnitude), IngamePlayerSettings.Instance.settings.lookSensitivity);
                }
                float sloshClamp = Mathf.Sqrt(minSlosh);
                Animator.Original.SetLayerWeight(sloshLayerIndex, Mathf.Clamp((movementMag + 0.5f) * (Mathf.Max(sloshClamp, LastPlayerHeldBy.playerActions.Movement.Look.ReadValue<Vector2>().magnitude * 0.002777f)) * Mathf.Max(1f, (float)sensitivity), sloshClamp, 1f));
            }
            else if (isHeldByEnemy)
            {
                if (LastEnemyHeldBy.IsOwner)
                {
                    SetSlosh(Mathf.Lerp(0f, 1f, LastEnemyHeldBy.agent.speed * 0.05f), 50);
                }
                float sloshClamp = Mathf.Sqrt(minSlosh);
                Animator.Original.SetLayerWeight(sloshLayerIndex, Mathf.Clamp((movementMag + 0.5f) * 0.25f * Mathf.Max(1f, (float)sensitivity), sloshClamp, 1f));
            }
            if (scrapValue <= 0 && hasBeenHeld && !exploding)
            {
                StartCoroutine(ExplodeCoroutine());
            }
            float currentLiquidWeight = Animator.Original.GetLayerWeight(levelLayerIndex);
            if (Mathf.Approximately(currentLiquidWeight, targetLiquidLevel))
            {
                return;
            }
            if (Mathf.Approximately(startWeight, -1f))
            {
                startWeight = currentLiquidWeight;
            }
            weightOverTime += Time.deltaTime;
            float lerpTarget = Mathf.Lerp(startWeight, targetLiquidLevel, weightCurve.Evaluate(weightOverTime));
            Animator.Original.SetLayerWeight(levelLayerIndex, lerpTarget);
            currentLiquidWeight = Animator.Original.GetLayerWeight(levelLayerIndex);
            if (!Mathf.Approximately(currentLiquidWeight, targetLiquidLevel))
            {
                return;
            }
            startWeight = -1f;
            weightOverTime = 0f;
        }
        public override void EquipItem()
        {
            base.EquipItem();
            SelectParticles particle = Particles["Drip"];
            particle.PlayAll(networked: false);
            particle.EmitAll(2, false);
        }
        public override void PocketItem()
        {
            base.PocketItem();
            Particles["Drip"].StopAll(true, false);
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            SelectParticles particle = Particles["Drip"];
            if (!IsServer || particle.AnyAlive())
            {
                return;
            }
            particle.PlayAll();
            particle.EmitAll(2);
        }
        internal override void GrabFromAny(bool fromPlayer = true, EnemyAI enemy = null)
        {
            base.GrabFromAny(fromPlayer, enemy);
            Animations["Beat"].ResumeAll();
        }
        internal override void DiscardFromAny(bool fromPlayer = true, EnemyAI enemy = null)
        {
            base.DiscardFromAny(fromPlayer, enemy);
            Animator.Original.SetLayerWeight(sloshLayerIndex, 0.1f);
            SelectAnimationParameters anim = Animations["Beat"];
            anim.ResetAllTimers();
            anim.ResumeAll();
        }
        public void HeartBeat()
        {
            if (scrapValue <= 0)
            {
                return;
            }
            Audio["HeartBeat"].PlayRandomClip(networked: false);
        }
        private IEnumerator ExplodeCoroutine()
        {
            Log.LogDebug("Bleeding Heart is exploding!");
            exploding = true;
            if (isHeld)
            {
                PlayerControllerB player = LastPlayerHeldBy;
                int playerOriginalSlot = player.currentItemSlot;
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
            else if (isHeldByEnemy)
            {
                EnemyForceDropItem();
            }
            yield return new WaitForEndOfFrame();
            EnableItemMeshes(false);
            if (IsServer)
            {
                Audio["Break"].PlayRandomOneshot();
            }
            yield return new WaitForEndOfFrame();
            Landmine.SpawnExplosion(transform.position + Vector3.up, true, killRange, damageRange, playerDamage, forceMultiplier);
            yield return new WaitForSeconds(0.5f);
            if (!IsServer)
            {
                yield break;
            }
            NetworkObject.Despawn();
        }
        public override int GetItemDataToSave()
        {
            return startingValue;
        }
        public override void LoadItemSaveData(int saveData)
        {
            startingValue = saveData;
            SetStartingValue();
        }
        private void SetIntensity(float intensity, bool networked = true)
        {
            intensityValue = intensity;
            targetLiquidLevel = intensityValue;
            SelectParticles particle = Particles["Drip"];
            BurstIntermediary burst = particle.bursts[burstIndex];
            burst.probability = Mathf.Clamp(targetLiquidLevel, burstMinMax.x, burstMinMax.y);
            particle.SetBurst(burstIndex, burst);
            particle.AllApplyEmission();
            if (!networked)
            {
                return;
            }
            SetIntensityRpc(intensity);
        }
        [Rpc(SendTo.NotMe)]
        private void SetIntensityRpc(float intensity)
        {
            SetIntensity(intensity, false);
        }
        private void SetSlosh(float magnitude, int sensitivity, bool networked = true)
        {
            movementMag = magnitude;
            this.sensitivity = sensitivity;
            if (!networked)
            {
                return;
            }
            SetSloshRpc(magnitude, sensitivity);
        }
        [Rpc(SendTo.NotMe)]
        private void SetSloshRpc(float magnitude, int sensitivity)
        {
            SetSlosh(magnitude, sensitivity, false);
        }
    }
}