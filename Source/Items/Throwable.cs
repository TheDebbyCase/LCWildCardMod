using Unity.Netcode;
using UnityEngine;
using System.Collections;
namespace LCWildCardMod.Items
{
    public class WildCardThrowable : WildCardProp
    {
        internal static float defaultSpeed = 7.5f;
        [Space(3f)]
        [Header("WildCardThrowable")]
        [Space(3f)]
        [SerializeField]
        internal AnimationCurve throwCurve = null;
        [SerializeField]
        internal AnimationCurve throwVerticalCurve = null;
        [SerializeField]
        internal AnimationCurve throwVerticalCurveNoBounce = null;
        [SerializeField]
        internal Transform transformToThrow = null;
        [SerializeField]
        internal bool throwToFloor = true;
        [SerializeField]
        internal bool discardOnThrow = true;
        [SerializeField]
        internal float throwDistance = 20f;
        [SerializeField]
        internal float throwSpeed = 1f;
        internal bool throwing = false;
        internal float throwTime = 0f;
        internal Vector3 targetPosition;
        internal bool lastThrownByEnemy = false;
        private Vector3 startThrowLocalPos = Vector3.zero;
        public override void OnDestroy()
        {
            if (throwing && isHeld)
            {
                LastPlayerHeldBy.throwingObject = false;
            }
            base.OnDestroy();
        }
        internal override void WildCardUse()
        {
            base.WildCardUse();
            if (!IsOwner || throwing || ((!isHeld || isPocketed) && !isHeldByEnemy))
            {
                return;
            }
            targetPosition = GetThrowDestination();
            if (discardOnThrow)
            {
                if (isHeld)
                {
                    LastPlayerHeldBy.DiscardHeldObject(placeObject: true, null, targetPosition);
                }
                else
                {
                    EnemyForceDropItem();
                }
            }
            Audio["Throw"]?.PlayRandomClip();
            Throw(targetPosition, true);
        }
        internal virtual void Throw(Vector3 newPosition, bool byEnemy, bool networked = true)
        {
            lastThrownByEnemy = byEnemy;
            targetPosition = newPosition;
            if (!discardOnThrow)
            {
                if (!byEnemy)
                {
                    LastPlayerHeldBy.throwingObject = true;
                }
                startThrowLocalPos = transformToThrow.localPosition;
            }
            throwTime = 0f;
            throwing = true;
            if (IsServer || !NetworkAnimations)
            {
                Animator?.SetBool("BeingThrown", true);
            }
            if (!networked)
            {
                return;
            }
            ThrowRpc(newPosition, byEnemy);
        }
        internal Vector3 GetThrowDestination()
        {
            Transform rayStart;
            if (isHeld)
            {
                rayStart = LastPlayerHeldBy.gameplayCamera.transform;
            }
            else
            {
                rayStart = LastEnemyHeldBy.eye;
            }
            Ray throwRay = new Ray(rayStart.position, rayStart.forward);
            Vector3 position = throwRay.GetPoint(throwDistance);
            if (Physics.Raycast(throwRay, out RaycastHit throwHit, throwDistance, StartOfRound.Instance.allPlayersCollideWithMask, QueryTriggerInteraction.Ignore))
            {
                position = throwRay.GetPoint(throwHit.distance - 0.05f);
            }
            if (!throwToFloor)
            {
                return position;
            }
            throwRay = new Ray(position, Vector3.down);
            if (Physics.Raycast(throwRay, out throwHit, throwDistance, StartOfRound.Instance.allPlayersCollideWithMask, QueryTriggerInteraction.Ignore))
            {
                return throwHit.point + (Vector3.up * 0.05f);
            }
            return throwRay.GetPoint(throwDistance * 1.5f);
        }
        public override void Update()
        {
            base.Update();
            if (discardOnThrow || !throwing)
            {
                return;
            }
            ThrowUpdate();
            if (throwTime < 1f || !IsOwner)
            {
                return;
            }
            ThrowEnd();
        }
        internal virtual void ThrowUpdate()
        {
            transformToThrow.position = Vector3.Lerp(transform.position, targetPosition, throwCurve.Evaluate(throwTime));
            float multiplier = defaultSpeed * throwSpeed;
            throwTime += (Time.deltaTime * multiplier) / Mathf.Max(multiplier * 0.5f, Vector3.Distance(transform.position, targetPosition));
        }
        internal virtual void ThrowEnd(bool networked = true)
        {
            if (IsServer || !NetworkAnimations)
            {
                Animator?.SetBool("BeingThrown", false);
            }
            Audio["Throw"]?.Stop(false);
            if (isHeld || isHeldByEnemy)
            {
                if (isHeld)
                {
                    LastPlayerHeldBy.throwingObject = false;
                }
                if (!discardOnThrow)
                {
                    throwTime = 0f;
                    targetPosition = transform.position;
                    transformToThrow.localPosition = startThrowLocalPos;
                }
            }
            StartCoroutine(FrameWait());
            if (!networked)
            {
                return;
            }
            ThrowEndRpc();
        }
        private IEnumerator FrameWait()
        {
            yield return new WaitForEndOfFrame();
            throwing = false;
        }
        public override void EquipItem()
        {
            base.EquipItem();
            ThrowEnd(false);
        }
        internal override void GrabFromAny(bool fromPlayer = true, EnemyAI enemy = null)
        {
            base.GrabFromAny(fromPlayer, enemy);
            ThrowEnd(false);
        }
        internal override void DiscardFromAny(bool fromPlayer = true, EnemyAI enemy = null)
        {
            base.DiscardFromAny(fromPlayer, enemy);
            if (discardOnThrow || !IsOwner || !throwing)
            {
                return;
            }
            if (fromPlayer && !LastPlayerHeldBy.throwingObject)
            {
                return;
            }
            ThrowEnd();
        }
        public override void FallWithCurve()
        {
            if (!discardOnThrow || !throwing)
            {
                base.FallWithCurve();
                return;
            }
            float magnitude = (startFallingPosition - targetFloorPosition).magnitude;
            float speed = (Time.deltaTime * defaultSpeed * throwSpeed) / magnitude;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(itemProperties.restingRotation.x, (float)(floorYRot + itemProperties.floorYOffset) + 90f, itemProperties.restingRotation.z), speed);
            transform.localPosition = Vector3.Lerp(startFallingPosition, targetFloorPosition, throwCurve.Evaluate(fallTime));
            AnimationCurve curve = throwVerticalCurve;
            if (magnitude > 5f)
            {
                curve = throwVerticalCurveNoBounce;
            }
            transform.localPosition = Vector3.Lerp(new Vector3(transform.localPosition.x, startFallingPosition.y, transform.localPosition.z), new Vector3(transform.localPosition.x, targetFloorPosition.y, transform.localPosition.z), curve.Evaluate(fallTime));
            fallTime += speed;
        }
        public override void OnHitGround()
        {
            base.OnHitGround();
            ThrowEnd(false);
        }
        [Rpc(SendTo.NotMe)]
        private void ThrowRpc(Vector3 newPosition, bool byEnemy)
        {
            Throw(newPosition, byEnemy, false);
        }
        [Rpc(SendTo.NotMe)]
        private void ThrowEndRpc()
        {
            ThrowEnd(false);
        }
    }
}