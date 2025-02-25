using GameNetcodeStuff;
using Unity.Netcode.Components;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class Cojiro : NoisemakerProp
    {
        readonly BepInEx.Logging.ManualLogSource log = WildCardMod.Log;
        public AudioSource flapSource;
        public NetworkAnimator itemAnimator;
        public bool isFloating;
        public PlayerControllerB previousPlayer;
        public override void GrabItem()
        {
            base.GrabItem();
            if (previousPlayer == null)
            {
                previousPlayer = playerHeldBy;
            }
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            int noiseIndex = noisemakerRandom.Next(0, noiseSFX.Length);
            float volume = (float)noisemakerRandom.Next((int)(minLoudness * 100f), (int)(maxLoudness * 100f)) / 100f;
            float pitch = (float)noisemakerRandom.Next((int)(minPitch * 100f), (int)(maxPitch * 100f)) / 100f;
            noiseAudio.pitch = pitch;
            noiseAudio.PlayOneShot(noiseSFX[noiseIndex], volume);
            if (base.IsServer)
            {
                itemAnimator.SetTrigger("playAnim");
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
                    WalkieTalkie.TransmitOneShotAudio(flapSource, flapSource.clip);
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
                WalkieTalkie.TransmitOneShotAudio(flapSource, flapSource.clip);
            }
            if (currentUseCooldown < 0)
            {
                currentUseCooldown = 0;
            }
            else if (currentUseCooldown == 0f && playerHeldBy == null && previousPlayer != null)
            {
                previousPlayer = null;
            }
            else if (currentUseCooldown == 0f && playerHeldBy != null && previousPlayer != playerHeldBy)
            {
                previousPlayer = playerHeldBy;
            }
            if (currentUseCooldown < 1 && isFloating)
            {
                currentUseCooldown += 2 * Time.deltaTime;
            }
            else if (currentUseCooldown > 1)
            {
                currentUseCooldown = 1;
            }
        }
    }
}