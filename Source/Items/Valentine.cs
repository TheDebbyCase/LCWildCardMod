using LCWildCardMod.Utils;
using Unity.Netcode;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class Valentine : WildCardProp
    {
        [Space(3f)]
        [Header("Valentine")]
        [Space(3f)]
        [SerializeField]
        private int valueLoss = 5;
        [SerializeField]
        private int nearMax = 10;
        [SerializeField]
        private float valueMaxMultiplier = 30f;
        [SerializeField]
        private float maxEffectIntensity = 0.5f;
        private int startingValue = 0;
        private float inverseMaxValue = 0f;
        public override void Start()
        {
            base.Start();
            SetStartingValue();
        }
        internal override void OnEnable()
        {
            EventsClass.OnRoundStart += SetStartingValue;
        }
        internal override void OnDisable()
        {
            EventsClass.OnRoundStart -= SetStartingValue;
        }
        private void SetStartingValue()
        {
            if (startingValue == 0)
            {
                startingValue = ScrapValue;
            }
            inverseMaxValue = 1f / (float)Mathf.Max(1, startingValue * valueMaxMultiplier);
        }
        public override void Update()
        {
            base.Update();
            if (!IsOwner || (!isHeld && !isHeldByEnemy) || isPocketed || !StartOfRound.Instance.currentLevel.planetHasTime || StartOfRound.Instance.inShipPhase || Audio["HeartBeat"]?.TimesNearby >= nearMax || currentUseCooldown > 0f)
            {
                return;
            }
            ScrapValue += valueLoss;
            currentUseCooldown = useCooldown;
        }
        public override void EquipItem()
        {
            base.EquipItem();
            if (!IsOwner)
            {
                return;
            }
            currentUseCooldown = useCooldown;
        }
        public void HeartBeat()
        {
            if (!IsOwner)
            {
                return;
            }
            Audio["HeartBeat"].PlayRandomClip();
            float intensity = Mathf.Lerp(0.01f, 1f, ((float)ScrapValue - (float)startingValue) * inverseMaxValue);
            if (intensity <= maxEffectIntensity)
            {
                return;
            }
            SetIntensity(intensity);
        }
        public override int GetItemDataToSave()
        {
            return startingValue;
        }
        public override void LoadItemSaveData(int saveData)
        {
            startingValue = saveData;
        }
        private void SetIntensity(float intensity, bool networked = true)
        {
            float value = (intensity * 2f) - 1f;
            float lerped = Mathf.Lerp(1f, 3f, value);
            Animator.SetFloat("BeatSpeed", lerped);
            Animator.SetFloat("RestSpeed", Mathf.Lerp(1f, 32f, value));
            Audio["HeartBeat"].SetPitch(value + 1f);
            MeshRenderers["Main"].SetColours(new Color(lerped, lerped, lerped, 1f));
            if (!networked)
            {
                return;
            }
            SetIntensityRpc(intensity);
        }
        [Rpc(SendTo.NotMe)]
        private void SetIntensityRpc(float intensity)
        {
            SetIntensity(intensity, false);
        }
    }
}