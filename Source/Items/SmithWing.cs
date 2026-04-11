using GameNetcodeStuff;
namespace LCWildCardMod.Items
{
    public class SmithWing : PhysicsProp
    {
        BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        internal PlayerControllerB lastPlayer;
        public float speedMultiplier = 1.5f;
        public override void GrabItem()
        {
            base.GrabItem();
            lastPlayer = playerHeldBy;
            lastPlayer.movementSpeed *= speedMultiplier;
            Log.LogDebug($"Wing Set {lastPlayer.playerUsername} Movement Speed to {lastPlayer.movementSpeed}");
        }
        public override void EquipItem()
        {
            base.EquipItem();
            if (lastPlayer == null)
            {
                return;
            }
            lastPlayer.movementSpeed *= speedMultiplier;
            Log.LogDebug($"Wing Set {lastPlayer.playerUsername} Movement Speed to {lastPlayer.movementSpeed}");
        }
        public override void PocketItem()
        {
            base.PocketItem();
            lastPlayer.movementSpeed /= speedMultiplier;
            Log.LogDebug($"Wing Set {lastPlayer.playerUsername} Movement Speed to {lastPlayer.movementSpeed}");
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            lastPlayer.movementSpeed /= speedMultiplier;
            Log.LogDebug($"Wing Set {lastPlayer.playerUsername} Movement Speed to {lastPlayer.movementSpeed}");
            lastPlayer = null;
        }
    }
}