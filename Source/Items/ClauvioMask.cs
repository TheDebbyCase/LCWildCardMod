using GameNetcodeStuff;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LCWildCardMod.Items
{
    public class ClauvioMask : PhysicsProp
    {
        readonly BepInEx.Logging.ManualLogSource log = WildCardMod.Log;
        public bool isLifted = false;
        public Transform meshTransform;
        public Animator maskAnimator;
        public Coroutine peekCoroutine;
        public PlayerControllerB previousPlayer;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            WildCardMod.wildcardKeyBinds.WildCardButton.started += MaskPeek;
        }
        public override void EquipItem()
        {
            base.EquipItem();
            if (base.IsOwner)
            {
                meshTransform.parent = playerHeldBy.gameplayCamera.transform;
                maskAnimator.SetBool("isOwner", true);
            }
            else
            {
                meshTransform.parent = playerHeldBy.bodyParts[0];
                maskAnimator.SetBool("isOwner", false);
            }
            previousPlayer = playerHeldBy;
            maskAnimator.SetBool("isHeld", true);
        }
        public override void PocketItem()
        {
            base.PocketItem();
            meshTransform.parent = this.transform;
            maskAnimator.SetBool("isHeld", false);
            if (peekCoroutine != null)
            {
                StopCoroutine(peekCoroutine);
                peekCoroutine = null;
            }
        }
        public override void DiscardItem()
        {
            meshTransform.parent = this.transform;
            maskAnimator.SetBool("isHeld", false);
            if (peekCoroutine != null)
            {
                StopCoroutine(peekCoroutine);
                peekCoroutine = null;
            }
            base.DiscardItem();
        }
        public void MaskPeek(InputAction.CallbackContext throwContext)
        {
            if (base.IsOwner && playerHeldBy != null)
            {
                log.LogDebug($"\"{this.itemProperties.itemName}\" Beginning Peek");
                peekCoroutine = StartCoroutine(PeekCoroutine(throwContext));
            }
        }
        public IEnumerator PeekCoroutine(InputAction.CallbackContext throwContext)
        {
            while (throwContext.action.IsPressed())
            {
                if (playerHeldBy != null && playerHeldBy.moveInputVector == Vector2.zero && !isLifted)
                {
                    isLifted = true;
                    SetTriggerServerRpc("Lift");
                    log.LogDebug($"\"{this.itemProperties.itemName}\" Waiting for Button Release");
                }
                yield return new WaitUntil(() => (!throwContext.action.IsPressed() || playerHeldBy == null || playerHeldBy.moveInputVector != Vector2.zero));
                if (isLifted)
                {
                    log.LogDebug($"\"{this.itemProperties.itemName}\" Button Released");
                    SetTriggerServerRpc("Lower");
                    isLifted = false;
                }
            }
            peekCoroutine = null;
        }
        [ServerRpc(RequireOwnership = false)]
        public void SetTriggerServerRpc(string name)
        {
            SetTriggerClientRpc(name);
        }
        [ClientRpc]
        public void SetTriggerClientRpc(string name)
        {
            maskAnimator.SetTrigger(name);
        }
    }
}