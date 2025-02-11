using Unity.Netcode.Components;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class Cojiro : NoisemakerProp
    {
        public AudioSource flapSource;
        public NetworkAnimator itemAnimator;
        public bool isFloating;
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            int noiseIndex = noisemakerRandom.Next(0, noiseSFX.Length);
            float volume = (float)noisemakerRandom.Next((int)(minLoudness * 100f), (int)(maxLoudness * 100f)) / 100f;
            float pitch = (float)noisemakerRandom.Next((int)(minPitch * 100f), (int)(maxPitch * 100f)) / 100f;
            noiseAudio.pitch = pitch;
            noiseAudio.PlayOneShot(noiseSFX[noiseIndex], volume);
            if (base.IsServer)
            {
                triggerAnimator.SetTrigger("playAnim");
            }
            WalkieTalkie.TransmitOneShotAudio(noiseAudio, noiseSFX[noiseIndex], volume);
            RoundManager.Instance.PlayAudibleNoise(base.transform.position, noiseRange, volume, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
            playerHeldBy.timeSinceMakingLoudNoise = 0f;
        }
        public override void Update()
        {
            base.Update();
            if (!isPocketed && playerHeldBy != null && (playerHeldBy.isFallingFromJump || playerHeldBy.isFallingNoJump))
            {
                if (!isFloating)
                {
                    isFloating = true;
                    if (base.IsServer)
                    {
                        itemAnimator.Animator.SetBool("Floating", true);
                    }
                    flapSource.Play();
                }
                playerHeldBy.fallValue *= 0.9f;
            }
            else if (isFloating)
            {
                isFloating = false;
                if (base.IsServer)
                {
                    itemAnimator.Animator.SetBool("Floating", false);
                }
                flapSource.Stop();
            }
        }
    }
}