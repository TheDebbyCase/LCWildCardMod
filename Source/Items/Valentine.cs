using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class Valentine : PhysicsProp
    {
        public ScanNodeProperties scanNode;
        public MeshRenderer meshRenderer;
        public NetworkAnimator itemAnimator;
        public AudioSource beatAudio;
        public Material materialRef;
        public int startingValue = 0;
        public float intensityValue;
        public Vector3 lastUpdatePosition;
        public int standStillAmount = 0;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (startingValue == 0)
            {
                startingValue = scrapValue;
            }
            materialRef = meshRenderer.material;
        }
        public override void Update()
        {
            base.Update();
            if (playerHeldBy != null && !isPocketed && base.IsOwner && StartOfRound.Instance.currentLevel.planetHasTime && !StartOfRound.Instance.inShipPhase && currentUseCooldown <= 0f && standStillAmount < 3)
            {
                ScrapValueServerRpc(scanNode.scrapValue + 1);
                currentUseCooldown = 2.5f;
                intensityValue = Mathf.Lerp(1f, 100f, ((float)scrapValue - (float)startingValue) / ((float)startingValue * 14f));
                SetIntensityServerRpc(intensityValue);
                if (intensityValue >= 75f)
                {
                    itemAnimator.Animator.SetFloat("beatSpeed", Mathf.Max(1, (intensityValue / 20f) - 3.75f));
                    beatAudio.pitch = Mathf.Max(1, (intensityValue / 20f) - 3.75f);
                }
                itemAnimator.Animator.SetFloat("restSpeed", Mathf.Max(1, intensityValue / 7.5f));
            }
            else if ((base.transform.position - lastUpdatePosition).magnitude >= 3f && playerHeldBy != null && !isPocketed && currentUseCooldown <= 0f && standStillAmount != 0)
            {
                standStillAmount = 0;
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
            itemAnimator.Animator.SetBool("isHeld", true);
        }
        public override void PocketItem()
        {
            base.PocketItem();
            standStillAmount = 0;
            itemAnimator.Animator.SetBool("isHeld", false);
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            standStillAmount = 0;
            itemAnimator.Animator.SetBool("isHeld", false);
        }
        public void HeartBeat()
        {
            beatAudio.Stop();
            beatAudio.Play();
            RoundManager.Instance.PlayAudibleNoise(base.transform.position, 25f, 0.25f, standStillAmount, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
            playerHeldBy.timeSinceMakingLoudNoise = 0f;
            if ((base.transform.position - lastUpdatePosition).magnitude < 3f)
            {
                standStillAmount++;
            }
            else
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
        }
        [ServerRpc(RequireOwnership = false)]
        public void SetIntensityServerRpc(float intensity)
        {
            SetIntensityClientRpc(intensity);
        }
        [ClientRpc]
        public void SetIntensityClientRpc(float intensity)
        {
            intensityValue = intensity;
        }
    }
}