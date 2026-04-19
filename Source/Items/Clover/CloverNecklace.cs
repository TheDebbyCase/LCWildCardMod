using LCWildCardMod.Utils;
using System.Collections.Generic;
using Unity.Netcode.Components;
namespace LCWildCardMod.Items.Clover
{
    public class CloverNecklace : PhysicsProp
    {
        BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        public NetworkAnimator itemAnimator;
        internal static CloverNecklace oneNecklace;
        internal static List<CloverBee> beeList = new List<CloverBee>();
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            EventsClass.OnRoundStart += DoChecks;
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            EventsClass.OnRoundStart -= DoChecks;
            if (oneNecklace != this)
            {
                return;
            }
            oneNecklace = null;
        }
        public override void GrabItem()
        {
            base.GrabItem();
            if (IsServer)
            {
                itemAnimator.Animator.SetBool("isHeld", true);
            }
            if (GameNetworkManager.Instance.localPlayerController != playerHeldBy)
            {
                return;
            }
            for (int i = 0; i < beeList.Count; i++)
            {
                beeList[i].ToggleHeld(true);
            }
        }
        public override void DiscardItem()
        {
            if (GameNetworkManager.Instance.localPlayerController == playerHeldBy)
            {
                for (int i = 0; i < beeList.Count; i++)
                {
                    beeList[i].ToggleHeld(false);
                }
            }
            base.DiscardItem();
            if (IsServer)
            {
                itemAnimator.Animator.SetBool("isHeld", false);
            }
        }
        internal void DoChecks()
        {
            if (oneNecklace == null && beeList.Count > 0)
            {
                oneNecklace = this;
                return;
            }
            Log.LogDebug($"{itemProperties.itemName} is Despawning");
            if (!IsServer)
            {
                return;
            }
            NetworkObject.Despawn();
        }
    }
}