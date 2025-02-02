using GameNetcodeStuff;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.GraphicsBuffer;
namespace LCWildCardMod.Items
{
    public class Halo : PhysicsProp
    {
        public ParticleSystem[] dripParticles;
        public ParticleSystem spinParticle;
        public AudioSource spawnMusic;
        public AudioSource throwAudio;
        public AudioClip[] throwClips;
        public AudioClip breakSound;
        public float minPitch;
        public float maxPitch;
        public NetworkAnimator itemAnimator;
        public int isExhausted = 0;
        public AnimationCurve throwCurve;
        public Component parentComponent;
        internal bool isThrowing = false;
        internal float throwTime = 0;
        internal Vector3 handPosition;
        internal Vector3 targetPosition;
        internal List<IHittable> hitList = new List<IHittable>();
        private System.Random random;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            random = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            WildCardMod.wildcardKeyBinds.ThrowButton.performed += ThrowButton;
            spinParticle.gameObject.SetActive(false);
            if (IsServer)
            {
                BeginMusic();
            }
            BeginMusicServerRpc();
            
        }
        public void BeginMusic()
        {
            if (isExhausted == 1)
            {
                this.GetComponentInChildren<MeshRenderer>().material.color = new Color(0.1f, 0.1f, 0.1f);
                spinParticle.gameObject.SetActive(false);
                StopDrip();
            }
            else
            {
                StartDrip();
                spawnMusic.Play();
            }
        }
        public void ThrowButton(InputAction.CallbackContext throwContext)
        {
            if (playerHeldBy != null && playerHeldBy.currentlyHeldObjectServer == this && isExhausted == 0)
            {
                if (throwAudio != null && throwClips.Length > 0)
                {
                    float pitch = (float)random.Next((int)(minPitch * 100f), (int)(maxPitch * 100f)) / 100f;
                    throwAudio.pitch = pitch;
                    int selectedClip = random.Next(0, throwClips.Length);
                    playerHeldBy.timeSinceMakingLoudNoise = 0f;
                    ThrowAudioServerRpc(pitch, selectedClip);
                }
                ThrowServerRpc();
            }
        }
        public override void Update()
        {
            base.Update();
            if (isThrowing)
            {
                if (playerHeldBy == null)
                {
                    ThrowEndServerRpc();
                }
                else
                {
                    ThrowCurveServerRpc();
                    if (throwTime >= 1)
                    {
                        ThrowEndServerRpc();
                    }
                    else
                    {
                        RaycastHit[] objectsHit = Physics.SphereCastAll(parentComponent.transform.position, 1f, playerHeldBy.gameplayCamera.transform.forward, 0f, 1084754248, QueryTriggerInteraction.Collide);
                        foreach (RaycastHit hit in objectsHit)
                        {
                            WildCardMod.Log.LogDebug(hit.transform.gameObject.name); 
                            if (hit.transform.TryGetComponent<IHittable>(out var hitComponent) && !hit.collider.isTrigger && !hitList.Contains(hitComponent) && playerHeldBy.transform != hit.transform && (hit.transform.GetComponent<PlayerControllerB>() || hit.transform.GetComponent<EnemyAICollisionDetect>()))
                            {
                                hitList.Add(hitComponent);
                                hitComponent.Hit(2, playerHeldBy.gameplayCamera.transform.forward, playerHeldBy, true, 1);
                            }
                        }
                    }
                }
            }
        }
        public void ThrowCurve()
        {
            handPosition = playerHeldBy.localItemHolder.transform.position;
            parentComponent.transform.position = Vector3.Lerp(handPosition, targetPosition, throwCurve.Evaluate(throwTime));
            throwTime += Mathf.Abs(Time.deltaTime * 0.5f);
        }
        public override void EquipItem()
        {
            base.EquipItem();
            itemAnimator.Animator.SetBool("BeingHeld", true);
        }
        public void Throw()
        {
            if (hitList.Count > 0)
            {
                hitList.Clear();
            }
            if (IsServer)
            {
                Ray ray = new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, 10f, StartOfRound.Instance.allPlayersCollideWithMask, QueryTriggerInteraction.Ignore))
                {
                    targetPosition = ray.GetPoint(hit.distance - 0.05f);
                }
                else
                {
                    targetPosition = ray.GetPoint(10f);
                }
                SyncThrowDestinationServerRpc(targetPosition);
            }
            throwTime = 0;
            isThrowing = true;
            itemAnimator.Animator.SetBool("BeingThrown", true);
            StopDrip();
        }
        public void ThrowEnd()
        {
            WildCardMod.Log.LogDebug("Halo Stopped Throwing!");
            parentComponent.transform.localPosition = Vector3.zero;
            this.transform.localPosition = itemProperties.positionOffset;
            isThrowing = false;
            itemAnimator.Animator.SetBool("BeingThrown", false);
            if (isExhausted == 0)
            {
                StartDrip();
            }
            else
            {
                spinParticle.gameObject.SetActive(false);
            }
            throwAudio.Stop();
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            itemAnimator.Animator.SetBool("BeingHeld", false);
        }
        public override void PocketItem()
        {
            base.PocketItem();
            if (isExhausted == 0)
            {
                StopDrip();
            }
        }
        public void StopDrip()
        {
            foreach (ParticleSystem particle in dripParticles)
            {
                particle.gameObject.SetActive(false);
            }
            if (isExhausted == 0)
            {
                spinParticle.gameObject.SetActive(true);
                spinParticle.Play();
            }
        }
        public void StartDrip()
        {
            if (isExhausted == 0)
            {
                foreach (ParticleSystem particle in dripParticles)
                {
                    particle.gameObject.SetActive(true);
                    particle.Play();
                }
            }
            spinParticle.gameObject.SetActive(false);
        }
        public void ExhaustHalo()
        {
            isExhausted = 1;
            this.GetComponent<AudioSource>().clip = breakSound;
            this.GetComponent<AudioSource>().Play();
            this.GetComponentInChildren<MeshRenderer>().material.color = new Color(0.1f, 0.1f, 0.1f);
            StopDrip();
        }
        public override int GetItemDataToSave()
        {
            return isExhausted;
        }
        public override void LoadItemSaveData(int saveData)
        {
            isExhausted = saveData;
        }
        [ServerRpc(RequireOwnership = false)]
        public void BeginMusicServerRpc()
        {
            BeginMusicClientRpc(isExhausted);
        }
        [ClientRpc]
        public void BeginMusicClientRpc(int id)
        {
            isExhausted = id;
            BeginMusic();
        }
        [ServerRpc(RequireOwnership = false)]
        public void ThrowAudioServerRpc(float pitch, int selectedClip)
        {
            ThrowAudioClientRpc(pitch, selectedClip);
        }
        [ClientRpc]
        public void ThrowAudioClientRpc(float pitch, int selectedClip)
        {
            throwAudio.pitch = pitch;
            throwAudio.clip = throwClips[selectedClip];
            throwAudio.Play();
        }
        [ServerRpc(RequireOwnership = false)]
        public void ThrowEndServerRpc()
        {
            ThrowEndClientRpc();
        }
        [ClientRpc]
        public void ThrowEndClientRpc()
        {
            ThrowEnd();
        }
        [ServerRpc(RequireOwnership = false)]
        public void ThrowCurveServerRpc()
        {
            ThrowCurveClientRpc();
        }
        [ClientRpc]
        public void ThrowCurveClientRpc()
        {
            ThrowCurve();
        }
        [ServerRpc(RequireOwnership = false)]
        public void ThrowServerRpc()
        {
            ThrowClientRpc();
        }
        [ClientRpc]
        public void ThrowClientRpc()
        {
            Throw();
        }
        [ServerRpc(RequireOwnership = false)]
        public void SyncThrowDestinationServerRpc(Vector3 target)
        {
            SyncThrowDestinationClientRpc(target);
        }
        [ClientRpc]
        public void SyncThrowDestinationClientRpc(Vector3 target)
        {
            targetPosition = target;
        }
        [ServerRpc(RequireOwnership = false)]
        public void ExhaustHaloServerRpc()
        {
            ExhaustHaloClientRpc();
        }
        [ClientRpc]
        public void ExhaustHaloClientRpc()
        {
            ExhaustHalo();
        }
    }
}