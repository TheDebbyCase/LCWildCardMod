using LCWildCardMod.Config;
using LethalCompanyInputUtils.Api;
using LobbyCompatibility.Enums;
using LobbyCompatibility.Features;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LCWildCardMod.Utils
{
    internal static class EventsClass
    {
        internal delegate void RoundStart();
        internal delegate void RoundEnd();
        internal static event RoundStart OnRoundStart;
        internal static event RoundEnd OnRoundEnd;
        internal static void RoundStarted()
        {
            OnRoundStart.Invoke();
        }
        internal static void RoundEnded()
        {
            OnRoundEnd.Invoke();
        }
    }
    internal class KeyBinds : LcInputActions
    {
        [InputAction("<Keyboard>/r", Name = "WildCardUse")]
        internal InputAction WildCardButton { get; set; }
    }
    internal static class SoftDepHelper
    {
        internal static void LobCompatRegister()
        {
            PluginHelper.RegisterPlugin(WildCardMod.modGUID, new Version(WildCardMod.modVersion), CompatibilityLevel.Everyone, VersionStrictness.Patch);
        }
    }
    public class AdditionalInfo : MonoBehaviour
    {
        public bool defaultEnabled;
        public string defaultRarities;
        public bool isBonus;
    }
    internal static class SkinsClass
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        static List<BepInEx.Configuration.ConfigEntry<int>> ConfigChances => WildCardMod.Instance.ModConfig.skinApplyChance;
        internal static void SetSkin(EnemyAI enemy)
        {
            Skin skinToApply = GetRandomSkin(enemy.enemyType.enemyName, SkinType.Enemy);
            if (skinToApply == null)
            {
                return;
            }
            switch ((skinToApply.target as EnemyType).enemyName)
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
            Skin skinToApply = GetRandomSkin(item.itemProperties.itemName, SkinType.Item);
            if (skinToApply == null)
            {
                return;
            }
            switch ((skinToApply.target as Item).itemName)
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
        static Skin GetRandomSkin(string target, SkinType type)
        {
            System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            Skin skinToApply = null;
            int nothingWeight = 0;
            int skinsWeight = 0;
            List<Skin> potentialSkins = new List<Skin>();
            for (int i = 0; i < WildCardMod.Instance.skinList.Count; i++)
            {
                Skin skin = WildCardMod.Instance.skinList[i];
                switch (type)
                {
                    case SkinType.Enemy:
                        {
                            if (!(skin.target is EnemyType))
                            {
                                continue;
                            }
                            if ((skin.target as EnemyType).enemyName == target)
                            {
                                potentialSkins.Add(skin);
                            }
                            break;
                        }
                    case SkinType.Item:
                        {
                            if (!(skin.target is Item))
                            {
                                continue;
                            }
                            if ((skin.target as Item).itemName == target)
                            {
                                potentialSkins.Add(skin);
                            }
                            break;
                        }
                }
            }
            if (potentialSkins.Count == 0)
            {
                return null;
            }
            for (int i = 0; i < potentialSkins.Count; i++)
            {
                int index = WildCardMod.Instance.skinList.IndexOf(potentialSkins[i]);
                if (ConfigChances[index].Value <= 0)
                {
                    Log.LogDebug($"Skin \"{potentialSkins[i].skinName}\" was disabled!");
                    potentialSkins.Remove(potentialSkins[i]);
                    i--;
                    continue;
                }
                Log.LogDebug($"Adding skin \"{potentialSkins[i].skinName}\"'s chance weight!");
                skinsWeight += ConfigChances[index].Value;
                nothingWeight += 100 - ConfigChances[index].Value;
            }
            float applyChance = (float)random.NextDouble();
            Log.LogDebug($"Rolling to see if a skin will be applied!");
            if (((float)nothingWeight / (float)(nothingWeight + skinsWeight)) < applyChance)
            {
                for (int i = 0; i < potentialSkins.Count; i++)
                {
                    Log.LogDebug($"Rolling to see if \"{potentialSkins[i].skinName}\" is selected!");
                    if (ConfigChances[WildCardMod.Instance.skinList.IndexOf(potentialSkins[i])].Value / skinsWeight >= applyChance)
                    {
                        Log.LogDebug($"Skin \"{potentialSkins[i].skinName}\" was selected!");
                        skinToApply = potentialSkins[i];
                        break;
                    }
                }
            }
            return skinToApply;
        }
    }
    [CreateAssetMenu(menuName = "WCScriptableObjects/Skin", order = 1)]
    public class Skin : ScriptableObject
    {
        public string skinName;
        public bool skinEnabled;
        public int skinChance;
        public ScriptableObject target;
        public Mesh newMesh;
        public Material newMaterial;
        public AudioClip[] newAudioClips;
        public RuntimeAnimatorController newAnimationController;
    }
    [CreateAssetMenu(menuName = "WCScriptableObjects/MapObject", order = 1)]
    public class MapObject : ScriptableObject
    {
        public string mapObjectName;
        public SpawnableMapObject spawnableMapObject;
        public Func<SelectableLevel, AnimationCurve> curveFunc;
        public List<LevelCurve> levelCurves;
        public bool autoHandle;
    }
    [Serializable]
    public class LevelCurve
    {
        public string level;
        public AnimationCurve curve;
    }
    internal static class MapObjectHelper
    {
        static WildCardConfig ModConfig => WildCardMod.Instance.ModConfig;
        static List<MapObject> MapObjects => WildCardMod.Instance.mapObjectsList;
        static List<MapObject> AutoMapObjects => WildCardMod.Instance.autoMapObjectsList;
        static int mapIndex = 0;
        internal static AnimationCurve MapObjectFunc(SelectableLevel level)
        {
            AnimationCurve curve;
            List<MapObject> maps = MapObjects;
            maps.AddRange(AutoMapObjects);
            if (maps[mapIndex].autoHandle || (ModConfig.useDefaultMapObjectCurve.Count > mapIndex && ModConfig.useDefaultMapObjectCurve[mapIndex].Value))
            {
                List<string> levelsList = new List<string>();
                for (int i = 0; i < maps[mapIndex].levelCurves.Count; i++)
                {
                    levelsList.Add(maps[mapIndex].levelCurves[i].level);
                }
                for (int i = 0; i < maps[mapIndex].levelCurves.Count; i++)
                {
                    LevelCurve levelCurve = maps[mapIndex].levelCurves[i];
                    if (!levelsList.Contains(levelCurve.level))
                    {
                        continue;
                    }
                    else if (levelCurve.level == level.name)
                    {
                        curve = levelCurve.curve;
                        mapIndex++;
                        return curve;
                    }
                }
                curve = maps[mapIndex].spawnableMapObject.numberToSpawn;
                mapIndex++;
                return curve;
            }
            else if (ModConfig.mapObjectMinNo.Count > mapIndex && ModConfig.mapObjectMaxNo.Count > mapIndex)
            {
                curve = new AnimationCurve(new Keyframe(0, ModConfig.mapObjectMinNo[mapIndex].Value), new Keyframe(1, ModConfig.mapObjectMaxNo[mapIndex].Value));
                mapIndex++;
                return curve;
            }
            else
            {
                curve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0f, 0f));
                mapIndex++;
                return curve;
            }
        }
    }
    internal enum SkinType
    {
        Item,
        Enemy
    }
}