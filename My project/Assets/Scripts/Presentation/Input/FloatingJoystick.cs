using OneDayGame.Domain.Input;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OneDayGame.Presentation.Input
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class FloatingJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField]
        private RectTransform _background;

        [SerializeField]
        private RectTransform _handle;

        [SerializeField]
        private float _moveRange = 90f;

        private RectTransform _rectTransform;
        private Vector2 _origin;
        private bool _isDragging;
        private Vector2 _direction;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_background == null)
            {
                _background = _rectTransform;
            }
        }

        public InputAxis Direction => new InputAxis(_direction.x, _direction.y);

        public void OnPointerDown(PointerEventData eventData)
        {
            _isDragging = true;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_background, eventData.position, eventData.pressEventCamera, out var localPoint);
            _origin = localPoint;
            UpdateHandle(localPoint);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_background, eventData.position, eventData.pressEventCamera, out var localPoint);
            UpdateHandle(localPoint);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isDragging = false;
            _direction = Vector2.zero;

            if (_handle != null)
            {
                _handle.anchoredPosition = Vector2.zero;
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
        }
    }
}
