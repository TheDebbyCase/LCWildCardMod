using UnityEngine;
namespace LCWildCardMod.Items
{
    public class ClauvioMask : PhysicsProp
    {
        public Transform meshTransform;
        public override void EquipItem()
        {
            base.EquipItem();
            if (base.IsOwner)
            {
                meshTransform.parent = playerHeldBy.gameplayCamera.transform;
                meshTransform.localPosition = new Vector3(0f, 0.05f, 0.1f);
            }
            else
            {
                meshTransform.parent = playerHeldBy.bodyParts[0];
                meshTransform.localPosition = new Vector3(0f, 0.25f, 0.175f);
            }
            meshTransform.localRotation = Quaternion.Euler(0f, 90f, 90f);
        }
        public override void PocketItem()
        {
            base.PocketItem();
            meshTransform.parent = this.transform;
            meshTransform.localPosition = new Vector3(0f, 0.075f, 0f);
            meshTransform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }
        public override void DiscardItem()
        {
            meshTransform.parent = this.transform;
            meshTransform.localPosition = new Vector3(0f, 0.075f, 0f);
            meshTransform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            base.DiscardItem();
        }
    }
}