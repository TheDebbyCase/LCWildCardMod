using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.ParticleSystem;
namespace LCWildCardMod.Items
{
    public class SmithHalo : ThrowableNoisemaker
    {
        public ParticleSystem[] dripParticles;
        public ParticleSystem spinParticle;
        public int isExhausted = 0;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            spinParticle.gameObject.SetActive(false);
            ExhaustHaloServerRpc();
            triggerAnimator.SetBool("OnFloor", true);
            if (isExhausted == 1)
            {
                ExhaustHalo();
            }
            else
            {
                StartDrip();
            }
        }
        public override void EquipItem()
        {
            base.EquipItem();
            triggerAnimator.SetBool("OnFloor", false);
            triggerAnimator.SetBool("IsHeld", true);
        }
        public override void Throw()
        {
            base.Throw();
            triggerAnimator.SetBool("IsHeld", false);
            triggerAnimator.SetBool("IsThrown", true);
            if (isExhausted == 0)
            {
                StopDrip();
                spinParticle.gameObject.SetActive(true);
                spinParticle.Play();
            }
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            triggerAnimator.SetBool("IsHeld", false);
        }
        public override void PocketItem()
        {
            base.PocketItem();
            triggerAnimator.SetBool("IsHeld", false);
            if (isExhausted == 0)
            {
                StopDrip();
            }
        }
        public override void OnHitGround()
        {
            base.OnHitGround();
            triggerAnimator.SetBool("OnFloor", true);
            triggerAnimator.SetBool("IsThrown", false);
            if (isExhausted == 0)
            {
                spinParticle.gameObject.SetActive(false);
                StartDrip();
            }
        }
        public void StopDrip()
        {
            foreach (ParticleSystem particle in dripParticles)
            {
                particle.gameObject.SetActive(false);
            }
        }
        public void StartDrip()
        {
            foreach (ParticleSystem particle in dripParticles)
            {
                particle.gameObject.SetActive(true);
                particle.Play();
            }
        }
        public void ExhaustHalo()
        {
            isExhausted = 1;
            this.GetComponentInChildren<MeshRenderer>().material.color = new Color(0.1f, 0.1f, 0.1f);
            spinParticle.gameObject.SetActive(false);
            StopDrip();
        }
        public override int GetItemDataToSave()
        {
            return int.Parse($"{isCollected}{isExhausted}");
        }
        public override void LoadItemSaveData(int saveData)
        {
            isCollected = saveData.ToString()[0];
            isExhausted = saveData.ToString()[1];
        }
        [ServerRpc(RequireOwnership = false)]
        public void ExhaustHaloServerRpc()
        {
            ExhaustHaloClientRpc(int.Parse($"{isCollected}{isExhausted}"));
        }
        [ClientRpc]
        public void ExhaustHaloClientRpc(int id)
        {
            if (id == 1)
            {
                isCollected = 0;
                isExhausted = id;
            }
            else if (id == 0)
            {
                isCollected = id;
                isExhausted = id;
            }
            else
            {
                isCollected = id.ToString()[0];
                isExhausted = id.ToString()[1];
            }
        }
    }
}