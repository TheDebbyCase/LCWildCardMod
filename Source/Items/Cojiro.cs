using GameNetcodeStuff;
using Unity.Netcode.Components;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class Cojiro : NoisemakerProp
    {
        BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        public AudioSource flapSource;
        public NetworkAnimator itemAnimator;
        public bool isFloating;
        public PlayerControllerB previousPlayer;
        public override void GrabItem()
        {
            base.GrabItem();
            if (previousPlayer != null)
            {
                return;
            }
            previousPlayer = playerHeldBy;
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            int noiseIndex = noisemakerRandom.Next(0, noiseSFX.Length);
            float volume = (float)noisemakerRandom.Next((int)(minLoudness * 100f), (int)(maxLoudness * 100f)) / 100f;
            float pitch = (float)noisemakerRandom.Next((int)(minPitch * 100f), (int)(maxPitch * 100f)) / 100f;
            noiseAudio.pitch = pitch;
            noiseAudio.PlayOneShot(noiseSFX[noiseIndex], volume);
            WalkieTalkie.TransmitOneShotAudio(noiseAudio, noiseSFX[noiseIndex], volume);
            RoundManager.Instance.PlayAudibleNoise(transform.position, noiseRange, volume, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
            playerHeldBy.timeSinceMakingLoudNoise = 0f;
            if (!IsServer)
            {
                return;
            }
            itemAnimator.SetTrigger("playAnim");
        }
        public override void Update()
        {
            base.Update();
            if (!isPocketed && playerHeldBy != null && (playerHeldBy.isFallingFromJump || playerHeldBy.isFallingNoJump))
            {
                if (!isFloating)
                {
                    isFloating = true;
                    if (IsServer)
                    {
                        itemAnimator.Animator.SetBool("Floating", true);
                    }
                    Log.LogDebug($"Cojiro is slowing \"{playerHeldBy.playerUsername}\"'s fall!");
                    flapSource.Play();
                    WalkieTalkie.TransmitOneShotAudio(flapSource, flapSource.clip);
                }
                playerHeldBy.fallValue *= 0.9f;
            }
            else if (isFloating)
            {
                isFloating = false;
                if (IsServer)
                {
                    itemAnimator.Animator.SetBool("Floating", false);
                }
                Log.LogDebug($"Cojiro stopped slowing \"{playerHeldBy.playerUsername}\"'s fall!");
                flapSource.Stop();
            }
            currentUseCooldown = Mathf.Max(currentUseCooldown, 0f);
            if (currentUseCooldown == 0f && playerHeldBy == null)
            {
                previousPlayer = null;
            }
            else if (currentUseCooldown == 0f && playerHeldBy != null)
            {
                previousPlayer = playerHeldBy;
            }
            if (currentUseCooldown < 1f && isFloating)
            {
                currentUseCooldown += 2f * Time.deltaTime;
            }
            currentUseCooldown = Mathf.Min(currentUseCooldown, 1f);
        }
    }
}