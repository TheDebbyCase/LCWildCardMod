using LCWildCardMod.Utils;
using LethalLib.Extras;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class CloverBee : WildCardProp
    {
        [Space(3f)]
        [Header("CloverBee")]
        [Space(3f)]
        [SerializeField]
        private Transform stinger = null;
        [SerializeField]
        private string[] noNecklaceTooltips = null;
        [SerializeField]
        private float stingerSpeed = 8f;
        [SerializeField]
        private float stingerSize = 0.25f;
        [SerializeField]
        private int stingerDamage = 25;
        [SerializeField]
        private int stingerHit = 1;
        [SerializeField]
        private float stingerRange = 20f;
        private Vector3 targetPosition = Vector3.zero;
        private bool isShooting = false;
        private float stingerTime = 0f;
        private Vector3 startingPosition = Vector3.zero;
        private Vector3 stingerLocalPos = Vector3.zero;
        public override void Start()
        {
            base.Start();
            CloverNecklace.beeList.Add(this);
            Properties.Add("NoNecklace", OriginalItem.Clone());
            Properties["NoNecklace"].toolTips = noNecklaceTooltips;
            stingerLocalPos = stinger.localPosition;
            insertedBattery.charge = 1f;
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsServer)
            {
                return;
            }
            Audio["Buzz"].PlayRandomClip();
        }
        public override void OnDestroy()
        {
            CloverNecklace.beeList.Remove(this);
            base.OnDestroy();
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!IsOwner)
            {
                return;
            }
            if (CloverNecklace.oneNecklace == null || (!isHeldByEnemy && !CloverNecklace.oneNecklace.LastPlayerHeldBy.IsLocal()))
            {
                Audio["Revolt"].PlayRandomClip();
                Shoot();
                if (isHeld)
                {
                    DamagePlayerLocal(stingerDamage, CauseOfDeath.Stabbing);
                    return;
                }
                LastEnemyHeldBy.HitEnemyOnLocalClient(stingerHit, playHitSFX: true);
                return;
            }
            Ray ray = new Ray(LastPlayerHeldBy.gameplayCamera.transform.position, LastPlayerHeldBy.gameplayCamera.transform.forward);
            targetPosition = ray.GetPoint(stingerRange);
            if (Physics.Raycast(ray, out RaycastHit hit, stingerRange, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                targetPosition = ray.GetPoint(hit.distance - 0.05f);
            }
            Audio["Shoot"].PlayRandomClip();
            Particles["Flash"].PlayAll();
            Shoot(targetPosition);
        }
        public override void Update()
        {
            base.Update();
            if (!isShooting)
            {
                return;
            }
            stinger.position = Vector3.Lerp(startingPosition, targetPosition, stingerTime);
            stingerTime += Time.deltaTime * stingerSpeed;
            if (!IsOwner)
            {
                return;
            }
            if (stingerTime >= 1)
            {
                EndShoot();
                return;
            }
            List<RaycastHit> objectsHit = Physics.SphereCastAll(stinger.position, stingerSize, Vector3.one, 0f, 1084754248, QueryTriggerInteraction.Collide).ToList();
            objectsHit.Sort((x, y) => x.distance.CompareTo(y.distance));
            for (int i = 0; i < objectsHit.Count; i++)
            {
                RaycastHit hit = objectsHit[i];
                if (!hit.transform.TryGetComponent(out IHittable hitComponent) || hitComponent == LastPlayerHeldBy as IHittable)
                {
                    continue;
                }
                Base.HitOrDamage(hitComponent, stingerDamage, stingerHit, (targetPosition - startingPosition).normalized, LastPlayerHeldBy, true, 1, CauseOfDeath.Stabbing);
                EndShoot();
                break;
            }
        }
        public override void EquipItem()
        {
            base.EquipItem();
            if (!IsServer)
            {
                return;
            }
            Audio["Buzz"].SetLoop(false);
        }
        public override void PocketItem()
        {
            base.PocketItem();
            if (!IsServer)
            {
                return;
            }
            Audio["Buzz"].SetLoop(false);
        }
        public override void DiscardItem()
        {
            if (IsServer)
            {
                Audio["Buzz"].SetLoop(true);
            }
            base.DiscardItem();
        }
        private IEnumerator WaitForParticles()
        {
            SelectParticles particle = Particles["Sparks"];
            yield return new WaitUntil(() => !particle.AnyAlive());
            stinger.localPosition = stingerLocalPos;
            stingerTime = 0;
            Log.LogDebug("Clover Bee Resetting Projectile");
            if (!IsServer)
            {
                yield break;
            }
            Animator.Trigger("New Stinger");
        }
        internal void ToggleHeld(bool held)
        {
            if (held)
            {
                itemProperties = OriginalItem;
                return;
            }
            itemProperties = Properties["NoNecklace"];
        }
        private void Shoot(Vector3 target = default, bool networked = true)
        {
            insertedBattery.empty = insertedBattery.charge <= 0;
            if (target != default)
            {
                Log.LogDebug("Clover Bee Shooting");
                targetPosition = target;
                startingPosition = stinger.position;
                isShooting = true;
            }
            if (!networked)
            {
                return;
            }
            ShootRpc(target);
        }
        private void EndShoot(bool networked = true)
        {
            Particles["Sparks"].PlayAll(networked: false);
            isShooting = false;
            StartCoroutine(WaitForParticles());
            if (!networked)
            {
                return;
            }
            EndShootRpc();
        }
        [Rpc(SendTo.NotMe)]
        private void ShootRpc(Vector3 target = default)
        {
           Shoot(target, false);
        }
        [Rpc(SendTo.NotMe)]
        private void EndShootRpc()
        {
            EndShoot(false);
        }
    }
}