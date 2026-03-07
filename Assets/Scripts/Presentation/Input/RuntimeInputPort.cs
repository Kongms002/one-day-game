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
        private string _attackActionName = "Attack";

        [SerializeField]
        private string _ultimateActionName = "Interact";

        [SerializeField]
        private FloatingJoystick _joystick;

        [SerializeField]
        private UltimatePressButton _attackButton;

        [SerializeField]
        private UltimatePressButton _ultimateButton;

        private InputAction _moveAction;
        private InputAction _attackAction;
        private InputAction _ultimateAction;
        private bool _attackPressed;
        private bool _ultimatePressed;

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
            _attackAction = actionMap.FindAction(_attackActionName, false);
            _ultimateAction = actionMap.FindAction(_ultimateActionName, false);

            if (_moveAction != null) _moveAction.Enable();
            if (_attackAction != null) _attackAction.Enable();
            if (_ultimateAction != null) _ultimateAction.Enable();
        }

        private void OnDisable()
        {
            if (_moveAction != null) _moveAction.Disable();
            if (_attackAction != null) _attackAction.Disable();
            if (_ultimateAction != null) _ultimateAction.Disable();

            _attackPressed = false;
            _ultimatePressed = false;
        }

        private void Update()
        {
            _attackPressed = ReadActionPressed(_attackAction);
            _ultimatePressed = ReadActionPressed(_ultimateAction);

            if (_attackButton != null)
            {
                _attackPressed |= _attackButton.ConsumePressed();
            }

            if (_ultimateButton != null)
            {
                _ultimatePressed |= _ultimateButton.ConsumePressed();
            }

            FrameTick?.Invoke(new VectorInputTick(MoveAxis, UltimatePressed, AnyActionPressed));
        }

        public InputAxis MoveAxis => ReadMoveAxis();

        public bool UltimatePressed => _ultimatePressed;

        public bool AnyActionPressed => _attackPressed || _ultimatePressed;

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

            if (_moveAction == null)
            {
                return InputAxis.Zero;
            }

            var movement = _moveAction.ReadValue<Vector2>();
            return new InputAxis(movement.x, movement.y);
        }
    }
}
