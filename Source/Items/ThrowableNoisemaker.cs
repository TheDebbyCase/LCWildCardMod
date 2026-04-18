using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Collections;
namespace LCWildCardMod.Items
{
    public class ThrowableNoisemaker : NoisemakerProp
    {
        BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        public AnimationCurve throwFallCurve;
        public AnimationCurve throwVerticalFallCurve;
        public AnimationCurve throwVerticalFallCurveNoBounce;
        internal Ray throwRay;
        internal RaycastHit throwHit;
        public AudioSource spawnMusic;
        public AudioSource throwAudio;
        public AudioClip[] throwClips;
        internal Vector3 handPosition;
        internal static HashSet<int> validParameters = new HashSet<int>();
        System.Random random;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            WildCardMod.Instance.KeyBinds.WildCardButton.performed += ThrowButton;
            random = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            if (base.IsServer && spawnMusic != null && spawnMusic.clip != null)
            {
                BeginMusicClientRpc(hasBeenHeld);
            }
            if (triggerAnimator == null)
            {
                return;
            }
            for (int i = 0; i < triggerAnimator.parameters.Length; i++)
            {
                validParameters.Add(Animator.StringToHash(triggerAnimator.parameters[i].name));
            }
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            WildCardMod.Instance.KeyBinds.WildCardButton.performed -= ThrowButton;
        }
        internal virtual void BeginMusic()
        {
            if (hasBeenHeld)
            {
                return;
            }
            spawnMusic.Play();
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            if (GameNetworkManager.Instance.localPlayerController == null || noiseSFX.Length == 0)
            {
                return;
            }
            int noiseIndex = random.Next(0, noiseSFX.Length);
            float volume = (float)random.Next((int)(minLoudness * 100f), (int)(maxLoudness * 100f)) / 100f;
            float pitch = (float)random.Next((int)(minPitch * 100f), (int)(maxPitch * 100f)) / 100f;
            noiseAudio.pitch = pitch;
            noiseAudio.PlayOneShot(noiseSFX[noiseIndex], volume);
            if (noiseAudioFar != null)
            {
                noiseAudioFar.pitch = pitch;
                noiseAudioFar.PlayOneShot(noiseSFXFar[noiseIndex], volume);
            }
            if (base.IsServer && validParameters.Contains(Animator.StringToHash("Activate")))
            {
                triggerAnimator?.SetTrigger("Activate");
            }
            WalkieTalkie.TransmitOneShotAudio(noiseAudio, noiseSFX[noiseIndex], volume);
            RoundManager.Instance.PlayAudibleNoise(base.transform.position, noiseRange, volume, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
            if (minLoudness < 0.6f || playerHeldBy == null)
            {
                return;
            }
            playerHeldBy.timeSinceMakingLoudNoise = 0f;
        }
        internal void ThrowButton(InputAction.CallbackContext throwContext)
        {
            if (playerHeldBy == null || isPocketed)
            {
                return;
            }
            handPosition = base.transform.localPosition;
            Log.LogDebug($"\"{itemProperties.itemName}\" Vector in Player Hand: {handPosition}");
            playerHeldBy.DiscardHeldObject(placeObject: true, null, GetThrowDestination());
            Throw();
            if (throwAudio == null || throwClips.Length == 0)
            {
                return;
            }
            float pitch = (float)random.Next((int)(minPitch * 100f), (int)(maxPitch * 100f)) / 100f;
            throwAudio.pitch = pitch;
            throwAudio.clip = throwClips[random.Next(0, throwClips.Length)];
            throwAudio.Play();
            StartCoroutine(StopAudioCoroutine());
            WalkieTalkie.TransmitOneShotAudio(throwAudio, throwAudio.clip);
        }
        public override void EquipItem()
        {
            base.EquipItem();
            spawnMusic.Stop();
            throwAudio.Stop();
        }
        public override void DiscardItem()
        {
            handPosition = base.transform.localPosition;
            base.DiscardItem();
        }
        internal virtual void Throw()
        {
            if (!base.IsOwner)
            {
                return;
            }
            FloorRotServerRpc(floorYRot);
        }
        public override void FallWithCurve()
        {
            float magnitude = (startFallingPosition - targetFloorPosition).magnitude;
            base.transform.rotation = Quaternion.Lerp(base.transform.rotation, Quaternion.Euler(itemProperties.restingRotation.x, (float)(floorYRot + itemProperties.floorYOffset) + 90f, itemProperties.restingRotation.z), 14f * Time.deltaTime / magnitude);
            base.transform.localPosition = Vector3.Lerp(startFallingPosition, targetFloorPosition, throwFallCurve.Evaluate(fallTime));
            AnimationCurve curve = throwVerticalFallCurve;
            if (magnitude > 5f)
            {
                curve = throwVerticalFallCurveNoBounce;
            }
            base.transform.localPosition = Vector3.Lerp(new Vector3(base.transform.localPosition.x, startFallingPosition.y, base.transform.localPosition.z), new Vector3(base.transform.localPosition.x, targetFloorPosition.y, base.transform.localPosition.z), curve.Evaluate(fallTime));
            fallTime += Mathf.Abs(Time.deltaTime * 4f / magnitude);
        }
        internal virtual Vector3 GetThrowDestination()
        {
            throwRay = new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward);
            Vector3 position = throwRay.GetPoint(20f);
            if (Physics.Raycast(throwRay, out throwHit, 20f, StartOfRound.Instance.allPlayersCollideWithMask, QueryTriggerInteraction.Ignore))
            {
                position = throwRay.GetPoint(throwHit.distance - 0.05f);
            }
            throwRay = new Ray(position, Vector3.down);
            if (Physics.Raycast(throwRay, out throwHit, 20f, StartOfRound.Instance.allPlayersCollideWithMask, QueryTriggerInteraction.Ignore))
            {
                return throwHit.point + Vector3.up * 0.05f;
            }
            return throwRay.GetPoint(30f);
        }
        internal IEnumerator StopAudioCoroutine()
        {
            yield return new WaitUntil(() => fallTime >= 1);
            throwAudio.Stop();
        }
        public override void LoadItemSaveData(int saveData)
        {
            hasBeenHeld = true;
        }
        [ClientRpc]
        public void BeginMusicClientRpc(bool held)
        {
            hasBeenHeld = held;
            BeginMusic();
        }
        [ServerRpc (RequireOwnership = false)]
        public void FloorRotServerRpc(int rot)
        {
            FloorRotClientRpc(rot);
        }
        [ClientRpc]
        public void FloorRotClientRpc(int rot)
        {
            floorYRot = rot;
        }
    }
}