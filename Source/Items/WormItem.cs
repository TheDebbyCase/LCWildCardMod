using System.Collections;
using Unity.Netcode;
using UnityEngine;
namespace LCWildCardMod.Items
{
    internal enum WormLook
    {
        Forward,
        Left,
        Right
    }
    public class WormItem : ThrowableNoisemaker
    {
        BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        internal WormLook lookDirection = WormLook.Forward;
        internal int playersFinishedForward;
        internal int playersFinishedLeft;
        internal int playersFinishedRight;
        internal Coroutine idleAnimCoroutine;
        System.Random random;
        internal override void BeginMusic()
        {
            base.BeginMusic();
            if (!IsServer)
            {
                return;
            }
            random = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            if (hasBeenHeld)
            {
                SetBoolClientRpc("OnFloor", false);
            }
            idleAnimCoroutine = StartCoroutine(IdleAnimation(false));
        }
        public override void GrabItem()
        {
            base.GrabItem();
            if (!IsServer)
            {
                return;
            }
            SetBoolClientRpc("OnFloor", false);
            SetBoolClientRpc("IsThrown", false);
            RestartCoroutine(ref idleAnimCoroutine, true);
        }
        public override void EquipItem()
        {
            base.EquipItem();
            if (!IsServer)
            {
                return;
            }
            SetBoolClientRpc("IsHeld", true);
        }
        public override void PocketItem()
        {
            base.PocketItem();
            if (!IsServer)
            {
                return;
            }
            SetBoolClientRpc("IsHeld", false);
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            if (!IsServer)
            {
                return;
            }
            SetBoolClientRpc("IsHeld", false);
            RestartCoroutine(ref idleAnimCoroutine, false);
        }
        public override void OnHitGround()
        {
            base.OnHitGround();
            if (!IsServer)
            {
                return;
            }
            SetBoolClientRpc("IsThrown", false);
        }
        internal override void Throw()
        {
            base.Throw();
            Log.LogDebug("Giwi Worm being thrown!");
            if (!IsServer)
            {
                return;
            }
            SetBoolClientRpc("IsThrown", true);
        }
        internal void RestartCoroutine(ref Coroutine coroutine, bool hand)
        {
            StopCoroutine(coroutine);
            ResetTriggersClientRpc();
            string trigger = "SnapForward";
            if (hand)
            {
                trigger = "SnapLeft";
            }
            SetTriggerClientRpc(trigger);
            coroutine = StartCoroutine(IdleAnimation(hand));
        }
        internal IEnumerator IdleAnimation(bool idleHand)
        {
            yield return new WaitUntil(() => IsSpawned);
            while (true)
            {
                if (idleHand)
                {
                    yield return new WaitForSeconds(random.Next(3, 7));
                    SetTriggerClientRpc("LookRight");
                    yield return new WaitUntil(() => lookDirection == WormLook.Right);
                    ResetTriggersClientRpc();
                    yield return new WaitForSeconds(random.Next(2, 5));
                    SetTriggerClientRpc("LookForward");
                    yield return new WaitUntil(() => lookDirection == WormLook.Forward);
                    ResetTriggersClientRpc();
                    yield return new WaitForSeconds(random.Next(1, 3));
                    SetTriggerClientRpc("LookLeft");
                    yield return new WaitUntil(() => lookDirection == WormLook.Left);
                    ResetTriggersClientRpc();
                    continue;
                }
                yield return new WaitForSeconds(random.Next(3, 6));
                SetTriggerClientRpc("LookLeft");
                yield return new WaitUntil(() => lookDirection == WormLook.Left);
                ResetTriggersClientRpc();
                yield return new WaitForSeconds(random.Next(1, 3));
                SetTriggerClientRpc("LookRight");
                yield return new WaitUntil(() => lookDirection == WormLook.Right);
                ResetTriggersClientRpc();
                yield return new WaitForSeconds(random.Next(1, 3));
                SetTriggerClientRpc("LookForward");
                yield return new WaitUntil(() => lookDirection == WormLook.Forward);
                ResetTriggersClientRpc();
            }
        }
        internal void FinishForward()
        {
            FinishAnimServerRpc((int)WormLook.Forward);
        }
        internal void FinishLeft()
        {
            if (triggerAnimator.GetCurrentAnimatorStateInfo(0).speed <= 0)
            {
                return;
            }
            FinishAnimServerRpc((int)WormLook.Left);
        }
        internal void FinishRight()
        {
            if (triggerAnimator.GetCurrentAnimatorStateInfo(0).speed <= 0)
            {
                return;
            }
            FinishAnimServerRpc((int)WormLook.Right);
        }
        [ClientRpc]
        public void SetTriggerClientRpc(string trigger)
        {
            triggerAnimator.SetTrigger(trigger);
        }
        [ClientRpc]
        public void ResetTriggersClientRpc()
        {
            triggerAnimator.ResetTrigger("LookForward");
            triggerAnimator.ResetTrigger("LookLeft");
            triggerAnimator.ResetTrigger("LookRight");
        }
        [ClientRpc]
        public void SetBoolClientRpc(string boolean, bool value)
        {
            triggerAnimator.SetBool(boolean, value);
        }
        [ServerRpc (RequireOwnership = false)]
        public void FinishAnimServerRpc(int look)
        {
            WormLook direction = (WormLook)look;
            Log.LogDebug($"Worm looking {direction}");
            switch (direction)
            {
                case WormLook.Forward:
                    {
                        playersFinishedForward++;
                        if (playersFinishedForward < StartOfRound.Instance.connectedPlayersAmount)
                        {
                            break;
                        }
                        playersFinishedForward = 0;
                        lookDirection = direction;
                        break;
                    }
                case WormLook.Left:
                    {
                        playersFinishedLeft++;
                        if (playersFinishedLeft < StartOfRound.Instance.connectedPlayersAmount)
                        {
                            break;
                        }
                        playersFinishedLeft = 0;
                        lookDirection = direction;
                        break;
                    }
                case WormLook.Right:
                    {
                        playersFinishedRight++;
                        if (playersFinishedRight < StartOfRound.Instance.connectedPlayersAmount)
                        {
                            break;
                        }
                        playersFinishedRight = 0;
                        lookDirection = direction;
                        break;
                    }
            }
        }
    }
}