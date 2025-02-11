using GameNetcodeStuff;
namespace LCWildCardMod.Items
{
    public class SmithWing : PhysicsProp
    {
        public PlayerControllerB lastPlayer;
        public float originalSpeed;
        public override void GrabItem()
        {
            base.GrabItem();
            lastPlayer = playerHeldBy;
            originalSpeed = lastPlayer.movementSpeed;
            lastPlayer.movementSpeed *= 1.5f;
        }
        public override void EquipItem()
        {
            base.EquipItem();
            if (lastPlayer != null)
            {
                lastPlayer.movementSpeed *= 1.5f;
            }
        }
        public override void PocketItem()
        {
            base.PocketItem();
            lastPlayer.movementSpeed = originalSpeed;
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            lastPlayer.movementSpeed = originalSpeed;
            lastPlayer = null;
        }
    }
}