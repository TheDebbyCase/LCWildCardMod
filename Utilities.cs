using LethalCompanyInputUtils.Api;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.InputSystem;

namespace LCWildCardMod
{
    public class KeyBinds : LcInputActions
    {
        [InputAction("<Mouse>/rightButton", Name = "Throw")]
        public InputAction ThrowButton { get; set; }
    }
}
