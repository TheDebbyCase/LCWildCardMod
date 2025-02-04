using LethalCompanyInputUtils.Api;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LCWildCardMod.Utils
{
    public class KeyBinds : LcInputActions
    {
        [InputAction("<Keyboard>/r", Name = "ExtraUse")]
        public InputAction ExtraButton { get; set; }
    }
    public class AdditionalInfo : MonoBehaviour
    {
        public bool defaultEnabled;
        public string defaultRarities;
    }
}