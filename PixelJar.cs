using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace LCWildCardMod
{
    public class PixelJar : PhysicsProp
    {
        public Texture[] floaterVariants;
        public Texture floaterCurrent;
        private NetworkVariable<int> textureIndex = new NetworkVariable<int>(-1);
        private System.Random rng = new System.Random();
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            textureIndex.OnValueChanged += SetTexture;
            GetTextureIndex();
            TextureUpdateServerRpc(textureIndex.Value);
        }
        public void GetTextureIndex()
        {
            if (IsServer)
            {
                if (floaterCurrent == null)
                {
                    textureIndex.Value = rng.Next(floaterVariants.Length);
                }
                else
                {
                    textureIndex.Value = Array.IndexOf(floaterVariants, floaterCurrent);
                }
            }
        }
        public void SetTexture(int oldIndex, int newIndex)
        {
            floaterCurrent = floaterVariants[newIndex];
            this.GetComponentInChildren<ParticleSystemRenderer>().material.mainTexture = floaterCurrent;
            //this.GetComponentInChildren<ParticleSystemRenderer>().material.SetTexture("_EmissionMap", floaterCurrent);
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
        [ServerRpc(RequireOwnership = false)]
        private void TextureUpdateServerRpc(int index)
        {
            TextureUpdateClientRpc(index);
        }
        [ClientRpc]
        private void TextureUpdateClientRpc(int index)
        {
            SetTexture(-1, index);

        }
    }
}

