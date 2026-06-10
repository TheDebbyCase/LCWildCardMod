using GameNetcodeStuff;
using LCWildCardMod.Utils;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace LCWildCardMod.MapObjects
{
    public class WormPit : WildCardMapObject, IHittable
    {
        [Space(3f)]
        [Header("WormPit")]
        [Space(3f)]
        [SerializeField]
        private Transform irisTransform = null;
        [SerializeField]
        private float damageRadius = 2f;
        [SerializeField]
        private Vector2 peepingFear = new Vector2(1f, 0.5f);
        [SerializeField]
        private Vector2 consumingFear = new Vector2(2f, 1f);
        [SerializeField]
        private Vector2 peepBetweenMinMax = new Vector2(2.5f, 7.5f);
        [SerializeField]
        private Vector2 peepHoldMinMax = new Vector2(4f, 7f);
        [SerializeField]
        private float lookSpeed = 135f;
        [SerializeField]
        private float peepCooldown = 0f;
        private float peepWaitBetween = 5f;
        private float peepWaitHold = 0f;
        private bool peeping = false;
        private float lookLerp = 0f;
        private Quaternion initialRot = Quaternion.identity;
        private float hunger = 0f;
        private float patience = 5f;
        private float sleepiness = 0f;
        private bool consuming = false;
        private float localForceTimer = 0f;
        private Vector2 lookMinMax = new Vector2(225f, 315f);
        private Vector2Int peepBetweenRound = Vector2Int.zero;
        private Vector2Int peepHoldRound = Vector2Int.zero;
        private Vector2 PeepBetweenMinMax
        {
            set
            {
                peepBetweenMinMax = value;
                peepBetweenRound = new Vector2Int(Mathf.RoundToInt(peepBetweenMinMax.x * 10f), Mathf.RoundToInt(peepBetweenMinMax.y * 10f) + 1);
            }
        }
        private Vector2 PeepHoldMinMax
        {
            set
            {
                peepHoldMinMax = value;
                peepHoldRound = new Vector2Int(Mathf.RoundToInt(peepHoldMinMax.x * 10f), Mathf.RoundToInt(peepHoldMinMax.y * 10f) + 1);
            }
        }
        internal override void Start()
        {
            PeepBetweenMinMax = peepBetweenMinMax;
            PeepHoldMinMax = peepHoldMinMax;
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsServer)
            {
                return;
            }
            State = 0;
        }
        internal override void Update()
        {
            base.Update();
            if (State == 0)
            {
                IrisLook(TargetPlayer);
            }
            else if (State == 2)
            {
                float forceMultiplier = 1f;
                if (localForceTimer > 0f)
                {
                    forceMultiplier += localForceTimer;
                    localForceTimer -= Time.deltaTime;
                }
                List<int> players = PlayersInRange;
                if (players.Contains((int)GameNetworkManager.Instance.localPlayerController.playerClientId))
                {
                    PlayerFearLocal(true, consumingFear.x, consumingFear.y);
                    PlayerForceLocal(forceMultiplier);
                }
            }
            if (!IsServer)
            {
                return;
            }
            Vector3 halfUp = Vector3.up * 0.5f;
            switch (State)
            {
                case 0:
                    {
                        PlayerControllerB target = TargetPlayer;
                        if (peeping)
                        {
                            if (target != null)
                            {
                                PlayerFear(target, true, peepingFear.x, peepingFear.y);
                            }
                            if (peepCooldown >= peepWaitHold)
                            {
                                Audio["Squish"].PlayRandomOneshot();
                                Animator.SetBool("HoldPeep", false);
                                peeping = false;
                                peepCooldown = 0f;
                            }
                        }
                        else if (peepCooldown >= peepWaitBetween)
                        {
                            Audio["Squish"].PlayRandomOneshot();
                            Animator.Trigger("Peep");
                            Animator.SetBool("HoldPeep", true);
                            peepWaitBetween = (float)Random.Next(peepBetweenRound.x, peepBetweenRound.y) / 10f;
                            peepWaitHold = (float)Random.Next(peepHoldRound.x, peepHoldRound.y) / 10f;
                            peeping = true;
                            peepCooldown = 0f;
                        }
                        if (hunger >= 5f)
                        {
                            peeping = false;
                            peepCooldown = 0f;
                            Animator.SetBool("HoldPeep", false);
                            hunger = 0f;
                            Audio["Growl"].PlayRandomOneshot();
                            State = 1;
                            break;
                        }
                        peepCooldown += Time.deltaTime;
                        if (!peeping || target == null)
                        {
                            break;
                        }
                        float multiplier = 1f;
                        if (Vector3.Distance(transform.position + halfUp, target.transform.position) <= 3f)
                        {
                            multiplier = 2f;
                        }
                        hunger += Time.deltaTime * multiplier;
                        break;
                    }
                case 1:
                    {
                        PlayerControllerB target = TargetPlayer;
                        if (target == null)
                        {
                            break;
                        }
                        float distance = Vector3.Distance(transform.position + halfUp, TargetPlayer.transform.position);
                        if ((distance <= 5f) || patience <= 0f)
                        {
                            patience = 5f;
                            State = 2;
                            break;
                        }
                        if (distance > 15f)
                        {
                            break;
                        }
                        patience -= Time.deltaTime;
                        break;
                    }
                case 2:
                    {
                        if (consuming)
                        {
                            if (sleepiness >= 12.5f)
                            {
                                consuming = false;
                                Animator.Trigger("Emerge");
                                Audio["Growl"].PlayRandomOneshot();
                                Audio["Music"].Stop();
                                sleepiness = ((float)Random.Next(50, 126) / 10f);
                                State = 3;
                                break;
                            }
                            if (PlayersInRange.Count == 0 && sleepiness >= 7.5f)
                            {
                                consuming = false;
                                Animator.Trigger("Emerge");
                                sleepiness = 0f;
                                Audio["Music"].Stop();
                                State = 0;
                                break;
                            }
                            sleepiness += Time.deltaTime;
                            break;
                        }
                        consuming = true;
                        sleepiness = 0f;
                        Animator.Trigger("Emerge");
                        Audio["Music"].PlayRandomClip();
                        for (int i = 0; i < PlayersInRange.Count; i++)
                        {
                            PlayerFear(StartOfRound.Instance.allPlayerScripts[PlayersInRange[i]], false, 0.25f, 0);
                        }
                        break;
                    }
                case 3:
                    {
                        if (sleepiness > 0f)
                        {
                            sleepiness -= Time.deltaTime;
                            break;
                        }
                        State = 0;
                        break;
                    }
            }
        }
        internal override PlayerControllerB GetClosestPlayer()
        {
            if (!peeping && State != 1)
            {
                return null;
            }
            return base.GetClosestPlayer();
        }
        internal override void OnStateChange(int newState)
        {
            base.OnStateChange(newState);
            if (State != 2)
            {
                return;
            }
            localForceTimer = 1f;
        }
        private void IrisLook(PlayerControllerB target)
        {
            if (Mathf.Approximately(lookLerp, 0f))
            {
                initialRot = irisTransform.rotation;
            }
            if (lookLerp < 360f)
            {
                lookLerp += Time.deltaTime * lookSpeed;
            }
            else if (lookLerp > 360f)
            {
                lookLerp = 360f;
            }
            if (Quaternion.Dot(initialRot, irisTransform.rotation) < 0.95f)
            {
                lookLerp = 0f;
                return;
            }
            Quaternion newRot = Quaternion.LookRotation(transform.up);
            if (target != null)
            {
                if (target.IsLocal())
                {
                    newRot = Quaternion.LookRotation(target.gameplayCamera.transform.position - transform.position);
                }
                else
                {
                    newRot = Quaternion.LookRotation(target.playerGlobalHead.transform.position - transform.position);
                }
            }
            if (newRot.eulerAngles.x < lookMinMax.x)
            {
                newRot = Quaternion.Euler(lookMinMax.x, newRot.eulerAngles.y, newRot.eulerAngles.z);
            }
            else if (newRot.eulerAngles.x > lookMinMax.y)
            {
                newRot = Quaternion.Euler(lookMinMax.y, newRot.eulerAngles.y, newRot.eulerAngles.z);
            }
            irisTransform.rotation = Quaternion.RotateTowards(initialRot, newRot, lookLerp);
        }
        public void PitChomp()
        {
            if (!IsServer || !consuming)
            {
                return;
            }
            HashSet<IHittable> hits = new HashSet<IHittable>();
            RaycastHit[] objectsHit = Physics.SphereCastAll(transform.position, damageRadius, Vector3.one, 0f, 1084754248, QueryTriggerInteraction.Collide);
            for (int i = 0; i < objectsHit.Length; i++)
            {
                RaycastHit hit = objectsHit[i];
                if (!hit.transform.TryGetComponent(out IHittable hitComponent) || !hits.Add(hitComponent))
                {
                    continue;
                }
                Base.HitOrDamage(hitComponent, basePlayerDamage, baseHitDamage, (hit.transform.position - transform.position).normalized, null, true, 1, CauseOfDeath.Mauling);
            }
        }
        public void PitAudioAnim()
        {
            if (State != 2 || !IsServer)
            {
                return;
            }
            Audio["Bite"].PlayRandomOneshot();
        }
        bool IHittable.Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit, bool playHitSFX, int hitID)
        {
            if (State != 0)
            {
                return false;
            }
            HitRpc();
            return true;
        }
        [Rpc(SendTo.Server)]
        private void HitRpc()
        {
            hunger = 5f;
            patience = 1f;
            State = 2;
        }
    }
}