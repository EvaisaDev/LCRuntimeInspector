using LethalCompanyInputUtils.Api;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.InputSystem;

namespace LCRuntimeInspector
{
    public class Inputs : LcInputActions
    {
        [InputAction("<Keyboard>/f7", Name = "OpenHierarchy")]
        public InputAction OpenHierarchy { get; set; }
        [InputAction("<Keyboard>/f8", Name = "OpenInspector")]
        public InputAction OpenInspector { get; set; }

        [InputAction("<Keyboard>/leftalt", Name = "ToggleCursor")]
        public InputAction ToggleCursor { get; set; }
    }
}
