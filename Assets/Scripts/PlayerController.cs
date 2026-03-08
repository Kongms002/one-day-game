using UnityEngine;
using OneDayGame.Presentation.Input;

public class PlayerController : MonoBehaviour
{
    public OneDayGame.Presentation.Input.FloatingJoystick joystick;
    public float speed = 5f;

    public Sprite[] directionSprites; // 0: South, 1: SouthEast, 2: East, 3: NorthEast, 4: North, 5: NorthWest, 6: West, 7: SouthWest
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (joystick == null)
        {
            return;
        }

        var axis = joystick.Direction;
        Vector3 dir = new Vector3(axis.X, axis.Y, 0f);

        if (dir.sqrMagnitude < 0.01f)
        {
            return;
        }

        if (sr != null && directionSprites != null && directionSprites.Length == 8)
        {
            int dirIndex = GetDirectionIndex(dir);
            sr.sprite = directionSprites[dirIndex];
        }

        if (sr != null && directionSprites != null && directionSprites.Length == 8)
        {
            int dirIndex = GetDirectionIndex(dir);
            sr.sprite = directionSprites[dirIndex];
        }

        dir.Normalize();
        transform.position += dir * speed * Time.deltaTime;
    }

    int GetDirectionIndex(Vector3 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        if (angle < 0)
            angle += 360f;

        if (angle >= 337.5f || angle < 22.5f)
            return 2; // East
        if (angle >= 22.5f && angle < 67.5f)
            return 3; // NorthEast
        if (angle >= 67.5f && angle < 112.5f)
            return 4; // North
        if (angle >= 112.5f && angle < 157.5f)
            return 5; // NorthWest
        if (angle >= 157.5f && angle < 202.5f)
            return 6; // West
        if (angle >= 202.5f && angle < 247.5f)
            return 7; // SouthWest
        if (angle >= 247.5f && angle < 292.5f)
            return 0; // South
        if (angle >= 292.5f && angle < 337.5f)
            return 1; // SouthEast

        return 0;
    }
}