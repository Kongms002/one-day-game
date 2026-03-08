using OneDayGame.Domain.Input;
using OneDayGame.Presentation.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace OneDayGame.Presentation.Input
{
    [DefaultExecutionOrder(-250)]
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
        private FloatingJoystick _legacyJoystick;

        [SerializeField]
        private UltimatePressButton _ultimateButton;

        [SerializeField]
        private bool _useUltimateInput;

        private InputAction _moveAction;
        private InputAction _ultimateAction;
        private bool _ultimatePressed;
        private bool _inputEnabled = true;

        public bool HasActiveJoystick =>
            _joystick != null &&
            _joystick.isActiveAndEnabled &&
            _joystick.gameObject != null &&
            _joystick.gameObject.activeInHierarchy;

        public string JoystickDebug =>
            _joystick == null
                ? "null"
                : $"{_joystick.name} active={_joystick.isActiveAndEnabled} hierarchy={_joystick.gameObject != null && _joystick.gameObject.activeInHierarchy} touching={_joystick.IsTouching}";

        public void ConfigureUltimateInput(bool useUltimateInput, string actionMapName, string ultimateActionName)
        {
            _useUltimateInput = useUltimateInput;

            if (!string.IsNullOrWhiteSpace(actionMapName))
            {
                _actionMapName = actionMapName;
            }

            if (!string.IsNullOrWhiteSpace(ultimateActionName))
            {
                _ultimateActionName = ultimateActionName;
            }

            _ultimatePressed = false;
            if (_ultimateAction != null)
            {
                if (_inputEnabled)
                {
                    _ultimateAction.Enable();
                }
                else
                {
                    _ultimateAction.Disable();
                }
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;

            if (_moveAction != null)
            {
                if (enabled) _moveAction.Enable();
                else _moveAction.Disable();
            }

            if (_ultimateAction != null)
            {
                if (enabled) _ultimateAction.Enable();
                else _ultimateAction.Disable();
            }

            if (_joystick != null && _joystick.gameObject != null)
            {
                _joystick.enabled = enabled;
                _joystick.gameObject.SetActive(enabled);
            }

            if (!enabled)
            {
                _ultimatePressed = false;
            }
        }

        public bool TrySetJoystickToWorldPosition(Vector3 worldPosition, Camera worldCamera = null)
        {
            if (_joystick == null)
            {
                return false;
            }

            var joystickRect = _joystick.GetComponent<RectTransform>();
            if (joystickRect == null)
            {
                return false;
            }

            EnsureRectTransformScaleChain(joystickRect);

            var canvas = joystickRect.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return false;
            }

            var canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect == null)
            {
                return false;
            }

            _joystick.enabled = true;
            if (_joystick.gameObject != null && !_joystick.gameObject.activeSelf)
            {
                _joystick.gameObject.SetActive(true);
            }

            var selectedCamera = worldCamera != null
                ? worldCamera
                : Camera.main;

            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && selectedCamera == null)
            {
                selectedCamera = canvas.worldCamera;
            }

            if (selectedCamera == null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                return false;
            }

            if (selectedCamera == null)
            {
                selectedCamera = Camera.main;
            }

            var screenPoint = selectedCamera != null
                ? selectedCamera.WorldToScreenPoint(worldPosition)
                : Vector3.zero;
            var eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, eventCamera, out var localPoint))
            {
                return false;
            }

            joystickRect.anchoredPosition = localPoint;
            var backgroundRect = joystickRect.Find("Background") as RectTransform;
            var handleRect = joystickRect.Find("Handle") as RectTransform;
            NormalizeFloatingJoystickHierarchy(
                ref joystickRect,
                ref backgroundRect,
                ref handleRect);

            if (backgroundRect != null)
            {
                backgroundRect.anchoredPosition = Vector2.zero;
                backgroundRect.gameObject.SetActive(true);
            }
            if (handleRect != null)
            {
                if (backgroundRect != null)
                {
                    handleRect.position = backgroundRect.position;
                }
                else
                {
                    handleRect.anchoredPosition = Vector2.zero;
                }
            }

            return true;
        }

        private void Awake()
        {
            ResolveLegacyJoystickReference();
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

            if (_moveAction != null && _inputEnabled)
            {
                _moveAction.Enable();
            }

            if (_ultimateAction != null && _inputEnabled && _useUltimateInput)
            {
                _ultimateAction.Enable();
            }

            if (_joystick != null)
            {
                _joystick.enabled = _inputEnabled;
            }
        }

        private void OnDisable()
        {
            if (_moveAction != null) _moveAction.Disable();
            if (_ultimateAction != null) _ultimateAction.Disable();

            _ultimatePressed = false;
        }

        public void AutoBindFallbackJoysticks()
        {
            if (!_inputEnabled && _joystick != null)
            {
                _joystick.enabled = false;
                if (_joystick.gameObject != null)
                {
                    _joystick.gameObject.SetActive(false);
                }
            }

            ResolveLegacyJoystickReference();
            if (_joystick != null)
            {
                ConfigureSerializedJoystickDefaults(_joystick);
            }

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

            if (_joystick != null)
            {
                _joystick.enabled = _inputEnabled;
                if (_joystick.gameObject != null)
                {
                    _joystick.gameObject.SetActive(_inputEnabled);
                }
            }
        }

        private static void ConfigureSerializedJoystickDefaults(FloatingJoystick joystick)
        {
            if (joystick == null)
            {
                return;
            }

            var joystickRect = joystick.GetComponent<RectTransform>();
            var background = joystickRect != null
                ? joystickRect.Find("Background") as RectTransform
                : null;
            var handle = joystickRect != null
                ? joystickRect.Find("Handle") as RectTransform
                : null;

            NormalizeFloatingJoystickHierarchy(ref joystickRect, ref background, ref handle);

            if (background != null && handle != null)
            {
                joystick.Configure(background, handle, true);
            }

            if (joystickRect != null)
            {
                ConfigureRuntimeFloatingRoot(joystickRect);
                EnsureRectTransformScaleChain(joystickRect);
            }
        }

        private static void ConfigureRuntimeFloatingRoot(RectTransform joystickRect)
        {
            if (joystickRect == null)
            {
                return;
            }

            joystickRect.localScale = Vector3.one;
            joystickRect.anchorMin = Vector2.zero;
            joystickRect.anchorMax = Vector2.one;
            joystickRect.pivot = new Vector2(0.5f, 0.5f);
            joystickRect.offsetMin = Vector2.zero;
            joystickRect.offsetMax = Vector2.zero;

            var touchArea = joystickRect.GetComponent<Image>();
            if (touchArea == null)
            {
                touchArea = joystickRect.gameObject.AddComponent<Image>();
            }

            touchArea.color = new Color(0f, 0f, 0f, 0.001f);
            touchArea.raycastTarget = true;
        }

        private static void NormalizeFloatingJoystickHierarchy(
            ref RectTransform joystickRect,
            ref RectTransform background,
            ref RectTransform handle)
        {
            if (joystickRect == null)
            {
                return;
            }

            if (background == null)
            {
                background = joystickRect.Find("Background") as RectTransform;
            }

            if (background == null)
            {
                return;
            }

            var nestedHandle = background.Find("Handle") as RectTransform;
            if (handle == null && nestedHandle != null)
            {
                nestedHandle.SetParent(joystickRect, false);
                handle = nestedHandle;
                nestedHandle = null;
            }

            if (nestedHandle != null && nestedHandle != handle)
            {
                Object.Destroy(nestedHandle.gameObject);
            }

            if (handle != null && handle.parent != joystickRect)
            {
                handle.SetParent(joystickRect, false);
            }
        }

        public void Update()
        {
            if (!_inputEnabled)
            {
                _ultimatePressed = false;
                FrameTick?.Invoke(new VectorInputTick(InputAxis.Zero, false, false));
                return;
            }

            if (_joystick == null || !_joystick.isActiveAndEnabled || (_joystick.gameObject != null && !_joystick.gameObject.activeInHierarchy))
            {
                AutoBindFallbackJoysticks();
            }

            if (_actionAsset != null)
            {
                if (_moveAction == null || _ultimateAction == null)
                {
                    var actionMap = _actionAsset.FindActionMap(_actionMapName, throwIfNotFound: false);
                    if (actionMap != null)
                    {
                        _moveAction = actionMap.FindAction(_moveActionName, false);
                        _ultimateAction = actionMap.FindAction(_ultimateActionName, false);
                    }
                }
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

        public bool AnyActionPressed => _ultimatePressed || (_joystick != null && _joystick.IsTouching);

        private static FloatingJoystick FindActiveFloatingJoystick()
        {
            var joysticks = Object.FindObjectsByType<FloatingJoystick>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < joysticks.Length; i++)
            {
                var joystick = joysticks[i];
                if (joystick == null || joystick.gameObject == null)
                {
                    continue;
                }

                if (!joystick.gameObject.activeSelf)
                {
                    joystick.gameObject.SetActive(true);
                }

                joystick.enabled = true;
                var joystickRect = joystick.GetComponent<RectTransform>();
                EnsureRectTransformScaleChain(joystickRect);

                var background = joystickRect != null
                    ? joystickRect.Find("Background") as RectTransform
                    : null;
                var handle = joystickRect != null
                    ? joystickRect.Find("Handle") as RectTransform
                    : null;

                if (background != null)
                {
                    background.gameObject.SetActive(true);
                }
                NormalizeFloatingJoystickHierarchy(ref joystickRect, ref background, ref handle);

                ConfigureSerializedJoystickDefaults(joystick);
                if (background != null && handle != null)
                {
                    joystick.Configure(background, handle, true);
                }

                if (joystick.isActiveAndEnabled)
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
            ResolveLegacyJoystickReference();
            if (_joystick != null)
            {
                ConfigureSerializedJoystickDefaults(_joystick);
            }

            var alreadyBound = FindActiveFloatingJoystick();
            if (alreadyBound != null)
            {
                _joystick = alreadyBound;
                return;
            }

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
                EnsureRectTransformScaleChain(existingRoot);

                var touchArea = existingRoot.GetComponent<Image>();
                if (touchArea == null)
                {
                    touchArea = existingRoot.gameObject.AddComponent<Image>();
                }

                touchArea.color = new Color(0f, 0f, 0f, 0.001f);
                touchArea.raycastTarget = true;

                var existingJoystick = existingRoot.GetComponent<FloatingJoystick>();
                if (existingJoystick != null)
                {
                    existingRoot.gameObject.SetActive(true);
                    existingJoystick.enabled = true;
                    if (existingRoot.parent != canvas.transform)
                    {
                        existingRoot.SetParent(canvas.transform, false);
                    }

                    var existingRootRect = existingRoot.GetComponent<RectTransform>();
                    var existingBackground = existingRoot.Find("Background") as RectTransform;
                    var existingHandle = existingRoot.Find("Handle") as RectTransform;
                    NormalizeFloatingJoystickHierarchy(
                        ref existingRootRect,
                        ref existingBackground,
                        ref existingHandle);

                    if (existingBackground != null && existingHandle != null)
                    {
                        existingJoystick.Configure(existingBackground, existingHandle, true);
                        ConfigureSerializedJoystickDefaults(existingJoystick);
                        _joystick = existingJoystick;
                        return;
                    }
                }
                else
                {
                    existingJoystick = existingRoot.gameObject.AddComponent<FloatingJoystick>();
                }

                var existingRootRectForCreate = existingRoot.GetComponent<RectTransform>();
                var createdBackground = existingRoot.Find("Background") as RectTransform;
                var createdHandle = existingRoot.Find("Handle") as RectTransform;
                NormalizeFloatingJoystickHierarchy(
                    ref existingRootRectForCreate,
                    ref createdBackground,
                    ref createdHandle);

                if (createdBackground == null)
                {
                    var existingRootBackgroundGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
                    existingRootBackgroundGo.transform.SetParent(existingRoot.transform, false);
                    createdBackground = existingRootBackgroundGo.GetComponent<RectTransform>();
                    createdBackground.anchorMin = new Vector2(0.5f, 0.5f);
                    createdBackground.anchorMax = new Vector2(0.5f, 0.5f);
                    createdBackground.pivot = new Vector2(0.5f, 0.5f);
                    createdBackground.sizeDelta = new Vector2(200f, 200f);

                    var backgroundImage = existingRootBackgroundGo.GetComponent<Image>();
                    backgroundImage.sprite = RuntimeSpriteLibrary.GetCircle();
                    backgroundImage.preserveAspect = true;
                    backgroundImage.color = new Color(1f, 1f, 1f, 0.25f);
                    backgroundImage.raycastTarget = false;
                }

                if (createdHandle == null)
                {
                    var existingRootHandleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
                    existingRootHandleGo.transform.SetParent(existingRoot.transform, false);
                    createdHandle = existingRootHandleGo.GetComponent<RectTransform>();
                    createdHandle.anchorMin = new Vector2(0.5f, 0.5f);
                    createdHandle.anchorMax = new Vector2(0.5f, 0.5f);
                    createdHandle.pivot = new Vector2(0.5f, 0.5f);
                    createdHandle.sizeDelta = new Vector2(90f, 90f);

                    var handleImage = existingRootHandleGo.GetComponent<Image>();
                    handleImage.sprite = RuntimeSpriteLibrary.GetCircle();
                    handleImage.preserveAspect = true;
                    handleImage.color = new Color(1f, 1f, 1f, 0.65f);
                    handleImage.raycastTarget = false;
                }

                existingJoystick.Configure(createdBackground, createdHandle, true);
                ConfigureSerializedJoystickDefaults(existingJoystick);
                _joystick = existingJoystick;
                return;
            }

            var joystickGo = new GameObject("JoyStick", typeof(RectTransform), typeof(Image), typeof(FloatingJoystick));
            joystickGo.transform.SetParent(canvas.transform, false);
            joystickGo.transform.SetAsLastSibling();
            var joystickRect = joystickGo.GetComponent<RectTransform>();
            joystickRect.anchorMin = Vector2.zero;
            joystickRect.anchorMax = Vector2.one;
            joystickRect.offsetMin = Vector2.zero;
            joystickRect.offsetMax = Vector2.zero;
            joystickRect.localScale = Vector3.one;

            var createdTouchArea = joystickGo.GetComponent<Image>();
            createdTouchArea.color = new Color(0f, 0f, 0f, 0.001f);
            createdTouchArea.raycastTarget = true;

            var newRuntimeBackgroundGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            newRuntimeBackgroundGo.transform.SetParent(joystickGo.transform, false);
            var bgRect = newRuntimeBackgroundGo.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.sizeDelta = new Vector2(200f, 200f);
            var runtimeBgImage = newRuntimeBackgroundGo.GetComponent<Image>();
            runtimeBgImage.sprite = RuntimeSpriteLibrary.GetCircle();
            runtimeBgImage.preserveAspect = true;
            runtimeBgImage.color = new Color(1f, 1f, 1f, 0.25f);
            runtimeBgImage.raycastTarget = false;

            var newRuntimeHandleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            newRuntimeHandleGo.transform.SetParent(joystickGo.transform, false);
            var handleRect = newRuntimeHandleGo.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(90f, 90f);
            var runtimeHandleImage = newRuntimeHandleGo.GetComponent<Image>();
            runtimeHandleImage.sprite = RuntimeSpriteLibrary.GetCircle();
            runtimeHandleImage.preserveAspect = true;
            runtimeHandleImage.color = new Color(1f, 1f, 1f, 0.65f);
            runtimeHandleImage.raycastTarget = false;

            _joystick = joystickGo.GetComponent<FloatingJoystick>();
            _joystick.Configure(bgRect, handleRect, true);
            ConfigureSerializedJoystickDefaults(_joystick);
            EnsureRectTransformScaleChain(joystickRect);
        }

        private static void EnsureRectTransformScaleChain(Transform target)
        {
            var current = target;
            while (current != null)
            {
                if (current is RectTransform rectTransform)
                {
                    if (Mathf.Approximately(rectTransform.localScale.x, 0f)
                        || Mathf.Approximately(rectTransform.localScale.y, 0f)
                        || Mathf.Approximately(rectTransform.localScale.z, 0f))
                    {
                        rectTransform.localScale = Vector3.one;
                    }
                }

                current = current.parent;
            }
        }

        private void ResolveLegacyJoystickReference()
        {
            if (_joystick != null || _legacyJoystick == null)
            {
                return;
            }

            _joystick = _legacyJoystick;
            _legacyJoystick = null;
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
            if (_joystick != null && _joystick.Direction.X != 0f || _joystick != null && _joystick.Direction.Y != 0f)
            {
                return _joystick.Direction;
            }

            if (_moveAction != null && _inputEnabled)
            {
                var actionValue = _moveAction.ReadValue<Vector2>();
                if (actionValue.sqrMagnitude > 0.0001f)
                {
                    return new InputAxis(actionValue.x, actionValue.y);
                }
            }

#if ENABLE_INPUT_SYSTEM
            if (_inputEnabled && Keyboard.current != null)
            {
                float x = 0f;
                float y = 0f;
                if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
                {
                    x -= 1f;
                }

                if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
                {
                    x += 1f;
                }

                if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed)
                {
                    y -= 1f;
                }

                if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed)
                {
                    y += 1f;
                }

                var vector = new Vector2(x, y);
                if (vector.sqrMagnitude > 1f)
                {
                    vector.Normalize();
                }

                if (vector.sqrMagnitude > 0.0001f)
                {
                    return new InputAxis(vector.x, vector.y);
                }
            }
#endif

            return InputAxis.Zero;
        }
    }
}
