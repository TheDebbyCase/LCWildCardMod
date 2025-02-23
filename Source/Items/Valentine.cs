using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class Valentine : PhysicsProp
    {
        readonly BepInEx.Logging.ManualLogSource log = WildCardMod.Log;
        public ScanNodeProperties scanNode;
        public MeshRenderer meshRenderer;
        public NetworkAnimator itemAnimator;
        public AudioSource beatAudio;
        public Material materialRef;
        public int startingValue = 0;
        public float intensityValue;
        public Vector3 lastUpdatePosition;
        public int standStillAmount = 0;
        public Coroutine startingValueCoroutine;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!StartOfRound.Instance.inShipPhase)
            {
                startingValueCoroutine = StartCoroutine(StartingValueCoroutine());
            }
            materialRef = meshRenderer.material;
        }
        public IEnumerator StartingValueCoroutine()
        {
            yield return new WaitUntil(() => RoundManager.Instance.dungeonFinishedGeneratingForAllPlayers);
            if (startingValue == 0)
            {
                startingValue = scrapValue;
            }
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
            if (base.IsServer)
            {
                itemAnimator.Animator.SetBool("isHeld", true);
            }
        }
        public override void PocketItem()
        {
            base.PocketItem();
            standStillAmount = 0;
            if (base.IsServer)
            {
                itemAnimator.Animator.SetBool("isHeld", false);
            }
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            standStillAmount = 0;
            if (base.IsServer)
            {
                itemAnimator.Animator.SetBool("isHeld", false);
            }
        }
        public void HeartBeat()
        {
            beatAudio.Stop();
            beatAudio.Play();
            RoundManager.Instance.PlayAudibleNoise(base.transform.position, 25f, 0.25f, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
            playerHeldBy.timeSinceMakingLoudNoise = 0f;
            if ((base.transform.position - lastUpdatePosition).magnitude < 3f)
            {
                standStillAmount++;
            }
            else
            {
                standStillAmount = 0;
            }
            log.LogDebug($"Valentine Stand Still Amount: {standStillAmount}, Value: {scrapValue}");
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
            if (intensityValue > 95f)
            {
                beatAudio.pitch = (intensityValue / 20f) - 3.75f;
            }
        }
    }
}