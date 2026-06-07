using GameNetcodeStuff;
using LCWildCardMod.Utils;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class Hellgato : WildCardThrowable, ILifeSaver
    {
        int ILifeSaver.Priority => -1;
        [Space(3f)]
        [Header("Hellgato")]
        [Space(3f)]
        [SerializeField]
        private int playerDamage = 15;
        [SerializeField]
        private int hitAmount = 1;
        [SerializeField]
        private float damageRadius = 5f;
        [SerializeField]
        private float killRadius = 2f;
        [SerializeField]
        private float forceMultiplier = 2f;
        [SerializeField]
        private float saveForceMultiplier = 0.5f;
        private bool exploding = false;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            ILifeSaver.Register(this);
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            ILifeSaver.Unregister(this);
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            Particles["Burst"].PlayAll(networked: false);
            if (!IsOwner)
            {
                return;
            }
            HashSet<IHittable> hits = new HashSet<IHittable>();
            RaycastHit[] objectsHit = Physics.SphereCastAll(transform.position, damageRadius, Vector3.one, 0f, 1084754248, QueryTriggerInteraction.Collide);
            for (int i = 0; i < objectsHit.Length; i++)
            {
                RaycastHit hit = objectsHit[i];
                if (!hit.transform.TryGetComponent(out IHittable hitComponent) || !hits.Add(hitComponent))
                {
                    continue;
                }
                PlayerControllerB playerHitBy = null;
                if (isHeld)
                {
                    playerHitBy = LastPlayerHeldBy;
                }
                Base.HitOrDamage(hitComponent, playerDamage, hitAmount, (hit.transform.position - transform.position).normalized, playerHitBy, true, 1, CauseOfDeath.Burning, forceMultiplier);
            }
        }
        public override void EquipItem()
        {
            base.EquipItem();
            Audio["Crackle"].SetLoop(true);
            Particles["Flame"].PlayAll(networked: false);
            Animations["Idle"].ResumeAll();
        }
        public override void PocketItem()
        {
            base.PocketItem();
            Audio["Crackle"].SetLoop(false);
            Particles["Flame"].StopAll(true, false);
            Animations["Idle"].PauseAll();
        }
        public override void OnHitGround()
        {
            if (IsOwner && throwing)
            {
                ExplodeRpc();
            }
            base.OnHitGround();
        }
        [Rpc(SendTo.Everyone)]
        private void ExplodeRpc()
        {
            Landmine.SpawnExplosion(transform.position, true, killRadius, damageRadius, playerDamage * 3, 0.5f);
            if (!IsOwner)
            {
                return;
            }
            if (isHeld)
            {
                LastPlayerHeldBy.DespawnHeldObject();
                return;
            }
            NetworkObject.Despawn();
        }
        bool ILifeSaver.CanSave(PlayerControllerB player, CauseOfDeath cause)
        {
            return isHeld && !isPocketed && LastPlayerHeldBy == player;
        }
        bool ILifeSaver.GraceWhen(PlayerControllerB player, CauseOfDeath cause)
        {
            return exploding;
        }
        void ILifeSaver.Save(PlayerControllerB player, CauseOfDeath cause, Vector3 hitVelocity, EnemyAI enemy)
        {
            if (!player.IsLocal() || exploding)
            {
                return;
            }
            exploding = true;
            player.externalForceAutoFade += hitVelocity * saveForceMultiplier;
            player.health = 20;
            HUDManager.Instance.UpdateHealthUI(20, false);
            ExplodeRpc();
        }
        bool ILifeSaver.TriggerWhen(PlayerControllerB player, CauseOfDeath cause)
        {
            return !exploding && isHeld && !isPocketed && LastPlayerHeldBy == player;
        }
        bool ILifeSaver.UntargetableWhen(PlayerControllerB player)
        {
            return false;
        }
    }
}