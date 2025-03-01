using System.Collections;
using Unity.Netcode;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class WormItem : ThrowableNoisemaker
    {
        readonly BepInEx.Logging.ManualLogSource log = WildCardMod.Log;
        public bool LookingForward = true;
        public bool LookingLeft;
        public bool LookingRight;
        public int playersFinishedForward;
        public int playersFinishedLeft;
        public int playersFinishedRight;
        public Coroutine idleAnimCoroutine;
        public override void BeginMusic()
        {
            base.BeginMusic();
            if (base.IsServer)
            {
                if (hasBeenHeld)
                {
                    SetBoolClientRpc("OnFloor", false);
                }
                idleAnimCoroutine = StartCoroutine(IdleAnimation(false));
            }
        }
        public override void GrabItem()
        {
            base.GrabItem();
            if (base.IsServer)
            {
                SetBoolClientRpc("OnFloor", false);
                SetBoolClientRpc("IsThrown", false);
                RestartCoroutine(ref idleAnimCoroutine, true);
            }
        }
        public override void EquipItem()
        {
            base.EquipItem();
            if (base.IsServer)
            {
                SetBoolClientRpc("IsHeld", true);
            }
        }
        public override void PocketItem()
        {
            base.PocketItem();
            if (base.IsServer)
            {
                SetBoolClientRpc("IsHeld", false);
            }
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            if (base.IsServer)
            {
                SetBoolClientRpc("IsHeld", false);
                RestartCoroutine(ref idleAnimCoroutine, false);
            }
        }
        public override void OnHitGround()
        {
            base.OnHitGround();
            if (base.IsServer)
            {
                SetBoolClientRpc("IsThrown", false);
            }
        }
        public override void Throw()
        {
            base.Throw();
            if (base.IsServer)
            {
                SetBoolClientRpc("IsThrown", true);
            }
        }
        public void RestartCoroutine(ref Coroutine coroutine, bool hand)
        {
            StopCoroutine(coroutine);
            ResetTriggersClientRpc();
            if (hand)
            {
                SetTriggerClientRpc("SnapLeft");
            }
            else
            {
                SetTriggerClientRpc("SnapForward");
            }
            coroutine = StartCoroutine(IdleAnimation(hand));
        }
        public IEnumerator IdleAnimation(bool idleHand)
        {
            yield return new WaitUntil(() => base.IsSpawned);
            while (true)
            {
                if (idleHand)
                {
                    yield return new WaitForSeconds(5f);
                    SetTriggerClientRpc("LookRight");
                    yield return new WaitUntil(() => LookingRight);
                    ResetTriggersClientRpc();
                    yield return new WaitForSeconds(3f);
                    SetTriggerClientRpc("LookForward");
                    yield return new WaitUntil(() => LookingForward);
                    ResetTriggersClientRpc();
                    yield return new WaitForSeconds(1f);
                    SetTriggerClientRpc("LookLeft");
                    yield return new WaitUntil(() => LookingLeft);
                    ResetTriggersClientRpc();
                }
                else
                {
                    yield return new WaitForSeconds(4f);
                    SetTriggerClientRpc("LookLeft");
                    yield return new WaitUntil(() => LookingLeft);
                    ResetTriggersClientRpc();
                    yield return new WaitForSeconds(2f);
                    SetTriggerClientRpc("LookRight");
                    yield return new WaitUntil(() => LookingRight);
                    ResetTriggersClientRpc();
                    yield return new WaitForSeconds(2f);
                    SetTriggerClientRpc("LookForward");
                    yield return new WaitUntil(() => LookingForward);
                    ResetTriggersClientRpc();
                }
            }
        }
        public void FinishForward()
        {
            FinishAnimServerRpc("Forward");
        }
        public void FinishLeft()
        {
            if (triggerAnimator.GetCurrentAnimatorStateInfo(0).speed > 0)
            {
                FinishAnimServerRpc("Left");
            }
        }
        public void FinishRight()
        {
            if (triggerAnimator.GetCurrentAnimatorStateInfo(0).speed > 0)
            {
                FinishAnimServerRpc("Right");
            }
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
        public void FinishAnimServerRpc(string look)
        {
            switch (look)
            {
                case "Forward":
                    {
                        playersFinishedForward++;
                        if (playersFinishedForward >= StartOfRound.Instance.connectedPlayersAmount)
                        {
                            playersFinishedForward = 0;
                            LookingForward = true;
                            LookingLeft = false;
                            LookingRight = false;
                        }
                        break;
                    }
                case "Left":
                    {
                        playersFinishedLeft++;
                        if (playersFinishedLeft >= StartOfRound.Instance.connectedPlayersAmount)
                        {
                            playersFinishedLeft = 0;
                            LookingForward = false;
                            LookingLeft = true;
                            LookingRight = false;
                        }
                        break;
                    }
                case "Right":
                    {
                        playersFinishedRight++;
                        if (playersFinishedRight >= StartOfRound.Instance.connectedPlayersAmount)
                        {
                            playersFinishedRight = 0;
                            LookingForward = false;
                            LookingLeft = false;
                            LookingRight = true;
                        }
                        break;
                    }
            }
        }
    }
}