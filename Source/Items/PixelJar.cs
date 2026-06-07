using LCWildCardMod.Utils;
using Unity.Netcode;
//using UnityEngine;
namespace LCWildCardMod.Items
{
    public class PixelJar : WildCardProp
    {
        //[Space(3f)]
        //[Header("PixelJar")]
        //[Space(3f)]
        private int floaterCurrent = -1;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsServer)
            {
                return;
            }
            SelectParticles particle = Particles["Floater"];
            if (floaterCurrent < 0)
            {
                floaterCurrent = particle.RandomTextureIndex();
            }
            TextureUpdate(floaterCurrent);
            particle.EmitAll(1);
            particle.PlayAll();
        }
        public override void EquipItem()
        {
            base.EquipItem();
            SelectParticles particle = Particles["Floater"];
            particle.EmitAll(1, false);
            particle.PlayAll(networked: false);
        }
        public override void PocketItem()
        {
            base.PocketItem();
            Particles["Floater"].StopAll(true, false);
        }
        public override int GetItemDataToSave()
        {
            return floaterCurrent;
        }
        public override void LoadItemSaveData(int saveData)
        {
            floaterCurrent = saveData;
        }
        private void TextureUpdate(int index, bool networked = true)
        {
            floaterCurrent = index;
            SelectParticles particle = Particles["Floater"];
            particle.SetMaterialsTexture(0, floaterCurrent);
            Log.LogDebug($"Chosen Pixel Jar texture: \"{particle.GetTexture(floaterCurrent).name}\"");
            if (!networked)
            {
                return;
            }
            TextureUpdateRpc(index);
        }
        [Rpc(SendTo.NotMe)]
        private void TextureUpdateRpc(int index)
        {
            TextureUpdate(index, false);
        }
    }
}