using OneDayGame.Domain.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace OneDayGame.Presentation.Input
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class FloatingJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField]
        [FormerlySerializedAs("joystickRoot")]
        private RectTransform _joystickRoot;

        [SerializeField]
        [FormerlySerializedAs("background")]
        private RectTransform _background;

        [SerializeField]
        [FormerlySerializedAs("handle")]
        private RectTransform _handle;

        [SerializeField]
        [FormerlySerializedAs("radius")]
        private float _moveRange = 90f;

        [SerializeField]
        [FormerlySerializedAs("floatingMode")]
        private bool _floatingMode = true;

        private RectTransform _rectTransform;
        private Vector2 _origin;
        private bool _isDragging;
        private Vector2 _direction;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_background == null)
            {
                if (_joystickRoot != null)
                {
                    _background = _joystickRoot.Find("Background") as RectTransform;
                }

                if (_background == null)
                {
                    var backgroundChild = transform.Find("Background") as RectTransform;
                    _background = backgroundChild != null ? backgroundChild : _rectTransform;
                }
            }

            if (_handle == null && _background != null)
            {
                _handle = _background.Find("Handle") as RectTransform;
            }

            if (_floatingMode && _background != null)
            {
                _background.gameObject.SetActive(false);
            }
        }

        public InputAxis Direction => new InputAxis(_direction.x, _direction.y);

        public void Configure(RectTransform background, RectTransform handle, bool floatingMode)
        {
            _background = background;
            _handle = handle;
            _floatingMode = floatingMode;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isDragging = true;

            if (_floatingMode && _background != null)
            {
                _background.gameObject.SetActive(true);
                _background.position = eventData.position;
                _origin = Vector2.zero;
                if (_handle != null)
                {
                    _handle.anchoredPosition = Vector2.zero;
                }
            }

            UpdateFromScreenPoint(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging)
            {
                return;
            }

            UpdateFromScreenPoint(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isDragging = false;
            _direction = Vector2.zero;

            if (_handle != null)
            {
                _handle.anchoredPosition = Vector2.zero;
            }

            if (_floatingMode && _background != null)
            {
                _background.gameObject.SetActive(false);
            }
        }

        private void UpdateHandle(Vector2 position)
        {
            var delta = position - _origin;
            if (delta.sqrMagnitude > _moveRange * _moveRange)
            {
                delta = delta.normalized * _moveRange;
            }

            _direction = (delta / _moveRange);

            if (_handle != null)
            {
                _handle.anchoredPosition = delta;
            }
        }

        private void OnDisable()
        {
            _direction = Vector2.zero;
            _isDragging = false;

            if (_handle != null)
            {
                _handle.anchoredPosition = Vector2.zero;
            }

            if (_floatingMode && _background != null)
            {
                _background.gameObject.SetActive(false);
            }
        }

        private void UpdateFromScreenPoint(PointerEventData eventData)
        {
            if (_background == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_background, eventData.position, eventData.pressEventCamera, out var localPoint);
            UpdateHandle(localPoint);
        }
    }
}
