using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDirection : MonoBehaviour
{
    public Sprite[] directionSprites; // 8방향 스프라이트
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        Vector2 dir = Vector2.zero;

        // 조이스틱 입력
        if (Gamepad.current != null)
        {
            dir = Gamepad.current.leftStick.ReadValue();
        }

        // 입력이 거의 없으면 방향 변경 안함
        if (dir.magnitude < 0.2f)
            return;

        int direction = GetDirection(dir);

        sr.sprite = directionSprites[direction - 1];
    }

    int GetDirection(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        if (angle < 0)
            angle += 360;

        if (angle >= 337.5 || angle < 22.5)
            return 3; // East

        if (angle < 67.5)
            return 4; // NorthEast

        if (angle < 112.5)
            return 5; // North

        if (angle < 157.5)
            return 6; // NorthWest

        if (angle < 202.5)
            return 7; // West

        if (angle < 247.5)
            return 8; // SouthWest

        if (angle < 292.5)
            return 1; // South

        return 2; // SouthEast
    }
}