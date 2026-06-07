using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace LCWildCardMod.Utils
{
    [CreateAssetMenu(menuName = "WildCard/Item", order = 1)]
    public class WildCardItem : Item
    {
        [Space(3f)]
        [Header("WildCardItem")]
        [Space(3f)]
        public bool defaultEnabled = true;
        public string defaultRarities = string.Empty;
        public bool usesButton = false;
        public bool enemyCanActivate = false;
        public bool enemyCanUseButton = false;
        public bool syncEnemyButton = false;
        internal bool enabled = false;
    }
    [CreateAssetMenu(menuName = "WildCard/Skin", order = 1)]
    public class WildCardSkin : ScriptableObject
    {
        public SkinType type;
        public string skinName;
        public string target;
        public bool skinEnabled;
        public int skinChance;
        public Mesh newMesh;
        public Material newMaterial;
        public AudioClip[] newAudioClips;
        public RuntimeAnimatorController newAnimationController;
        internal bool enabled = false;
        private static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        internal static bool AnyEnabled => WildCardMod.Instance.skinList.Any((x) => x.enabled);
        internal static void SetSkin(EnemyAI enemy)
        {
            WildCardSkin skinToApply = GetRandomSkin(enemy.enemyType.enemyName, SkinType.Enemy);
            if (skinToApply == null)
            {
                return;
            }
            switch (skinToApply.target)
            {
                case "Earth Leviathan":
                    {
                        Log.LogDebug($"Skin \"{skinToApply.skinName}\" is being applied!");
                        Transform meshContainerTransform = enemy.transform.Find("MeshContainer");
                        SkinnedMeshRenderer meshRenderer = meshContainerTransform.Find("Renderer").GetComponent<SkinnedMeshRenderer>();
                        meshRenderer.sharedMesh = skinToApply.newMesh;
                        meshRenderer.sharedMaterial = skinToApply.newMaterial;
                        meshContainerTransform.Find("Armature").Find("Bone").Find("Bone.001").Find("Bone.003").Find("Bone.002").Find("ScanNode").GetComponent<ScanNodeProperties>().headerText = skinToApply.skinName;
                        SandWormAI sandWorm = enemy.transform.GetComponent<SandWormAI>();
                        sandWorm.ambientRumbleSFX[0] = skinToApply.newAudioClips[0];
                        sandWorm.ambientRumbleSFX[1] = skinToApply.newAudioClips[0];
                        sandWorm.ambientRumbleSFX[2] = skinToApply.newAudioClips[0];
                        sandWorm.creatureSFX.volume *= 1.5f;
                        sandWorm.roarSFX[0] = skinToApply.newAudioClips[1];
                        sandWorm.roarSFX[1] = skinToApply.newAudioClips[2];
                        break;
                    }
                default:
                    {
                        Log.LogError($"\"{skinToApply.skinName}\" did not match any known enemy type!");
                        break;
                    }
            }
        }
        internal static void SetSkin(GrabbableObject item)
        {
            WildCardSkin skinToApply = GetRandomSkin(item.itemProperties.itemName, SkinType.Item);
            if (skinToApply == null)
            {
                return;
            }
            switch (skinToApply.target)
            {
                case "Clown horn":
                    {
                        Log.LogDebug($"Skin \"{skinToApply.skinName}\" is being applied!");
                        Item newProperties = UnityEngine.Object.Instantiate(item.itemProperties);
                        newProperties.itemName = skinToApply.skinName;
                        newProperties.isConductiveMetal = false;
                        newProperties.grabSFX = skinToApply.newAudioClips[0];
                        newProperties.dropSFX = skinToApply.newAudioClips[1];
                        newProperties.toolTips[0] = "Squeeze : [LMB]";
                        newProperties.positionOffset = new Vector3(0.05f, 0.15f, -0.05f);
                        item.useCooldown = 0.5f;
                        MeshFilter prefabMeshFilter = newProperties.spawnPrefab.GetComponent<MeshFilter>();
                        prefabMeshFilter.mesh = skinToApply.newMesh;
                        prefabMeshFilter.sharedMesh = skinToApply.newMesh;
                        MeshFilter itemMeshFilter = item.transform.GetComponent<MeshFilter>();
                        itemMeshFilter.mesh = skinToApply.newMesh;
                        itemMeshFilter.sharedMesh = skinToApply.newMesh;
                        MeshRenderer itemMeshRenderer = item.transform.GetComponent<MeshRenderer>();
                        itemMeshRenderer.material = skinToApply.newMaterial;
                        itemMeshRenderer.sharedMaterial = skinToApply.newMaterial;
                        item.transform.Find("ScanNode").GetComponent<ScanNodeProperties>().headerText = skinToApply.skinName;
                        Animator anim = item.gameObject.AddComponent<Animator>();
                        anim.runtimeAnimatorController = skinToApply.newAnimationController;
                        item.transform.GetComponent<NoisemakerProp>().triggerAnimator = anim;
                        item.itemProperties = newProperties;
                        break;
                    }
                default:
                    {
                        Log.LogError($"\"{skinToApply.skinName}\" did not match any known item!");
                        break;
                    }
            }
        }
        private static WildCardSkin GetRandomSkin(string target, SkinType type)
        {
            System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            int nothingWeight = 0;
            int skinsWeight = 0;
            List<WildCardSkin> potentialSkins = new List<WildCardSkin>();
            for (int i = 0; i < WildCardMod.Instance.skinList.Count; i++)
            {
                WildCardSkin skin = WildCardMod.Instance.skinList[i];
                if (!skin.enabled || skin.type != type || skin.target != target)
                {
                    continue;
                }
                potentialSkins.Add(skin);
            }
            if (potentialSkins.Count == 0)
            {
                return null;
            }
            for (int i = 0; i < potentialSkins.Count; i++)
            {
                WildCardSkin skin = potentialSkins[i];
                string skinName = skin.skinName;
                int applyWeight = WildCardMod.Instance.ModConfig.SkinApplyChance[skinName];
                if (applyWeight <= 0)
                {
                    Log.LogDebug($"Skin \"{skinName}\" was disabled!");
                    potentialSkins.RemoveAt(i);
                    i--;
                    continue;
                }
                Log.LogDebug($"Adding skin \"{skinName}\"'s chance weight!");
                skinsWeight += applyWeight;
                nothingWeight += 100 - applyWeight;
            }
            float applyChance = (float)random.NextDouble();
            Log.LogDebug($"Rolling to see if a skin will be applied!");
            float inverseNothing = 1f / (float)nothingWeight;
            float inverseSkins = 1f / (float)skinsWeight;
            if (((float)nothingWeight * (inverseNothing + inverseSkins)) >= applyChance)
            {
                return null;
            }
            for (int i = potentialSkins.Count - 1; i > 1; i--)
            {
                int j = random.Next(i + 1);
                (potentialSkins[i], potentialSkins[j]) = (potentialSkins[j], potentialSkins[i]);
            }
            for (int i = 0; i < potentialSkins.Count; i++)
            {
                WildCardSkin skin = potentialSkins[i];
                string skinName = skin.skinName;
                Log.LogDebug($"Rolling to see if \"{skinName}\" is selected!");
                if (WildCardMod.Instance.ModConfig.SkinApplyChance[skinName] * inverseSkins >= applyChance)
                {
                    Log.LogDebug($"Skin \"{skinName}\" was selected!");
                    return skin;
                }
            }
            return null;
        }
    }
    [CreateAssetMenu(menuName = "WildCard/MapObject", order = 1)]
    public class WildCardMapObject : ScriptableObject
    {
        public string mapObjectName;
        public SpawnableMapObject spawnableMapObject;
        public List<SelectablePair<AnimationCurve>> levelCurves;
        public bool autoHandle;
        public string[] configBools;
        internal bool enabled = false;
        public Func<SelectableLevel, AnimationCurve> GetCurveFunc()
        {
            AnimationCurve curve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 2f));
            if (levelCurves != null && (autoHandle || WildCardMod.Instance.ModConfig.DefaultMapObjectCurve[mapObjectName]))
            {
                return (x) =>
                {
                    for (int i = 0; i < levelCurves.Count; i++)
                    {
                        SelectablePair<AnimationCurve> levelCurve = levelCurves[i];
                        string checkLevelName = levelCurve.id;
                        if (checkLevelName != x.name)
                        {
                            continue;
                        }
                        curve = levelCurve.selectable;
                        break;
                    }
                    return curve;
                };
            }
            else if (WildCardMod.Instance.ModConfig.MapObjectMinMax.TryGetValue(mapObjectName, out (int, int) minMax))
            {
                int min = minMax.Item1;
                int max = minMax.Item2;
                curve.keys[0].value = min;
                curve.keys[1].value = (min + max) * 0.5f;
                curve.keys[2].value = max;
            }
            return (x) => curve;
        }
    }
}