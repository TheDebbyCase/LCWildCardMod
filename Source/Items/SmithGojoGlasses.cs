using GameNetcodeStuff;
using LCWildCardMod.Utils;
using Unity.Netcode;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class SmithGojoGlasses : WildCardProp, ILifeSaver
    {
        [Space(3f)]
        [Header("SmithGojoGlasses")]
        [Space(3f)]
        [SerializeField]
        private Transform meshTransform = default;
        [SerializeField]
        private float enemyTimer = 1f;
        [SerializeField]
        private GameObject effectsGameObject = default;
        [SerializeField]
        private int hitAmount = 1;
        [SerializeField]
        internal float playerForce = 2.5f;
        private ListDict<int, RepeatingTimer> enemyCooldowns = new ListDict<int, RepeatingTimer>();
        private bool active = false;
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
        public override void OnDestroy()
        {
            meshTransform.SetParent(transform);
            base.OnDestroy();
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            ToggleEffects();
        }
        private void ToggleEffects(bool? overrideActive = null)
        {
            if (overrideActive.HasValue)
            {
                active = overrideActive.Value;
            }
            else
            {
                active = !active;
            }
            Animator.SetBool("Lifted", !active);
            effectsGameObject.SetActive(active);
            if (!IsOwner)
            {
                return;
            }
            if (!active)
            {
                Particles["Sparkles"].StopAll(true);
                Audio["Technique"].Stop();
                enemyCooldowns.Clear();
                return;
            }
            Particles["Sparkles"].PlayAll(true);
            Audio["Technique"].PlayRandomClip();
        }
        public override void GrabItemFromEnemy(EnemyAI enemy)
        {
            Animator.SetBool("IsEnemy", true);
            meshTransform.SetParent(enemy.eye);
            base.GrabItemFromEnemy(enemy);
        }
        public override void EquipItem()
        {
            base.EquipItem();
            Animator.SetBool("IsEnemy", false);
            Animator.SetBool("IsOwner", IsOwner);
            if (IsOwner)
            {
                meshTransform.SetParent(LastPlayerHeldBy.gameplayCamera.transform);
                return;
            }
            meshTransform.SetParent(LastPlayerHeldBy.bodyParts[0]);
        }
        public override void PocketItem()
        {
            ToggleEffects(false);
            meshTransform.SetParent(transform);
            base.PocketItem();
        }
        public override void UseUpBatteries()
        {
            base.UseUpBatteries();
            ToggleEffects(false);
        }
        internal override void DiscardFromAny(bool fromPlayer = true, EnemyAI enemy = null)
        {
            base.DiscardFromAny(fromPlayer, enemy);
            ToggleEffects(false);
            meshTransform.SetParent(transform);
        }
        internal void EnemyOverlapped(EnemyAI enemy)
        {
            if (!active || (!isHeld && (!isHeldByEnemy || enemy == LastEnemyHeldBy)) || isPocketed)
            {
                return;
            }
            int id = enemy.thisEnemyIndex;
            if (!enemyCooldowns.ContainsKey(id))
            {
                enemyCooldowns.Add(id, new RepeatingTimer(enemyTimer));
            }
            RepeatingTimer thisTimer = enemyCooldowns[target: id];
            if (!thisTimer.Complete)
            {
                return;
            }
            thisTimer.Restart();
            EnemyBehaviourRpc(enemy.NetworkObject, enemy.RpcTarget.Single(enemy.OwnerClientId, RpcTargetUse.Temp));
            IHittable hittable = enemy?.GetComponentInChildren<IHittable>();
            if (hittable == null)
            {
                return;
            }
            Audio["Hit"].PlayRandomClip();
            hittable.Hit(hitAmount, (enemy.transform.position - LastPlayerHeldBy.transform.position).normalized, LastPlayerHeldBy, true, 1);
        }
        [Rpc(SendTo.SpecifiedInParams)]
        private void EnemyBehaviourRpc(NetworkObjectReference enemyRef, RpcParams rpcParams)
        {
            if (!enemyRef.TryGet(out NetworkObject enemyObject) ||!enemyObject.TryGetComponent(out EnemyAI enemy))
            {
                return;
            }
            enemy.StopSearch(enemy.currentSearch);
            enemy.SetDestinationToPosition(enemy.ChooseFarthestNodeFromPosition(transform.position).position);
        }
        public override void Update()
        {
            base.Update();
            if (!IsOwner || !active)
            {
                return;
            }
            for (int i = 0; i < enemyCooldowns.Count; i++)
            {
                enemyCooldowns[index: i].Tick();
            }
        }
        bool ILifeSaver.UntargetableWhen(PlayerControllerB player)
        {
            return active && isHeld && player == LastPlayerHeldBy;
        }
        bool ILifeSaver.GraceWhen(PlayerControllerB player, CauseOfDeath cause)
        {
            return active && isHeld && LastPlayerHeldBy == player && (cause == CauseOfDeath.Gunshots || cause == CauseOfDeath.Blast);
        }
        bool ILifeSaver.TriggerWhen(PlayerControllerB player, CauseOfDeath cause)
        {
            return false;
        }
        bool ILifeSaver.CanSave(PlayerControllerB player, CauseOfDeath cause)
        {
            return false;
        }
        void ILifeSaver.Save(PlayerControllerB player, CauseOfDeath cause, Vector3 hitVelocity, EnemyAI enemy)
        {
            
        }
    }
    public class GojoTrigger : MonoBehaviour
    {
        [SerializeField]
        private SmithGojoGlasses glasses = null;
        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Player") && other.gameObject.TryGetComponent(out PlayerControllerB player) && (glasses.isHeld && player == glasses.LastPlayerHeldBy))
            {
                player.externalForceAutoFade += (player.transform.position - transform.position).normalized * glasses.playerForce;
                return;
            }
            if (!glasses.IsOwner || !other.CompareTag("Enemy") || !other.gameObject.TryGetComponent(out EnemyAICollisionDetect enemy) || (glasses.isHeldByEnemy && enemy.mainScript != glasses.LastEnemyHeldBy))
            {
                return;
            }
            glasses.EnemyOverlapped(enemy.mainScript);
        }
    }
}