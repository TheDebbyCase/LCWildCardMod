using LethalCompanyInputUtils.Api;
using UnityEngine.InputSystem;
namespace LCWildCardMod
{
    public class KeyBinds : LcInputActions
    {
        [InputAction("<Mouse>/rightButton", Name = "Throw")]
        public InputAction ThrowButton { get; set; }
    }
}
