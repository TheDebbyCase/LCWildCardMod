using LethalCompanyInputUtils.Api;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
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
        private readonly System.Random random = new System.Random();
        public void SetSkin(EnemyAI enemy, Skin skin)
        {
            int index = WildCardMod.skinList.IndexOf(skin);
            if (skin.targetEnemy.enemyName == "Earth Leviathan")
            {
                int applyChance = random.Next(0, 100);
                if (WildCardMod.ModConfig.isSkinEnabled[index].Value && WildCardMod.ModConfig.skinApplyChance[index].Value > 0 && applyChance < WildCardMod.ModConfig.skinApplyChance[index].Value)
                {
                    enemy.transform.Find("MeshContainer").Find("Renderer").GetComponent<SkinnedMeshRenderer>().sharedMesh = skin.newMesh;
                    enemy.transform.Find("MeshContainer").Find("Renderer").GetComponent<SkinnedMeshRenderer>().sharedMaterial = skin.newMaterial;
                    enemy.transform.Find("MeshContainer").Find("Armature").Find("Bone").Find("Bone.001").Find("Bone.003").Find("Bone.002").Find("ScanNode").GetComponent<ScanNodeProperties>().headerText = skin.skinName;
                    enemy.transform.GetComponent<SandWormAI>().ambientRumbleSFX[0] = skin.newAudioClips[0];
                    enemy.transform.GetComponent<SandWormAI>().ambientRumbleSFX[1] = skin.newAudioClips[0];
                    enemy.transform.GetComponent<SandWormAI>().ambientRumbleSFX[2] = skin.newAudioClips[0];
                    enemy.transform.GetComponent<SandWormAI>().creatureSFX.volume *= 1.5f;
                    enemy.transform.GetComponent<SandWormAI>().roarSFX[0] = skin.newAudioClips[1];
                    enemy.transform.GetComponent<SandWormAI>().roarSFX[1] = skin.newAudioClips[2];
                }
                else if (applyChance < WildCardMod.ModConfig.skinApplyChance[index].Value)
                {
                    WildCardMod.Log.LogInfo($"{skin.skinName} was disabled!");
                }
                else
                {
                    WildCardMod.Log.LogDebug($"{skin.skinName} was not applied!");
                }
            }
            else
            {
                WildCardMod.Log.LogError($"{skin.skinName} did not match any known enemy type!");
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
        public Mesh newMesh;
        public Material newMaterial;
        public AudioClip[] newAudioClips;
    }
}