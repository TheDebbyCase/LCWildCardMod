using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class Valentine : PhysicsProp
    {
        BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        public ScanNodeProperties scanNode;
        public MeshRenderer meshRenderer;
        public NetworkAnimator itemAnimator;
        public AudioSource beatAudio;
        public Material materialRef;
        public int startingValue = 0;
        internal float intensityValue;
        internal Vector3 lastUpdatePosition;
        internal int standStillAmount = 0;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            StartCoroutine(StartingValueCoroutine());
            materialRef = meshRenderer.material;
            beatAudio.volume /= 2f;
        }
        internal IEnumerator StartingValueCoroutine()
        {
            if (startingValue != 0)
            {
                yield break;
            }
            yield return new WaitUntil(() => scrapValue > 0);
            startingValue = scrapValue;
        }
        public override void Update()
        {
            base.Update();
            if (base.IsOwner && playerHeldBy != null && !isPocketed && StartOfRound.Instance.currentLevel.planetHasTime && !StartOfRound.Instance.inShipPhase && currentUseCooldown <= 0f && standStillAmount < 11)
            {
                ScrapValueServerRpc(scanNode.scrapValue + 1);
                currentUseCooldown = 2.5f;
                intensityValue = Mathf.Lerp(1f, 100f, ((float)scrapValue - (float)startingValue) / ((float)startingValue * 14f));
                SetIntensityServerRpc(intensityValue);
            }
            if (materialRef.GetColor("_EmissionColor") != Color.white * intensityValue)
            {
                materialRef.SetColor("_EmissionColor", Color.white * intensityValue);
            }
        }
        public override void EquipItem()
        {
            base.EquipItem();
            currentUseCooldown = 2.5f;
            lastUpdatePosition = base.transform.position;
            standStillAmount = 0;
            if (!base.IsServer)
            {
                return;
            }
            itemAnimator.Animator.SetBool("isHeld", true);
        }
        public override void PocketItem()
        {
            base.PocketItem();
            standStillAmount = 0;
            if (!base.IsServer)
            {
                return;
            }
            itemAnimator.Animator.SetBool("isHeld", false);
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            standStillAmount = 0;
            if (!base.IsServer)
            {
                return;
            }
            itemAnimator.Animator.SetBool("isHeld", false);
        }
        public void HeartBeat()
        {
            beatAudio.Stop();
            beatAudio.Play();
            if (StartOfRound.Instance.currentLevel.planetHasTime)
            {
                RoundManager.Instance.PlayAudibleNoise(base.transform.position, 25f, 0.25f, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
            }
            WalkieTalkie.TransmitOneShotAudio(beatAudio, beatAudio.clip);
            playerHeldBy.timeSinceMakingLoudNoise = 0f;
            standStillAmount++;
            if ((base.transform.position - lastUpdatePosition).magnitude >= 3f)
            {
                standStillAmount = 0;
            }
            lastUpdatePosition = base.transform.position;
        }
        public override int GetItemDataToSave()
        {
            return startingValue;
        }
        public override void LoadItemSaveData(int saveData)
        {
            startingValue = saveData;
        }
        [ServerRpc(RequireOwnership = false)]
        public void ScrapValueServerRpc(int newValue)
        {
            ScrapValueClientRpc(newValue);
        }
        [ClientRpc]
        public void ScrapValueClientRpc(int newValue)
        {
            SetScrapValue(newValue);
            Log.LogDebug($"Valentine Stand Still Amount: {standStillAmount}, Value: {scrapValue}");
        }
        [ServerRpc(RequireOwnership = false)]
        public void SetIntensityServerRpc(float intensity)
        {
            intensityValue = intensity;
            if (intensityValue > 95f)
            {
                itemAnimator.Animator.SetFloat("beatSpeed", (intensityValue / 20f) - 3.75f);
            }
            else if (intensityValue > 7.5f)
            {
                itemAnimator.Animator.SetFloat("restSpeed", intensityValue / 7.5f);
            }
            SetIntensityClientRpc(intensity);
        }
        [ClientRpc]
        public void SetIntensityClientRpc(float intensity)
        {
            intensityValue = intensity;
            if (intensityValue <= 95f)
            {
                return;
            }
            beatAudio.pitch = (intensityValue / 20f) - 3.75f;
        }
    }
}