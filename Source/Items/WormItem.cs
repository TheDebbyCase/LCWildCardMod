using LCWildCardMod.Utils;
using Unity.Netcode;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class WormItem : WildCardThrowable
    {
        [Space(3f)]
        [Header("WormItem")]
        [Space(3f)]
        [SerializeField]
        private float eyeOffsetMultiplier = 0.025f;
        private int playersFinishedLast = 0;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Animator.SetBool("Spawned", !hasBeenHeld);
            if (!IsServer)
            {
                return;
            }
            Animations["Look"].SetFunctionAll(() => playersFinishedLast >= StartOfRound.Instance.connectedPlayersAmount + 1);
        }
        internal override void GrabFromAny(bool fromPlayer = true, EnemyAI enemy = null)
        {
            base.GrabFromAny(fromPlayer, enemy);
            Animator.SetBool("Spawned", false);
            if (!IsServer)
            {
                return;
            }
            playersFinishedLast = 0;
            SelectAnimationParameters lookAnim = Animations["Look"];
            lookAnim.UpdateValues(0, null, 1f, 1f);
            lookAnim.DoAnimation(0, 1f);
            lookAnim.SetTimer(0, 3.5f, oneTime: true);
            lookAnim.ResetTimer(0, false);
        }
        internal override void DiscardFromAny(bool fromPlayer = true, EnemyAI enemy = null)
        {
            base.DiscardFromAny(fromPlayer, enemy);
            if (!IsServer)
            {
                return;
            }
            Animations["Look"].UpdateAllValues(null, 0f, 0f);
        }
        public void FinishAnimation()
        {
            FinishAnimRpc();
        }
        [Rpc(SendTo.Server)]
        private void FinishAnimRpc()
        {
            playersFinishedLast++;
        }
        public void RandomizeEye()
        {
            if (!IsOwner)
            {
                return;
            }
            Vector2 offset = UnityEngine.Random.insideUnitCircle * eyeOffsetMultiplier;
            MeshRenderers["Eye"].SetOffsets(offset);
            RandomizeEyeRpc(offset);
        }
        [Rpc(SendTo.NotMe)]
        private void RandomizeEyeRpc(Vector2 offset)
        {
            MeshRenderers["Eye"].SetOffsets(offset);
        }
    }
}