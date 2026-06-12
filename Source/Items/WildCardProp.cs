using GameNetcodeStuff;
using LCWildCardMod.Utils;
using LethalLib.Extras;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LCWildCardMod.Items
{
    public class WildCardProp : PhysicsProp, IWildCardBase
    {
        internal static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        string IWildCardBase.Name
        {
            get
            {
                if (ScanNode != null)
                {
                    return ScanNode.headerText;
                }
                if (itemProperties != null)
                {
                    return itemProperties.itemName;
                }
                return gameObject.name.Replace("(Clone)", string.Empty);
            }
            set
            {
                if (ScanNode != null)
                {
                    ScanNode.headerText = value;
                }
                if (!Properties.TryGetValue(value, out Item newProperties))
                {
                    newProperties = Properties["Default"].Clone();
                    newProperties.itemName = value;
                    Properties.Add(value, newProperties);
                }
                itemProperties = newProperties;
            }
        }
        Transform IWildCardBase.Transform => transform;
        ListDict<string, SelectAudioClips> IWildCardBase.Audio => Audio;
        ListDict<string, SelectAnimationParameters> IWildCardBase.Animations => Animations;
        ListDict<string, SelectParticles> IWildCardBase.Particles => Particles;
        ListDict<string, SelectRenderers> IWildCardBase.MeshRenderers => MeshRenderers;
        ListDict<string, SelectModelVariants> IWildCardBase.ModelVariants => ModelVariants;
        ListDict<string, SelectLights> IWildCardBase.Lights => Lights;
        int IWildCardBase.State
        {
            get
            {
                return State;
            }
            set
            {
                State = value;
            }
        }
        AnimationHandler IWildCardBase.Animator
        {
            get
            {
                return Animator;
            }
            set
            {
                animator = value;
            }
        }
        bool IWildCardBase.NetworkAnimations
        {
            get
            {
                return networkAnimations;
            }
            set
            {
                NetworkAnimations = value;
            }
        }
        System.Random IWildCardBase.Random => Random;
        [Space(3f)]
        [Header("WildCardProp")]
        [Space(3f)]
        [SerializeField]
        private List<SelectablePair<SelectAudioClips>> audioClips = null;
        [SerializeField]
        private List<SelectablePair<SelectAnimationParameters>> animations = null;
        [SerializeField]
        private List<SelectablePair<SelectParticles>> particles = null;
        [SerializeField]
        private List<SelectablePair<SelectRenderers>> meshRenderers = null;
        [SerializeField]
        private List<SelectablePair<SelectModelVariants>> modelVariants = null;
        [SerializeField]
        private List<SelectablePair<SelectLights>> lights = null;
        [SerializeField]
        private bool networkAnimations = false;
        [SerializeField]
        private RepeatingAction enemyUse = null;
        [SerializeField]
        private RepeatingAction enemyActivate = null;
        [HideInInspector]
        [SerializeField]
        private ListDict<string, SelectAudioClips> audioDict = null;
        [HideInInspector]
        [SerializeField]
        private ListDict<string, SelectAnimationParameters> animDict = null;
        [HideInInspector]
        [SerializeField]
        private ListDict<string, SelectParticles> particleDict = null;
        [HideInInspector]
        [SerializeField]
        private ListDict<string, SelectRenderers> renderDict = null;
        [HideInInspector]
        [SerializeField]
        private ListDict<string, SelectModelVariants> variantDict = null;
        [HideInInspector]
        [SerializeField]
        private ListDict<string, SelectLights> lightDict = null;
        [HideInInspector]
        [SerializeReference]
        private AnimationHandler animator = null;
        [HideInInspector]
        [SerializeReference]
        private bool initialized = false;
        private WildCardItem item = null;
        private Dictionary<string, Item> properties = null;
        private int state = -1;
        private PlayerControllerB lastPlayer = null;
        private ScanNodeProperties scanNode = null;
        private bool buttonDown = false;
        private System.Random random = null;
        internal EnemyAI heldByEnemy = null;
        private EnemyAI lastEnemy = null;
        internal IWildCardBase Base
        {
            get
            {
                return this;
            }
        }
        internal ListDict<string, SelectAudioClips> Audio
        {
            get
            {
                audioDict ??= new ListDict<string, SelectAudioClips>();
                return audioDict;
            }
        }
        internal ListDict<string, SelectAnimationParameters> Animations
        {
            get
            {
                animDict ??= new ListDict<string, SelectAnimationParameters>();
                return animDict;
            }
        }
        internal ListDict<string, SelectParticles> Particles
        {
            get
            {
                particleDict ??= new ListDict<string, SelectParticles>();
                return particleDict;
            }
        }
        internal ListDict<string, SelectRenderers> MeshRenderers
        {
            get
            {
                renderDict ??= new ListDict<string, SelectRenderers>();
                return renderDict;
            }
        }
        internal ListDict<string, SelectModelVariants> ModelVariants
        {
            get
            {
                variantDict ??= new ListDict<string, SelectModelVariants>();
                return variantDict;
            }
        }
        internal ListDict<string, SelectLights> Lights
        {
            get
            {
                lightDict ??= new ListDict<string, SelectLights>();
                return lightDict;
            }
        }
        internal bool NetworkAnimations
        {
            get
            {
                if (animator == null)
                {
                    return false;
                }
                return animator.IsNetworked;
            }
            set
            {
                if (animator == null)
                {
                    return;
                }
                if (value == animator.IsNetworked)
                {
                    return;
                }
                networkAnimations = value;
                if (animator == null && animator.IsNetworked == networkAnimations)
                {
                    return;
                }
                animator.SetNetworkEnabled(networkAnimations);
            }
        }
        internal WildCardItem OriginalItem
        {
            get
            {
                item ??= Properties["Default"] as WildCardItem;
                return item;
            }
        }
        internal WildCardItem CurrentItem
        {
            get
            {
                itemProperties ??= OriginalItem;
                return itemProperties as WildCardItem;
            }
        }
        internal int ScrapValue
        {
            get
            {
                return scrapValue;
            }
            set
            {
                if (value == scrapValue)
                {
                    return;
                }
                else if (value < 0)
                {
                    value = 0;
                }
                SetScrapValue(value);
                SetValueRpc(value);
            }
        }
        internal Dictionary<string, Item> Properties
        {
            get
            {
                properties ??= new Dictionary<string, Item>();
                properties.TryAdd("Default", itemProperties);
                return properties;
            }
        }
        internal int State
        {
            get
            {
                return state;
            }
            set
            {
                if (state == value)
                {
                    return;
                }
                state = value;
                OnStateChange(value);
                SetStateRpc(value);
            }
        }
        internal PlayerControllerB LastPlayerHeldBy
        {
            get
            {
                if (playerHeldBy != null)
                {
                    lastPlayer = playerHeldBy;
                }
                return lastPlayer;
            }
        }
        internal EnemyAI LastEnemyHeldBy
        {
            get
            {
                if (heldByEnemy != null)
                {
                    lastEnemy = heldByEnemy;
                }
                return lastEnemy;
            }
        }
        internal ScanNodeProperties ScanNode
        {
            get
            {
                if (scanNode == null)
                {
                    scanNode = GetComponentInChildren<ScanNodeProperties>();
                }
                return scanNode;
            }
        }
        internal AnimationHandler Animator
        {
            get
            {
                if (animator?.OriginalNetworked == null)
                {
                    animator = null;
                }
                return animator;
            }
            set
            {
                animator = value;
            }
        }
        internal System.Random Random
        {
            get
            {
                if (RoundManager.Instance != null)
                {
                    random ??= new System.Random(StartOfRound.Instance.randomMapSeed + IWildCardBase.totalBases);
                }
                return random;
            }
        }
        internal bool ButtonDown => buttonDown;
        internal virtual void InitializePrefab()
        {
            AudioSource baseAudio = gameObject.AddComponent<AudioSource>();
            baseAudio.spatialBlend = 1f;
            baseAudio.maxDistance = 25f;
            baseAudio.rolloffMode = AudioRolloffMode.Custom;
            baseAudio.SetCustomCurve(AudioSourceCurveType.CustomRolloff, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));
        }
        internal virtual void Awake()
        {
            GetComponent<AudioSource>().outputAudioMixerGroup = WildUtils.DiageticMasterGroup;
            IWildCardBase.Awake(this);
            enemyUse.SetRandom(Random);
            enemyUse.SetAction(WildCardUse);
            enemyActivate.SetRandom(Random);
            enemyActivate.SetAction(EnemyItemActivate);
            enemyActivate.LoopWaitsFor = () => currentUseCooldown <= 0f;
        }
        public override void Start()
        {
            base.Start();
            if (!WildCardMod.ModConfig.Debug)
            {
                return;
            }
            Log.LogDebug($"Spawning {(this as IWildCardBase).Name}");
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsServer)
            {
                return;
            }
            if (WildCardMod.ModConfig.Debug)
            {
                SetValueRpc(ScrapValue);
            }
        }
        protected override void OnNetworkPostSpawn()
        {
            base.OnNetworkPostSpawn();
            IWildCardBase.OnNetworkPostSpawn(this, hasBeenHeld);
        }
        internal virtual void OnEnable()
        {
            if (!CurrentItem.usesButton)
            {
                return;
            }
            WildCardMod.KeyBinds.WildCardButton.performed += WildCardUse;
        }
        internal virtual void OnDisable()
        {
            WildCardMod.KeyBinds.WildCardButton.performed -= WildCardUse;
        }
        internal virtual void WildCardUse(InputAction.CallbackContext useContext)
        {
            WildCardUse();
        }
        internal virtual void WildCardUse()
        {
            
        }
        internal virtual void OnStateChange(int newState)
        {
            
        }
        public override void Update()
        {
            base.Update();
            if (!IsServer)
            {
                return;
            }
            IWildCardBase.Update(this);
            if (!isHeldByEnemy)
            {
                return;
            }
            WildCardItem item = CurrentItem;
            if (item.usesButton && item.enemyCanUseButton)
            {
                enemyUse.Tick();
            }
            if (!item.enemyCanActivate)
            {
                return;
            }
            enemyActivate.Tick();
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            this.buttonDown = buttonDown;
            if (IsOwner)
            {
                Audio["Activate"]?.PlayRandomClip();
            }
            if (NetworkAnimations && !IsServer)
            {
                return;
            }
            Animator?.Trigger("Activate");
        }
        internal virtual void ItemActivateFromEnemy(bool used, bool buttonDown = true)
        {
            ItemActivate(used, buttonDown);
        }
        private void EnemyItemActivate()
        {
            if (!IsServer)
            {
                return;
            }
            WildCardItem item = CurrentItem;
            if (RequireCooldown() || !UseItemBatteries(!item.holdButtonUse, !buttonDown))
            {
                return;
            }
            ItemActivateFromEnemy(isBeingUsed, !buttonDown);
            EnemyItemActivateRpc(isBeingUsed, !buttonDown);
        }
        public override void EquipItem()
        {
            base.EquipItem();
            Animator?.SetBool("BeingHeld", true);
            for (int i = 0; i < Lights.Count; i++)
            {
                SelectLights lights = Lights[i];
                if (!lights.disableWhilePocketed)
                {
                    continue;
                }
                lights.EnableAll(false);
            }
        }
        public override void PocketItem()
        {
            base.PocketItem();
            for (int i = 0; i < Lights.Count; i++)
            {
                SelectLights lights = Lights[i];
                if (!lights.disableWhilePocketed)
                {
                    continue;
                }
                lights.DisableAll(false);
            }
        }
        public override void GrabItem()
        {
            base.GrabItem();
            lastPlayer = playerHeldBy;
            GrabFromAny();
        }
        public override void DiscardItem()
        {
            lastPlayer = playerHeldBy;
            DiscardFromAny();
            base.DiscardItem();
        }
        public override void GrabItemFromEnemy(EnemyAI enemy)
        {
            base.GrabItemFromEnemy(enemy);
            heldByEnemy = enemy;
            lastEnemy = enemy;
            enemyUse.ResetTimer();
            enemyActivate.ResetTimer();
            if (IsServer || !NetworkAnimations)
            {
                Animator?.SetBool("BeingHeld", true);
            }
            GrabFromAny(false, enemy);
        }
        public override void DiscardItemFromEnemy()
        {
            isHeldByEnemy = false;
            lastEnemy = heldByEnemy;
            DiscardFromAny(false, heldByEnemy);
            heldByEnemy = null;
            if (grabbableToEnemies && !deactivated && HoarderBugAI.HoarderBugItems.Count > 0)
            {
                HoarderBugItem bugItem = HoarderBugAI.HoarderBugItems.FirstOrDefault((x) => x.itemGrabbableObject == this);
                if (bugItem != null && Vector3.Distance(bugItem.itemNestPosition, transform.position) > 7.5f)
                {
                    HoarderBugAI.grabbableObjectsInMap.Add(gameObject);
                }
            }
            base.DiscardItemFromEnemy();
        }
        internal virtual void GrabFromAny(bool fromPlayer = true, EnemyAI enemy = null)
        {
            isHeldByEnemy = !fromPlayer;
            Audio["SpawnMusic"]?.Stop(false);
        }
        internal virtual void DiscardFromAny(bool fromPlayer = true, EnemyAI enemy = null)
        {
            if (!IsServer && NetworkAnimations)
            {
                return;
            }
            Animator?.SetBool("BeingHeld", false);
        }
        public override void LoadItemSaveData(int saveData)
        {
            hasBeenHeld = true;
        }
        internal virtual void DamagePlayerLocal(int damage, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown, int animation = 0, Vector3 force = default)
        {
            GameNetworkManager.Instance.localPlayerController.DamagePlayer(damage, true, true, causeOfDeath, animation, false, force);
        }
        internal void EnemyForceDropItem(bool hide = false, bool networked = true)
        {
            if (networked)
            {
                if (LastEnemyHeldBy is HoarderBugAI bug && bug.heldItem.itemGrabbableObject == this)
                {
                    bug.DropItemAndCallDropRPC(GetComponent<NetworkObject>(), false);
                    return;
                }
                if (LastEnemyHeldBy is BaboonBirdAI baboon && baboon.heldScrap == this)
                {
                    baboon.DropHeldItemAndSync(true);
                    return;
                }
            }
            parentObject = null;
            transform.SetParent(StartOfRound.Instance.propsContainer, worldPositionStays: true);
            EnablePhysics(!hide);
            if (hide)
            {
                EnableItemMeshes(false);
                grabbable = false;
            }
            else
            {
                fallTime = 0f;
                startFallingPosition = transform.parent.InverseTransformPoint(transform.position);
                targetFloorPosition = transform.parent.InverseTransformPoint(targetFloorPosition);
                floorYRot = -1;
            }
            DiscardItemFromEnemy();
            if (!networked)
            {
                return;
            }
            EnemyForceDropItemRpc(hide);
        }
        [Rpc(SendTo.NotMe)]
        private void EnemyItemActivateRpc(bool used, bool buttonDown)
        {
            ItemActivateFromEnemy(used, buttonDown);
        }
        [Rpc(SendTo.NotMe)]
        private void EnemyForceDropItemRpc(bool hide)
        {
            EnemyForceDropItem(hide, false);
        }
        [Rpc(SendTo.SpecifiedInParams)]
        private void DamagePlayerRpc(int damage, int causeOfDeath = 0, int animation = 0, Vector3 force = default, RpcParams rpcParams = default)
        {
            DamagePlayerLocal(damage, (CauseOfDeath)causeOfDeath, animation, force);
        }
        [Rpc(SendTo.NotMe)]
        private void PlayClipRpc(int id, int index, bool oneShot, float volume, float pitch)
        {
            SelectAudioClips clips = Audio[id];
            clips?.PlayClip(index, oneShot, false, volume, pitch);
        }
        [Rpc(SendTo.NotMe)]
        private void RepeatClipRpc(int id, bool oneShot)
        {
            SelectAudioClips clips = Audio[id];
            clips?.RepeatClip(oneShot, false);
        }
        [Rpc(SendTo.NotMe)]
        private void StopAudioRpc(int id)
        {
            SelectAudioClips clips = Audio[id];
            clips?.Stop(false);
        }
        [Rpc(SendTo.NotMe)]
        private void PauseAudioRpc(int id, bool pause)
        {
            SelectAudioClips clips = Audio[id];
            clips?.SetPaused(pause);
        }
        [Rpc(SendTo.NotMe)]
        private void MuteAudioRpc(int id, bool oneShot)
        {
            SelectAudioClips clips = Audio[id];
            clips?.SetMute(oneShot, false);
        }
        [Rpc(SendTo.NotMe)]
        private void DogNoiseRpc(int id, float volume, float pitch)
        {
            Audio[id]?.DogNoise(volume, pitch, false);
        }
        [Rpc(SendTo.NotMe)]
        private void PlayParticlesRpc(int id, bool restart, int index = -1)
        {
            SelectParticles particle = Particles[id];
            if (index < 0)
            {
                particle?.PlayAll(restart, false);
                return;
            }
            particle?.Play(index, restart, false);
        }
        [Rpc(SendTo.NotMe)]
        private void StopParticlesRpc(int id, bool clear, int index = -1)
        {
            SelectParticles particle = Particles[id];
            if (index < 0)
            {
                particle?.StopAll(clear, false);
                return;
            }
            particle?.Stop(index, clear, false);
        }
        [Rpc(SendTo.NotMe)]
        private void PauseParticlesRpc(int id, int index = -1)
        {
            SelectParticles particle = Particles[id];
            if (index < 0)
            {
                particle?.PauseAll(false);
                return;
            }
            particle?.Pause(index, false);
        }
        [Rpc(SendTo.NotMe)]
        private void ClearParticlesRpc(int id, int index = -1)
        {
            SelectParticles particle = Particles[id];
            if (index < 0)
            {
                particle?.ClearAll(false);
                return;
            }
            particle?.Clear(index, false);
        }
        [Rpc(SendTo.NotMe)]
        private void EmitParticlesRpc(int id, int count, int index = -1)
        {
            SelectParticles particle = Particles[id];
            if (index < 0)
            {
                particle?.EmitAll(count, false);
                return;
            }
            particle?.Emit(index, count, false);
        }
        [Rpc(SendTo.NotMe)]
        private void SetStateRpc(int newState)
        {
            state = newState;
            OnStateChange(newState);
        }
        [Rpc(SendTo.NotMe)]
        private void SetNetworkAnimationsRpc(bool value)
        {
            networkAnimations = value;
            if (animator == null || animator.IsNetworked == networkAnimations)
            {
                return;
            }
            animator.SetNetworkEnabled(networkAnimations);
        }
        [Rpc(SendTo.NotMe)]
        private void SetValueRpc(int value)
        {
            SetScrapValue(value);
        }
        [Rpc(SendTo.NotMe)]
        private void SetParameterRpc(int hash, float value = 0f)
        {
            animator?.SetParameter(hash, value);
        }
        [Rpc(SendTo.NotMe)]
        private void SetVariantRpc(int id, int variantIndex)
        {
            SelectModelVariants variants = ModelVariants[id];
            variants.Switch(variantIndex, false);
        }
        [Rpc(SendTo.NotMe)]
        private void SetLightsEnabledRpc(int id, bool enable, int index = -1)
        {
            SelectLights lights = Lights[id];
            if (index < 0)
            {
                if (enable)
                {
                    lights.EnableAll(false);
                    return;
                }
                lights.DisableAll(false);
                return;
            }
            if (enable)
            {
                lights.Enable(index, false);
                return;
            }
            lights.Disable(index, false);
        }
        void IWildCardBase.Initialize()
        {
            if (initialized)
            {
                return;
            }
            InitializePrefab();
            IWildCardBase.Initialize(this, ref audioClips, ref animations, ref particles, ref meshRenderers, ref modelVariants, ref lights, out audioDict, out animDict, out particleDict, out renderDict, out variantDict, out lightDict);
            initialized = true;
        }
        void IWildCardBase.PlayClipNetworked(int id, int index, bool oneShot, float volume, float pitch)
        {
            PlayClipRpc(id, index, oneShot, volume, pitch);
        }
        void IWildCardBase.RepeatClipNetworked(int id, bool oneShot)
        {
            RepeatClipRpc(id, oneShot);
        }
        void IWildCardBase.StopAudioNetworked(int id)
        {
            StopAudioRpc(id);
        }
        void IWildCardBase.PauseAudioNetworked(int id, bool pause)
        {
            PauseAudioRpc(id, pause);
        }
        void IWildCardBase.MuteAudioNetworked(int id, bool oneShot)
        {
            MuteAudioRpc(id, oneShot);
        }
        void IWildCardBase.DogNoiseNetworked(int id, float volume, float pitch)
        {
            DogNoiseRpc(id, volume, pitch);
        }
        void IWildCardBase.PlayParticlesNetworked(int id, bool restart, int index)
        {
            PlayParticlesRpc(id, restart, index);
        }
        void IWildCardBase.StopParticlesNetworked(int id, bool clear, int index)
        {
            StopParticlesRpc(id, clear, index);
        }
        void IWildCardBase.PauseParticlesNetworked(int id, int index)
        {
            PauseParticlesRpc(id, index);
        }
        void IWildCardBase.ClearParticlesNetworked(int id, int index)
        {
            ClearParticlesRpc(id, index);
        }
        void IWildCardBase.EmitParticlesNetworked(int id, int count, int index)
        {
            EmitParticlesRpc(id, count, index);
        }
        void IWildCardBase.SetStateNetworked(int newState)
        {
            SetStateRpc(newState);
        }
        void IWildCardBase.SetNetworkAnimationsNetworked(bool value)
        {
            SetNetworkAnimationsRpc(value);
        }
        void IWildCardBase.SetParameterNetworked(int hash, float value)
        {
            SetParameterRpc(hash, value);
        }
        void IWildCardBase.SetVariantNetworked(int id, int variantIndex)
        {
            SetVariantRpc(id, variantIndex);
        }
        void IWildCardBase.SetLightEnabledNetworked(int id, bool enable, int index)
        {
            SetLightsEnabledRpc(id, enable, index);
        }
        void IWildCardBase.DamagePlayer(PlayerControllerB player, int damage, CauseOfDeath causeOfDeath, int animation, Vector3 force)
        {
            DamagePlayerRpc(damage, (int)causeOfDeath, animation, force, player.GetRPCTarget());
        }
        bool IWildCardBase.HitOrDamage(IHittable hittable, int playerDamage, int hitForce, Vector3 hitDirection, PlayerControllerB playerWhoHit, bool playHitSFX, int hitID, CauseOfDeath playerDeathCause, float playerForceMultiplier)
        {
            return IWildCardBase.HitOrDamage(this, hittable, playerDamage, hitForce, hitDirection, playerWhoHit, playHitSFX, hitID, playerDeathCause, playerForceMultiplier);
        }
    }
}