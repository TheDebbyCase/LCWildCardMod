using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LCWildCardMod.Items
{
    public class ClauviMask : WildCardProp
    {
        [Space(3f)]
        [Header("ClauviMask")]
        [Space(3f)]
        [SerializeField]
        private Transform meshTransform = default;
        private Coroutine peekCoroutine = default;
        private bool isLifted = false;
        public override void OnDestroy()
        {
            EndPeek(true, false);
            base.OnDestroy();
        }
        public override void GrabItemFromEnemy(EnemyAI enemy)
        {
            Animator.SetBool("IsEnemy", true);
            meshTransform.SetParent(enemy.eye);
            base.GrabItemFromEnemy(enemy);
        }
        public override void EquipItem()
        {
            base.EquipItem();
            Animator.SetBool("IsEnemy", false);
            Animator.SetBool("IsOwner", IsOwner);
            if (IsOwner)
            {
                meshTransform.SetParent(LastPlayerHeldBy.gameplayCamera.transform);
                return;
            }
            meshTransform.SetParent(LastPlayerHeldBy.bodyParts[0]);
        }
        public override void PocketItem()
        {
            if (IsOwner)
            {
                EndPeek(true);
            }
            base.PocketItem();
        }
        internal override void DiscardFromAny(bool fromPlayer = true, EnemyAI enemy = null)
        {
            base.DiscardFromAny(fromPlayer, enemy);
            if (!IsOwner)
            {
                return;
            }
            EndPeek(true);
        }
        internal override void WildCardUse(InputAction.CallbackContext useContext)
        {
            Vector2 movement = LastPlayerHeldBy.moveInputVector;
            if (!IsOwner || !isHeld || isLifted || !Mathf.Approximately(movement.x, 0f) || !Mathf.Approximately(movement.y, 0f))
            {
                return;
            }
            peekCoroutine = StartCoroutine(Peek(useContext.action));
        }
        internal override void WildCardUse()
        {
            base.WildCardUse();
            if (!isHeldByEnemy)
            {
                return;
            }
            peekCoroutine = StartCoroutine(Peek());
        }
        private IEnumerator Peek(InputAction action)
        {
            BeginPeek();
            while (action.IsPressed() && isHeld && !isPocketed)
            {
                Vector2 movement = LastPlayerHeldBy.moveInputVector;
                if (!Mathf.Approximately(movement.x, 0f) || !Mathf.Approximately(movement.y, 0f))
                {
                    break;
                }
                yield return null;
            }
            EndPeek();
        }
        private IEnumerator Peek()
        {
            BeginPeek();
            yield return new WaitForSeconds(Random.Next(10, 51) * 0.1f);
            EndPeek();
        }
        [Rpc(SendTo.NotMe)]
        private void BeginPeekRpc()
        {
            BeginPeek(false);
        }
        [Rpc(SendTo.NotMe)]
        private void EndPeekRpc(bool force)
        {
            EndPeek(force, false);
        }
        private void BeginPeek(bool networked = true)
        {
            Log.LogDebug($"\"{CurrentItem.itemName}\" Beginning Peek");
            isLifted = true;
            Animator.SetBool("Lifted", isLifted);
            if (!networked)
            {
                return;
            }
            BeginPeekRpc();
        }
        private void EndPeek(bool force = false, bool networked = true)
        {
            if (force)
            {
                meshTransform.SetParent(transform);
            }
            Log.LogDebug($"\"{CurrentItem.itemName}\" Ending Peek");
            if (peekCoroutine != null)
            {
                StopCoroutine(peekCoroutine);
                peekCoroutine = null;
            }
            isLifted = false;
            Animator.SetBool("Lifted", isLifted);
            if (!networked)
            {
                return;
            }
            EndPeekRpc(force);
        }
    }
}