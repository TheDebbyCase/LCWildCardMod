using LethalCompanyInputUtils.Api;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using LethalLib;
using LethalLib.Modules;
namespace LCWildCardMod.Utils
{
    public class KeyBinds : LcInputActions
    {
        [InputAction("<Keyboard>/r", Name = "WildCardUse")]
        public InputAction WildCardButton { get; set; }
    }
    public class AdditionalInfo : MonoBehaviour
    {
        public bool defaultEnabled;
        public string defaultRarities;
        public bool isBonus;
    }
    public class SkinsClass
    {
        BepInEx.Logging.ManualLogSource log = WildCardMod.Log;
        List<BepInEx.Configuration.ConfigEntry<int>> configChances = WildCardMod.ModConfig.skinApplyChance;
        List<Skin> allSkins = WildCardMod.skinList;
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
                                item.itemProperties.itemName = skinToApply.skinName;
                                item.itemProperties.isConductiveMetal = false;
                                item.itemProperties.grabSFX = skinToApply.newAudioClips[0];
                                item.itemProperties.dropSFX = skinToApply.newAudioClips[1];
                                item.itemProperties.toolTips[0] = "Squeeze : [LMB]";
                                item.itemProperties.positionOffset = new Vector3(0.05f, 0.15f, -0.05f);
                                item.useCooldown = 0.5f;
                                item.transform.GetComponent<MeshFilter>().sharedMesh = skinToApply.newMesh;
                                item.itemProperties.spawnPrefab.GetComponent<MeshFilter>().sharedMesh = skinToApply.newMesh;
                                item.transform.GetComponent<MeshRenderer>().sharedMaterial = skinToApply.newMaterial;
                                item.transform.Find("ScanNode").GetComponent<ScanNodeProperties>().headerText = skinToApply.skinName;
                                Animator anim = item.gameObject.AddComponent<Animator>();
                                anim.runtimeAnimatorController = skinToApply.newAnimationController;
                                item.transform.GetComponent<NoisemakerProp>().triggerAnimator = anim;
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
}