using Unity.Netcode.Components;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class Cojiro : NoisemakerProp
    {
        public AudioSource flapSource;
        public NetworkAnimator itemAnimator;
        public bool isFloating;
        public override void Update()
        {
            base.Update();
            if (!isPocketed && playerHeldBy != null && (playerHeldBy.isFallingFromJump || playerHeldBy.isFallingNoJump))
            {
                if (!isFloating)
                {
                    isFloating = true;
                    itemAnimator.Animator.SetBool("Floating", true);
                    flapSource.Play();
                }
                playerHeldBy.fallValue *= 0.9f;
            }
            else if (isFloating)
            {
                isFloating = false;
                itemAnimator.Animator.SetBool("Floating", false);
                flapSource.Stop();
            }
        }
    }
}