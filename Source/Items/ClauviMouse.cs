using LCWildCardMod.Utils;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class ClauviMouse : WildCardProp
    {
        [Space(3f)]
        [Header("ClauviMouse")]
        [Space(3f)]
        [SerializeField]
        private float intensityMultiplier = 0.5f;
        [SerializeField]
        private int maxAgitate = 10;
        [SerializeField]
        private Vector2 agitateMinMax = new Vector2(4.5f, 9f);
        [SerializeField]
        private int maxCryTime = 20;
        [SerializeField]
        private (int, int) minMaxRound;
        private Coroutine agitateCounter = default;
        private int agitate = default;
        private int Agitate
        {
            get
            {
                return agitate;
            }
            set
            {
                if (agitate != value)
                {
                    SetAgitateRpc(value);
                }
                agitate = value;
            }
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            minMaxRound = (Mathf.RoundToInt(agitateMinMax.x * 10f), Mathf.RoundToInt(agitateMinMax.y * 10f) + 1);
            State = 0;
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            float multiplier = 1f;
            if (State == 1)
            {
                multiplier = Mathf.Max(1f, (float)Agitate * intensityMultiplier);
                currentUseCooldown /= multiplier;
            }
            Animator.SetFloat("Intensity", multiplier);
            if (IsOwner)
            {
                Agitate = Mathf.Max(agitate - 1, 0);
                if (Agitate == 0 && State == 1)
                {
                    State = 0;
                }
            }
            base.ItemActivate(used, buttonDown);
        }
        public override void GrabItem()
        {
            base.GrabItem();
            if (!IsServer || agitateCounter != null)
            {
                return;
            }
            agitateCounter = StartCoroutine(AgitateCoroutine());
        }
        public override void PocketItem()
        {
            base.PocketItem();
            Particles["Sleep"].StopAll(true, false);
        }
        public override void EquipItem()
        {
            base.EquipItem();
            if (State == 0)
            {
                Particles["Sleep"].PlayAll(networked: false);
            }
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            Particles["Sleep"].PlayAll(networked: false);
        }
        private IEnumerator AgitateCoroutine()
        {
            while (Agitate < maxAgitate)
            {
                yield return new WaitForSeconds((float)Random.Next(minMaxRound.Item1, minMaxRound.Item2) * 0.1f);
                Agitate++;
            }
            yield return new WaitUntil(() => !StartOfRound.Instance.inShipPhase && StartOfRound.Instance.currentLevel.planetHasTime);
            Log.LogDebug($"Clauvi Mouse Crying");
            State = 1;
            StartCoroutine(CryingCoroutine());
        }
        private IEnumerator CryingCoroutine()
        {
            int cryingTime = 0;
            while (State == 1 || cryingTime < maxCryTime)
            {
                cryingTime++;
                yield return new WaitForSeconds(1f);
            }
            Log.LogDebug($"Clauvi Mouse Sleebing");
            agitateCounter = StartCoroutine(AgitateCoroutine());
        }
        internal override void OnStateChange(int id)
        {
            Particles["Sleep"].SetMaterialTexture(0, 0, id);
            MeshRenderers["Main"].SetMaterialTexture(0, 1, id);
            SelectAudioClips startClips;
            SelectAudioClips endClips;
            if (State == 0)
            {
                startClips = Audio["Sleep"];
                endClips = Audio["Cry"];
            }
            else
            {
                startClips = Audio["Cry"];
                startClips.SetAudibleLoop(true);
                endClips = Audio["Sleep"];
            }
            endClips.SetAudibleLoop(false);
            endClips.Stop(false);
            Animator.SetBool("Sleeping", State == 0);
            if (!IsServer)
            {
                return;
            }
            startClips.PlayRandomClip();
        }
        [Rpc(SendTo.NotMe)]
        private void SetAgitateRpc(int newValue)
        {
            agitate = newValue;
        }
    }
}