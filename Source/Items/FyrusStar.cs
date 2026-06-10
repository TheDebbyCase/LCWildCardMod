using GameNetcodeStuff;
using LCWildCardMod.Utils;
using System.Collections;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class FyrusStar : WildCardProp, ILifeSaver
    {
        int ILifeSaver.Priority => 1;
        [Space(3f)]
        [Header("FyrusStar")]
        [Space(3f)]
        [SerializeField]
        private Transform musicTransform = null;
        [SerializeField]
        private float speedMultiplier = 1.25f;
        [SerializeField]
        private float hitCooldownMax = 0.5f;
        [SerializeField]
        private int hitAmount = 1;
        [SerializeField]
        private float forceMultiplier = 0.5f;
        private TrailRenderer trailRenderer = null;
        private float inverseSpeedMultiplier = 0f;
        private float hitCooldown = 0f;
        private bool active = false;
        private bool activated = false;
        public override void Start()
        {
            base.Start();
            ILifeSaver.Register(this);
            trailRenderer = musicTransform.GetComponent<TrailRenderer>();
            hitCooldown = hitCooldownMax;
        }
        public override void OnDestroy()
        {
            ILifeSaver.Unregister(this);
            if (active)
            {
                musicTransform.SetParent(transform);
                LastPlayerHeldBy.MultiplyPlayerSpeed(inverseSpeedMultiplier);
            }
            base.OnDestroy();
        }
        public override void PocketItem()
        {
            base.PocketItem();
            Particles["Sparkles"].StopAll(true, false);
        }
        public override void EquipItem()
        {
            base.EquipItem();
            Particles["Sparkles"].PlayAll(false, false);
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            Particles["Sparkles"].PlayAll(false, false);
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            activated = true;
            StartCoroutine(StarCoroutine(IsOwner, !isHeld));
            if (isHeld)
            {
                LastPlayerHeldBy.DiscardHeldObject();
                return;
            }
            EnemyForceDropItem(true);
        }
        public override void Update()
        {
            base.Update();
            if (!active || !isHeld)
            {
                return;
            }
            if (hitCooldown <= 0f)
            {
                return;
            }
            hitCooldown -= Time.deltaTime;
        }
        public override void PlayDropSFX()
        {
            if (!activated)
            {
                base.PlayDropSFX();
                return;
            }
            hasHitGround = true;
        }
        private IEnumerator StarCoroutine(bool isLocal, bool byEnemy)
        {
            yield return new WaitForEndOfFrame();
            Particles["Sparkles"].StopAll(true, false);
            EnableItemMeshes(false);
            EnablePhysics(false);
            grabbable = false;
            yield return new WaitForSeconds(1.2f);
            Transform musicParent;
            if (byEnemy)
            {
                musicParent = LastEnemyHeldBy.transform;
                LastEnemyHeldBy.agent.speed *= speedMultiplier;
            }
            else
            {
                Log.LogDebug($"{LastPlayerHeldBy.playerUsername} has begun Fyrus Star invincibility");
                LastPlayerHeldBy.MultiplyPlayerSpeed(speedMultiplier);
                musicParent = LastPlayerHeldBy.transform;
            }
            inverseSpeedMultiplier = 1f / speedMultiplier;
            musicTransform.SetParent(musicParent);
            musicTransform.localPosition = new Vector3(0f, 1f, -1f);
            active = true;
            SelectAudioClips audio = Audio["StarStart"];
            if (isLocal)
            {
                audio.Set3DSettings(newDoppler: 0f);
                audio.PlayRandomClip();
            }
            yield return new WaitUntil(() => audio.IsPlaying);
            trailRenderer.emitting = true;
            yield return new WaitUntil(() => !audio.IsPlaying);
            if (byEnemy)
            {
                LastEnemyHeldBy.agent.speed *= inverseSpeedMultiplier;
            }
            else
            {
                LastPlayerHeldBy.MultiplyPlayerSpeed(inverseSpeedMultiplier);
                Log.LogDebug($"{LastPlayerHeldBy.playerUsername}'s Fyrus Star invincibility has ended");
            }
            if (isLocal)
            {
                Audio["StarEnd"].PlayRandomOneshot();
            }
            trailRenderer.emitting = false;
            musicTransform.SetParent(transform);
            active = false;
            if (!IsServer)
            {
                yield break;
            }
            yield return new WaitForSeconds(1f);
            NetworkObject.Despawn();
        }
        bool ILifeSaver.UntargetableWhen(PlayerControllerB player)
        {
            return false;
        }
        bool ILifeSaver.TriggerWhen(PlayerControllerB player, CauseOfDeath cause)
        {
            return active && LastPlayerHeldBy == player;
        }
        bool ILifeSaver.CanSave(PlayerControllerB player, CauseOfDeath cause)
        {
            return active && LastPlayerHeldBy == player;
        }
        void ILifeSaver.Save(PlayerControllerB player, CauseOfDeath cause, Vector3 hitVelocity, EnemyAI enemy)
        {
            WildCardMod.Instance.Log.LogDebug($"Fyrus star saved {player.playerUsername}!");
            if (!player.IsLocal())
            {
                return;
            }
            player.externalForceAutoFade += hitVelocity * forceMultiplier;
            if (hitCooldown > 0f)
            {
                return;
            }
            IHittable hittable = enemy?.GetComponentInChildren<IHittable>();
            if (hittable == null)
            {
                return;
            }
            hittable.Hit(hitAmount, (enemy.transform.position - player.transform.position).normalized, player, true, 1);
            hitCooldown = hitCooldownMax;
        }
        bool ILifeSaver.GraceWhen(PlayerControllerB player, CauseOfDeath cause)
        {
            return active && LastPlayerHeldBy == player;
        }
    }
}