using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LCRuntimeInspector
{
    public class RuntimeInspectorController : MonoBehaviour
    {
        public GameObject Hierarchy;
        public GameObject Inspector;
        public QuickMenuManager quickMenuManager;

        public void Awake()
        {
            
            Plugin.Inputs.OpenHierarchy.performed += OpenHierarchy;
            Plugin.Inputs.OpenInspector.performed += OpenInspector;
            Plugin.Inputs.ToggleCursor.performed += ToggleCursor;
        }

        public void Update()
        {
            if (quickMenuManager == null && Time.frameCount % 60 == 0) quickMenuManager = UnityEngine.Object.FindObjectOfType<QuickMenuManager>();

            if (Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
            }

            if ((Hierarchy.activeSelf || Inspector.activeSelf) && StartOfRound.Instance && StartOfRound.Instance.localPlayerController)
            {
                if (Cursor.visible)
                {
                    StartOfRound.Instance.localPlayerController.playerActions.Disable();
                    IngamePlayerSettings.Instance.playerInput.actions.Disable();
                }
                else if((quickMenuManager == null || !quickMenuManager.isMenuOpen) && !StartOfRound.Instance.localPlayerController.inTerminalMenu)
                {
                    StartOfRound.Instance.localPlayerController.playerActions.Enable();
                    IngamePlayerSettings.Instance.playerInput.actions.Enable();
                }
            }
        }

        private void ToggleCursor(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            if (Hierarchy.activeSelf || Inspector.activeSelf)
            {

                Cursor.visible = !Cursor.visible;
                Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
            }
        }


        private void OpenInspector(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            Inspector.SetActive(!Inspector.activeSelf);


            Cursor.visible = Hierarchy.activeSelf || Inspector.activeSelf || (quickMenuManager != null && quickMenuManager.isMenuOpen);
            Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;


            if (!(Hierarchy.activeSelf || Inspector.activeSelf) && StartOfRound.Instance && StartOfRound.Instance.localPlayerController)
            {
                if ((quickMenuManager == null || !quickMenuManager.isMenuOpen) && !StartOfRound.Instance.localPlayerController.inTerminalMenu)
                {
                    StartOfRound.Instance.localPlayerController.playerActions.Enable();
                    IngamePlayerSettings.Instance.playerInput.actions.Enable();
                }
            }

        }

        private void OpenHierarchy(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            Hierarchy.SetActive(!Hierarchy.activeSelf);

            Cursor.visible = Hierarchy.activeSelf || Inspector.activeSelf || (quickMenuManager != null && quickMenuManager.isMenuOpen);
            Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;

            if (!(Hierarchy.activeSelf || Inspector.activeSelf) && StartOfRound.Instance && StartOfRound.Instance.localPlayerController)
            {
                if ((quickMenuManager == null || !quickMenuManager.isMenuOpen) && !StartOfRound.Instance.localPlayerController.inTerminalMenu)
                {
                    StartOfRound.Instance.localPlayerController.playerActions.Enable();
                    IngamePlayerSettings.Instance.playerInput.actions.Enable();
                }
            }
        }
    }
}
