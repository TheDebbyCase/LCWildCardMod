using System;
using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Components;
namespace LCWildCardMod.Items
{
    public class ThrowableNoisemaker : NoisemakerProp
    {
        public AnimationCurve throwFallCurve;
        public AnimationCurve throwVerticalFallCurve;
        public AnimationCurve throwVerticalFallCurveNoBounce;
        public Ray throwRay;
        public RaycastHit throwHit;
        public AudioSource spawnMusic;
        public AudioSource throwAudio;
        public AudioClip[] throwClips;
        public NetworkAnimator itemAnimator;
        public int isCollected = 0;
        private System.Random random;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            random = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            if (spawnMusic != null && spawnMusic.clip != null)
            {
                if (IsServer)
                {
                    BeginMusic();
                }
                BeginMusicServerRpc();
            }
        }
        public virtual void BeginMusic()
        {
            if (isCollected == 0)
            {
                spawnMusic.Play();
            }
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            if (!(GameNetworkManager.Instance.localPlayerController == null))
            {
                int num = random.Next(0, noiseSFX.Length);
                float num2 = (float)random.Next((int)(minLoudness * 100f), (int)(maxLoudness * 100f)) / 100f;
                float pitch = (float)random.Next((int)(minPitch * 100f), (int)(maxPitch * 100f)) / 100f;
                noiseAudio.pitch = pitch;
                noiseAudio.PlayOneShot(noiseSFX[num], num2);
                if (noiseAudioFar != null)
                {
                    noiseAudioFar.pitch = pitch;
                    noiseAudioFar.PlayOneShot(noiseSFXFar[num], num2);
                }

                if (itemAnimator != null)
                {
                    itemAnimator.SetTrigger("Activate");
                }

                WalkieTalkie.TransmitOneShotAudio(noiseAudio, noiseSFX[num], num2);
                RoundManager.Instance.PlayAudibleNoise(base.transform.position, noiseRange, num2, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
                if (minLoudness >= 0.6f && playerHeldBy != null)
                {
                    playerHeldBy.timeSinceMakingLoudNoise = 0f;
                }
            }
        }
        public virtual void Throw()
        {
            playerHeldBy.DiscardHeldObject(placeObject: true, null, GetThrowDestination());
            if (throwAudio != null && throwClips.Length > 0)
            {
                float pitch = (float)random.Next((int)(minPitch * 100f), (int)(maxPitch * 100f)) / 100f;
                throwAudio.pitch = pitch;
                throwAudio.clip = throwClips[random.Next(0, throwClips.Length)];
                throwAudio.Play();
                ThrowAudioServerRpc(pitch);
            }
        }
        public override void OnHitGround()
        {
            base.OnHitGround();
            throwAudio.Stop();
        }
        public override void EquipItem()
        {
            base.EquipItem();
            if (isCollected == 0)
            {
                isCollected = 1;
            }
            spawnMusic.Stop();
            throwAudio.Stop();
        }
        public override void Update()
        {
            base.Update();
            if (playerHeldBy != null && playerHeldBy.currentlyHeldObjectServer == this && WildCardMod.wildcardKeyBinds.ThrowButton.triggered && IsOwner)
            {
                Throw();
            }
        }
        public override void FallWithCurve()
        {
            float magnitude = (startFallingPosition - targetFloorPosition).magnitude;
            base.transform.rotation = Quaternion.Lerp(base.transform.rotation, Quaternion.Euler(itemProperties.restingRotation.x, base.transform.eulerAngles.y, itemProperties.restingRotation.z), 14f * Time.deltaTime / magnitude);
            base.transform.localPosition = Vector3.Lerp(startFallingPosition, targetFloorPosition, throwFallCurve.Evaluate(fallTime));
            if (magnitude > 5f)
            {
                base.transform.localPosition = Vector3.Lerp(new Vector3(base.transform.localPosition.x, startFallingPosition.y, base.transform.localPosition.z), new Vector3(base.transform.localPosition.x, targetFloorPosition.y, base.transform.localPosition.z), throwVerticalFallCurveNoBounce.Evaluate(fallTime));
            }
            else
            {
                base.transform.localPosition = Vector3.Lerp(new Vector3(base.transform.localPosition.x, startFallingPosition.y, base.transform.localPosition.z), new Vector3(base.transform.localPosition.x, targetFloorPosition.y, base.transform.localPosition.z), throwVerticalFallCurve.Evaluate(fallTime));
            }

            fallTime += Mathf.Abs(Time.deltaTime * 12f / magnitude);
        }
        public virtual Vector3 GetThrowDestination()
        {
            Vector3 position = base.transform.position;
            throwRay = new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward);
            if (Physics.Raycast(throwRay, out throwHit, 20f, StartOfRound.Instance.allPlayersCollideWithMask, QueryTriggerInteraction.Ignore))
            {
                position = throwRay.GetPoint(throwHit.distance - 0.05f);
            }
            else
            {
                position = throwRay.GetPoint(20f);
            }
            throwRay = new Ray(position, Vector3.down);
            if (Physics.Raycast(throwRay, out throwHit, 20f, StartOfRound.Instance.allPlayersCollideWithMask, QueryTriggerInteraction.Ignore))
            {
                return throwHit.point + Vector3.up * 0.05f;
            }
            return throwRay.GetPoint(30f);
        }
        public override int GetItemDataToSave()
        {
            return isCollected;
        }
        public override void LoadItemSaveData(int saveData)
        {
            isCollected = saveData;
        }
        [ServerRpc(RequireOwnership = false)]
        public void ThrowAudioServerRpc(float pitch)
        {
            ThrowAudioClientRpc(pitch);
        }
        [ClientRpc]
        public void ThrowAudioClientRpc(float pitch)
        {
            throwAudio.pitch = pitch;
            throwAudio.Play();
        }
        [ServerRpc(RequireOwnership = false)]
        public void BeginMusicServerRpc()
        {
            BeginMusicClientRpc(isCollected);
        }
        [ClientRpc]
        public void BeginMusicClientRpc(int id)
        {
            isCollected = id;
            BeginMusic();
        }
    }
}