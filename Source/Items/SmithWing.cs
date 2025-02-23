using GameNetcodeStuff;
namespace LCWildCardMod.Items
{
    public class SmithWing : PhysicsProp
    {
        readonly BepInEx.Logging.ManualLogSource log = WildCardMod.Log;
        public PlayerControllerB lastPlayer;
        public float speedMultiplier = 1.5f;
        public override void GrabItem()
        {
            base.GrabItem();
            lastPlayer = playerHeldBy;
            lastPlayer.movementSpeed *= speedMultiplier;
        }
        public override void EquipItem()
        {
            base.EquipItem();
            if (lastPlayer != null)
            {
                lastPlayer.movementSpeed *= speedMultiplier;
            }
        }
        public override void PocketItem()
        {
            base.PocketItem();
            lastPlayer.movementSpeed /= speedMultiplier;
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            lastPlayer.movementSpeed /= speedMultiplier;
            lastPlayer = null;
        }
    }
}