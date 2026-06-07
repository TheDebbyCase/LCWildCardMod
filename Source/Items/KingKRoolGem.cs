using System.Collections;
using Unity.Netcode;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class KingKRoolGem : WildCardProp
    {
        [Space(3f)]
        [Header("KingKRoolGem")]
        [Space(3f)]
        [SerializeField]
        private int minValue = 5;
        [SerializeField]
        private float valueMultiplier = 0.5f;
        public override void OnHitGround()
        {
            base.OnHitGround();
            if (!IsOwner)
            {
                return;
            }
            ScrapValue = Mathf.RoundToInt((float)ScrapValue * valueMultiplier);
            if (ScrapValue <= minValue)
            {
                StartCoroutine(DespawnCoroutine());
                Audio["Break"].SetVolume(0.75f);
            }
            Audio["Break"].PlayRandomOneshot();
        }
        private IEnumerator DespawnCoroutine()
        {
            yield return new WaitForEndOfFrame();
            Disable();
            yield return new WaitForSeconds(Audio["Break"].LastClip.length);
            DespawnRpc();
        }
        private void Disable(bool networked = true)
        {
            EnableItemMeshes(false);
            EnablePhysics(false);
            grabbable = false;
            Lights["Main"].DisableAll(false);
            if (!networked)
            {
                return;
            }
            DisableRpc();
        }
        [Rpc(SendTo.NotMe)]
        private void DisableRpc()
        {
            Disable(false);
        }
        [Rpc(SendTo.Server)]
        private void DespawnRpc()
        {
            NetworkObject.Despawn();
        }
    }
}