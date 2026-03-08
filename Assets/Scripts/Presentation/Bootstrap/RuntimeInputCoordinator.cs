using OneDayGame.Application;
using OneDayGame.Domain.Input;
using OneDayGame.Presentation.Input;
using UnityEngine;

namespace OneDayGame.Presentation.Bootstrap
{
    public sealed class RuntimeInputCoordinator
    {
        public IInputPort EnsureRuntimeReferences(
            IInputPort inputPort,
            bool useUltimateInput,
            string actionMapName,
            string ultimateActionName)
        {
            var runtimePort = inputPort as RuntimeInputPort;
            if (runtimePort == null)
            {
                runtimePort = Object.FindFirstObjectByType<RuntimeInputPort>();
            }

            if (runtimePort == null)
            {
                var inputObject = new GameObject("RuntimeInputPort");
                runtimePort = inputObject.AddComponent<RuntimeInputPort>();
            }
            else
            {
                if (!runtimePort.isActiveAndEnabled)
                {
                    runtimePort.enabled = true;
                }

                var runtimeObject = runtimePort.gameObject;
                if (runtimeObject != null && !runtimeObject.activeInHierarchy)
                {
                    runtimeObject.SetActive(true);
                }
            }

            runtimePort.AutoBindFallbackJoysticks();
            runtimePort.ConfigureUltimateInput(useUltimateInput, actionMapName, ultimateActionName);

            var ultimateButtons = Object.FindObjectsByType<UltimatePressButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < ultimateButtons.Length; i++)
            {
                var button = ultimateButtons[i];
                if (button == null)
                {
                    continue;
                }

                button.gameObject.SetActive(false);
            }

            return runtimePort;
        }

        public void SetRuntimeInputEnabled(IInputPort inputPort, bool enabled)
        {
            var runtimePort = inputPort as RuntimeInputPort;
            if (runtimePort == null)
            {
                return;
            }

            runtimePort.SetInputEnabled(enabled);
        }

        public bool ShouldRuntimeInputBeEnabled(bool isRunInputEnabled, RunSessionService runSession)
        {
            return isRunInputEnabled && runSession != null && !runSession.IsDead;
        }

        public void RepositionRuntimeJoystickAtPlayerStart(IInputPort inputPort, Vector3 playerStartPosition)
        {
            var runtimePort = inputPort as RuntimeInputPort;
            if (runtimePort == null)
            {
                return;
            }

            runtimePort.TrySetJoystickToWorldPosition(playerStartPosition, Camera.main);
        }
    }
}
