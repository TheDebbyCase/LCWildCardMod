using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
namespace LCWildCardMod.Items
{
    public class Poppy : WildCardProp
    {
        [Space(3f)]
        [Header("Poppy")]
        [Space(3f)]
        [Range(0f, 99f)]
        [SerializeField]
        private int healAmount = 50;
        [SerializeField]
        private float effectTime = 30f;
        [SerializeField]
        private float targetVignette = 0.25f;
        [SerializeField]
        private float targetFilmGrain = 1f;
        [SerializeField]
        private Color targetColourFilter = new Color(1f, 0.5f, 0f);
        [SerializeField]
        private float targetSaturation = -75f;
        [Min(0f)]
        [SerializeField]
        private float transitionTime = 5f;
        private bool active = false;
        private bool vignetteWasActive;
        private VolumeParameter[] origVignetteParameters;
        private bool filmGrainWasActive;
        private VolumeParameter[] origFilmGrainParameters;
        private bool colourAdjustWasActive;
        private VolumeParameter[] origColourAdjustParameters;
        private Vignette vignette;
        private FilmGrain filmGrain;
        private ColorAdjustments colourAdjust;
        public override void Start()
        {
            base.Start();
            VolumeProfile volume = HUDManager.Instance.playerGraphicsVolume.sharedProfile;
            if (!volume.TryGet(out vignette))
            {
                vignette = volume.Add<Vignette>();
                vignette.active = false;
            }
            if (!volume.TryGet(out filmGrain))
            {
                filmGrain = volume.Add<FilmGrain>();
                filmGrain.active = false;
            }
            if (!volume.TryGet(out colourAdjust))
            {
                colourAdjust = volume.Add<ColorAdjustments>();
                colourAdjust.active = false;
            }
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            if (!active)
            {
                return;
            }
            VolumeProfile volume = HUDManager.Instance.playerGraphicsVolume.sharedProfile;
            if (volume.TryGet(out FilmGrain filmGrain))
            {
                filmGrain.active = filmGrainWasActive;
                for (int i = 0; i < origFilmGrainParameters.Length; i++)
                {
                    VolumeParameter parameter = filmGrain.parameters[i];
                    VolumeParameter origParameter = origFilmGrainParameters[i];
                    parameter.SetValue(origParameter);
                    parameter.overrideState = origParameter.overrideState;
                }
            }
            if (volume.TryGet(out Vignette vignette))
            {
                vignette.active = vignetteWasActive;
                for (int i = 0; i < origVignetteParameters.Length; i++)
                {
                    VolumeParameter parameter = vignette.parameters[i];
                    VolumeParameter origParameter = origVignetteParameters[i];
                    parameter.SetValue(origParameter);
                    parameter.overrideState = origParameter.overrideState;
                }
            }
            if (volume.TryGet(out ColorAdjustments colourAdjust))
            {
                colourAdjust.active = colourAdjustWasActive;
                for (int i = 0; i < origColourAdjustParameters.Length; i++)
                {
                    VolumeParameter parameter = colourAdjust.parameters[i];
                    VolumeParameter origParameter = origColourAdjustParameters[i];
                    parameter.SetValue(origParameter);
                    parameter.overrideState = origParameter.overrideState;
                }
            }
            SoundManager.Instance.SetDiageticMixerSnapshot();
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            if (active)
            {
                return;
            }
            base.ItemActivate(used, buttonDown);
            LastPlayerHeldBy.health = Mathf.Min(100, LastPlayerHeldBy.health + healAmount);
            LastPlayerHeldBy.DiscardHeldObject();
            StartCoroutine(EffectCoroutine(IsOwner));
        }
        internal override void ItemActivateFromEnemy(bool used, bool buttonHeld = true)
        {
            if (active || !isHeldByEnemy)
            {
                return;
            }
            if (IsOwner == LastEnemyHeldBy.IsOwner)
            {
                EnemyForceDropItem(true);
            }
            StartCoroutine(EffectCoroutine(false, true));
        }
        private IEnumerator EffectCoroutine(bool isLocal, bool despawnAnyway = false)
        {
            active = true;
            if (isLocal)
            {
                HUDManager.Instance.UpdateHealthUI(LastPlayerHeldBy.health, false);
            }
            yield return new WaitForEndOfFrame();
            EnableItemMeshes(false);
            EnablePhysics(false);
            grabbable = false;
            if (!isLocal)
            {
                if (despawnAnyway && IsServer)
                {
                    NetworkObject.Despawn();
                }
                yield break;
            }
            vignetteWasActive = vignette.active;
            origVignetteParameters = new VolumeParameter[vignette.parameters.Count];
            for (int i = 0; i < origVignetteParameters.Length; i++)
            {
                origVignetteParameters[i] = (VolumeParameter)vignette.parameters[i].Clone();
            }
            if (vignette.mode.value == VignetteMode.Procedural)
            {
                if (!vignette.color.overrideState)
                {
                    vignette.color.Override(Color.black);
                }
                if (!vignette.center.overrideState)
                {
                    vignette.center.Override(new Vector2(0.5f, 0.5f));
                }
                if (!vignette.intensity.overrideState)
                {
                    vignette.intensity.Override(0f);
                }
                if (!vignette.roundness.overrideState)
                {
                    vignette.roundness.Override(1f);
                }
                if (vignette.intensity.value < 0.25f)
                {
                    vignette.smoothness.Override(0.2f);
                    vignette.rounded.Override(true);
                }
            }
            else
            {
                if (!vignette.color.overrideState)
                {
                    vignette.color.Override(Color.black);
                }
                if (!vignette.IsActive() || !vignette.opacity.overrideState)
                {
                    vignette.opacity.Override(0f);
                }
            }
            filmGrainWasActive = filmGrain.active;
            origFilmGrainParameters = new VolumeParameter[filmGrain.parameters.Count];
            for (int i = 0; i < origFilmGrainParameters.Length; i++)
            {
                origFilmGrainParameters[i] = (VolumeParameter)filmGrain.parameters[i].Clone();
            }
            if (!filmGrain.intensity.overrideState)
            {
                filmGrain.intensity.Override(0f);
            }
            if (!filmGrain.type.overrideState)
            {
                filmGrain.type.Override(FilmGrainLookup.Large02);
            }
            colourAdjustWasActive = colourAdjust.active;
            origColourAdjustParameters = new VolumeParameter[colourAdjust.parameters.Count];
            for (int i = 0; i < origColourAdjustParameters.Length; i++)
            {
                origColourAdjustParameters[i] = (VolumeParameter)colourAdjust.parameters[i].Clone();
            }
            if (!colourAdjust.colorFilter.overrideState)
            {
                colourAdjust.colorFilter.Override(Color.white);
            }
            if (!colourAdjust.saturation.overrideState)
            {
                colourAdjust.saturation.Override(0f);
            }
            if (!colourAdjust.IsActive())
            {
                colourAdjust.postExposure.Override(0.2f);
                colourAdjust.contrast.Override(24.5f);
            }
            float originalVignette = vignette.intensity.value;
            float originalFilmGrain = filmGrain.intensity.value;
            float originalSaturation = colourAdjust.saturation.value;
            Color originalColourFilter = colourAdjust.colorFilter.value;
            float timer = 0f;
            float detransTime = effectTime + transitionTime;
            float maxTime = detransTime + transitionTime;
            float inverseTransitionTime = 1f / transitionTime;
            bool reachedEffect = false;
            SoundManager.Instance.SetDiageticMixerSnapshot(1, transitionTime);
            vignette.active = true;
            filmGrain.active = true;
            colourAdjust.active = true;
            bool deTransitioningSnapshot = false;
            while (timer < maxTime)
            {
                yield return null;
                timer += Time.deltaTime;
                float t = timer * inverseTransitionTime;
                if (GameNetworkManager.Instance.localPlayerController.isPlayerControlled)
                {
                    if (timer < transitionTime)
                    {
                        vignette.intensity.Interp(originalVignette, targetVignette, t);
                        filmGrain.intensity.Interp(originalFilmGrain, targetFilmGrain, t);
                        colourAdjust.saturation.Interp(originalSaturation, targetSaturation, t);
                        colourAdjust.colorFilter.Interp(originalColourFilter, targetColourFilter, t);
                        continue;
                    }
                    if (!reachedEffect)
                    {
                        reachedEffect = true;
                        vignette.intensity.value = targetVignette;
                        filmGrain.intensity.value = targetFilmGrain;
                        colourAdjust.saturation.value = targetSaturation;
                        colourAdjust.colorFilter.value = targetColourFilter;
                    }
                }
                if (timer < detransTime)
                {
                    continue;
                }
                if (!deTransitioningSnapshot)
                {
                    deTransitioningSnapshot = true;
                    SoundManager.Instance.SetDiageticMixerSnapshot(transitionTime: transitionTime);
                }
                t = (timer - detransTime) * inverseTransitionTime;
                vignette.intensity.Interp(targetVignette, originalVignette, t);
                filmGrain.intensity.Interp(targetFilmGrain, originalFilmGrain, t);
                colourAdjust.saturation.Interp(targetSaturation, originalSaturation, t);
                colourAdjust.colorFilter.Interp(targetColourFilter, originalColourFilter, t);
            }
            DespawnRpc();
        }
        [Rpc(SendTo.Server)]
        private void DespawnRpc()
        {
            NetworkObject.Despawn();
        }
    }
}