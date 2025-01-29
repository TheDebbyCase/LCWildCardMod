using LethalCompanyInputUtils.Api;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LCWildCardMod.Utils
{
    public class KeyBinds : LcInputActions
    {
        [InputAction("<Keyboard>/r", Name = "Throw")]
        public InputAction ThrowButton { get; set; }
    }
    public class AdditionalInfo : MonoBehaviour
    {
        public string defaultRarities;
    }
}