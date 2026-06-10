using UnityEngine;
using System.Collections;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using LCWildCardMod.Utils;
using Unity.Netcode;
namespace LCWildCardMod.Items
{
    public class WildCardSwingable : WildCardProp
    {
        internal const int HitMask = 1084754248;
        [Space(3f)]
        [Header("WildCardSwingable")]
        [Space(3f)]
        [SerializeField]
        internal int hitForce = 1;
        [SerializeField]
        internal int playerDamage = 15;
        [SerializeField]
        internal CauseOfDeath playerCOD = CauseOfDeath.Bludgeoning;
        [SerializeField]
        internal float playerForceMultiplier = 1f;
        internal bool reeling = false;
        internal bool forceEndReel = false;
        private Coroutine reelingCoroutine = null;
        public override void Start()
        {
            base.Start();
            SelectAudioClips surfaceClips = Audio["Surface"];
            if (surfaceClips == null)
            {
                return;
            }
            surfaceClips.Add(StartOfRound.Instance.footstepSurfaces.Select((x) => x.hitSurfaceSFX));
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (reeling || !buttonDown)
            {
                return;
            }
            reeling = true;
            StartReel();
        }
        internal virtual void StartReel()
        {
            if (reelingCoroutine != null)
            {
                StopCoroutine(reelingCoroutine);
            }
            if (isHeld)
            {
                LastPlayerHeldBy.activatingItem = true;
                LastPlayerHeldBy.twoHanded = true;
                if (IsOwner)
                {
                    LastPlayerHeldBy.playerBodyAnimator.ResetTrigger("shovelHit");
                    LastPlayerHeldBy.playerBodyAnimator.SetBool("reelingUp", true);
                }
            }
            if (IsOwner)
            {
                Audio["Reel"]?.PlayRandomOneshot();
            }
            reelingCoroutine = StartCoroutine(Reel());
        }
        private IEnumerator Reel()
        {
            yield return new WaitForSeconds(0.35f);
            yield return new WaitUntil(() => !ButtonDown || (!isHeld && !isHeldByEnemy) || forceEndReel);
            bool cancel = (!isHeld && !isHeldByEnemy) || forceEndReel;
            Swing(cancel);
            yield return new WaitForSeconds(0.13f);
            yield return new WaitForEndOfFrame();
            Hit(cancel);
            yield return new WaitForSeconds(0.3f);
            reeling = false;
            forceEndReel = false;
            reelingCoroutine = null;
        }
        public override void Update()
        {
            base.Update();
            if (!reeling)
            {
                return;
            }
            ReelUpdate();
        }
        internal virtual void ReelUpdate()
        {

        }
        public override void DiscardItem()
        {
            if (isHeld)
            {
                LastPlayerHeldBy.activatingItem = false;
            }
            base.DiscardItem();
        }
        internal virtual void Swing(bool cancel = false)
        {
            if (!IsOwner)
            {
                return;
            }
            if (isHeld)
            {
                LastPlayerHeldBy.playerBodyAnimator.SetBool("reelingUp", value: false);
            }
            if (cancel)
            {
                return;
            }
            Audio["Swing"]?.PlayRandomOneshot();
            if (!isHeld)
            {
                return;
            }
            LastPlayerHeldBy.UpdateSpecialAnimationValue(specialAnimation: true, (short)LastPlayerHeldBy.transform.localEulerAngles.y, 0.4f);
        }
        internal virtual void Hit(bool cancel = false)
        {
            if (isHeld)
            {
                LastPlayerHeldBy.activatingItem = false;
                LastPlayerHeldBy.twoHanded = false;
            }
            if (cancel)
            {
                return;
            }
            if (!IsOwner)
            {
                return;
            }
            bool hitSuccess = false;
            bool hitNonSurface = false;
            bool hittingPlayer = false;
            int surfaceIndex = -1;
            Transform castStart;
            HashSet<IHittable> hits;
            if (isHeld)
            {
                hits = new HashSet<IHittable> { LastPlayerHeldBy };
                castStart = LastPlayerHeldBy.gameplayCamera.transform;
            }
            else
            {
                hits = new HashSet<IHittable>() { LastEnemyHeldBy.GetComponentInChildren<EnemyAICollisionDetect>() };
                castStart = LastEnemyHeldBy.eye;
            }
            List<RaycastHit> objectsHitList = Physics.SphereCastAll(castStart.position + castStart.right * -0.35f, 0.8f, castStart.forward, 1.5f, HitMask, QueryTriggerInteraction.Collide).ToList();
            objectsHitList.Sort((x, y) => x.distance.CompareTo(y.distance));
            for (int i = 0; i < objectsHitList.Count; i++)
            {
                RaycastHit hit = objectsHitList[i];
                int layer = hit.transform.gameObject.layer;
                if (layer == 8 || layer == 11)
                {
                    if (hit.collider.isTrigger)
                    {
                        continue;
                    }
                    hitSuccess = true;
                    string surfaceTag = hit.collider.gameObject.tag;
                    for (int j = 0; j < StartOfRound.Instance.footstepSurfaces.Length; j++)
                    {
                        if (StartOfRound.Instance.footstepSurfaces[j].surfaceTag == surfaceTag)
                        {
                            surfaceIndex = j;
                            break;
                        }
                    }
                    continue;
                }
                if (!hit.transform.TryGetComponent(out IHittable hittable) || !hits.Add(hittable) || (hit.point != Vector3.zero && Physics.Linecast(LastPlayerHeldBy.gameplayCamera.transform.position, hit.point, out var _, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)))
                {
                    continue;
                }
                hitSuccess = true;
                bool doHit = false;
                EnemyAICollisionDetect enemy = hittable as EnemyAICollisionDetect;
                if (enemy != null)
                {
                    doHit = enemy.mainScript != null && (!StartOfRound.Instance.hangarDoorsClosed || enemy.mainScript.isInsidePlayerShip == LastPlayerHeldBy.isInHangarShipRoom);
                }
                else
                {
                    PlayerControllerB checkingPlayer = hittable as PlayerControllerB;
                    doHit = checkingPlayer == null || !hittingPlayer;
                    hittingPlayer = checkingPlayer != null && !hittingPlayer;
                }
                if (!doHit)
                {
                    continue;
                }
                PlayerControllerB playerHitBy = null;
                if (isHeld)
                {
                    playerHitBy = LastPlayerHeldBy;
                }
                bool doneHit = Base.HitOrDamage(hittable, playerDamage, hitForce, castStart.forward, playerHitBy, true, 1, playerCOD, playerForceMultiplier);
                if (hitNonSurface)
                {
                    continue;
                }
                hitNonSurface = doneHit;
            }
            if (!hitSuccess)
            {
                return;
            }
            Audio["Hit"]?.PlayRandomClip();
            if (isHeld)
            {
                LastPlayerHeldBy.playerBodyAnimator.SetTrigger("shovelHit");
            }
            if (hitNonSurface || surfaceIndex < 0)
            {
                return;
            }
            Audio["Surface"]?.PlayOneshot(surfaceIndex);
        }
        internal virtual void ForceEndReel(bool networked = true)
        {
            forceEndReel = true;
            if (!networked)
            {
                return;
            }
            ForceEndReelRpc();
        }
        [Rpc(SendTo.NotMe)]
        private void ForceEndReelRpc()
        {
            ForceEndReel(false);
        }
    }
}