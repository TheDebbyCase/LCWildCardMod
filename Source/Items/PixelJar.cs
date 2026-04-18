using Unity.Netcode;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class PixelJar : PhysicsProp
    {
        BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        public Texture[] floaterVariants;
        public ParticleSystem particle;
        public ParticleSystemRenderer particleRenderer;
        internal int floaterCurrent = -1;
        System.Random randomIndex;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            particle = GetComponentInChildren<ParticleSystem>();
            particleRenderer = GetComponentInChildren<ParticleSystemRenderer>();
            randomIndex = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            if (!base.IsServer)
            {
                return;
            }
            if (floaterCurrent < 0)
            {
                floaterCurrent = randomIndex.Next(floaterVariants.Length);
            }
            TextureUpdateClientRpc(floaterCurrent);
        }
        public override void EquipItem()
        {
            base.EquipItem();
            if (particle.isPlaying)
            {
                return;
            }
            particle.Emit(1);
            particle.Play();
        }
        public override void PocketItem()
        {
            base.PocketItem();
            if (!particle.isPlaying)
            {
                return;
            }
            particle.Stop();
            particle.Clear();
        }
        internal void SetTexture(int index)
        {
            particleRenderer.material.mainTexture = floaterVariants[index];
            Log.LogDebug($"Chosen Pixel Jar texture: \"{particleRenderer.material.mainTexture.name}\"");
        }
        public override int GetItemDataToSave()
        {
            return floaterCurrent;
        }
        public override void LoadItemSaveData(int saveData)
        {
            floaterCurrent = saveData;
        }
        [ClientRpc]
        private void TextureUpdateClientRpc(int index)
        {
            SetTexture(index);
        }
    }
}