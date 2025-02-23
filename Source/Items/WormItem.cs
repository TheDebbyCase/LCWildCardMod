using System.Collections;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class WormItem : ThrowableNoisemaker
    {
        public bool idleAnim;
        public Coroutine idleAnimCoroutine;
        public override void BeginMusic()
        {
            base.BeginMusic();
            if (base.IsServer)
            {
                if (hasBeenHeld)
                {
                    triggerAnimator.SetBool("OnFloor", false);
                }
                idleAnimCoroutine = StartCoroutine(IdleAnimation(false));
            }
        }
        public override void GrabItem()
        {
            base.GrabItem();
            if (base.IsServer)
            {
                triggerAnimator.SetBool("OnFloor", false);
                triggerAnimator.SetBool("IsThrown", false);
                if (idleAnim)
                {
                    StopCoroutine(idleAnimCoroutine);
                    idleAnim = false;
                    idleAnimCoroutine = StartCoroutine(IdleAnimation(true));
                }
                else
                {
                    idleAnimCoroutine = StartCoroutine(IdleAnimation(true));
                }
            }
        }
        public override void EquipItem()
        {
            base.EquipItem();
            if (base.IsServer)
            {
                triggerAnimator.SetBool("IsHeld", true);
            }
        }
        public override void PocketItem()
        {
            base.PocketItem();
            if (base.IsServer)
            {
                triggerAnimator.SetBool("IsHeld", false);
            }
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            if (base.IsServer)
            {
                triggerAnimator.SetBool("IsHeld", false);
                StopCoroutine(idleAnimCoroutine);
                idleAnim = false;
                FaceDirection("Forward");
            }
        }
        public override void OnHitGround()
        {
            base.OnHitGround();
            if (base.IsServer)
            {
                triggerAnimator.SetBool("IsThrown", false);
                idleAnimCoroutine = StartCoroutine(IdleAnimation(false));
            }
        }
        public override void Throw()
        {
            base.Throw();
            if (base.IsServer)
            {
                triggerAnimator.SetBool("IsThrown", true);
            }
        }
        public IEnumerator IdleAnimation(bool inHand)
        {
            while (true)
            {
                idleAnim = true;
                if (inHand)
                {
                    FaceDirection("Left");
                    yield return new WaitForSeconds(5f);
                    FaceDirection("Forward");
                    yield return new WaitForSeconds(3f);
                    FaceDirection("Right");
                }
                else
                {
                    FaceDirection("Forward");
                    yield return new WaitForSeconds(5f);
                    FaceDirection("Left");
                    yield return new WaitForSeconds(3f);
                    FaceDirection("Right");
                }
                yield return new WaitForSeconds(3f);
            }
        }
        public void FaceDirection(string direction)
        {
            switch (direction)
            {
                case "Left":
                    {
                        if (triggerAnimator.GetBool("LookingRight"))
                        {
                            FaceDirection("Forward");
                            triggerAnimator.SetBool("LookingRight", false);
                            itemAnimator.SetTrigger("LookLeft");
                            triggerAnimator.SetBool("LookingForward", false);
                            triggerAnimator.SetBool("LookingLeft", true);
                        }
                        else
                        {
                            itemAnimator.SetTrigger("LookLeft");
                            triggerAnimator.SetBool("LookingForward", false);
                            triggerAnimator.SetBool("LookingLeft", true);
                        }
                        break;
                    }
                case "Right":
                    {
                        if (triggerAnimator.GetBool("LookingLeft"))
                        {
                            FaceDirection("Forward");
                            triggerAnimator.SetBool("LookingLeft", false);
                            itemAnimator.SetTrigger("LookRight");
                            triggerAnimator.SetBool("LookingForward", false);
                            triggerAnimator.SetBool("LookingRight", true);
                        }
                        else
                        {
                            itemAnimator.SetTrigger("LookRight");
                            triggerAnimator.SetBool("LookingForward", false);
                            triggerAnimator.SetBool("LookingRight", true);
                        }
                        break;
                    }
                case "Forward":
                    {
                        if (!triggerAnimator.GetBool("LookingForward"))
                        {
                            itemAnimator.SetTrigger("LookForward");
                            triggerAnimator.SetBool("LookingForward", true);
                            triggerAnimator.SetBool("LookingLeft", false);
                            triggerAnimator.SetBool("LookingRight", false);
                        }
                        break;
                    }
            }
        }
    }
}