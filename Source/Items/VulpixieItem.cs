using Unity.Netcode;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class VulpixieItem : WildCardProp
    {
        [Space(3f)]
        [Header("VulpixieItem")]
        [Space(3f)]
        [SerializeField]
        private Transform meshTransform = null;
        [SerializeField]
        private float scaleMaxMultiplier = 3f;
        private Vector3 meshScale;
        public void RandomizeScale()
        {
            meshScale = meshTransform.localScale;
            if (!IsOwner)
            {
                return;
            }
            SetScale(Vector3.Scale(meshScale, (UnityEngine.Random.insideUnitSphere * (scaleMaxMultiplier * 0.5f)) + Vector3.one));
        }
        public void ResetScale()
        {
            if (!IsOwner)
            {
                return;
            }
            SetScale(meshScale);
        }
        private void SetScale(Vector3 scale, bool networked = true)
        {
            meshTransform.localScale = scale;
            if (!networked)
            {
                return;
            }
            SetScaleRpc(scale);
        }
        [Rpc(SendTo.NotMe)]
        private void SetScaleRpc(Vector3 scale)
        {
            SetScale(scale, false);
        }
    }
}