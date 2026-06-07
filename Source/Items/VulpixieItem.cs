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
        private float scaleMaxMultiplier = 2f;
        private Vector3 meshScale;
        private Vector3 meshRot;
        public void RandomizeScale()
        {
            meshScale = meshTransform.localScale;
            meshRot = meshTransform.localRotation.eulerAngles;
            if (!IsOwner)
            {
                return;
            }
            Vector3 newScale = Vector3.Scale(meshScale, (UnityEngine.Random.insideUnitSphere * (scaleMaxMultiplier * 0.5f)) + Vector3.one);
            Vector3 newRot = UnityEngine.Random.rotation.eulerAngles;
            SetScale(newScale, newRot);
        }
        public void ResetScale()
        {
            SetScale(meshScale, meshRot, false);
        }
        private void SetScale(Vector3 scale, Vector3 rot, bool networked = true)
        {
            meshTransform.localScale = scale;
            meshTransform.localRotation = Quaternion.Euler(rot);
            if (!networked)
            {
                return;
            }
            SetScaleRpc(scale, rot);
        }
        [Rpc(SendTo.NotMe)]
        private void SetScaleRpc(Vector3 scale, Vector3 rot)
        {
            SetScale(scale, rot, false);
        }
    }
}