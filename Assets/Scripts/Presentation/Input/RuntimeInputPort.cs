using OneDayGame.Domain.Input;
using OneDayGame.Presentation.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

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
        private UltimatePressButton _ultimateButton;

        [SerializeField]
        private bool _useUltimateInput;

        private InputAction _moveAction;
        private InputAction _ultimateAction;
        private bool _ultimatePressed;

        public bool HasActiveJoystick => _joystick != null && _joystick.isActiveAndEnabled;

        private void Awake()
        {
            EnsureEventSystemInputModule();
            EnsureRuntimeJoystick();
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
            if (_joystick == null || !_joystick.isActiveAndEnabled || (_joystick.gameObject != null && !_joystick.gameObject.activeInHierarchy))
            {
                AutoBindFallbackJoysticks();
            }

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
            if (_joystick != null && _joystick.isActiveAndEnabled)
            {
                return;
            }

            var foundJoystick = FindActiveFloatingJoystick();
            if (foundJoystick != null)
            {
                _joystick = foundJoystick;
                return;
            }

            _joystick = null;
            EnsureRuntimeJoystick();
        }

        private static FloatingJoystick FindActiveFloatingJoystick()
        {
            var joysticks = Object.FindObjectsByType<FloatingJoystick>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < joysticks.Length; i++)
            {
                var joystick = joysticks[i];
                if (joystick != null && joystick.isActiveAndEnabled)
                {
                    return joystick;
                }
            }

            return null;
        }

        private static void EnsureEventSystemInputModule()
        {
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                go.transform.SetAsLastSibling();
                return;
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            var old = eventSystem.GetComponent<StandaloneInputModule>();
            if (old != null)
            {
                Destroy(old);
            }
        }

        private void EnsureRuntimeJoystick()
        {
            if (_joystick != null && _joystick.isActiveAndEnabled)
            {
                return;
            }

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            var existingRoot = canvas.transform.Find("JoyStick");
            if (existingRoot != null)
            {
                existingRoot.SetAsLastSibling();
                var existingJoystick = existingRoot.GetComponent<FloatingJoystick>();
                if (existingJoystick != null)
                {
                    var existingBackground = existingRoot.Find("Background") as RectTransform;
                    var existingHandle = existingBackground != null
                        ? existingBackground.Find("Handle") as RectTransform
                        : existingRoot.Find("Handle") as RectTransform;

                    if (existingBackground != null && existingHandle != null)
                    {
                        existingJoystick.Configure(existingBackground, existingHandle, true);
                        _joystick = existingJoystick;
                        return;
                    }
                }

                Destroy(existingRoot.gameObject);
            }

            var joystickGo = new GameObject("JoyStick", typeof(RectTransform), typeof(Image), typeof(FloatingJoystick));
            joystickGo.transform.SetParent(canvas.transform, false);
            joystickGo.transform.SetAsLastSibling();
            var joystickRect = joystickGo.GetComponent<RectTransform>();
            joystickRect.anchorMin = Vector2.zero;
            joystickRect.anchorMax = Vector2.one;
            joystickRect.offsetMin = Vector2.zero;
            joystickRect.offsetMax = Vector2.zero;

            var touchArea = joystickGo.GetComponent<Image>();
            touchArea.color = new Color(0f, 0f, 0f, 0.001f);
            touchArea.raycastTarget = true;

            var backgroundGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            backgroundGo.transform.SetParent(joystickGo.transform, false);
            var bgRect = backgroundGo.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.sizeDelta = new Vector2(200f, 200f);
            var bgImage = backgroundGo.GetComponent<Image>();
            bgImage.sprite = RuntimeSpriteLibrary.GetCircle();
            bgImage.preserveAspect = true;
            bgImage.color = new Color(1f, 1f, 1f, 0.25f);
            bgImage.raycastTarget = false;

            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(backgroundGo.transform, false);
            var handleRect = handleGo.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(90f, 90f);
            var handleImage = handleGo.GetComponent<Image>();
            handleImage.sprite = RuntimeSpriteLibrary.GetCircle();
            handleImage.preserveAspect = true;
            handleImage.color = new Color(1f, 1f, 1f, 0.65f);
            handleImage.raycastTarget = false;

            _joystick = joystickGo.GetComponent<FloatingJoystick>();
            _joystick.Configure(bgRect, handleRect, true);
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
            if (_moveAction != null)
            {
                var actionValue = _moveAction.ReadValue<Vector2>();
                if (actionValue.sqrMagnitude > 0.0001f)
                {
                    return new InputAxis(actionValue.x, actionValue.y);
                }
            }

            if (_joystick != null && !_joystick.Direction.IsZero)
            {
                return _joystick.Direction;
            }

            return InputAxis.Zero;
        }
    }
}
