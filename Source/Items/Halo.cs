using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LCWildCardMod.Items
{
    public class SmithHalo : PhysicsProp
    {
        readonly BepInEx.Logging.ManualLogSource log = WildCardMod.Log;
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
        public bool isThrowing = false;
        public float throwTime = 0;
        public Vector3 handPosition;
        public Vector3 targetPosition;
        public Coroutine exhaustCoroutine;
        public List<IHittable> hitList = new List<IHittable>();
        private System.Random random;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            random = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            WildCardMod.wildcardKeyBinds.WildCardButton.performed += ThrowButton;
            spinParticle.gameObject.SetActive(false);
            if (base.IsServer)
            {
                BeginMusicClientRpc(isExhausted);
            }
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
            if (playerHeldBy != null && playerHeldBy.currentlyHeldObjectServer == this && isExhausted == 0 && !isThrowing && base.IsOwner)
            {
                if (throwAudio != null && throwClips.Length > 0)
                {
                    float pitch = (float)random.Next((int)(minPitch * 100f), (int)(maxPitch * 100f)) / 100f;
                    int selectedClip = random.Next(0, throwClips.Length);
                    ThrowAudioServerRpc(pitch, selectedClip);
                }
                ThrowServerRpc();
            }
        }
        public override void Update()
        {
            base.Update();
            if (base.IsOwner)
            {
                if (isThrowing)
                {
                    if (playerHeldBy == null)
                    {
                        ThrowCurveServerRpc(parentComponent.transform.position);
                        ThrowEndServerRpc();
                    }
                    else
                    {
                        ThrowCurve();
                        if (throwTime >= 1)
                        {
                            ThrowCurveServerRpc(parentComponent.transform.position);
                            ThrowEndServerRpc();
                        }
                        else
                        {
                            RaycastHit[] objectsHit = Physics.SphereCastAll(parentComponent.transform.position, 0.5f, playerHeldBy.gameplayCamera.transform.forward, 0f, 1084754248, QueryTriggerInteraction.Collide);
                            for (int i = 0; i < objectsHit.Length; i++)
                            {
                                if (playerHeldBy != null && objectsHit[i].transform.TryGetComponent<IHittable>(out var hitComponent) && !hitList.Contains(hitComponent) && playerHeldBy.transform != objectsHit[i].transform && (objectsHit[i].transform.GetComponent<PlayerControllerB>() || objectsHit[i].transform.GetComponent<EnemyAICollisionDetect>()))
                                {
                                    hitList.Add(hitComponent);
                                    if (objectsHit[i].transform.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
                                    {              
                                        hitComponent.Hit(1, playerHeldBy.gameplayCamera.transform.forward, playerHeldBy, true, 1);
                                        log.LogDebug($"Halo Hit {player.playerUsername}");
                                    }
                                    else if (objectsHit[i].transform.TryGetComponent<EnemyAICollisionDetect>(out EnemyAICollisionDetect enemy))
                                    {
                                        hitComponent.Hit(2, playerHeldBy.gameplayCamera.transform.forward, playerHeldBy, true, 1);
                                        log.LogDebug($"Halo Hit {enemy.mainScript.enemyType.enemyName}");
                                    }
                                }
                                else if (playerHeldBy == null)
                                {
                                    log.LogDebug($"Halo Hit But Throwing Player Is Missing");
                                }
                            }
                        }
                    }
                }
                else if (parentComponent.transform.localPosition != Vector3.zero)
                {
                    parentComponent.transform.localPosition = Vector3.zero;
                }
            }
        }
        public void ThrowCurve()
        {
            handPosition = playerHeldBy.localItemHolder.transform.position;
            parentComponent.transform.position = Vector3.Lerp(handPosition, targetPosition, throwCurve.Evaluate(throwTime));
            throwTime += Mathf.Abs(Time.deltaTime * 0.75f);
            ThrowCurveServerRpc(parentComponent.transform.position);
        }
        public override void EquipItem()
        {
            base.EquipItem();
            parentComponent.transform.localPosition = Vector3.zero;
            this.transform.localPosition = itemProperties.positionOffset;
            if (base.IsServer)
            {
                itemAnimator.Animator.SetBool("BeingHeld", true);
            }
            spawnMusic.Stop();
        }
        public void Throw()
        {
            log.LogDebug("Halo Throw Begun");
            throwTime = 0;
            isThrowing = true;
            playerHeldBy.throwingObject = true;
            if (base.IsServer)
            {
                itemAnimator.Animator.SetBool("BeingThrown", true);
            }
            if (base.IsOwner)
            {
                if (hitList.Count > 0)
                {
                    hitList.Clear();
                }
                Ray ray = new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, 10f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                {
                    targetPosition = ray.GetPoint(hit.distance - 0.05f);
                }
                else
                {
                    targetPosition = ray.GetPoint(10f);
                }
                SyncThrowDestinationServerRpc(targetPosition);
                StopDripServerRpc();
            }
        }
        public void ThrowEnd()
        {
            parentComponent.transform.localPosition = Vector3.zero;
            this.transform.localPosition = itemProperties.positionOffset;
            playerHeldBy.throwingObject = false;
            if (playerHeldBy != null)
            {
                this.transform.position = playerHeldBy.localItemHolder.transform.position;
            }
            isThrowing = false;
            if (base.IsServer)
            {
                itemAnimator.Animator.SetBool("BeingThrown", false);
            }
            throwAudio.Stop();
            if (isExhausted == 0)
            {
                StartDrip();
            }
            else
            {
                spinParticle.gameObject.SetActive(false);
            }
            log.LogDebug("Halo Throw Ended");
        } 
        public override void DiscardItem()
        {
            base.DiscardItem();
            if (base.IsServer)
            {
                itemAnimator.Animator.SetBool("BeingHeld", false);
            }
        }
        public override void PocketItem()
        {
            base.PocketItem();
            if (isExhausted == 0 && base.IsOwner)
            {
                StopDripServerRpc();
            }
        }
        public IEnumerator ExhaustCoroutine()
        {
            this.GetComponent<AudioSource>().clip = breakSound;
            this.GetComponent<AudioSource>().Play();
            this.GetComponentInChildren<MeshRenderer>().material.color = new Color(0.1f, 0.1f, 0.1f);
            if (base.IsOwner)
            {
                ThrowEndServerRpc();
                StopDripServerRpc();
            }
            yield return new WaitForSeconds(1f);
            log.LogDebug("Halo Fully Exhausted");
            isExhausted = 1;
        }
        public void StartDrip()
        {
            if (isExhausted == 0)
            {
                for (int i = 0; i < dripParticles.Length; i++)
                {
                    dripParticles[i].gameObject.SetActive(true);
                    dripParticles[i].Play();
                }
            }
            spinParticle.gameObject.SetActive(false);
        }
        public void StopDrip()
        {
            for (int i = 0; i < dripParticles.Length; i++)
            {
                dripParticles[i].gameObject.SetActive(false);
            }
            if (isExhausted == 0 && itemAnimator.Animator.GetBool("BeingThrown"))
            {
                spinParticle.gameObject.SetActive(true);
                spinParticle.Play();
            }
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
            WalkieTalkie.TransmitOneShotAudio(throwAudio, throwAudio.clip);
            RoundManager.Instance.PlayAudibleNoise(base.transform.position, 25f, 0.75f, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
            playerHeldBy.timeSinceMakingLoudNoise = 0f;
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
        public void ThrowCurveServerRpc(Vector3 position)
        {
            ThrowCurveClientRpc(position);
        }
        [ClientRpc]
        public void ThrowCurveClientRpc(Vector3 position)
        {
            parentComponent.transform.position = position;
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
            exhaustCoroutine = StartCoroutine(ExhaustCoroutine());
        }
        [ServerRpc(RequireOwnership = false)]
        public void StopDripServerRpc()
        {
            StopDripClientRpc();
        }
        [ClientRpc]
        public void StopDripClientRpc()
        {
            StopDrip();
        }
    }
}