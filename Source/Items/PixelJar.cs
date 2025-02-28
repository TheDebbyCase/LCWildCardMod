using System;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class PixelJar : PhysicsProp
    {
        readonly BepInEx.Logging.ManualLogSource log = WildCardMod.Log;
        public Texture[] floaterVariants;
        public Texture floaterCurrent;
        public ParticleSystem particle;
        public ParticleSystemRenderer particleRenderer;
        private int textureIndex = new int();
        private System.Random randomIndex;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            particle = this.GetComponentInChildren<ParticleSystem>();
            particleRenderer = this.GetComponentInChildren<ParticleSystemRenderer>();
            randomIndex = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            if (base.IsServer)
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
                TextureUpdateClientRpc(textureIndex);
            }
        }
        public override void EquipItem()
        {
            base.EquipItem();
            if (!particle.isPlaying)
            {
                particle.Emit(1);
                particle.Play();
            }
        }
        public override void PocketItem()
        {
            base.PocketItem();
            if (particle.isPlaying)
            {
                particle.Stop();
                particle.Clear();
            }
        }
        public void SetTexture(int index)
        {
            floaterCurrent = floaterVariants[index];
            particleRenderer.material.mainTexture = floaterCurrent;
            log.LogDebug($"Chosen Pixel Jar texture: \"{particleRenderer.material.mainTexture.name}\"");
        }
        public override int GetItemDataToSave()
        {
            return textureIndex;
        }
        public override void LoadItemSaveData(int saveData)
        {
            floaterCurrent = floaterVariants[saveData];
        }
        [ClientRpc]
        private void TextureUpdateClientRpc(int index)
        {
            SetTexture(index);
        }
    }
}