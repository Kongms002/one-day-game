using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OneDayGame.Presentation.Input
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class UltimatePressButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public event Action Pressed;

        private bool _pressed;

        public bool IsPressed => _pressed;

        public bool ConsumePressed()
        {
            if (!_pressed)
            {
                return false;
            }

            _pressed = false;
            return true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;
            Pressed?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
        }

        private void OnDisable()
        {
            _pressed = false;
        }
    }
}
