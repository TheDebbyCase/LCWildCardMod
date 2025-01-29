using System;
using Unity.Netcode;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class ThrowableNoisemaker : NoisemakerProp
    {
        public AnimationCurve throwFallCurve;
        public AnimationCurve throwVerticalFallCurve;
        public AnimationCurve throwVerticalFallCurveNoBounce;
        public Ray throwRay;
        public RaycastHit throwHit;
        public AudioSource throwAudio;
        public AudioClip[] throwClips;
        private readonly System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
        public bool CheckThrowPress()
        {
            if (WildCardMod.wildcardKeyBinds.ThrowButton.triggered && IsOwner)
            {
                return true;
            }
            return false;
        }
        public virtual void Throw()
        {
            playerHeldBy.DiscardHeldObject(placeObject: true, null, GetThrowDestination());
            float pitch = (float)random.Next((int)(minPitch * 100f), (int)(maxPitch * 100f)) / 100f;
            throwAudio.pitch = pitch;
            throwAudio.clip = throwClips[random.Next(0, throwClips.Length)];
            throwAudio.Play();
            ThrowAudioServerRpc(pitch);
        }
        public override void Update()
        {
            base.Update();
            if (playerHeldBy != null && playerHeldBy.currentlyHeldObjectServer == this && CheckThrowPress())
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
        public Vector3 GetThrowDestination()
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
    }
}