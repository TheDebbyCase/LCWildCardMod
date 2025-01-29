using System;
using Unity.Netcode;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class PixelJar : PhysicsProp
    {
        public Texture[] floaterVariants;
        public Texture floaterCurrent;
        private int textureIndex = new int();
        private System.Random randomIndex = new System.Random();
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                if (floaterCurrent == null)
                {
                    textureIndex = randomIndex.Next(floaterVariants.Length);
                    floaterCurrent = floaterVariants[textureIndex];
                }
                else
                {
                    textureIndex = Array.IndexOf(floaterVariants, floaterCurrent);
                }
            }
            TextureUpdateServerRpc();
        }
        public void SetTexture(Texture texture)
        {
            this.GetComponentInChildren<ParticleSystemRenderer>().material.mainTexture = texture;
            WildCardMod.Log.LogDebug($"Pixel Jar texture: {this.GetComponentInChildren<ParticleSystemRenderer>().material.mainTexture.name}");
        }
        public override void EquipItem()
        {
            base.EquipItem();
            if (!this.GetComponentInChildren<ParticleSystem>().isPlaying)
            {
                this.GetComponentInChildren<ParticleSystem>().Emit(1);
                this.GetComponentInChildren<ParticleSystem>().Play();
            }
        }
        public override void PocketItem()
        {
            base.PocketItem();
            if (this.GetComponentInChildren<ParticleSystem>().isPlaying)
            {
                this.GetComponentInChildren<ParticleSystem>().Stop();
                this.GetComponentInChildren<ParticleSystem>().Clear();
            }
        }
        public override int GetItemDataToSave()
        {
            return textureIndex;
        }
        public override void LoadItemSaveData(int saveData)
        {
            floaterCurrent = floaterVariants[saveData];
        }
        [ServerRpc(RequireOwnership = false)]
        private void TextureUpdateServerRpc()
        {
            SetTexture(floaterCurrent);
            TextureUpdateClientRpc(textureIndex);
        }
        [ClientRpc]
        private void TextureUpdateClientRpc(int index)
        {
            floaterCurrent = floaterVariants[index];
            SetTexture(floaterCurrent);
        }
    }
}