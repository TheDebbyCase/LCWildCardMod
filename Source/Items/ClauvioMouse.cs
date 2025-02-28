using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class ClauvioMouse : PhysicsProp
    {
        readonly BepInEx.Logging.ManualLogSource log = WildCardMod.Log;
        public NetworkAnimator itemAnimator;
        public MeshRenderer meshRenderer;
        public ParticleSystem particleSystem;
        public AnimationCurve sleebCurve;
        public Texture[] particleTextures;
        public Texture[] faceTextures;
        public AudioSource passiveSource;
        public AudioClip[] passiveClips;
        public AudioClip[] squeakClips;
        public Coroutine agitateCounter;
        public Coroutine cryingCoroutine;
        public int stateId;
        public int agitate;
        public bool agitating;
        public bool crying;
        private System.Random random;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            random = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            if (base.IsServer)
            {
                ChangeStateClientRpc(0);
            }
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (base.IsServer)
            {
                agitate--;
                if (agitate == 0 && stateId == 1)
                {
                    ChangeStateClientRpc(0);
                }
                else if (agitate < 0)
                {
                    agitate = 0;
                }
                if (stateId == 1)
                {
                    float multiplier = Mathf.Max(1f, (float)agitate / 2);
                    useCooldown = 0.5f / multiplier;
                    itemAnimator.Animator.SetFloat("Intensity", multiplier);
                }
                else
                {
                    useCooldown = 0.5f;
                    itemAnimator.Animator.SetFloat("Intensity", 1);
                }
                itemAnimator.SetTrigger("Pet");
            }
            AudioSource audioSource = this.GetComponent<AudioSource>();
            int index = random.Next(0, squeakClips.Length);
            audioSource.PlayOneShot(squeakClips[index], 1f);
            WalkieTalkie.TransmitOneShotAudio(audioSource, squeakClips[index]);
        }
        public override void GrabItem()
        {
            base.GrabItem();
            if (base.IsServer && agitateCounter == null)
            {
                agitateCounter = StartCoroutine(AgitateCounter());
            }
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
            if (!particleSystem.isPlaying)
            {
                particleSystem.Play();
            }
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            if (!particleSystem.isPlaying)
            {
                particleSystem.Play();
            }
        }
        public IEnumerator AgitateCounter()
        {
            agitating = true;
            while (agitate < 10)
            {
                yield return new WaitForSeconds((float)random.Next(5, 50) / 10f);
                WalkieNoiseClientRpc();
                agitate++;
            }
            yield return new WaitUntil(() => !StartOfRound.Instance.inShipPhase);
            log.LogDebug($"Clauvio Mouse Crying");
            ChangeStateClientRpc(1);
            cryingCoroutine = StartCoroutine(CryingCoroutine());
            agitating = false;
        }
        public IEnumerator CryingCoroutine()
        {
            crying = true;
            int cryingTime = 0;
            while (stateId == 1)
            {
                DogNoiseClientRpc();
                WalkieNoiseClientRpc();
                cryingTime++;
                yield return new WaitForSeconds(1f);
            }
            log.LogDebug($"Clauvio Mouse Sleebing");
            agitateCounter = StartCoroutine(AgitateCounter());
            crying = false;
        }
        public void ChangeState(int id)
        {
            stateId = id;
            particleSystem.gameObject.GetComponent<ParticleSystemRenderer>().material.mainTexture = particleTextures[stateId];
            meshRenderer.materials[1].mainTexture = faceTextures[stateId];
            passiveSource.Stop();
            passiveSource.clip = passiveClips[stateId];
            if (stateId == 0)
            {
                passiveSource.rolloffMode = AudioRolloffMode.Custom;
                passiveSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, sleebCurve);
                passiveSource.volume = 0.1f;
            }
            else if (stateId == 1)
            {
                passiveSource.rolloffMode = AudioRolloffMode.Linear;
                passiveSource.volume = 1f;
            }
            passiveSource.Play();
            WalkieTalkie.TransmitOneShotAudio(passiveSource, passiveSource.clip);
        }
        public override void LoadItemSaveData(int saveData)
        {
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
            RoundManager.Instance.PlayAudibleNoise(base.transform.position, 25f, 1f, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
            if (playerHeldBy != null)
            {
                playerHeldBy.timeSinceMakingLoudNoise = 0f;
            }
        }
        [ClientRpc]
        public void WalkieNoiseClientRpc()
        {
            WalkieTalkie.TransmitOneShotAudio(passiveSource, passiveSource.clip);
        }
    }
}