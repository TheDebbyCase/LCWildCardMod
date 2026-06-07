using Unity.Netcode;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class Cojiro : WildCardProp
    {
        [Space(3f)]
        [Header("Cojiro")]
        [Space(3f)]
        [SerializeField]
        private float slowAmount = 0.1f;
        [SerializeField]
        private float pocketCooldown = 1f;
        [SerializeField]
        private float cooldownRecover = 2f;
        internal bool isFloating;
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            if (isFloating)
            {
                return;
            }
            base.ItemActivate(used, buttonDown);
        }
        public override void Update()
        {
            base.Update();
            if (!IsOwner)
            {
                return;
            }
            if (isHeld && !isPocketed && (LastPlayerHeldBy.isFallingFromJump || LastPlayerHeldBy.isFallingNoJump))
            {
                if (!isFloating)
                {
                    ToggleFloating();
                    Log.LogDebug($"Cojiro is slowing \"{LastPlayerHeldBy.playerUsername}\"'s fall!");
                }
                LastPlayerHeldBy.fallValue *= 1f - slowAmount;
            }
            else if (isFloating)
            {
                ToggleFloating();
            }
            if (!isFloating || currentUseCooldown >= pocketCooldown)
            {
                return;
            }
            currentUseCooldown += cooldownRecover * Time.deltaTime;
        }
        private void ToggleFloating(bool networked = true)
        {
            isFloating = !isFloating;
            Animator.SetBool("Floating", isFloating);
            if (!isFloating)
            {
                Audio["Flap"].Stop(false);
            }
            else if (networked)
            {
                Audio["Flap"].PlayRandomClip();
            }
            if (!networked)
            {
                return;
            }
            SetFloatingRpc();
        }
        [Rpc(SendTo.NotMe)]
        private void SetFloatingRpc()
        {
            ToggleFloating(false);
        }
    }
}