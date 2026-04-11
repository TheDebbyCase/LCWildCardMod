using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class ClauvioMouse : PhysicsProp
    {
        BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        public NetworkAnimator itemAnimator;
        public MeshRenderer meshRenderer;
        public ParticleSystem particleSystem;
        public AnimationCurve sleebCurve;
        public Texture[] particleTextures;
        public Texture[] faceTextures;
        public AudioSource passiveSource;
        public AudioClip[] passiveClips;
        public AudioClip[] squeakClips;
        internal Coroutine agitateCounter;
        internal Coroutine cryingCoroutine;
        internal int stateId;
        internal int agitate;
        internal bool agitating;
        internal bool crying;
        System.Random random;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            random = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            if (!base.IsServer)
            {
                return;
            }
            ChangeStateClientRpc(0);
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            AudioSource audioSource = this.GetComponent<AudioSource>();
            int index = random.Next(0, squeakClips.Length);
            audioSource.PlayOneShot(squeakClips[index], 1f);
            WalkieTalkie.TransmitOneShotAudio(audioSource, squeakClips[index]);
            if (!base.IsServer)
            {
                return;
            }
            agitate--;
            if (agitate == 0 && stateId == 1)
            {
                ChangeStateClientRpc(0);
            }
            agitate = Mathf.Max(agitate, 0);
            useCooldown = 0.5f;
            float multiplier = 1f;
            if (stateId == 1)
            {
                multiplier = Mathf.Max(1f, (float)agitate / 2);
                useCooldown /= multiplier;
            }
            SetCooldownClientRpc(useCooldown);
            itemAnimator.Animator.SetFloat("Intensity", multiplier);
            itemAnimator.SetTrigger("Pet");
        }
        public override void GrabItem()
        {
            base.GrabItem();
            if (!base.IsServer || agitateCounter != null)
            {
                return;
            }
            agitateCounter = StartCoroutine(AgitateCounter());
        }
        public override void PocketItem()
        {
            base.PocketItem();
            particleSystem.Stop();
            particleSystem.Clear();
        }
        public override void EquipItem()
        {
            base.EquipItem();
            if (particleSystem.isPlaying)
            {
                return;
            }
            particleSystem.Play();
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            if (particleSystem.isPlaying)
            {
                return;
            }
            particleSystem.Play();
        }
        internal IEnumerator AgitateCounter()
        {
            agitating = true;
            while (agitate < 10)
            {
                yield return new WaitForSeconds((float)random.Next(45, 90) / 10f);
                WalkieNoiseClientRpc();
                agitate++;
            }
            yield return new WaitUntil(() => !StartOfRound.Instance.inShipPhase && StartOfRound.Instance.currentLevel.planetHasTime);
            Log.LogDebug($"Clauvio Mouse Crying");
            ChangeStateClientRpc(1);
            cryingCoroutine = StartCoroutine(CryingCoroutine());
            agitating = false;
        }
        internal IEnumerator CryingCoroutine()
        {
            crying = true;
            int cryingTime = 0;
            while (stateId == 1 || cryingTime < 30)
            {
                DogNoiseClientRpc();
                WalkieNoiseClientRpc();
                cryingTime++;
                yield return new WaitForSeconds(1f);
            }
            Log.LogDebug($"Clauvio Mouse Sleebing");
            agitateCounter = StartCoroutine(AgitateCounter());
            crying = false;
        }
        internal void ChangeState(int id)
        {
            stateId = id;
            particleSystem.gameObject.GetComponent<ParticleSystemRenderer>().material.mainTexture = particleTextures[stateId];
            meshRenderer.materials[1].mainTexture = faceTextures[stateId];
            passiveSource.Stop();
            passiveSource.clip = passiveClips[stateId];
            passiveSource.volume = 1f;
            if (stateId == 0)
            {
                passiveSource.rolloffMode = AudioRolloffMode.Custom;
                passiveSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, sleebCurve);
                passiveSource.volume /= 10f;
            }
            else
            {
                passiveSource.rolloffMode = AudioRolloffMode.Linear;
            }
            passiveSource.Play();
            WalkieTalkie.TransmitOneShotAudio(passiveSource, passiveSource.clip);
        }
        public override void LoadItemSaveData(int saveData)
        {
            if (!base.IsServer)
            {
                return;
            }
            agitateCounter = StartCoroutine(AgitateCounter());
        }

        [ClientRpc]
        public void ChangeStateClientRpc(int id)
        {
            ChangeState(id);
        }
        [ClientRpc]
        public void DogNoiseClientRpc()
        {
            if (StartOfRound.Instance.currentLevel.planetHasTime)
            {
                RoundManager.Instance.PlayAudibleNoise(base.transform.position, 25f, 1f, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
            }
            if (playerHeldBy == null)
            {
                return;
            }
            playerHeldBy.timeSinceMakingLoudNoise = 0f;
        }
        [ClientRpc]
        public void WalkieNoiseClientRpc()
        {
            WalkieTalkie.TransmitOneShotAudio(passiveSource, passiveSource.clip);
        }
        [ClientRpc]
        public void SetCooldownClientRpc(float value)
        {
            useCooldown = value;
        }
    }
}