using UnityEngine;
using UnityEngine.EventSystems;

public class FloatingJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Joystick UI")]
    public RectTransform joystickRoot;
    public RectTransform background;
    public RectTransform handle;

    [Header("Settings")]
    public float radius = 100f;

    private Vector2 input;

    public float Horizontal => input.x;
    public float Vertical => input.y;

    private void Start()
    {
        joystickRoot.gameObject.SetActive(false);
        input = Vector2.zero;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        joystickRoot.gameObject.SetActive(true);

        joystickRoot.position = eventData.position;
        handle.anchoredPosition = Vector2.zero;
        input = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 direction = eventData.position - (Vector2)joystickRoot.position;

        // 반지름보다 멀어지면 잘라냄
        Vector2 clamped = Vector2.ClampMagnitude(direction, radius);

        handle.anchoredPosition = clamped;

        input = clamped / radius;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        input = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
        joystickRoot.gameObject.SetActive(false);
    }
}