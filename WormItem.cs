﻿using UnityEngine;
using Unity.Netcode;
namespace LCWildCardMod
{
    public class WormItem : ThrowableNoisemaker
    {
        public AudioSource spawnMusic;
        public AudioSource throwAudio;
        public int isCollected = 0;
        private System.Random random;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            random = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            if (isCollected == 0)
            {
                spawnMusic.Play();
            }
            else
            {
                triggerAnimator.SetBool("OnFloor", false);
            }
        }
        public override void EquipItem()
        {
            base.EquipItem();
            triggerAnimator.SetBool("IsHeld", true);
            triggerAnimator.SetBool("OnFloor", false);
            spawnMusic.Stop();
            throwAudio.Stop();
            isCollected = 1;
            FaceLeft();
        }
        public override void PocketItem()
        {
            base.PocketItem();
            triggerAnimator.SetBool("IsHeld", true);
            FaceForward();
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            triggerAnimator.SetBool("IsHeld", false);
            FaceForward();
        }
        public override void OnHitGround()
        {
            base.OnHitGround();
            throwAudio.Stop();
            triggerAnimator.SetBool("IsThrown", false);
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
            triggerAnimator.SetBool("IsHeld", false);
            triggerAnimator.SetBool("IsThrown", true);
            float pitch = (float)random.Next((int)(minPitch * 100f), (int)(maxPitch * 100f)) / 100f;
            throwAudio.pitch = pitch;
            throwAudio.Play();
            ThrowAudioServerRpc(pitch);
            FaceForward();
        }
        public void FaceLeft()
        {
            if (triggerAnimator.GetBool("LookingRight"))
            {
                FaceForward();
                triggerAnimator.SetBool("LookingRight", false);
                triggerAnimator.SetTrigger("LookLeft");
                triggerAnimator.SetBool("LookingForward", false);
                triggerAnimator.SetBool("LookingLeft", true);
            }
            else
            {
                triggerAnimator.SetTrigger("LookLeft");
                triggerAnimator.SetBool("LookingForward", false);
                triggerAnimator.SetBool("LookingLeft", true);
            }
        }
        public void FaceRight()
        {
            if (triggerAnimator.GetBool("LookingLeft"))
            {
                FaceForward();
                triggerAnimator.SetBool("LookingLeft", false);
                triggerAnimator.SetTrigger("LookRight");
                triggerAnimator.SetBool("LookingForward", false);
                triggerAnimator.SetBool("LookingRight", true);
            }
            else
            {
                triggerAnimator.SetTrigger("LookRight");
                triggerAnimator.SetBool("LookingForward", false);
                triggerAnimator.SetBool("LookingRight", true);
            }
        }
        public void FaceForward()
        {
            if (!triggerAnimator.GetBool("LookingForward"))
            {
                triggerAnimator.SetTrigger("LookForward");
                triggerAnimator.SetBool("LookingForward", true);
                triggerAnimator.SetBool("LookingLeft", false);
                triggerAnimator.SetBool("LookingRight", false);
            }
        }
        public override int GetItemDataToSave()
        {
            return isCollected;
        }
        public override void LoadItemSaveData(int saveData)
        {
            isCollected = saveData;
        }
        [ServerRpc(RequireOwnership = false)]
        public void ThrowAudioServerRpc(float pitch)
        {
            ThrowAudioClientRpc(pitch);
        }
        [ClientRpc]
        public void ThrowAudioClientRpc(float pitch)
        {
            throwAudio.pitch = pitch;
            throwAudio.Play();
        }
    }
}