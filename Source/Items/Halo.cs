using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LCWildCardMod.Items
{
    public class SmithHalo : PhysicsProp
    {
        BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        internal static List<SmithHalo> allSpawnedHalos = new List<SmithHalo>();
        public ParticleSystem[] dripParticles;
        public ParticleSystem spinParticle;
        public AudioSource spawnMusic;
        public AudioSource throwAudio;
        public AudioClip[] throwClips;
        public AudioClip breakSound;
        public float minPitch;
        public float maxPitch;
        public NetworkAnimator itemAnimator;
        public AnimationCurve throwCurve;
        public Component parentComponent;
        internal PlayerControllerB savedPlayer;
        internal int isExhausted = 0;
        internal bool exhausting = false;
        internal bool isThrowing = false;
        internal float throwTime = 0;
        internal Vector3 handPosition;
        internal Vector3 targetPosition;
        internal Coroutine exhaustCoroutine;
        internal List<IHittable> hitList = new List<IHittable>();
        internal bool resetList = false;
        System.Random random;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            allSpawnedHalos.Add(this);
            random = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            WildCardMod.Instance.KeyBinds.WildCardButton.performed += ThrowButton;
            spinParticle.gameObject.SetActive(false);
            if (!base.IsServer)
            {
                return;
            }
            BeginMusicClientRpc(isExhausted);
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            allSpawnedHalos.Remove(this);
            WildCardMod.Instance.KeyBinds.WildCardButton.performed -= ThrowButton;
            if (exhausting && savedPlayer == GameNetworkManager.Instance.localPlayerController)
            {
                HUDManager.Instance.UpdateHealthUI(savedPlayer.health, false);
            }
        }
        internal void BeginMusic()
        {
            if (isExhausted == 1)
            {
                GetComponentInChildren<MeshRenderer>().material.color = new Color(0.1f, 0.1f, 0.1f);
                spinParticle.gameObject.SetActive(false);
                StopDrip();
                return;
            }
            StartDrip();
            if (!hasBeenHeld)
            {
                spawnMusic.Play();
            }
        }
        internal void ThrowButton(InputAction.CallbackContext throwContext)
        {
            if (playerHeldBy == null)
            {
                return;
            }
            if (!base.IsOwner || playerHeldBy.currentlyHeldObjectServer != this || isExhausted == 1 || isThrowing)
            {
                return;
            }
            if (throwAudio != null && throwClips.Length > 0)
            {
                float pitch = (float)random.Next((int)(minPitch * 100f), (int)(maxPitch * 100f)) / 100f;
                int selectedClip = random.Next(0, throwClips.Length);
                ThrowAudioServerRpc(pitch, selectedClip);
            }
            ThrowServerRpc();
        }
        public override void Update()
        {
            base.Update();
            if (!base.IsOwner)
            {
                return;
            }
            if (!isThrowing)
            {
                parentComponent.transform.localPosition = Vector3.zero;
                return;
            }
            if (playerHeldBy == null)
            {
                ThrowCurveServerRpc(parentComponent.transform.position);
                ThrowEndServerRpc();
                return;
            }
            ThrowCurve();
            if (throwTime >= 1)
            {
                ThrowCurveServerRpc(parentComponent.transform.position);
                ThrowEndServerRpc();
                return;
            }
            RaycastHit[] objectsHit = Physics.SphereCastAll(parentComponent.transform.position, 0.5f, playerHeldBy.gameplayCamera.transform.forward, 0f, 1084754248, QueryTriggerInteraction.Collide);
            if (throwTime >= 0.5f && !resetList)
            {
                IEnumerable<IHittable> hittables = objectsHit.Select((x) => x.transform.GetComponent<IHittable>());
                for (int i = 0; i < hitList.Count; i++)
                {
                    if (hittables.Contains(hitList[i]))
                    {
                        continue;
                    }
                    hitList.RemoveAt(i);
                    i--;
                }
                resetList = true;
            }
            for (int i = 0; i < objectsHit.Length; i++)
            {
                RaycastHit hit = objectsHit[i];
                if (!hit.transform.TryGetComponent(out IHittable hitComponent))
                {
                    continue;
                }
                if (hitList.Contains(hitComponent) || playerHeldBy.transform == hit.transform)
                {
                    continue;
                }
                hitList.Add(hitComponent);
                if (hit.transform.TryGetComponent(out PlayerControllerB player))
                {
                    player.DamagePlayerFromOtherClientServerRpc(10, playerHeldBy.gameplayCamera.transform.forward, (int)playerHeldBy.playerClientId);
                    Log.LogDebug($"Halo Hit {player.playerUsername}");
                }
                else
                {
                    hitComponent.Hit(1, playerHeldBy.gameplayCamera.transform.forward, playerHeldBy, true, 1);
                    EnemyAICollisionDetect enemy = hitComponent as EnemyAICollisionDetect;
                    if (enemy != null)
                    {
                        Log.LogDebug($"Halo Hit {enemy.mainScript.enemyType.enemyName}");
                    }
                    else
                    {
                        Log.LogDebug($"Halo Hit {hitComponent.GetType()}");
                    }
                }
            }
        }
        internal void ThrowCurve()
        {
            handPosition = transform.position;
            parentComponent.transform.position = Vector3.Lerp(handPosition, targetPosition, throwCurve.Evaluate(throwTime));
            throwTime += Mathf.Abs(Time.deltaTime * 0.75f);
            ThrowCurveServerRpc(parentComponent.transform.position);
        }
        public override void EquipItem()
        {
            base.EquipItem();
            parentComponent.transform.localPosition = Vector3.zero;
            transform.localPosition = itemProperties.positionOffset;
            StartDrip();
            spawnMusic.Stop();
            if (!base.IsServer)
            {
                return;
            }
            itemAnimator.Animator.SetBool("BeingHeld", true);
        }
        internal void Throw()
        {
            Log.LogDebug("Halo Throw Begun");
            throwTime = 0;
            isThrowing = true;
            playerHeldBy.throwingObject = true;
            if (base.IsServer)
            {
                itemAnimator.Animator.SetBool("BeingThrown", true);
            }
            if (!base.IsOwner)
            {
                return;
            }
            hitList.Clear();
            Ray ray = new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward);
            targetPosition = ray.GetPoint(10f);
            if (Physics.Raycast(ray, out RaycastHit hit, 10f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                targetPosition = ray.GetPoint(hit.distance - 0.05f);
            }
            SyncThrowDestinationServerRpc(targetPosition);
            StopDripServerRpc();
        }
        internal void ThrowEnd()
        {
            if (base.IsServer)
            {
                itemAnimator.Animator.SetBool("BeingThrown", false);
            }
            parentComponent.transform.localPosition = Vector3.zero;
            transform.localPosition = itemProperties.positionOffset;
            if (playerHeldBy != null)
            {
                playerHeldBy.throwingObject = false;
            }
            isThrowing = false;
            hitList.Clear();
            resetList = false;
            throwAudio.Stop();
            if (isExhausted == 0)
            {
                StartDrip();
            }
            else
            {
                spinParticle.gameObject.SetActive(false);
            }
            Log.LogDebug("Halo Throw Ended");
        } 
        public override void DiscardItem()
        {
            if (base.IsServer)
            {
                itemAnimator.Animator.SetBool("BeingHeld", false);
            }
            if (base.IsOwner && (isThrowing || playerHeldBy.throwingObject))
            {
                ThrowCurveServerRpc(parentComponent.transform.position);
                ThrowEndServerRpc();
            }
            base.DiscardItem();
        }
        public override void PocketItem()
        {
            base.PocketItem();
            if (isExhausted == 1 || !base.IsOwner)
            {
                return;
            }
            StopDripServerRpc();
        }
        internal IEnumerator ExhaustCoroutine()
        {
            exhausting = true;
            AudioSource audio = GetComponent<AudioSource>();
            audio.clip = breakSound;
            audio.Play();
            GetComponentInChildren<MeshRenderer>().material.color = new Color(0.1f, 0.1f, 0.1f);
            if (savedPlayer == GameNetworkManager.Instance.localPlayerController)
            {
                ThrowEndServerRpc();
                StopDripServerRpc();
            }
            yield return null;
            if (savedPlayer == GameNetworkManager.Instance.localPlayerController && !HUDManager.Instance.playerIsCriticallyInjured)
            {
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                HUDManager.Instance.UpdateHealthUI(1);
            }
            yield return new WaitForSeconds(2f);
            Log.LogDebug("Halo Fully Exhausted");
            if (savedPlayer == GameNetworkManager.Instance.localPlayerController)
            {
                for (int i = 2; i <= 10; i++)
                {
                    if (savedPlayer.isPlayerDead || savedPlayer.health < 100)
                    {
                        break;
                    }
                    yield return new WaitForSeconds(0.1f);
                    HUDManager.Instance.UpdateHealthUI(i * 10, false);
                }
            }
            isExhausted = 1;
            exhausting = false;
            exhaustCoroutine = null;
        }
        internal void StartDrip()
        {
            spinParticle.gameObject.SetActive(false);
            if (isExhausted == 1)
            {
                return;
            }
            for (int i = 0; i < dripParticles.Length; i++)
            {
                ParticleSystem particle = dripParticles[i];
                particle.gameObject.SetActive(true);
                particle.Play();
            }
        }
        internal void StopDrip()
        {
            for (int i = 0; i < dripParticles.Length; i++)
            {
                dripParticles[i].gameObject.SetActive(false);
            }
            if (isExhausted == 1 || !itemAnimator.Animator.GetBool("BeingThrown"))
            {
                return;
            }
            spinParticle.gameObject.SetActive(true);
            spinParticle.Play();
        }
        internal void ExhaustLocal(PlayerControllerB player, Vector3 hitVelocity = default)
        {
            if (player != GameNetworkManager.Instance.localPlayerController)
            {
                return;
            }
            player.externalForceAutoFade += hitVelocity;
            if (exhausting)
            {
                return;
            }
            WildCardMod.Instance.Log.LogDebug("Halo exhausting...");
            if (player.criticallyInjured)
            {
                player.MakeCriticallyInjured(false);
            }
            player.health = 100;
            ExhaustHaloServerRpc((int)player.playerClientId);
        }
        public override int GetItemDataToSave()
        {
            return isExhausted;
        }
        public override void LoadItemSaveData(int saveData)
        {
            hasBeenHeld = true;
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
            if (isThrowing || (playerHeldBy != null && playerHeldBy.throwingObject))
            {
                ThrowEnd();
            }
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
        public void ExhaustHaloServerRpc(int id)
        {
            ExhaustHaloClientRpc(id);
        }
        [ClientRpc]
        public void ExhaustHaloClientRpc(int id)
        {
            savedPlayer = StartOfRound.Instance.allPlayerScripts[id];
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