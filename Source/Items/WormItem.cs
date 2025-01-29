using UnityEngine;
using Unity.Netcode;
namespace LCWildCardMod.Items
{
    public class WormItem : ThrowableNoisemaker
    {
        public AudioSource spawnMusic;
        public int isCollected = 0;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            WildCardMod.Log.LogDebug($"Spawned isCollected id is: {isCollected}");
            if (IsServer)
            {
                BeginMusic(isCollected);
            }
            CollectedWormServerRpc();
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
        public override void Throw()
        {
            base.Throw();
            triggerAnimator.SetBool("IsHeld", false);
            triggerAnimator.SetBool("IsThrown", true);
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
        public void BeginMusic(int id)
        {
            if (id == 0)
            {
                spawnMusic.Play();
            }
            else
            {
                triggerAnimator.SetBool("OnFloor", false);
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
        public void CollectedWormServerRpc()
        {
            CollectedWormClientRpc(isCollected);
        }
        [ClientRpc]
        public void CollectedWormClientRpc(int id)
        {
            isCollected = id;
            BeginMusic(isCollected);
        }
    }
}