using OneDayGame.Domain.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OneDayGame.Presentation.Input
{
    public sealed class RuntimeInputPort : MonoBehaviour, IInputPort
    {
        [SerializeField]
        private InputActionAsset _actionAsset;

        [SerializeField]
        private string _actionMapName = "Player";

        [SerializeField]
        private string _moveActionName = "Move";

        [SerializeField]
        private string _ultimateActionName = "Interact";

        [SerializeField]
        private FloatingJoystick _joystick;

        [SerializeField]
        private global::FloatingJoystick _legacyJoystick;

        [SerializeField]
        private UltimatePressButton _ultimateButton;

        [SerializeField]
        private bool _useUltimateInput;

        private InputAction _moveAction;
        private InputAction _ultimateAction;
        private bool _ultimatePressed;

        private void Awake()
        {
            AutoBindFallbackJoysticks();
        }

        public event System.Action<VectorInputTick> FrameTick;

        private void OnEnable()
        {
            if (_actionAsset == null)
            {
                return;
            }

            var actionMap = _actionAsset.FindActionMap(_actionMapName, throwIfNotFound: false);
            if (actionMap == null)
            {
                return;
            }

            _moveAction = actionMap.FindAction(_moveActionName, false);
            _ultimateAction = actionMap.FindAction(_ultimateActionName, false);

            if (_moveAction != null) _moveAction.Enable();
            if (_ultimateAction != null) _ultimateAction.Enable();
        }

        private void OnDisable()
        {
            if (_moveAction != null) _moveAction.Disable();
            if (_ultimateAction != null) _ultimateAction.Disable();

            _ultimatePressed = false;
        }

        private void Update()
        {
            _ultimatePressed = _useUltimateInput && ReadActionPressed(_ultimateAction);

            if (_useUltimateInput && _ultimateButton != null)
            {
                _ultimatePressed |= _ultimateButton.ConsumePressed();
            }

            FrameTick?.Invoke(new VectorInputTick(MoveAxis, UltimatePressed, AnyActionPressed));
        }

        public InputAxis MoveAxis => ReadMoveAxis();

        public bool UltimatePressed => _ultimatePressed;

        public bool AnyActionPressed => _ultimatePressed;

        public void AutoBindFallbackJoysticks()
        {
            if (_joystick == null)
            {
                _joystick = FindObjectOfType<FloatingJoystick>();
            }

            if (_legacyJoystick == null)
            {
                _legacyJoystick = FindObjectOfType<global::FloatingJoystick>();
            }
        }

        private bool ReadActionPressed(InputAction action)
        {
            if (action == null)
            {
                return false;
            }

            return action.WasPressedThisFrame();
        }

        private InputAxis ReadMoveAxis()
        {
            if (_joystick != null && !_joystick.Direction.IsZero)
            {
                return _joystick.Direction;
            }

            if (_legacyJoystick != null)
            {
                return new InputAxis(_legacyJoystick.Horizontal, _legacyJoystick.Vertical);
            }

            return InputAxis.Zero;
        }
    }
}
