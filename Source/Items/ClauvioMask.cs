using GameNetcodeStuff;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LCWildCardMod.Items
{
    public class ClauvioMask : PhysicsProp
    {
        BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        public Transform meshTransform;
        public Animator maskAnimator;
        internal bool isLifted = false;
        internal Coroutine peekCoroutine;
        internal PlayerControllerB previousPlayer;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            WildCardMod.Instance.KeyBinds.WildCardButton.started += MaskPeek;
        }
        public override void EquipItem()
        {
            base.EquipItem();
            if (base.IsOwner)
            {
                meshTransform.parent = playerHeldBy.gameplayCamera.transform;
            }
            else
            {
                meshTransform.parent = playerHeldBy.bodyParts[0];
            }
            maskAnimator.SetBool("isOwner", base.IsOwner);
            maskAnimator.SetBool("isHeld", true);
            previousPlayer = playerHeldBy;
        }
        public override void PocketItem()
        {
            base.PocketItem();
            meshTransform.parent = transform;
            maskAnimator.SetBool("isHeld", false);
            if (peekCoroutine == null)
            {
                return;
            }
            StopCoroutine(peekCoroutine);
            peekCoroutine = null;
        }
        public override void DiscardItem()
        {
            meshTransform.parent = transform;
            maskAnimator.SetBool("isHeld", false);
            if (peekCoroutine != null)
            {
                StopCoroutine(peekCoroutine);
                peekCoroutine = null;
            }
            base.DiscardItem();
        }
        internal void MaskPeek(InputAction.CallbackContext throwContext)
        {
            if (!base.IsOwner || playerHeldBy == null)
            {
                return;
            }
            Log.LogDebug($"\"{itemProperties.itemName}\" Beginning Peek");
            peekCoroutine = StartCoroutine(PeekCoroutine(throwContext));
        }
        internal IEnumerator PeekCoroutine(InputAction.CallbackContext throwContext)
        {
            while (throwContext.action.IsPressed())
            {
                if (playerHeldBy != null && playerHeldBy.moveInputVector == Vector2.zero && !isLifted)
                {
                    isLifted = true;
                    SetTriggerServerRpc("Lift");
                    Log.LogDebug($"\"{itemProperties.itemName}\" Waiting for Button Release");
                }
                yield return new WaitUntil(() => (!throwContext.action.IsPressed() || playerHeldBy == null || playerHeldBy.moveInputVector != Vector2.zero));
                if (isLifted)
                {
                    Log.LogDebug($"\"{itemProperties.itemName}\" Button Released");
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