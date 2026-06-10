using GameNetcodeStuff;
using LCWildCardMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class SmithHalo : WildCardThrowable, ILifeSaver
    {
        int ILifeSaver.Priority => 0;
        [Space(3f)]
        [Header("SmithHalo")]
        [Space(3f)]
        [SerializeField]
        private int playerDamage = 10;
        [SerializeField]
        private int hitAmount = 1;
        [SerializeField]
        private float forceMultiplier = 2f;
        [SerializeField]
        private float saveForceMultiplier = 0.5f;
        [SerializeField]
        private float exhaustColourMultiplier = 0.1f;
        [SerializeField]
        private float uiRegenTime = 1f;
        private PlayerControllerB savedPlayer = null;
        private bool exhausted = false;
        private bool exhausting = false;
        private readonly HashSet<IHittable> hitList = new HashSet<IHittable>();
        private bool resetList = false;
        public override void Start()
        {
            base.Start();
            ILifeSaver.Register(this);
            Particles["Spin"].StopAll(true, false);
            if (!exhausted && !exhausting)
            {
                Particles["Drip"].PlayAll(networked: false);
                return;
            }
            MeshRenderers["Main"].SetColours(exhaustColourMultiplier);
            exhausted = true;
            exhausting = false;
        }
        public override void OnDestroy()
        {
            ILifeSaver.Unregister(this);
            if (exhausting && savedPlayer.IsLocal())
            {
                HUDManager.Instance.UpdateHealthUI(savedPlayer.health, false);
            }
            base.OnDestroy();
        }
        internal override void WildCardUse()
        {
            if (exhausted || exhausting)
            {
                return;
            }
            base.WildCardUse();
        }
        internal override void ThrowUpdate()
        {
            base.ThrowUpdate();
            if (!IsOwner || (!isHeld && !isHeldByEnemy))
            {
                return;
            }
            RaycastHit[] objectsHit = Physics.SphereCastAll(transformToThrow.position, 0.5f, Vector3.one, 0f, 1084754248, QueryTriggerInteraction.Collide);
            Vector3 hitDirection = (targetPosition - transformToThrow.position).normalized;
            if (throwTime >= 0.5f)
            {
                hitDirection *= -1f;
                if (!resetList)
                {
                    hitList.Clear();
                    if (isHeld)
                    {
                        hitList.Add(LastPlayerHeldBy);
                    }
                    else
                    {
                        hitList.Add(LastEnemyHeldBy.GetComponentInChildren<EnemyAICollisionDetect>());
                    }
                    resetList = true;
                }
            }
            for (int i = 0; i < objectsHit.Length; i++)
            {
                RaycastHit hit = objectsHit[i];
                if (!hit.transform.TryGetComponent(out IHittable hitComponent) || !hitList.Add(hitComponent))
                {
                    continue;
                }
                PlayerControllerB playerHitBy = null;
                if (isHeld)
                {
                    playerHitBy = LastPlayerHeldBy;
                }
                Base.HitOrDamage(hitComponent, playerDamage, hitAmount, hitDirection, playerHitBy, true, 1, CauseOfDeath.Bludgeoning, forceMultiplier);
            }
        }
        public override void EquipItem()
        {
            base.EquipItem();
            if (exhausted || exhausting)
            {
                return;
            }
            Particles["Drip"].PlayAll(networked: false);
        }
        internal override void Throw(Vector3 newPosition, bool byEnemy, bool networked = true)
        {
            base.Throw(newPosition, byEnemy, networked);
            hitList.Clear();
            if (isHeld)
            {
                hitList.Add(LastPlayerHeldBy);
            }
            else
            {
                hitList.Add(LastEnemyHeldBy.GetComponentInChildren<EnemyAICollisionDetect>());
            }
            Particles["Drip"].StopAll(true, false);
            Particles["Spin"].PlayAll(networked: false);
        }
        internal override void ThrowEnd(bool networked = true)
        {
            base.ThrowEnd(networked);
            hitList.Clear();
            resetList = false;
            Particles["Spin"].StopAll(true, false);
            if (exhausted || exhausting)
            {
                return;
            }
            Particles["Drip"].PlayAll(networked: false);
        }
        public override void PocketItem()
        {
            base.PocketItem();
            if (exhausted || exhausting)
            {
                return;
            }
            Particles["Drip"].StopAll(false, false);
        }
        private IEnumerator ExhaustCoroutine()
        {
            exhausting = true;
            MeshRenderers["Main"].SetColours(exhaustColourMultiplier);
            ThrowEnd(false);
            Particles["Drip"].StopAll(false, false);
            if (!savedPlayer.IsLocal())
            {
                yield break;
            }
            Audio["Break"].PlayRandomClip();
            yield return null;
            if (!HUDManager.Instance.playerIsCriticallyInjured)
            {
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                HUDManager.Instance.UpdateHealthUI(1);
            }
            yield return new WaitForSeconds(uiRegenTime * 2f);
            Log.LogDebug("Halo Fully Exhausted");
            float interval = uiRegenTime * 0.1f;
            for (int i = 2; i <= 10; i++)
            {
                if (!savedPlayer.isPlayerControlled || savedPlayer.health != 100)
                {
                    break;
                }
                yield return new WaitForSeconds(interval);
                HUDManager.Instance.UpdateHealthUI(i * 10, false);
            }
            EndExhaust();
        }
        public override int GetItemDataToSave()
        {
            return Convert.ToInt32(exhausted);
        }
        public override void LoadItemSaveData(int saveData)
        {
            exhausted = Convert.ToBoolean(saveData);
        }
        private void ExhaustHalo(int id, bool networked = true)
        {
            savedPlayer = StartOfRound.Instance.allPlayerScripts[id];
            StartCoroutine(ExhaustCoroutine());
            if (!networked)
            {
                return;
            }
            ExhaustHaloRpc(id);
        }
        private void EndExhaust(bool networked = true)
        {
            exhausted = true;
            exhausting = false;
            if (!networked)
            {
                return;
            }
            EndExhaustRpc();
        }
        [Rpc(SendTo.NotMe)]
        private void ExhaustHaloRpc(int id)
        {
            ExhaustHalo(id, false);
        }
        [Rpc(SendTo.NotMe)]
        private void EndExhaustRpc()
        {
            EndExhaust(false);
        }
        bool ILifeSaver.UntargetableWhen(PlayerControllerB player)
        {
            return exhausting && savedPlayer == player;
        }
        bool ILifeSaver.TriggerWhen(PlayerControllerB player, CauseOfDeath cause)
        {
            return !exhausted && !exhausting && isHeld && !isPocketed && LastPlayerHeldBy == player;
        }
        bool ILifeSaver.CanSave(PlayerControllerB player, CauseOfDeath cause)
        {
            return (!exhausted && isHeld && !isPocketed && LastPlayerHeldBy == player) || (exhausting && savedPlayer == player);
        }
        void ILifeSaver.Save(PlayerControllerB player, CauseOfDeath cause, Vector3 hitVelocity, EnemyAI enemy)
        {
            if (!player.IsLocal())
            {
                return;
            }
            player.externalForceAutoFade += hitVelocity * saveForceMultiplier;
            if (exhausting)
            {
                return;
            }
            Log.LogDebug("Halo exhausting...");
            if (player.criticallyInjured)
            {
                player.MakeCriticallyInjured(false);
            }
            player.health = 100;
            ExhaustHalo((int)player.playerClientId);
        }
        bool ILifeSaver.GraceWhen(PlayerControllerB player, CauseOfDeath cause)
        {
            return exhausting && savedPlayer == player;
        }
    }
}