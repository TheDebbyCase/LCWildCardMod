using LCWildCardMod.Utils;
using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;
namespace LCWildCardMod.Items.Clover
{
    public class CloverNecklace : PhysicsProp
    {
        readonly BepInEx.Logging.ManualLogSource log = WildCardMod.Log;
        public MapObject mapObject;
        public NetworkAnimator itemAnimator;
        public List<CloverBee> beeList = new List<CloverBee>();
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            CloverNecklace[] necklaceObjects = FindObjectsByType<CloverNecklace>(FindObjectsSortMode.None);
            CloverBee[] beeObjects = FindObjectsByType<CloverBee>(FindObjectsSortMode.None);
            if (necklaceObjects.Length > 1 || beeObjects.Length == 0)
            {
                log.LogDebug($"{itemProperties.itemName} is Despawning");
                this.NetworkObject.Despawn();
            }
            else
            {
                for (int i = 0; i < beeObjects.Length; i++)
                {
                    beeObjects[i].SetNecklaceController(this);
                    log.LogDebug($"Located {beeObjects[i].itemProperties.itemName} No. {i + 1}");
                    beeList.Add(beeObjects[i]);
                }
            }
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            for (int i = 0; i < beeList.Count; i++)
            {
                if (beeList[i].IsSpawned)
                {
                    log.LogDebug($"Removing {beeList[i].itemProperties.itemName} No. {i + 1} from list");
                    beeList[i].RemoveNecklaceController(this);
                }
            }
        }
        public override void GrabItem()
        {
            base.GrabItem();
            if (base.IsServer)
            {
                itemAnimator.Animator.SetBool("isHeld", true);
            }
            if (GameNetworkManager.Instance.localPlayerController == playerHeldBy)
            {
                for (int i = 0; i < beeList.Count; i++)
                {
                    beeList[i].ToggleHeld(true);
                }
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
            if (base.IsServer)
            {
                itemAnimator.Animator.SetBool("isHeld", false);
            }
        }
    }
}