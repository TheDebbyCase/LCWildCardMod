using GameNetcodeStuff;
using LCWildCardMod.Utils;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace LCWildCardMod.MapObjects
{
    public class WildCardMapObject : NetworkBehaviour, IWildCardBase
    {
        internal static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        string IWildCardBase.Name
        {
            get
            {
                gameObject.name = gameObject.name.Replace("(Clone)", string.Empty);
                return gameObject.name;
            }
            set
            {
                ScanNodeProperties scanNode = GetComponentInChildren<ScanNodeProperties>();
                if (scanNode != null)
                {
                    scanNode.headerText = value;
                }
                gameObject.name = value;
            }
        }
        bool IWildCardBase.IsServer => IsServer;
        bool IWildCardBase.IsOwner => IsOwner;
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
                Animator = value;
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
        [Header("WildCardMapObject")]
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
        internal int basePlayerDamage = 15;
        [SerializeField]
        internal int baseHitDamage = 1;
        [SerializeField]
        internal float playerRange = 20f;
        [SerializeField]
        internal float maxForceDistance = 5f;
        [SerializeField]
        internal float maxForce = 3.5f;
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
        private int state = -1;
        private PlayerControllerB targetPlayer = null;
        private System.Random random = null;
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
        internal PlayerControllerB TargetPlayer
        {
            get
            {
                if (!IsServer)
                {
                    return targetPlayer;
                }
                PlayerControllerB newPlayer = GetClosestPlayer();
                if (targetPlayer != newPlayer)
                {
                    int id = -1;
                    if (newPlayer != null)
                    {
                        id = (int)newPlayer.playerClientId;
                    }
                    SetPlayerRpc(id);
                }
                targetPlayer = newPlayer;
                return targetPlayer;
            }
        }
        internal List<int> PlayersInRange
        {
            get
            {
                return GetPlayersInRange();
            }
        }
        internal System.Random Random
        {
            get
            {
                random ??= new System.Random(StartOfRound.Instance.randomMapSeed + IWildCardBase.totalBases);
                return random;
            }
        }
        internal virtual void InitializePrefab()
        {

        }
        internal virtual void Awake()
        {
            IWildCardBase.Awake(this);
        }
        internal virtual void Start()
        {
            if (!WildCardMod.ModConfig.Debug)
            {
                return;
            }
            Log.LogDebug($"Spawning {(this as IWildCardBase).Name}");
        }
        protected override void OnNetworkPostSpawn()
        {
            base.OnNetworkPostSpawn();
            IWildCardBase.OnNetworkPostSpawn(this);
        }
        internal virtual void Update()
        {
            IWildCardBase.Update(this);
        }
        internal virtual void OnStateChange(int newState)
        {
            
        }
        internal virtual void DamagePlayerLocal(int damage, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown, int animation = 0, Vector3 force = default)
        {
            GameNetworkManager.Instance.localPlayerController.DamagePlayer(damage, true, true, causeOfDeath, animation, false, force);
        }
        internal void PlayerFear(PlayerControllerB player, bool overTime, float amount, float cap)
        {
            PlayerFearRpc(overTime, amount, cap, player.GetRPCTarget());
        }
        internal virtual void PlayerFearLocal(bool overTime, float amount, float cap)
        {
            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            if (overTime)
            {
                player.IncreaseFearLevelOverTime(amount, cap);
                return;
            }
            player.JumpToFearLevel(amount);
        }
        internal virtual void PlayerForceLocal(float multiplier = 1f)
        {
            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            Vector3 targetPlayerVector = transform.position - player.transform.position;
            if (targetPlayerVector.magnitude > maxForceDistance)
            {
                return;
            }
            float max = maxForce * multiplier;
            player.externalForces += targetPlayerVector.normalized * (max - Mathf.Pow(targetPlayerVector.magnitude * (Mathf.Sqrt(max) / maxForceDistance), 2f));
        }
        internal virtual PlayerControllerB GetClosestPlayer()
        {
            PlayerControllerB closest = null;
            float distance = playerRange;
            List<int> players = PlayersInRange;
            for (int i = 0; i < players.Count; i++)
            {
                PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[players[i]];
                float newDistance = Vector3.Distance(player.playerGlobalHead.transform.position, transform.position + (Vector3.up * 0.5f));
                if (newDistance > distance)
                {
                    continue;
                }
                closest = player;
                distance = newDistance;
            }
            return closest;
        }
        internal virtual List<int> GetPlayersInRange()
        {
            List<int> newPlayers = new List<int>();
            Vector3 halfUp = Vector3.up * 0.5f;
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[i];
                if (!player.isPlayerControlled || Physics.Linecast(transform.position + halfUp, player.playerGlobalHead.position, 1107298560, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }
                float newDistance = Vector3.Distance(player.playerGlobalHead.transform.position, transform.position + halfUp);
                if (newDistance > playerRange)
                {
                    continue;
                }
                newPlayers.Add(i);
            }
            return newPlayers;
        }
        [Rpc(SendTo.SpecifiedInParams)]
        private void DamagePlayerRpc(int damage, int causeOfDeath = 0, int animation = 0, Vector3 force = default, RpcParams rpcParams = default)
        {
            DamagePlayerLocal(damage, (CauseOfDeath)causeOfDeath, animation, force);
        }
        [Rpc(SendTo.SpecifiedInParams)]
        private void PlayerFearRpc(bool overTime, float amount, float cap, RpcParams rpcParams = default)
        {
            PlayerFearLocal(overTime, amount, cap);
        }
        [Rpc(SendTo.NotMe)]
        private void SetPlayerRpc(int id)
        {
            if (id < 0)
            {
                targetPlayer = null;
                return;
            }
            targetPlayer = StartOfRound.Instance.allPlayerScripts[id];
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
            OnStateChange(state);
        }
        [Rpc(SendTo.NotMe)]
        private void SetNetworkAnimationsRpc(bool value)
        {
            networkAnimations = value;
            if (animator != null && animator.IsNetworked != networkAnimations)
            {
                animator.SetNetworkEnabled(networkAnimations);
            }
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