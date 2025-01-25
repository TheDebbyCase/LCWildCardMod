using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Unity.Netcode;
using LethalLib;
using UnityEngine.InputSystem;

namespace LCWildCardMod
{
    public class WormItem : ThrowableNoisemaker
    {
        public Animator animator;
        public GameObject HeadJoint;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            HeadJoint = GameObject.Find("HeadJoint");
            animator = this.GetComponent<Animator>();
        }
        public override void EquipItem()
        {
            base.EquipItem();
            FaceLeft();
        }
        public override void PocketItem()
        {
            base.PocketItem();
            FaceForward();
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            FaceForward();
        }
        public override void Update()
        {
            base.Update();
            if (playerHeldBy != null && playerHeldBy.currentlyHeldObjectServer == this)
            {
                CheckThrowPress();
            }
        }
        public void CheckThrowPress()
        {
            if (!WildCardMod.wildcardKeyBinds.ThrowButton.triggered)
            {
                return;
            }
            playerHeldBy.DiscardHeldObject(placeObject: true, null, GetThrowDestination());
            animator.SetTrigger("LookForward");
        }
        public void FaceLeft()
        {
            if (animator.GetCurrentAnimatorStateInfo(0).speed == 2.5)
            {
                animator.SetTrigger("LookForward");
                animator.SetTrigger("TurnLeft");
            }
            else
            {
                animator.SetTrigger("TurnLeft");
            }
            WildCardMod.Log.LogDebug(animator.GetCurrentAnimatorStateInfo(0).fullPathHash.ToString());
        }
        public void FaceRight()
        {
            if (animator.GetCurrentAnimatorStateInfo(0).speed == 2.5)
            {
                animator.SetTrigger("LookForward");
                animator.SetTrigger("TurnRight");
            }
            else
            {
                animator.SetTrigger("TurnRight");
            }
            WildCardMod.Log.LogDebug(animator.GetCurrentAnimatorStateInfo(0).fullPathHash.ToString());
        }
        public void FaceForward()
        {
            if (animator.GetCurrentAnimatorStateInfo(0).speed != 1)
            {
                animator.SetTrigger("LookForward");
            }
            WildCardMod.Log.LogDebug(animator.GetCurrentAnimatorStateInfo(0).fullPathHash.ToString());
        }
    }
}
