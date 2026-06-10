using LCWildCardMod.Utils;
using System.Collections.Generic;
//using UnityEngine;
namespace LCWildCardMod.Items
{
    public class CloverNecklace : WildCardProp
    {
        internal static CloverNecklace oneNecklace = null;
        internal static List<CloverBee> beeList = new List<CloverBee>();
        //[Space(3f)]
        //[Header("CloverNecklace")]
        //[Space(3f)]
        public override void Start()
        {
            base.Start();
            if (!EventsClass.RoundStarted)
            {
                return;
            }
            DoChecks();
        }
        internal override void OnEnable()
        {
            EventsClass.OnRoundStart += DoChecks;
        }
        internal override void OnDisable()
        {
            EventsClass.OnRoundStart -= DoChecks;
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (oneNecklace != this)
            {
                return;
            }
            oneNecklace = null;
        }
        public override void GrabItem()
        {
            base.GrabItem();
            if (!LastPlayerHeldBy.IsLocal())
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
            if (LastPlayerHeldBy.IsLocal())
            {
                for (int i = 0; i < beeList.Count; i++)
                {
                    beeList[i].ToggleHeld(false);
                }
            }
            base.DiscardItem();
        }
        private void DoChecks()
        {
            if (oneNecklace == null && beeList.Count > 0)
            {
                oneNecklace = this;
                return;
            }
            Log.LogDebug($"{CurrentItem.itemName} is Despawning");
            if (!IsServer)
            {
                return;
            }
            NetworkObject.Despawn();
        }
    }
}