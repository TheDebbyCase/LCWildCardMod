using LCWildCardMod.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode.Components;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine;
using System.Collections.ObjectModel;
using GameNetcodeStuff;
namespace LCWildCardMod.Utils
{
    [Serializable]
    public class SelectLights : Selectable<Light>
    {
        public SelectLights(int id, params Light[] arrayItems) : base(id, arrayItems) { }
        [SerializeField]
        public bool enableOnSpawn = true;
        [SerializeField]
        public bool disableWhilePocketed = true;
        public override void Initialize(IWildCardBase wildCardBase)
        {
            base.Initialize(wildCardBase);
            Set(wildCardBase.Transform.GetComponentsInChildren<Light>());
        }
        public void EnableAll(bool networked = true)
        {
            for (int i = 0; i < Count; i++)
            {
                Enable(i, false);
            }
            if (!networked)
            {
                return;
            }
            Base.SetLightEnabledNetworked(Id, true);
        }
        public void Enable(int index, bool networked = true)
        {
            if (!InRange(index))
            {
                return;
            }
            SetEnabled(index, true);
            if (!networked)
            {
                return;
            }
            Base.SetLightEnabledNetworked(Id, true, index);
        }
        public void DisableAll(bool networked = true)
        {
            for (int i = 0; i < Count; i++)
            {
                Disable(i, false);
            }
            if (!networked)
            {
                return;
            }
            Base.SetLightEnabledNetworked(Id, false);
        }
        public void Disable(int index, bool networked = true)
        {
            if (!InRange(index))
            {
                return;
            }
            SetEnabled(index, false);
            if (!networked)
            {
                return;
            }
            Base.SetLightEnabledNetworked(Id, false, index);
        }
        private void SetEnabled(int index, bool enable)
        {
            if (!InRange(index))
            {
                return;
            }
            this[index].enabled = enable;
        }
    }
    [Serializable]
    public class SelectRenderers : SelectableWithTextures<Renderer>
    {
        public SelectRenderers(int id, params Renderer[] arrayItems) : base(id, arrayItems) { }
        [SerializeField]
        public bool applyOnAwake = false;
        [SerializeField]
        public List<Mesh> meshes = default;
        [SerializeField]
        public bool enabled = true;
        [SerializeField]
        public ShadowCastingMode shadowMode = ShadowCastingMode.On;
        [SerializeField]
        public MotionVectorGenerationMode motionMode = MotionVectorGenerationMode.Camera;
        public void AllApplyAll()
        {
            for (int i = 0; i < Count; i++)
            {
                ApplyAll(i);
            }
        }
        public void ApplyAll(int index)
        {
            if (!InRange(index))
            {
                return;
            }
            ApplySettings(index);
        }
        public void AllApplySettings()
        {
            for (int i = 0; i < Count; i++)
            {
                ApplySettings(i);
            }
        }
        public void ApplySettings(int index)
        {
            if (!InRange(index))
            {
                return;
            }
            Renderer renderer = this[index];
            renderer.enabled = enabled;
            renderer.shadowCastingMode = shadowMode;
            renderer.motionVectorGenerationMode = motionMode;
        }
        public void AllApplyMeshes(int index)
        {
            if (!InRangeMeshes(index))
            {
                return;
            }
            for (int i = 0; i < Count; i++)
            {
                ApplyMesh(i, index);
            }
        }
        public void ApplyMesh(int index, int meshIndex)
        {
            Renderer renderer = this[index];
            Mesh mesh = meshes[meshIndex];
            switch (renderer)
            {
                case MeshRenderer meshRenderer:
                    {
                        meshRenderer.GetComponent<MeshFilter>().sharedMesh = mesh;
                        break;
                    }
                case SkinnedMeshRenderer skinnedMeshRenderer:
                    {
                        skinnedMeshRenderer.sharedMesh = mesh;
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }
        public void AddMeshes(params Mesh[] newMeshes)
        {
            for (int i = 0; i < newMeshes.Length; i++)
            {
                Mesh newMesh = newMeshes[i];
                if (!meshes.Contains(newMesh))
                {
                    meshes.Add(newMesh);
                }
            }
        }
        public void SetMeshes(params Mesh[] newMeshes)
        {
            meshes = newMeshes.ToList();
        }
        public void SetMesh(int index, Mesh newMesh)
        {
            if (!InRangeMeshes(index))
            {
                return;
            }
            meshes[index] = newMesh;
        }
        public void RemoveMeshes(params Mesh[] toRemoveMeshes)
        {
            for (int i = 0; i < toRemoveMeshes.Length; i++)
            {
                meshes.Remove(toRemoveMeshes[i]);
            }
        }
        public void RemoveMeshes(int start, int count)
        {
            if (!InRangeMeshes(start) || !InRangeMeshes(start + count) || count <= 0)
            {
                return;
            }
            meshes.RemoveRange(start, count);
        }
        public void RemoveMesh(int index)
        {
            if (!InRangeMeshes(index))
            {
                return;
            }
            RemoveMeshes(index, 1);
        }
        public void ClearMeshes()
        {
            SetMeshes();
        }
        public bool InRangeMeshes(int index)
        {
            return index >= 0 && index < meshes.Count;
        }
        public int IndexOfMesh(Mesh mesh)
        {
            return meshes.IndexOf(mesh);
        }
        public override void Initialize(IWildCardBase wildCardBase)
        {
            base.Initialize(wildCardBase);
            for (int i = 0; i < Count; i++)
            {
                Renderer renderer = this[i];
                if (renderer.motionVectorGenerationMode != MotionVectorGenerationMode.Object)
                {
                    continue;
                }
                renderer.motionVectorGenerationMode = motionMode;
            }
        }
        public override void SetMaterialsTexture(int itemIndex, int textureIndex)
        {
            if (!InRange(itemIndex) || !InRangeTextures(textureIndex))
            {
                return;
            }
            int count = this[itemIndex].materials.Length;
            for (int i = 0; i < count; i++)
            {
                SetMaterialTexture(itemIndex, i, textureIndex);
            }
        }
        public override void SetMaterialTexture(int itemIndex, int materialIndex, int textureIndex)
        {
            if (!InRange(itemIndex) || !InRangeTextures(textureIndex))
            {
                return;
            }
            Material[] materials = this[itemIndex].materials;
            if (materialIndex < 0 || materialIndex >= materials.Length)
            {
                return;
            }
            materials[materialIndex].mainTexture = GetTexture(textureIndex);
        }
        public void SetColours(float multiplier)
        {
            for (int i = 0; i < Count; i++)
            {
                SetColour(i, multiplier);
            }
        }
        public void SetColour(int index, float multiplier)
        {
            if (!InRange(index))
            {
                return;
            }
            int count = this[index].materials.Length;
            for (int i = 0; i < count; i++)
            {
                SetColourMaterial(index, i, multiplier);
            }
        }
        public void SetColourMaterial(int index, int materialIndex, float multiplier)
        {
            if (!InRange(index))
            {
                return;
            }
            Material[] materials = this[index].materials;
            if (materialIndex < 0 || materialIndex >= materials.Length)
            {
                return;
            }
            materials[materialIndex].color *= multiplier;
        }
        public void SetColours(Color color, bool multiply = false)
        {
            for (int i = 0; i < Count; i++)
            {
                SetColour(i, color, multiply);
            }
        }
        public void SetColour(int index, Color color, bool multiply = false)
        {
            if (!InRange(index))
            {
                return;
            }
            int count = this[index].materials.Length;
            for (int i = 0; i < count; i++)
            {
                SetColourMaterial(index, i, color, multiply);
            }
        }
        public void SetColourMaterial(int index, int materialIndex, Color color, bool multiply = false)
        {
            if (!InRange(index))
            {
                return;
            }
            Material[] materials = this[index].materials;
            if (materialIndex < 0 || materialIndex >= materials.Length)
            {
                return;
            }
            if (multiply)
            {
                materials[materialIndex].color *= color;
                return;
            }
            materials[materialIndex].color = color;
        }
        public void SetOffsets(Vector2 offset)
        {
            for (int i = 0; i < Count; i++)
            {
                SetOffset(i, offset);
            }
        }
        public void SetOffset(int index, Vector2 offset)
        {
            if (!InRange(index))
            {
                return;
            }
            int count = this[index].materials.Length;
            for (int i = 0; i < count; i++)
            {
                SetOffsetMaterial(index, i, offset);
            }
        }
        public void SetOffsetMaterial(int index, int materialIndex, Vector2 offset)
        {
            if (!InRange(index))
            {
                return;
            }
            Material[] materials = this[index].materials;
            if (materialIndex < 0 || materialIndex >= materials.Length)
            {
                return;
            }
            materials[materialIndex].mainTextureOffset = offset;
        }

    }
    [Serializable]
    public class SelectParticles : SelectableWithTextures<Particles>
    {
        public SelectParticles(int id, params ParticleSystem[] systems) : base(id, systems.Select((x) => Particles.Create(x)).ToArray()) { }
        [SerializeField]
        public bool applyOnAwake = false;
        [SerializeField]
        public bool playOnAwake = false;
        [SerializeField]
        public bool looping = true;
        [SerializeField]
        public bool useGravity = true;
        [Min(0f)]
        [SerializeField]
        public float duration = 1f;
        [Min(0f)]
        [SerializeField]
        public float simulationSpeed = 1f;
        [Min(0f)]
        [SerializeField]
        public float minLife = 1f;
        [Min(0f)]
        [SerializeField]
        public float maxLife = 1f;
        [SerializeField]
        public float minGravity = 0f;
        [SerializeField]
        public float maxGravity = 0f;
        [Min(0f)]
        [SerializeField]
        public float minSize = 0.05f;
        [Min(0f)]
        [SerializeField]
        public float maxSize = 0.05f;
        [Range(-180f, 180f)]
        [SerializeField]
        public float minRotation = 0f;
        [Range(-180f, 180f)]
        [SerializeField]
        public float maxRotation = 0f;
        [Range(0, 1f)]
        [SerializeField]
        public float flipAmount = 0f;
        [Min(1f)]
        [SerializeField]
        public int maxParticles = 5;
        [Min(0f)]
        [SerializeField]
        public float minTimeRate = 0f;
        [Min(0f)]
        [SerializeField]
        public float maxTimeRate = 0f;
        [Min(0f)]
        [SerializeField]
        public float minDistanceRate = 0f;
        [Min(0f)]
        [SerializeField]
        public float maxDistanceRate = 0f;
        [SerializeField]
        public List<BurstIntermediary> bursts = new List<BurstIntermediary>();
        public void AllApplyAll()
        {
            for (int i = 0; i < Count; i++)
            {
                ApplyAll(i);
            }
        }
        public void ApplyAll(int index)
        {
            if (!InRange(index))
            {
                return;
            }
            ApplySize(index);
            ApplyGravity(index);
            ApplyLifetime(index);
            ApplyRotation(index);
            ApplyEmission(index);
        }
        public override void SetMaterialsTexture(int itemIndex, int textureIndex)
        {
            if (!InRange(itemIndex) || !InRangeTextures(textureIndex))
            {
                return;
            }
            int count = this[itemIndex].Renderer.materials.Length;
            for (int i = 0; i < count; i++)
            {
                SetMaterialTexture(itemIndex, i, textureIndex);
            }
        }
        public override void SetMaterialTexture(int itemIndex, int materialIndex, int textureIndex)
        {
            if (!InRange(itemIndex) || !InRangeTextures(textureIndex))
            {
                return;
            }
            Material[] materials = this[itemIndex].Renderer.materials;
            if (materialIndex < 0 || materialIndex >= materials.Length)
            {
                return;
            }
            materials[materialIndex].mainTexture = GetTexture(textureIndex);
        }
        public void SetColours(float multiplier)
        {
            for (int i = 0; i < Count; i++)
            {
                SetColour(i, multiplier);
            }
        }
        public void SetColour(int index, float multiplier)
        {
            if (!InRange(index))
            {
                return;
            }
            int count = this[index].Renderer.materials.Length;
            for (int i = 0; i < count; i++)
            {
                SetColourMaterial(index, i, multiplier);
            }
        }
        public void SetColourMaterial(int index, int materialIndex, float multiplier)
        {
            if (!InRange(index))
            {
                return;
            }
            Material[] materials = this[index].Renderer.materials;
            if (materialIndex < 0 || materialIndex >= materials.Length)
            {
                return;
            }
            materials[materialIndex].color *= multiplier;
        }
        public void SetColours(Color color, bool multiply = false)
        {
            for (int i = 0; i < Count; i++)
            {
                SetColour(i, color, multiply);
            }
        }
        public void SetColour(int index, Color color, bool multiply = false)
        {
            if (!InRange(index))
            {
                return;
            }
            int count = this[index].Renderer.materials.Length;
            for (int i = 0; i < count; i++)
            {
                SetColourMaterial(index, i, color, multiply);
            }
        }
        public void SetColourMaterial(int index, int materialIndex, Color color, bool multiply = false)
        {
            if (!InRange(index))
            {
                return;
            }
            Material[] materials = this[index].Renderer.materials;
            if (materialIndex < 0 || materialIndex >= materials.Length)
            {
                return;
            }
            if (multiply)
            {
                materials[materialIndex].color *= color;
                return;
            }
            materials[materialIndex].color = color;
        }
        public void AllApplyEmission(bool replace, params BurstIntermediary[] newBursts)
        {
            if (replace)
            {
                SetBursts(newBursts);
            }
            else
            {
                AddBursts(newBursts);
            }
            AllApplyEmission();
        }
        public void AllApplyEmission()
        {
            for (int i = 0; i < Count; i++)
            {
                ApplyEmission(i);
            }
        }
        public void ApplyEmission(int index, bool replace, params BurstIntermediary[] newBursts)
        {
            if (!InRange(index))
            {
                return;
            }
            if (replace)
            {
                SetBursts(newBursts);
            }
            else
            {
                AddBursts(newBursts);
            }
            ApplyEmission(index);
        }
        public void ApplyEmission(int index)
        {
            if (!InRange(index))
            {
                return;
            }
            ParticleSystem system = this[index].System;
            ParticleSystem.MainModule main = system.main;
            main.maxParticles = maxParticles;
            ParticleSystem.EmissionModule emission = system.emission;
            ParticleSystem.MinMaxCurve timeRate = emission.rateOverTime;
            ParticleSystem.MinMaxCurve distRate = emission.rateOverDistance;
            if (Mathf.Approximately(minTimeRate, maxTimeRate))
            {
                timeRate.mode = ParticleSystemCurveMode.Constant;
                timeRate.constant = minTimeRate;
                emission.rateOverTimeMultiplier = 1f;
            }
            else
            {
                timeRate.mode = ParticleSystemCurveMode.TwoConstants;
                timeRate.constantMin = minTimeRate;
                timeRate.constantMax = maxTimeRate;
            }
            if (Mathf.Approximately(minDistanceRate, maxDistanceRate))
            {
                distRate.mode = ParticleSystemCurveMode.Constant;
                distRate.constant = minDistanceRate;
                emission.rateOverDistanceMultiplier = 1f;
            }
            else
            {
                distRate.mode = ParticleSystemCurveMode.TwoConstants;
                distRate.constantMin = minDistanceRate;
                distRate.constantMax = maxDistanceRate;
            }
            emission.rateOverTime = timeRate;
            emission.rateOverDistance = distRate;
            ParticleSystem.Burst[] newBursts = new ParticleSystem.Burst[bursts.Count];
            for (int i = 0; i < bursts.Count; i++)
            {
                newBursts[i] = WildUtils.CreateBurst(bursts[i]);
            }
            emission.SetBursts(newBursts);
        }
        public void AddBursts(params BurstIntermediary[] newBursts)
        {
            for (int i = 0; i < newBursts.Length; i++)
            {
                BurstIntermediary newBurst = newBursts[i];
                if (!bursts.Contains(newBurst))
                {
                    bursts.Add(newBurst);
                }
            }
        }
        public void SetBursts(params BurstIntermediary[] newBursts)
        {
            bursts = newBursts.ToList();
        }
        public void SetBurst(int index, BurstIntermediary newBurst)
        {
            if (!InRangeBursts(index))
            {
                return;
            }
            bursts[index] = newBurst;
        }
        public void ReplaceBurst(int index, BurstIntermediary newBurst)
        {
            if (!InRangeBursts(index))
            {
                return;
            }
            bursts[index] = newBurst;
        }
        public void RemoveBursts(params BurstIntermediary[] toRemoveBursts)
        {
            for (int i = 0; i < toRemoveBursts.Length; i++)
            {
                bursts.Remove(toRemoveBursts[i]);
            }
        }
        public void RemoveBursts(int start, int count)
        {
            if (!InRangeBursts(start) || !InRangeBursts(start + count) || count <= 0)
            {
                return;
            }
            bursts.RemoveRange(start, count);
        }
        public void RemoveBurst(int index)
        {
            if (!InRangeBursts(index))
            {
                return;
            }
            RemoveBursts(index, 1);
        }
        public void ClearBursts()
        {
            SetBursts();
        }
        public void AllApplyTiming()
        {
            for (int i = 0; i < Count; i++)
            {
                ApplyTiming(i);
            }
        }
        public void ApplyTiming(int index)
        {
            if (!InRange(index))
            {
                return;
            }
            ParticleSystem.MainModule system = this[index].System.main;
            system.loop = looping;
            system.duration = duration;
            system.simulationSpeed = simulationSpeed;
        }
        public void AllApplyRotation()
        {
            for (int i = 0; i < Count; i++)
            {
                ApplyRotation(i);
            }
        }
        public void ApplyRotation(int index)
        {
            if (!InRange(index))
            {
                return;
            }
            ParticleSystem.MainModule system = this[index].System.main;
            ParticleSystem.MinMaxCurve rotation = system.startRotation;
            if (Mathf.Approximately(minRotation, maxRotation))
            {
                rotation.mode = ParticleSystemCurveMode.Constant;
                rotation.constant = minRotation;
                system.startRotationMultiplier = 1f;
            }
            else
            {
                rotation.mode = ParticleSystemCurveMode.TwoConstants;
                rotation.constantMin = minRotation;
                rotation.constantMax = maxRotation;
            }
            system.flipRotation = flipAmount;
            system.startRotation = rotation;
        }
        public void AllApplyLifetime()
        {
            for (int i = 0; i < Count; i++)
            {
                ApplyLifetime(i);
            }
        }
        public void ApplyLifetime(int index)
        {
            if (!InRange(index))
            {
                return;
            }
            ParticleSystem.MainModule system = this[index].System.main;
            ParticleSystem.MinMaxCurve lifetime = system.startLifetime;
            if (Mathf.Approximately(minLife, maxLife))
            {
                lifetime.mode = ParticleSystemCurveMode.Constant;
                lifetime.constant = minLife;
                system.startLifetimeMultiplier = 1f;
            }
            else
            {
                lifetime.mode = ParticleSystemCurveMode.TwoConstants;
                lifetime.constantMin = minLife;
                lifetime.constantMax = maxLife;
            }
            system.startLifetime = lifetime;
        }
        public void AllApplyGravity()
        {
            for (int i = 0; i < Count; i++)
            {
                ApplyGravity(i);
            }
        }
        public void ApplyGravity(int index)
        {
            if (!InRange(index))
            {
                return;
            }
            ParticleSystem.MainModule system = this[index].System.main;
            ParticleSystem.MinMaxCurve gravity = system.gravityModifier;
            float min = minGravity;
            float max = maxGravity;
            if (!useGravity)
            {
                min = 0f;
                max = 0f;
            }
            if (Mathf.Approximately(minGravity, maxGravity))
            {
                gravity.mode = ParticleSystemCurveMode.Constant;
                gravity.constant = min;
                system.gravityModifierMultiplier = 1f;
            }
            else
            {
                gravity.mode = ParticleSystemCurveMode.TwoConstants;
                gravity.constantMin = min;
                gravity.constantMax = max;
            }
            system.gravityModifier = gravity;
        }
        public void AllApplySize()
        {
            for (int i = 0; i < Count; i++)
            {
                ApplySize(i);
            }
        }
        public void ApplySize(int index)
        {
            if (!InRange(index))
            {
                return;
            }
            ParticleSystem.MainModule system = this[index].System.main;
            ParticleSystem.MinMaxCurve size = system.startSize;
            if (Mathf.Approximately(minSize, maxSize))
            {
                size.mode = ParticleSystemCurveMode.Constant;
                size.constant = minSize;
                system.startSizeMultiplier = 1f;
            }
            else
            {
                size.mode = ParticleSystemCurveMode.TwoConstants;
                size.constantMin = minSize;
                size.constantMax = maxSize;
            }
            system.startSize = size;
        }
        public void PlayAll(bool restart = false, bool networked = true)
        {
            for (int i = 0; i < Count; i++)
            {
                Play(i, restart, false);
            }
            if (!networked)
            {
                return;
            }
            Base.PlayParticlesNetworked(Id, restart);
        }
        public void Play(int index, bool restart = false, bool networked = true)
        {
            if (!InRange(index))
            {
                return;
            }
            ParticleSystem system = this[index].System;
            if (restart)
            {
                system.Stop();
            }
            system.Play();
            if (!networked)
            {
                return;
            }
            Base.PlayParticlesNetworked(Id, restart, index);
        }
        public void StopAll(bool clear, bool networked = true)
        {
            for (int i = 0; i < Count; i++)
            {
                Stop(i, clear, false);
            }
            if (networked)
            {
                Base.StopParticlesNetworked(Id, clear);
            }
        }
        public void Stop(int index, bool clear, bool networked = true)
        {
            if (!InRange(index))
            {
                return;
            }
            Particles particle = this[index];
            particle.System.Stop();
            if (clear)
            {
                particle.System.Clear();
            }
            if (networked)
            {
                Base.StopParticlesNetworked(Id, clear, index);
            }
        }
        public void PauseAll(bool networked = true)
        {
            for (int i = 0; i < Count; i++)
            {
                Pause(i, false);
            }
            if (networked)
            {
                Base.PauseParticlesNetworked(Id);
            }
        }
        public void Pause(int index, bool networked = true)
        {
            if (!InRange(index))
            {
                return;
            }
            this[index].System.Pause();
            if (networked)
            {
                Base.PauseParticlesNetworked(Id, index);
            }
        }
        public void ClearAll(bool networked = true)
        {
            for (int i = 0; i < Count; i++)
            {
                Clear(i, false);
            }
            if (networked)
            {
                Base.ClearParticlesNetworked(Id);
            }
        }
        public void Clear(int index, bool networked = true)
        {
            if (!InRange(index))
            {
                return;
            }
            this[index].System.Clear();
            if (networked)
            {
                Base.ClearParticlesNetworked(Id, index);
            }
        }
        public void EmitAll(int count, bool networked = true)
        {
            for (int i = 0; i < Count; i++)
            {
                Emit(i, count, false);
            }
            if (networked)
            {
                Base.EmitParticlesNetworked(Id, count);
            }
        }
        public void Emit(int index, int count, bool networked = true)
        {
            if (!InRange(index))
            {
                return;
            }
            this[index].System.Emit(count);
            if (networked)
            {
                Base.EmitParticlesNetworked(Id, count, index);
            }
        }
        public bool AnyAlive()
        {
            for (int i = 0; i < Count; i++)
            {
                if (!this[i].System.IsAlive())
                {
                    continue;
                }
                return true;
            }
            return false;
        }
        public bool InRangeBursts(int index)
        {
            return index >= 0 && index < bursts.Count;
        }
        public int IndexOfBurst(BurstIntermediary burst)
        {
            return bursts.IndexOf(burst);
        }
    }
    [Serializable]
    public class SelectAnimationParameters : SelectRepeater<RepeatingAnimation>
    {
        public SelectAnimationParameters(int id, AnimationHandler animationHandler) : base(id, animationHandler.Base.Random)
        {
            SetAnimator(animationHandler);
        }
        public SelectAnimationParameters(int id, NetworkAnimator networkAnimator) : this(id, AnimationHandler.Create(networkAnimator)) { }
        public void SetAllValues(RepeatingAnimation.RandomAnimationType? type = null, params float?[] toChange)
        {
            for (int i = 0; i < Count; i++)
            {
                SetValues(i, type, toChange);
            }
        }
        public void SetValues(int index, RepeatingAnimation.RandomAnimationType? type = null, params float?[] toChange)
        {
            if (!InRange(index))
            {
                return;
            }
            this[index].SetValues(type, toChange);
        }
        public void UpdateAllValues(params float?[] toChange)
        {
            for (int i = 0; i < Count; i++)
            {
                UpdateValues(i, toChange);
            }
        }
        public void UpdateValues(int index, params float?[] toChange)
        {
            if (!InRange(index))
            {
                return;
            }
            this[index].UpdateValues(toChange);
        }
        public void DoAllAnimations(float? overrideValue = null)
        {
            for (int i = 0; i < Count; i++)
            {
                DoAnimation(i, overrideValue);
            }
        }
        public void DoAnimation(int index, float? overrideValue = null)
        {
            if (!InRange(index))
            {
                return;
            }
            this[index].DoAnimation(overrideValue);
        }
        public void SetAnimator(AnimationHandler animator)
        {
            for (int i = 0; i < Count; i++)
            {
                this[i].SetAnimator(animator);
            }
        }
    }
    [Serializable]
    public abstract class SelectRepeater<T> : Selectable<T> where T : Repeater
    {
        public SelectRepeater(int id, System.Random random) : base(id)
        {
            SetRandom(random);
        }
        [SerializeField]
        public bool playOnSpawn = false;
        public void TickAll()
        {
            for (int i = 0; i < Count; i++)
            {
                Tick(i);
            }
        }
        public void Tick(int index)
        {
            if (!InRange(index))
            {
                return;
            }
            this[index].Tick();
        }
        public void PauseAll()
        {
            for (int i = 0; i < Count; i++)
            {
                Pause(i);
            }
        }
        public void Pause(int index)
        {
            if (!InRange(index))
            {
                return;
            }
            this[index].Pause();
        }
        public void ResumeAll()
        {
            for (int i = 0; i < Count; i++)
            {
                Resume(i);
            }
        }
        public void Resume(int index)
        {
            if (!InRange(index))
            {
                return;
            }
            this[index].Resume();
        }
        public void SetFunctionAll(Func<bool> waitFor)
        {
            for (int i = 0; i < Count; i++)
            {
                SetFunction(i, waitFor);
            }
        }
        public void SetFunction(int index, Func<bool> waitFor)
        {
            if (!InRange(index))
            {
                return;
            }
            this[index].LoopWaitsFor = waitFor;
        }
        public void PlayAll(bool resetTimer)
        {
            for (int i = 0; i < Count; i++)
            {
                Play(i, resetTimer);
            }
        }
        public void Play(int index, bool resetTimer)
        {
            if (!InRange(index))
            {
                return;
            }
            Repeater repeater = this[index];
            if (resetTimer)
            {
                repeater.ResetTimer();
            }
            repeater.TimerTrigger();
        }
        public void PlayAll(Func<Repeater, bool> resetIf)
        {
            resetIf ??= (x) => false;
            for (int i = 0; i < Count; i++)
            {
                Play(i, resetIf);
            }
        }
        public void Play(int index, Func<Repeater, bool> resetIf)
        {
            if (!InRange(index))
            {
                return;
            }
            Repeater repeater = this[index];
            resetIf ??= (x) => false;
            if (resetIf.Invoke(repeater))
            {
                repeater.ResetTimer();
            }
            repeater.TimerTrigger();
        }
        public void SetAllTimers(float? newMin = null, float? newMax = null, bool oneTime = false)
        {
            for (int i = 0; i < Count; i++)
            {
                SetTimer(i, newMin, newMax, oneTime);
            }
        }
        public void SetTimer(int index, float? newMin = null, float? newMax = null, bool oneTime = false)
        {
            if (!InRange(index))
            {
                return;
            }
            this[index].SetTimer(newMin, newMax, oneTime);
        }
        public void ResetAllTimers(bool undoOneTime = true)
        {
            for (int i = 0; i < Count; i++)
            {
                ResetTimer(i, undoOneTime);
            }
        }
        public void ResetTimer(int index, bool undoOneTime = true)
        {
            if (!InRange(index))
            {
                return;
            }
            this[index].ResetTimer(undoOneTime);
        }
        public void SetRandom(System.Random random)
        {
            for (int i = 0; i < Count; i++)
            {
                this[i].SetRandom(random);
            }
        }
    }
    [Serializable]
    public class SelectAudioClips : Selectable<AudioClip>
    {
        public SelectAudioClips(int id, params AudioClip[] clips) : base(id, clips) { }
        [SerializeField]
        public bool playOnSpawn = false;
        [SerializeField]
        private bool hudAudio = false;
        [SerializeField]
        private Transform audioParent = default;
        [SerializeField]
        private AnimationCurve customCurve = default;
        [SerializeField]
        private bool looping = false;
        [SerializeField]
        private bool seamlessLoop = false;
        [Min(0f)]
        [SerializeField]
        private float minLoop = 0.5f;
        [Min(0f)]
        [SerializeField]
        private float maxLoop = 5f;
        [Min(0f)]
        [SerializeField]
        private float volumeMultiplier = 1f;
        [Min(0f)]
        [SerializeField]
        private float minVolume = 0.8f;
        [Min(0f)]
        [SerializeField]
        private float maxVolume = 1.2f;
        [Min(0f)]
        [SerializeField]
        private float pitchMultiplier = 1f;
        [Min(0f)]
        [SerializeField]
        private float minPitch = 0.75f;
        [Min(0f)]
        [SerializeField]
        private float maxPitch = 1.25f;
        [Min(0f)]
        [SerializeField]
        private float doppler = 1f;
        [Range(0f, 360f)]
        [SerializeField]
        private float spread = 0f;
        [SerializeField]
        public bool doWalkie = false;
        [SerializeField]
        public bool doAudible = false;
        [SerializeField]
        private bool audibleLoop = false;
        [SerializeField]
        private float minAudible = 2f;
        [SerializeField]
        private float maxAudible = 5f;
        [SerializeField]
        public bool trackTimesNearby = false;
        [Min(0f)]
        [SerializeField]
        private float audibleRange = 25f;
        [SerializeField]
        private bool doFar = false;
        [SerializeField]
        private bool vanillaOcclusion = false;
        [HideInInspector]
        [SerializeField]
        private AudioSource source = default;
        [HideInInspector]
        [SerializeField]
        private AudioSource farSource = default;
        [HideInInspector]
        [SerializeField]
        private bool loopCurrent = false;
        [HideInInspector]
        [SerializeField]
        private Vector2Int minMaxLoopRound;
        [HideInInspector]
        [SerializeField]
        private Vector2Int minMaxAudibleRound;
        [HideInInspector]
        [SerializeField]
        private float inverseVolumeMultiplier;
        [HideInInspector]
        [SerializeField]
        private Vector2Int minMaxVolumeRound;
        [HideInInspector]
        [SerializeField]
        private float inversePitchMultiplier;
        [HideInInspector]
        [SerializeField]
        private Vector2Int minMaxPitchRound;
        private float loopTime = 0f;
        private float audibleTime = 0f;
        private int timesNearby = 0;
        private Vector3 lastPlayedPosition = default;
        private AudioClip lastClip = default;
        public bool HUDAudio
        {
            get
            {
                return hudAudio;
            }
        }
        public bool Loops
        {
            get
            {
                return looping;
            }
        }
        public float CloseRange
        {
            get
            {
                if (hudAudio)
                {
                    return 0f;
                }
                return audibleRange;
            }
            private set
            {
                if (hudAudio)
                {
                    return;
                }
                source.maxDistance = value;
                audibleRange = value;
                if (!FarNoise)
                {
                    return;
                }
                FarRange = value * 3f;
            }
        }
        public float FarRange
        {
            get
            {
                if (hudAudio)
                {
                    return 0f;
                }
                return farSource.maxDistance;
            }
            private set
            {
                if (hudAudio || farSource == null)
                {
                    return;
                }
                farSource.maxDistance = value;
            }
        }
        public bool IsPlaying
        {
            get
            {
                return (source.isPlaying && IndexOf(source.clip) != -1) || (farSource != null && farSource.isPlaying && IndexOf(farSource.clip) != -1);
            }
        }
        public int TimesNearby
        {
            get
            {
                if (hudAudio)
                {
                    return 0;
                }
                return timesNearby;
            }
            private set
            {
                if (hudAudio)
                {
                    return;
                }
                timesNearby = value;
            }
        }
        public Vector3 LastPosition
        {
            get
            {
                return lastPlayedPosition;
            }
            private set
            {
                lastPlayedPosition = value;
            }
        }
        public AudioClip LastClip
        {
            get
            {
                if (lastClip == null)
                {
                    if (IsPlaying)
                    {
                        lastClip = source.clip;
                    }
                    else
                    {
                        lastClip = Random();
                    }
                }
                return lastClip;
            }
            private set
            {
                lastClip = value;
            }
        }
        internal float ClipTime
        {
            get
            {
                return ((float)source.timeSamples / (float)lastClip.frequency) / source.clip.length;
            }
            set
            {
                source.time = value * source.clip.length;
                if (!doFar || farSource != null)
                {
                    return;
                }
                farSource.time = value * farSource.clip.length;
            }
        }
        internal bool FarNoise
        {
            get
            {
                if (!doFar || farSource == null || !farSource.enabled)
                {
                    return false;
                }
                if (GameNetworkManager.Instance == null)
                {
                    return doFar;
                }
                PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
                return localPlayer.isInsideFactory || localPlayer.spectatedPlayerScript?.isInsideFactory == true;
            }
            set
            {
                if (!doFar || farSource == null)
                {
                    return;
                }
                farSource.enabled = value;
            }
        }
        public override void Initialize(IWildCardBase wildCardBase)
        {
            loopCurrent = looping && !seamlessLoop;
            SetLoop(looping, minLoop, maxLoop);
            SetAudibleLoop(audibleLoop, minAudible, maxAudible);
            SetVolume(volumeMultiplier, minVolume, maxVolume);
            SetPitch(pitchMultiplier, minPitch, maxPitch);
            if (hudAudio)
            {
                return;
            }
            if (audioParent == null)
            {
                audioParent = wildCardBase.Transform;
            }
            GameObject sourceGO = new GameObject($"{audioParent.name}_Audio_{Id}");
            sourceGO.transform.SetParent(audioParent);
            sourceGO.transform.localPosition = Vector3.zero;
            source = sourceGO.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Custom;
            Set3DSettings(doppler, spread, customCurve);
            if (vanillaOcclusion)
            {
                source.gameObject.AddComponent<OccludeAudio>();
            }
            else if (doFar)
            {
                GameObject farSourceGO = new GameObject($"{audioParent.name}_FarAudio_{Id}");
                farSourceGO.transform.SetParent(audioParent);
                farSourceGO.transform.localPosition = Vector3.zero;
                farSource = farSourceGO.AddComponent<AudioSource>();
                farSource.playOnAwake = false;
                farSource.spatialBlend = 1f;
                farSource.rolloffMode = AudioRolloffMode.Custom;
                farSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, new AnimationCurve(new Keyframe(0.2222f, 0f), new Keyframe(0.333f, 0.2f), new Keyframe(1f, 0f)));
                farSource.spread = 75f;
                AudioReverbFilter reverbFilter = farSourceGO.AddComponent<AudioReverbFilter>();
                reverbFilter.dryLevel = -5000f;
                reverbFilter.room = 0f;
                reverbFilter.roomHF = -3000f;
                reverbFilter.roomLF = -500f;
                reverbFilter.decayTime = 5f;
                reverbFilter.decayHFRatio = 2f;
                reverbFilter.reflectionsLevel = 0f;
                reverbFilter.reflectionsDelay = 0f;
                reverbFilter.reverbLevel = 1500f;
                reverbFilter.reverbDelay = 0f;
                reverbFilter.hfReference = 2000f;
                reverbFilter.lfReference = 500f;
                reverbFilter.diffusion = 100f;
                reverbFilter.density = 100f;
            }
            CloseRange = audibleRange;
        }
        public void HUDOverride()
        {
            source = HUDManager.Instance.UIAudio;
        }
        public void SetMixer(AudioMixerGroup group)
        {
            if (source == null)
            {
                return;
            }
            source.outputAudioMixerGroup = group;
            if (farSource == null)
            {
                return;
            }
            farSource.outputAudioMixerGroup = group;
        }
        public void LoopTick()
        {
            if (loopCurrent)
            {
                if (loopTime > 0f)
                {
                    loopTime -= Time.deltaTime;
                }
                else if (!IsPlaying)
                {
                    float newTime = minLoop;
                    if (!Mathf.Approximately(minLoop, maxLoop))
                    {
                        newTime = Base.Random.Next(minMaxLoopRound.x, minMaxLoopRound.y) * 0.01f;
                    }
                    loopTime = newTime;
                    if (Count == 1)
                    {
                        RepeatClip();
                    }
                    else
                    {
                        PlayRandomClip();
                    }
                }
            }
            if (hudAudio)
            {
                return;
            }
            if (!FarNoise)
            {
                FarRange = 0f;
            }
            else if (FarRange <= 0f)
            {
                FarRange = CloseRange * 3f;
            }
            if (!audibleLoop)
            {
                return;
            }
            if (audibleTime > 0f)
            {
                audibleTime -= Time.deltaTime;
                return;
            }
            if (!IsPlaying)
            {
                return;
            }
            audibleTime = Base.Random.Next(minMaxAudibleRound.x, minMaxAudibleRound.y) * 0.01f;
            DogNoise(0.5f, 1f);
        }
        public void SetLoop(bool loop, float? loopMin = null, float? loopMax = null)
        {
            if (loopMin.HasValue)
            {
                minLoop = loopMin.Value;
            }
            if (loopMax.HasValue)
            {
                maxLoop = loopMax.Value;
            }
            minLoop = Mathf.Clamp(minLoop, 0f, maxLoop);
            maxLoop = Mathf.Clamp(maxLoop, minLoop, maxLoop);
            minMaxLoopRound = new Vector2Int(Mathf.RoundToInt(minLoop * 100f), Mathf.RoundToInt(maxLoop * 100f) + 1);
            if (seamlessLoop)
            {
                if (source != null)
                {
                    source.loop = loop;
                    if (FarNoise)
                    {
                        farSource.loop = loop;
                    }
                }
                return;
            }
            looping = loop;
        }
        public void SetVolume(float? multiplier = null, float? volMin = null, float? volMax = null)
        {
            if (multiplier.HasValue)
            {
                if (source != null && IsPlaying)
                {
                    source.volume *= multiplier.Value * inverseVolumeMultiplier;
                    if (FarNoise)
                    {
                        farSource.volume *= multiplier.Value * inverseVolumeMultiplier;
                    }
                }
                volumeMultiplier = multiplier.Value;
                inverseVolumeMultiplier = 1f / volumeMultiplier;
            }
            if (volMin.HasValue)
            {
                minVolume = volMin.Value;
            }
            if (volMax.HasValue)
            {
                maxVolume = volMax.Value;
            }
            minVolume = Mathf.Clamp(minVolume, 0f, maxVolume);
            maxVolume = Mathf.Clamp(maxVolume, minVolume, maxVolume);
            minMaxVolumeRound = new Vector2Int(Mathf.RoundToInt(minVolume * 100f), Mathf.RoundToInt(maxVolume * 100f) + 1);
        }
        public void SetPitch(float? multiplier = null, float? pitchMin = null, float? pitchMax = null)
        {
            if (multiplier.HasValue)
            {
                if (source != null && IsPlaying)
                {
                    source.pitch *= multiplier.Value * inversePitchMultiplier;
                    if (FarNoise)
                    {
                        farSource.pitch *= multiplier.Value * inversePitchMultiplier;
                    }
                }
                pitchMultiplier = multiplier.Value;
                inversePitchMultiplier = 1f / pitchMultiplier;
            }
            if (pitchMin.HasValue)
            {
                minPitch = pitchMin.Value;
            }
            if (pitchMax.HasValue)
            {
                maxPitch = pitchMax.Value;
            }
            minPitch = Mathf.Clamp(minPitch, 0f, maxPitch);
            maxPitch = Mathf.Clamp(maxPitch, minPitch, maxPitch);
            minMaxPitchRound = new Vector2Int(Mathf.RoundToInt(minPitch * 100f), Mathf.RoundToInt(maxPitch * 100f) + 1);
        }
        public void SetAudibleLoop(bool loop, float? audibleMin = null, float? audibleMax = null)
        {
            audibleLoop = loop;
            if (audibleMin.HasValue)
            {
                minAudible = audibleMin.Value;
            }
            if (audibleMax.HasValue)
            {
                maxAudible = audibleMax.Value;
            }
            minAudible = Mathf.Clamp(minAudible, 0f, maxAudible);
            maxAudible = Mathf.Clamp(maxAudible, minAudible, maxAudible);
            minMaxAudibleRound = new Vector2Int(Mathf.RoundToInt(minAudible * 100f), Mathf.RoundToInt(maxAudible * 100f) + 1);
        }
        public void Set3DSettings(float? newDoppler = null, float? newSpread = null, AnimationCurve newCurve = null)
        {
            if (newDoppler.HasValue)
            {
                source.dopplerLevel = newDoppler.Value;
                doppler = newDoppler.Value;
            }
            if (newSpread.HasValue)
            {
                source.spread = newSpread.Value;
                spread = newSpread.Value;
            }
            if (newCurve != null && newCurve.keys.Length > 1)
            {
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, newCurve);
                customCurve = newCurve;
            }
        }
        public bool PlayRandomOneshot(bool networked = true)
        {
            return PlayRandomClip(true, networked);
        }
        public bool PlayRandomClip(bool oneShot = false, bool networked = true)
        {
            return PlayClip(RandomIndex(), oneShot, networked);
        }
        public bool PlayOneshot(int index, bool networked = true)
        {
            if (!InRange(index))
            {
                return false;
            }
            return PlayOneshot(this[index], networked);
        }
        public bool PlayOneshot(AudioClip clip, bool networked = true)
        {
            return PlayClip(clip, true, networked);
        }
        public bool PlayClip(int index, bool oneShot = false, bool networked = true, float? volumeOverride = null, float? pitchOverride = null)
        {
            if (!InRange(index))
            {
                return false;
            }
            return PlayClip(this[index], oneShot, networked, volumeOverride, pitchOverride);
        }
        public bool PlayClip(AudioClip clip, bool oneShot = false, bool networked = true, float? volumeOverride = null, float? pitchOverride = null)
        {
            if (source == null || clip == null)
            {
                return false;
            }
            float volume = volumeMultiplier;
            if (volumeOverride.HasValue)
            {
                volume = volumeOverride.Value;
            }
            else
            {
                volume *= Base.Random.Next(minMaxVolumeRound.x, minMaxVolumeRound.y) * 0.01f;
            }
            if (volume <= 0f)
            {
                return false;
            }
            LastClip = clip;
            float pitch = pitchMultiplier;
            if (pitchOverride.HasValue)
            {
                pitch = pitchOverride.Value;
            }
            else
            {
                pitch *= Base.Random.Next(minMaxPitchRound.x, minMaxPitchRound.y) * 0.01f;
            }
            source.pitch = pitch;
            source.loop = seamlessLoop;
            bool far = FarNoise;
            if (far)
            {
                farSource.pitch = pitch;
                farSource.loop = seamlessLoop;
            }
            if (!seamlessLoop)
            {
                loopCurrent = Loops;
            }
            if (oneShot)
            {
                source.PlayOneShot(clip, volume);
                if (far)
                {
                    farSource.PlayOneShot(clip, volume);
                }
            }
            else
            {
                source.volume = volume;
                source.clip = clip;
                source.Play();
                if (far)
                {
                    farSource.volume = volume;
                    farSource.clip = clip;
                    farSource.Play();
                }
            }
            if (hudAudio)
            {
                return true;
            }
            if (networked)
            {
                Base.PlayClipNetworked(Id, IndexOf(clip), oneShot, volume, pitch);
            }
            if (trackTimesNearby)
            {
                TimesNearby++;
                if (Vector3.Distance(LastPosition, Base.Transform.position) > 2f)
                {
                    TimesNearby = 0;
                }
            }
            else
            {
                TimesNearby = 0;
            }
            LastPosition = Base.Transform.position;
            if (doWalkie)
            {
                WalkieTalkie.TransmitOneShotAudio(source, clip, volume);
            }
            if (!doAudible || CloseRange <= 0f)
            {
                return true;
            }
            DogNoise(volume * 0.5f, pitch, networked);
            return true;
        }
        public void DogNoise(float volume, float pitch, bool networked = true)
        {
            bool isInShip = false;
            if (Base is WildCardProp prop && prop.isHeld && volume >= 0.3f)
            {
                prop.LastPlayerHeldBy.timeSinceMakingLoudNoise = 0f;
                isInShip = prop.isInElevator && StartOfRound.Instance.hangarDoorsClosed;
            }
            RoundManager.Instance.PlayAudibleNoise(LastPosition, CloseRange * Mathf.Max(0.5f, volume) * (1 + (Mathf.Abs(pitch - 1f) * 0.5f)), volume, TimesNearby, isInShip);
            if (!networked)
            {
                return;
            }
            Base.DogNoiseNetworked(Id, volume, pitch);
        }
        public void RepeatClip(bool oneShot = false, bool networked = true)
        {
            Stop(false);
            PlayClip(LastClip, oneShot);
            if (networked)
            {
                Base.RepeatClipNetworked(Id, oneShot);
            }
        }
        public void Stop(bool networked = true)
        {
            if (Loops)
            {
                loopCurrent = false;
            }
            source?.Stop(true);
            farSource?.Stop(true);
            if (networked)
            {
                Base.StopAudioNetworked(Id);
            }
        }
        public void SetPaused(bool pause, bool networked = true)
        {
            if (pause)
            {
                if (Loops)
                {
                    loopCurrent = false;
                }
                source?.Pause();
                farSource?.Pause();
            }
            else
            {
                if (Loops)
                {
                    loopCurrent = true;
                }
                source?.UnPause();
                farSource?.UnPause();
            }
            if (networked)
            {
                Base.PauseAudioNetworked(Id, pause);
            }
        }
        public void SetMute(bool mute, bool networked = true)
        {
            if (mute)
            {
                Mute();
            }
            else
            {
                Unmute();
            }
            if (networked)
            {
                Base.MuteAudioNetworked(Id, mute);
            }
        }
        public void Mute()
        {
            if (source != null)
            {
                source.mute = true;
            }
            if (farSource != null)
            {
                farSource.mute = true;
            }
        }
        public void Unmute()
        {
            if (source != null)
            {
                source.mute = false;
            }
            if (farSource != null)
            {
                farSource.mute = false;
            }
        }
    }
    [Serializable]
    public class SelectModelVariants : SelectableWithWeights<GameObject>
    {
        public SelectModelVariants(int id, params WeightedOption<GameObject>[] arrayItems) : base(id, arrayItems) { }
        private int currentVariant = -1;
        [SerializeField]
        private Transform targetTransform = null;
        [SerializeField]
        private bool targetIsParent = false;
        [SerializeField]
        private string baseName = string.Empty;
        [SerializeField]
        [Min(0f)]
        private int baseWeight = 0;
        [SerializeField]
        private bool instantiateVariant = false;
        [SerializeField]
        public bool randomOnSpawn = false;
        [SerializeField]
        private bool perClient = false;
        public int VariantIndex
        {
            get
            {
                return currentVariant;
            }
            set
            {
                if (!InRange(value) && value != -1)
                {
                    return;
                }
                Switch(value, !perClient);
            }
        }
        public GameObject Variant
        {
            get
            {
                GameObject variant = null;
                if (InRange(currentVariant))
                {
                    variant = this[currentVariant].option;
                }
                return variant ?? targetTransform.gameObject;
            }
        }
        public override void Initialize(IWildCardBase wildCardBase)
        {
            if (targetTransform == null)
            {
                targetTransform = Base.Transform;
                targetIsParent = true;
            }
            if (baseWeight > 0)
            {
                Insert(0, new WeightedOption<GameObject>(baseName, targetTransform.gameObject, baseWeight));
                currentVariant = 0;
                Base.Name = baseName;
            }
            base.Initialize(wildCardBase);
        }
        public bool Switch(int index, bool networked)
        {
            bool success = false;
            try
            {
                if (currentVariant == index)
                {
                    return success;
                }
                if (!InRange(index))
                {
                    if (!targetIsParent)
                    {
                        if (InRange(currentVariant))
                        {
                            this[currentVariant].option.SetActive(false);
                        }
                        targetTransform.gameObject.SetActive(true);
                        currentVariant = -1;
                        success = true;
                    }
                    return success;
                }
                if (!instantiateVariant)
                {
                    GameObject setFrom = targetTransform.gameObject;
                    if (InRange(currentVariant))
                    {
                        setFrom = this[currentVariant].option ?? setFrom;
                    }
                    setFrom.SetActive(false);
                    GameObject setTo = this[index].option ?? targetTransform.gameObject;
                    setTo.SetActive(true);
                    currentVariant = index;
                    success = true;
                    return success;
                }
                Vector3 position = targetTransform.localPosition;
                Quaternion rotation = targetTransform.localRotation;
                Vector3 scale = targetTransform.localScale;
                Transform parent = targetTransform;
                if (!targetIsParent)
                {
                    parent = targetTransform.parent;
                }
                if (InRange(currentVariant) && this[currentVariant].option != null)
                {
                    UnityEngine.Object.Destroy(this[currentVariant].option);
                }
                GameObject realGO = UnityEngine.Object.Instantiate(this[index].option, parent);
                realGO.transform.localPosition = position;
                realGO.transform.localRotation = rotation;
                realGO.transform.localScale = scale;
                targetTransform ??= realGO.transform;
                currentVariant = index;
                success = true;
                return success;
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                return success;
            }
            finally
            {
                if (success)
                {
                    string newName = baseName;
                    if (InRange(currentVariant))
                    {
                        newName = this[currentVariant].name;
                    }
                    Base.Name = newName;
                    if (networked)
                    {
                        if (perClient)
                        {
                            Base.SetVariantNetworked(Id, RandomOptionIndex());
                        }
                        else
                        {
                            Base.SetVariantNetworked(Id, currentVariant);
                        }
                    }
                }
            }
        }
        public bool SwitchRandom(bool ignoreWeights = false, bool networked = true)
        {
            return Switch(RandomOptionIndex(ignoreWeights), networked);
        }
    }
    [Serializable]
    public abstract class SelectableWithWeights<T> : Selectable<WeightedOption<T>>
    {
        public SelectableWithWeights(int id, params WeightedOption<T>[] arrayItems) : base(id, arrayItems) { }
        public int TotalWeight
        {
            get
            {
                int total = 0;
                for (int i = 0; i < Count; i++)
                {
                    total += this[i].weight;
                }
                return total;
            }
        }
        public int[] Weights
        {
            get
            {
                int[] weights = new int[Count];
                for (int i = 0; i < weights.Length; i++)
                {
                    weights[i] = this[i].weight;
                }
                return weights;
            }
        }
        public override void Initialize(IWildCardBase wildCardBase)
        {
            Ratio();
        }
        public T RandomOption(bool ignoreWeights = false)
        {
            int randomIndex = RandomOptionIndex(ignoreWeights);
            if (!InRange(randomIndex))
            {
                return default;
            }
            return this[randomIndex].option;
        }
        public int RandomOptionIndex(bool ignoreWeights = false)
        {
            if (ignoreWeights)
            {
                return RandomIndex();
            }
            int weight = Base.Random.Next(1, TotalWeight + 1);
            int cumulative = 0;
            for (int i = 0; i < Count; i++)
            {
                WeightedOption<T> option = this[i];
                int newWeight = option.weight;
                if (newWeight == 0)
                {
                    continue;
                }
                cumulative += newWeight;
                if (cumulative < weight)
                {
                    continue;
                }
                return i;
            }
            return -1;
        }
        public void Equalize(bool ignoreZero = false)
        {
            for (int i = 0; i < Count; i++)
            {
                if (ignoreZero && GetWeight(i) <= 0)
                {
                    continue;
                }
                SetWeight(i, 1);
            }
        }
        public void SetWeight(int index, int weight)
        {
            if (!InRange(index))
            {
                return;
            }
            this[index].weight = weight;
        }
        public int GetWeight(int index)
        {
            if (!InRange(index))
            {
                return 0;
            }
            return this[index].weight;
        }
        public void Ratio()
        {
            int gcd = Weights.GCD();
            if (gcd == 0)
            {
                return;
            }
            for (int i = 0; i < Count; i++)
            {
                if (this[i].weight < 0)
                {
                    this[i].weight = 0;
                }
                this[i].weight /= gcd;
            }
        }
    }
    [Serializable]
    public abstract class SelectableWithTextures<T> : Selectable<T>
    {
        public SelectableWithTextures(int id, params T[] arrayItems) : base(id, arrayItems) { }
        [SerializeField]
        private List<Texture> textures = new List<Texture>();
        public ReadOnlyCollection<Texture> Textures
        {
            get
            {
                return new ReadOnlyCollection<Texture>(textures);
            }
        }
        public void SetAllMaterialsTexture(int textureIndex)
        {
            for (int i = 0; i < Count; i++)
            {
                SetMaterialsTexture(i, textureIndex);
            }
        }
        public void SetAllMaterialsRandomTexture()
        {
            SetAllMaterialsTexture(RandomTextureIndex());
        }
        public void SetMaterialsRandomTexture(int itemIndex)
        {
            SetMaterialsTexture(itemIndex, RandomTextureIndex());
        }
        public abstract void SetMaterialsTexture(int itemIndex, int textureIndex);
        public void SetMaterialRandomTexture(int itemIndex, int materialIndex)
        {
            SetMaterialTexture(itemIndex, materialIndex, RandomTextureIndex());
        }
        public abstract void SetMaterialTexture(int itemIndex, int materialIndex, int textureIndex);
        public void AddTextures(params Texture[] newTextures)
        {
            for (int i = 0; i < newTextures.Length; i++)
            {
                Texture newTexture = newTextures[i];
                if (!textures.Contains(newTexture))
                {
                    textures.Add(newTexture);
                }
            }
        }
        public void SetTextures(params Texture[] newTextures)
        {
            textures = newTextures.ToList();
        }
        public void SetTexture(int index, Texture newTexture)
        {
            if (!InRangeTextures(index))
            {
                return;
            }
            textures[index] = newTexture;
        }
        public Texture GetTexture(int index)
        {
            if (!InRangeTextures(index))
            {
                return null;
            }
            return textures[index];
        }
        public void RemoveTextures(params Texture[] toRemoveTextures)
        {
            for (int i = 0; i < toRemoveTextures.Length; i++)
            {
                textures.Remove(toRemoveTextures[i]);
            }
        }
        public void RemoveTextures(int start, int count)
        {
            if (!InRangeTextures(start) || !InRangeTextures(start + count) || count <= 0)
            {
                return;
            }
            textures.RemoveRange(start, count);
        }
        public void RemoveTexture(int index)
        {
            if (!InRangeTextures(index))
            {
                return;
            }
            RemoveTextures(index, 1);
        }
        public void ClearTextures()
        {
            SetTextures();
        }
        public bool InRangeTextures(int index)
        {
            return index >= 0 && index < textures.Count;
        }
        public int IndexOfTexture(Texture texture)
        {
            return textures.IndexOf(texture);
        }
        public Texture RandomTexture()
        {
            if (textures.Count == 0)
            {
                return default;
            }
            return textures[RandomTextureIndex()];
        }
        public int RandomTextureIndex()
        {
            if (textures.Count == 0)
            {
                return default;
            }
            return Base.Random.Next(0, textures.Count);
        }
    }
    [Serializable]
    public abstract class Selectable<T>
    {
        public Selectable(int id, params T[] arrayItems)
        {
            this.Id = id;
            Set(arrayItems);
        }
        [HideInInspector]
        [SerializeField]
        private int id = default;
        public int Id
        {
            get
            {
                return id;
            }
            internal set
            {
                id = value;
            }
        }
        [SerializeField]
        private List<T> items = new List<T>();
        public ReadOnlyCollection<T> Items => new ReadOnlyCollection<T>(items);
        public IWildCardBase Base
        {
            get
            {
                return wildCardBase;
            }
        }
        private IWildCardBase wildCardBase = default;
        [HideInInspector]
        [SerializeField]
        internal bool initialized = false;
        public int Count
        {
            get
            {
                if (items == null)
                {
                    return 0;
                }
                return items.Count;
            }
        }
        public T this[int index]
        {
            get
            {
                if (!InRange(index))
                {
                    return default;
                }
                return items[index];
            }
            set
            {
                if (!InRange(index))
                {
                    return;
                }
                items[index] = value;
            }
        }
        public void SetBase(IWildCardBase wildCardBase)
        {
            this.wildCardBase = wildCardBase;
            if (initialized)
            {
                return;
            }
            Initialize(wildCardBase);
            initialized = true;
        }
        public virtual void Initialize(IWildCardBase wildCardBase)
        {

        }
        public T Random()
        {
            if (Count == 0)
            {
                return default;
            }
            return this[RandomIndex()];
        }
        public int RandomIndex()
        {
            if (Count == 0)
            {
                return default;
            }
            return Base.Random.Next(0, Count);
        }
        public bool InRange(int index)
        {
            return index >= 0 && index < Count;
        }
        public void Add(params T[] newSelectables)
        {
            if (newSelectables == null)
            {
                return;
            }
            Add(newSelectables.AsEnumerable());
        }
        public void Add(IEnumerable<T> newSelectables)
        {
            if (newSelectables == null)
            {
                return;
            }
            if (items == null)
            {
                Set(newSelectables);
                return;
            }
            items.AddRange(newSelectables);
        }
        public void Set(params T[] newSelectables)
        {
            if (newSelectables == null)
            {
                items?.Clear();
                return;
            }
            Set(newSelectables.AsEnumerable());
        }
        public void Set(IEnumerable<T> newSelectables)
        {
            if (newSelectables == null)
            {
                items?.Clear();
                return;
            }
            items = newSelectables.ToList();
        }
        public void Set(int index, T newSelectable)
        {
            if (!InRange(index))
            {
                return;
            }
            items[index] = newSelectable;
        }
        public void Remove(params T[] toRemoveSelectables)
        {
            if (toRemoveSelectables == null || items == null)
            {
                return;
            }
            Remove(toRemoveSelectables.AsEnumerable());
        }
        public void Remove(IEnumerable<T> toRemoveSelectables)
        {
            if (toRemoveSelectables == null || items == null)
            {
                return;
            }
            for (int i = 0; i < Count; i++)
            {
                if (!toRemoveSelectables.Contains(items[i]))
                {
                    continue;
                }
                items.RemoveAt(i);
                i--;
            }
        }
        public void Remove(int start, int count)
        {
            if (!InRange(start) || !InRange(start + count))
            {
                return;
            }
            if (count == 1)
            {
                items.RemoveAt(start);
                return;
            }
            items.RemoveRange(start, count);
        }
        public void Remove(int index)
        {
            Remove(index, 1);
        }
        public void Insert(int index, T item)
        {
            if (item == null || items == null || !InRange(index))
            {
                return;
            }
            items.Insert(index, item);
        }
        public void Insert(int index, params T[] toInsertSelectables)
        {
            if (toInsertSelectables == null || items == null || !InRange(index))
            {
                return;
            }
            Insert(index, toInsertSelectables.AsEnumerable());
        }
        public void Insert(int index, IEnumerable<T> toInsertSelectables)
        {
            if (toInsertSelectables == null || items == null || !InRange(index))
            {
                return;
            }
            items.InsertRange(index, toInsertSelectables);
        }
        public void Clear()
        {
            Set();
        }
        public int IndexOf(T selectable)
        {
            return items.IndexOf(selectable);
        }
    }
}