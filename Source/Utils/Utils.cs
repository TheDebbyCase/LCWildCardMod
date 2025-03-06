using LCWildCardMod.Config;
using LethalCompanyInputUtils.Api;
using LobbyCompatibility.Enums;
using LobbyCompatibility.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LCWildCardMod.Utils
{
    public class KeyBinds : LcInputActions
    {
        [InputAction("<Keyboard>/r", Name = "WildCardUse")]
        public InputAction WildCardButton { get; set; }
    }
    public class SoftDepHelper
    {
        public static void LobCompatRegister()
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
    public class SkinsClass : MonoBehaviour
    {
        readonly BepInEx.Logging.ManualLogSource log = WildCardMod.Log;
        readonly List<BepInEx.Configuration.ConfigEntry<int>> configChances = WildCardMod.ModConfig.skinApplyChance;
        readonly List<Skin> allSkins = WildCardMod.skinList;
        public void SetSkin(List<Skin> potentialSkins, EnemyAI enemy = null, GrabbableObject item = null)
        {
            System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            Skin skinToApply = null;
            int nothingWeight = 0;
            int skinsWeight = 0;
            for (int i = 0; i < potentialSkins.Count; i++)
            {
                int index = allSkins.IndexOf(potentialSkins[i]);
                if (configChances[index].Value <= 0)
                {
                    log.LogDebug($"Skin \"{potentialSkins[i].skinName}\" was disabled!");
                    potentialSkins.Remove(potentialSkins[i]);
                    i--;
                    continue;
                }
                log.LogDebug($"Adding skin \"{potentialSkins[i].skinName}\"'s chance weight!");
                skinsWeight += configChances[index].Value;
                nothingWeight += 100 - configChances[index].Value;
            }
            float applyChance = (float)random.NextDouble();
            log.LogDebug($"Rolling to see if a skin will be applied!");
            if (((float)nothingWeight / (float)(nothingWeight + skinsWeight)) < applyChance)
            {
                potentialSkins.Sort((x, y) => configChances[allSkins.IndexOf(potentialSkins[potentialSkins.IndexOf(x)])].Value.CompareTo(configChances[allSkins.IndexOf(potentialSkins[potentialSkins.IndexOf(y)])].Value));
                for (int i = 0; i < potentialSkins.Count; i++)
                {
                    log.LogDebug($"Rolling to see if \"{potentialSkins[i].skinName}\" is selected!");
                    if (configChances[allSkins.IndexOf(potentialSkins[i])].Value / skinsWeight >= applyChance)
                    {
                        log.LogDebug($"Skin \"{potentialSkins[i].skinName}\" was selected!");
                        skinToApply = potentialSkins[i];
                        break;
                    }
                }
                if (skinToApply.targetEnemy != null)
                {
                    switch (skinToApply.targetEnemy.enemyName)
                    {
                        case "Earth Leviathan":
                            {
                                log.LogDebug($"Skin \"{skinToApply.skinName}\" is being applied!");
                                enemy.transform.Find("MeshContainer").Find("Renderer").GetComponent<SkinnedMeshRenderer>().sharedMesh = skinToApply.newMesh;
                                enemy.transform.Find("MeshContainer").Find("Renderer").GetComponent<SkinnedMeshRenderer>().sharedMaterial = skinToApply.newMaterial;
                                enemy.transform.Find("MeshContainer").Find("Armature").Find("Bone").Find("Bone.001").Find("Bone.003").Find("Bone.002").Find("ScanNode").GetComponent<ScanNodeProperties>().headerText = skinToApply.skinName;
                                enemy.transform.GetComponent<SandWormAI>().ambientRumbleSFX[0] = skinToApply.newAudioClips[0];
                                enemy.transform.GetComponent<SandWormAI>().ambientRumbleSFX[1] = skinToApply.newAudioClips[0];
                                enemy.transform.GetComponent<SandWormAI>().ambientRumbleSFX[2] = skinToApply.newAudioClips[0];
                                enemy.transform.GetComponent<SandWormAI>().creatureSFX.volume *= 1.5f;
                                enemy.transform.GetComponent<SandWormAI>().roarSFX[0] = skinToApply.newAudioClips[1];
                                enemy.transform.GetComponent<SandWormAI>().roarSFX[1] = skinToApply.newAudioClips[2];
                                break;
                            }
                        default:
                            {
                                log.LogError($"\"{potentialSkins[0].skinName}\" did not match any known enemy type!");
                                break;
                            }
                    }
                }
                else if (skinToApply.targetItem != null)
                {
                    switch (skinToApply.targetItem.itemName)
                    {
                        case "Clown horn":
                            {
                                log.LogDebug($"Skin \"{skinToApply.skinName}\" is being applied!");
                                Item newProperties = Instantiate(item.itemProperties);
                                newProperties.itemName = skinToApply.skinName;
                                newProperties.isConductiveMetal = false;
                                newProperties.grabSFX = skinToApply.newAudioClips[0];
                                newProperties.dropSFX = skinToApply.newAudioClips[1];
                                newProperties.toolTips[0] = "Squeeze : [LMB]";
                                newProperties.positionOffset = new Vector3(0.05f, 0.15f, -0.05f);
                                item.useCooldown = 0.5f;
                                newProperties.spawnPrefab.GetComponent<MeshFilter>().mesh = skinToApply.newMesh;
                                item.transform.GetComponent<MeshFilter>().mesh = skinToApply.newMesh;
                                item.transform.GetComponent<MeshRenderer>().material = skinToApply.newMaterial;
                                newProperties.spawnPrefab.GetComponent<MeshFilter>().sharedMesh = skinToApply.newMesh;
                                item.transform.GetComponent<MeshFilter>().sharedMesh = skinToApply.newMesh;
                                item.transform.GetComponent<MeshRenderer>().sharedMaterial = skinToApply.newMaterial;
                                item.transform.Find("ScanNode").GetComponent<ScanNodeProperties>().headerText = skinToApply.skinName;
                                Animator anim = item.gameObject.AddComponent<Animator>();
                                anim.runtimeAnimatorController = skinToApply.newAnimationController;
                                item.transform.GetComponent<NoisemakerProp>().triggerAnimator = anim;
                                item.itemProperties = newProperties;
                                break;
                            }
                        default:
                            {
                                log.LogError($"\"{potentialSkins[0].skinName}\" did not match any known item!");
                                break;
                            }
                    }
                }
            }
            else
            {
                log.LogDebug($"No skin for \"{potentialSkins[0].targetEnemy.enemyName}\" was chosen to apply!");
            }
        }
    }
    [CreateAssetMenu(menuName = "WCScriptableObjects/Skin", order = 1)]
    public class Skin : ScriptableObject
    {
        public string skinName;
        public bool skinEnabled;
        public int skinChance;
        public EnemyType targetEnemy;
        public Item targetItem;
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
    public class MapObjectHelper
    {
        readonly BepInEx.Logging.ManualLogSource log = WildCardMod.Log;
        public static WildCardConfig ModConfig;
        public static List<MapObject> mapObjects;
        public static List<MapObject> autoMapObjects;
        public int levelIndex = 0;
        public int mapIndex = 0;
        public void Init(List<MapObject> maps, List<MapObject> autoMaps, WildCardConfig config)
        {
            ModConfig = config;
            mapObjects = maps;
            autoMapObjects = autoMaps;
        }
        public AnimationCurve MapObjectFunc(SelectableLevel level)
        {
            AnimationCurve curve;
            if (ModConfig.useDefaultMapObjectCurve[mapIndex].Value)
            {
                List<string> levelsList = new List<string>();
                for (int i = 0; i < mapObjects[mapIndex].levelCurves.Count; i++)
                {
                    levelsList.Add(mapObjects[mapIndex].levelCurves[i].level);
                }
                for (int i = 0; i < mapObjects[mapIndex].levelCurves.Count; i++)
                {
                    LevelCurve levelCurve = mapObjects[mapIndex].levelCurves[i];
                    if (!levelsList.Contains(levelCurve.level))
                    {
                        continue;
                    }
                    else if (levelCurve.level == level.name)
                    {
                        curve = levelCurve.curve;
                        IterateIndices();
                        return curve;
                    }
                }
                curve = mapObjects[mapIndex].spawnableMapObject.numberToSpawn;
                IterateIndices();
                return curve;
            }
            else
            {
                curve = new AnimationCurve(new Keyframe(0, ModConfig.mapObjectMinNo[mapIndex].Value), new Keyframe(1, ModConfig.mapObjectMaxNo[mapIndex].Value));
                IterateIndices();
                return curve;
            }
        }
        public void IterateIndices()
        {
            if (levelIndex == StartOfRound.Instance.levels.Length)
            {
                levelIndex = 0;
                mapIndex++;
                if (mapIndex == mapObjects.Count)
                {
                    mapIndex = 0;
                }
            }
            levelIndex++;
        }
        public AnimationCurve AutoMapObjectFunc(SelectableLevel level)
        {
            AnimationCurve curve;
            List<string> levelsList = new List<string>();
            for (int i = 0; i < autoMapObjects[mapIndex].levelCurves.Count; i++)
            {
                levelsList.Add(autoMapObjects[mapIndex].levelCurves[i].level);
            }
            for (int i = 0; i < autoMapObjects[mapIndex].levelCurves.Count; i++)
            {
                LevelCurve levelCurve = autoMapObjects[mapIndex].levelCurves[i];
                if (!levelsList.Contains(levelCurve.level))
                {
                    continue;
                }
                else if (levelCurve.level == level.name)
                {
                    curve = levelCurve.curve;
                    IterateIndices();
                    return curve;
                }
            }
            curve = autoMapObjects[mapIndex].spawnableMapObject.numberToSpawn;
            IterateIndices();
            return curve;
        }
    }
}