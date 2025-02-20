using GameNetcodeStuff;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LCWildCardMod.Items
{
    public class ClauvioMask : PhysicsProp
    {
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
            if (base.IsOwner && playerHeldBy != null && playerHeldBy.moveInputVector == Vector2.zero)
            {
                WildCardMod.Log.LogDebug("Mask Beginning Coroutine");
                peekCoroutine = StartCoroutine(PeekCoroutine(throwContext));
            }
        }
        public IEnumerator PeekCoroutine(InputAction.CallbackContext throwContext)
        {
            AnimTriggerServerRpc("Lift");
            WildCardMod.Log.LogDebug("Waiting for Button Release");
            yield return new WaitUntil(() => (!throwContext.action.IsPressed() || playerHeldBy == null || playerHeldBy.moveInputVector != Vector2.zero));
            WildCardMod.Log.LogDebug("Button Released");
            AnimTriggerServerRpc("Lower");
            peekCoroutine = null;
        }
        [ServerRpc(RequireOwnership = false)]
        public void AnimTriggerServerRpc(string name)
        {
            AnimTriggerClientRpc(name);
        }
        [ClientRpc]
        public void AnimTriggerClientRpc(string name)
        {
            maskAnimator.SetTrigger(name);
        }
    }
}